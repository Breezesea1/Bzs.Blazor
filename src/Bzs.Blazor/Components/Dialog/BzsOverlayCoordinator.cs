namespace Bzs.Blazor;

internal enum BzsOverlayHostState
{
    Missing,
    PresentStatic,
    ActiveInteractive,
    Disposed,
}

internal readonly record struct BzsOverlayDialogId(Guid Value);

internal sealed record BzsOverlayDialogSnapshot(
    BzsOverlayDialogId Id,
    BzsDialogOptions Options,
    RenderFragment Content);

internal sealed class BzsOverlayChangedEventArgs : EventArgs
{
    public BzsOverlayChangedEventArgs(IReadOnlyList<BzsOverlayDialogSnapshot> snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        Snapshot = Array.AsReadOnly(snapshot.ToArray());
    }

    public IReadOnlyList<BzsOverlayDialogSnapshot> Snapshot { get; }
}

/// <summary>
/// Owns service-driven overlay request state for one Blazor service scope.
/// </summary>
internal sealed class BzsOverlayCoordinator : IDisposable, IAsyncDisposable
{
    private readonly object _gate = new();
    private readonly List<BzsOverlayDialogRequest> _requests = [];
    private bool _disposed;

    /// <summary>Occurs after the host-renderable overlay snapshot changes.</summary>
    public event EventHandler<BzsOverlayChangedEventArgs>? Changed;

    /// <summary>Gets an immutable dialog snapshot in stack order.</summary>
    public IReadOnlyList<BzsOverlayDialogSnapshot> Snapshot
    {
        get
        {
            lock (_gate)
            {
                return CreateSnapshot();
            }
        }
    }

    internal BzsOverlayDialogRequest<TResult> CreateDialogRequest<TResult>(BzsDialogOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        return new BzsOverlayDialogRequest<TResult>(
            new BzsOverlayDialogId(Guid.NewGuid()),
            options,
            OnRequestCompleted);
    }

    internal Task<BzsDialogResult<TResult>> Enqueue<TResult>(
        BzsOverlayDialogRequest<TResult> request,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(request);

        BzsOverlayChangedEventArgs? changed = null;
        var disposed = false;
        lock (_gate)
        {
            if (_disposed)
            {
                disposed = true;
            }
            else
            {
                _requests.Add(request);
                changed = CreateChangedArgs();
            }
        }

        if (disposed)
        {
            request.TryComplete(BzsDialogResult<TResult>.HostDisposed());
            return request.Task;
        }

        PublishChanged(changed!);
        request.RegisterCancellation(ct);
        return request.Task;
    }

    /// <summary>Cancels one active request when it is still present.</summary>
    public bool Cancel(BzsOverlayDialogId id)
    {
        var request = FindRequest(id);
        return request?.TryCancel() ?? false;
    }

    /// <summary>Dismisses one active request when it is still present.</summary>
    public bool Dismiss(
        BzsOverlayDialogId id,
        BzsDialogDismissReason reason = BzsDialogDismissReason.Programmatic)
    {
        if (!Enum.IsDefined(reason))
        {
            throw new ArgumentOutOfRangeException(nameof(reason), reason, "The dialog dismissal reason is not supported.");
        }

        var request = FindRequest(id);
        return request?.TryDismiss(reason) ?? false;
    }

    /// <summary>Completes all active dialog requests with a host-disposed result.</summary>
    public void Dispose()
    {
        List<BzsOverlayDialogRequest>? requests = null;
        BzsOverlayChangedEventArgs? changed = null;
        lock (_gate)
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            requests = [.. _requests];
            _requests.Clear();
            changed = CreateChangedArgs();
        }

        foreach (var request in requests)
        {
            request.TryHostDisposed();
        }

        PublishChanged(changed);
    }

    /// <inheritdoc />
    public ValueTask DisposeAsync()
    {
        Dispose();
        return ValueTask.CompletedTask;
    }

    private BzsOverlayDialogRequest? FindRequest(BzsOverlayDialogId id)
    {
        lock (_gate)
        {
            return _requests.FirstOrDefault(candidate => candidate.Id == id);
        }
    }

    private void OnRequestCompleted(BzsOverlayDialogRequest request)
    {
        BzsOverlayChangedEventArgs? changed = null;
        lock (_gate)
        {
            if (_requests.Remove(request))
            {
                changed = CreateChangedArgs();
            }
        }

        if (changed is not null)
        {
            PublishChanged(changed);
        }
    }

    private IReadOnlyList<BzsOverlayDialogSnapshot> CreateSnapshot() =>
        Array.AsReadOnly(_requests.Select(static request => request.Snapshot).ToArray());

    private BzsOverlayChangedEventArgs CreateChangedArgs() => new(CreateSnapshot());

    private void PublishChanged(BzsOverlayChangedEventArgs changed)
    {
        var handlers = Changed;
        if (handlers is null)
        {
            return;
        }

        foreach (EventHandler<BzsOverlayChangedEventArgs> handler in handlers.GetInvocationList())
        {
            try
            {
                handler(this, changed);
            }
            catch
            {
                // One host subscriber must not prevent request cleanup or later subscribers.
            }
        }
    }
}

internal abstract class BzsOverlayDialogRequest
{
    public abstract BzsOverlayDialogId Id { get; }

    public abstract BzsOverlayDialogSnapshot Snapshot { get; }

    public abstract bool TryCancel();

    public abstract bool TryDismiss(BzsDialogDismissReason reason);

    public abstract bool TryHostDisposed();
}

internal sealed class BzsOverlayDialogRequest<TResult> : BzsOverlayDialogRequest
{
    private readonly object _gate = new();
    private readonly TaskCompletionSource<BzsDialogResult<TResult>> _completion =
        new(TaskCreationOptions.RunContinuationsAsynchronously);
    private readonly Action<BzsOverlayDialogRequest> _onCompleted;
    private readonly BzsDialogOptions _options;
    private CancellationTokenRegistration _cancellationRegistration;
    private bool _hasCancellationRegistration;
    private bool _completed;
    private RenderFragment? _content;

    public BzsOverlayDialogRequest(
        BzsOverlayDialogId id,
        BzsDialogOptions options,
        Action<BzsOverlayDialogRequest> onCompleted)
    {
        Id = id;
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _onCompleted = onCompleted ?? throw new ArgumentNullException(nameof(onCompleted));
    }

    public override BzsOverlayDialogId Id { get; }

    public Task<BzsDialogResult<TResult>> Task => _completion.Task;

    public override BzsOverlayDialogSnapshot Snapshot => new(
        Id,
        _options,
        _content ?? throw new InvalidOperationException("The dialog request content must be created before it is enqueued."));

    public void SetContent(RenderFragment content)
    {
        ArgumentNullException.ThrowIfNull(content);
        if (_content is not null)
        {
            throw new InvalidOperationException("Dialog request content can only be set once.");
        }

        _content = content;
    }

    public bool TryComplete(BzsDialogResult<TResult> result)
    {
        ArgumentNullException.ThrowIfNull(result);

        CancellationTokenRegistration cancellationRegistration = default;
        lock (_gate)
        {
            if (_completed)
            {
                return false;
            }

            _completed = true;
            if (_hasCancellationRegistration)
            {
                cancellationRegistration = _cancellationRegistration;
                _hasCancellationRegistration = false;
            }
        }

        cancellationRegistration.Dispose();
        _onCompleted(this);
        return _completion.TrySetResult(result);
    }

    public override bool TryCancel() => TryComplete(BzsDialogResult<TResult>.Cancelled());

    public override bool TryDismiss(BzsDialogDismissReason reason) =>
        TryComplete(BzsDialogResult<TResult>.Dismissed(reason));

    public override bool TryHostDisposed() => TryComplete(BzsDialogResult<TResult>.HostDisposed());

    public void RegisterCancellation(CancellationToken ct)
    {
        if (!ct.CanBeCanceled)
        {
            return;
        }

        var registration = ct.Register(static state =>
        {
            var request = (BzsOverlayDialogRequest<TResult>)state!;
            request.TryCancel();
        }, this);

        var disposeRegistration = false;
        lock (_gate)
        {
            if (_completed)
            {
                disposeRegistration = true;
            }
            else
            {
                _cancellationRegistration = registration;
                _hasCancellationRegistration = true;
            }
        }

        if (disposeRegistration)
        {
            registration.Dispose();
        }
    }
}
