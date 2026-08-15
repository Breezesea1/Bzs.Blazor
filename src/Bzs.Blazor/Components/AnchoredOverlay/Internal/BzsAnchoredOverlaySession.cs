namespace Bzs.Blazor;

internal sealed class BzsAnchoredOverlaySession : IAsyncDisposable
{
    internal const string ModulePath = "./_content/Bzs.Blazor/Components/Popover/BzsPopover.razor.js";
    internal const string InitializeMethod = "initialize";
    internal const string SetOpenMethod = "setOpen";
    internal const string SetOpenAtMethod = "setOpenAt";
    internal const string DisposeMethod = "dispose";

    private readonly BzsJsModule _module;
    private readonly Func<Task> _closeRequested;
    private readonly int _immediateAttemptLimit;
    private readonly string _instanceId = $"bzs-anchored-overlay-{Guid.NewGuid():N}";
    private readonly CancellationTokenSource _lifetimeCancellation = new();
    private readonly object _gate = new();
    private readonly SemaphoreSlim _renderSynchronization = new(1, 1);
    private DotNetObjectReference<BzsAnchoredOverlaySession>? _dotNetReference;
    private Task? _closeRequestTask;
    private BzsAnchoredOverlayState _desiredState;
    private long _desiredVersion;
    private long _synchronizedVersion = -1;
    private int _initializationAttemptCount;
    private int _synchronizationAttemptCount;
    private bool _hasDesiredState;
    private bool _initialized;
    private bool _restoreFocusPending;
    private bool _disposed;

    internal BzsAnchoredOverlaySession(
        IJSRuntime jsRuntime,
        Func<Task> closeRequested,
        int immediateAttemptLimit,
        Microsoft.Extensions.Logging.ILoggerFactory? loggerFactory = null)
    {
        ArgumentNullException.ThrowIfNull(jsRuntime);
        ArgumentNullException.ThrowIfNull(closeRequested);
        ArgumentOutOfRangeException.ThrowIfLessThan(immediateAttemptLimit, 1);

        _closeRequested = closeRequested;
        _immediateAttemptLimit = immediateAttemptLimit;
        _module = new BzsJsModule(
            jsRuntime,
            ModulePath,
            loggerFactory,
            new BzsJsModuleOptions(TreatObjectDisposedAsTransient: true));
    }

    internal void SetDesiredState(BzsAnchoredOverlayState state)
    {
        if (!Enum.IsDefined(state.Placement))
        {
            throw new ArgumentOutOfRangeException(
                nameof(state),
                state.Placement,
                "The anchored overlay placement is not supported.");
        }

        lock (_gate)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            if (state.Open && _closeRequestTask is null)
            {
                _restoreFocusPending = false;
            }

            if (_hasDesiredState && _desiredState == state)
            {
                return;
            }

            _desiredState = state;
            _hasDesiredState = true;
            _desiredVersion++;
            _synchronizationAttemptCount = 0;
        }
    }

    internal async ValueTask AfterRenderAsync(ElementReference root)
    {
        CancellationToken cancellationToken;
        lock (_gate)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            cancellationToken = _lifetimeCancellation.Token;
        }

        try
        {
            await _renderSynchronization.WaitAsync(cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            return;
        }

        try
        {
            await SynchronizeRenderAsync(root);
        }
        finally
        {
            _renderSynchronization.Release();
        }
    }

    private async ValueTask SynchronizeRenderAsync(ElementReference root)
    {
        DotNetObjectReference<BzsAnchoredOverlaySession> dotNetReference;
        lock (_gate)
        {
            if (_disposed)
            {
                return;
            }

            if (!_hasDesiredState)
            {
                throw new InvalidOperationException(
                    "Anchored overlay desired state must be set before rendering is synchronized.");
            }

            dotNetReference = _dotNetReference ??= DotNetObjectReference.Create(this);
        }

        if (!await EnsureInitializedAsync(root, dotNetReference))
        {
            return;
        }

        await SynchronizeAsync();
    }

    internal Task RequestCloseAsync(bool restoreFocus) =>
        RequestCloseCoreAsync(restoreFocus, ignoreDisposed: false, applyBrowserPolicy: false);

    [JSInvokable]
    public Task CloseFromBrowserAsync(bool restoreFocus = false) =>
        RequestCloseCoreAsync(restoreFocus, ignoreDisposed: true, applyBrowserPolicy: true);

    public async ValueTask DisposeAsync()
    {
        DotNetObjectReference<BzsAnchoredOverlaySession>? dotNetReference;
        lock (_gate)
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            dotNetReference = _dotNetReference;
            _dotNetReference = null;
        }

        _lifetimeCancellation.Cancel();

        Exception? disposalException = null;
        try
        {
            if (_module.IsLoaded)
            {
                try
                {
                    await _module.TryInvokeVoidAsync(DisposeMethod, _instanceId);
                }
                catch (Exception exception)
                {
                    disposalException = exception;
                }
            }

            try
            {
                await _module.DisposeAsync();
            }
            catch (Exception exception)
            {
                disposalException ??= exception;
            }
        }
        finally
        {
            try
            {
                dotNetReference?.Dispose();
            }
            catch (Exception exception)
            {
                disposalException ??= exception;
            }

            _lifetimeCancellation.Dispose();
        }

        if (disposalException is not null)
        {
            System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(disposalException).Throw();
        }
    }

    private async Task<bool> EnsureInitializedAsync(
        ElementReference root,
        DotNetObjectReference<BzsAnchoredOverlaySession> dotNetReference)
    {
        while (true)
        {
            CancellationToken cancellationToken;
            lock (_gate)
            {
                if (_disposed)
                {
                    return false;
                }

                if (_initialized)
                {
                    return true;
                }

                _initializationAttemptCount++;
                cancellationToken = _lifetimeCancellation.Token;
            }

            var initialized = await _module.TryInvokeVoidAsync(
                InitializeMethod,
                cancellationToken,
                _instanceId,
                root,
                dotNetReference);

            lock (_gate)
            {
                if (_disposed)
                {
                    return false;
                }

                if (initialized)
                {
                    _initialized = true;
                    _initializationAttemptCount = 0;
                    _synchronizationAttemptCount = 0;
                    return true;
                }

                if (_initializationAttemptCount >= _immediateAttemptLimit)
                {
                    return false;
                }
            }
        }
    }

    private async Task SynchronizeAsync()
    {
        while (true)
        {
            BzsAnchoredOverlayState state;
            long version;
            bool restoreFocus;
            CancellationToken cancellationToken;
            lock (_gate)
            {
                if (_disposed)
                {
                    return;
                }

                if (_synchronizedVersion == _desiredVersion
                    && (!_restoreFocusPending || _desiredState.Open))
                {
                    return;
                }

                state = _desiredState;
                version = _desiredVersion;
                restoreFocus = !state.Open && _restoreFocusPending;
                if (restoreFocus)
                {
                    _restoreFocusPending = false;
                }
                _synchronizationAttemptCount++;
                cancellationToken = _lifetimeCancellation.Token;
            }

            var synchronized = state.InvocationPoint is { } point
                ? await _module.TryInvokeVoidAsync(
                    SetOpenAtMethod,
                    cancellationToken,
                    _instanceId,
                    state.Open,
                    GetPlacementName(state.Placement),
                    state.CloseOnOutsideInteraction,
                    state.CloseOnEscape,
                    restoreFocus,
                    point.ClientX,
                    point.ClientY)
                : await _module.TryInvokeVoidAsync(
                    SetOpenMethod,
                    cancellationToken,
                    _instanceId,
                    state.Open,
                    GetPlacementName(state.Placement),
                    state.CloseOnOutsideInteraction,
                    state.CloseOnEscape,
                    restoreFocus);

            lock (_gate)
            {
                if (_disposed)
                {
                    return;
                }

                if (version != _desiredVersion)
                {
                    continue;
                }

                if (synchronized)
                {
                    _synchronizedVersion = version;
                    _synchronizationAttemptCount = 0;
                    return;
                }

                if (_synchronizationAttemptCount >= _immediateAttemptLimit)
                {
                    return;
                }
            }
        }
    }

    private Task RequestCloseCoreAsync(
        bool restoreFocus,
        bool ignoreDisposed,
        bool applyBrowserPolicy)
    {
        TaskCompletionSource? closeStart = null;
        Task closeRequestTask;
        lock (_gate)
        {
            if (_disposed)
            {
                if (ignoreDisposed)
                {
                    return Task.CompletedTask;
                }

                throw new ObjectDisposedException(nameof(BzsAnchoredOverlaySession));
            }

            if (!_hasDesiredState || !_desiredState.Open)
            {
                return Task.CompletedTask;
            }

            if (applyBrowserPolicy)
            {
                restoreFocus &= _desiredState.RestoreFocusOnBrowserClose;
            }

            _restoreFocusPending |= restoreFocus;
            if (_closeRequestTask is not null)
            {
                return _closeRequestTask;
            }

            closeStart = new TaskCompletionSource();
            closeRequestTask = RunCloseRequestAsync(closeStart.Task);
            _closeRequestTask = closeRequestTask;
        }

        closeStart.TrySetResult();
        return closeRequestTask;
    }

    private async Task RunCloseRequestAsync(Task closeStart)
    {
        await closeStart;
        try
        {
            await _closeRequested();
        }
        finally
        {
            lock (_gate)
            {
                _closeRequestTask = null;
                if (!_disposed && (!_hasDesiredState || _desiredState.Open))
                {
                    _restoreFocusPending = false;
                }
            }
        }
    }

    internal static string GetPlacementName(BzsPopoverPlacement placement) => placement switch
    {
        BzsPopoverPlacement.BottomStart => "bottom-start",
        BzsPopoverPlacement.Bottom => "bottom",
        BzsPopoverPlacement.BottomEnd => "bottom-end",
        BzsPopoverPlacement.TopStart => "top-start",
        BzsPopoverPlacement.Top => "top",
        BzsPopoverPlacement.TopEnd => "top-end",
        BzsPopoverPlacement.Start => "start",
        BzsPopoverPlacement.End => "end",
        _ => throw new ArgumentOutOfRangeException(
            nameof(placement),
            placement,
            "The anchored overlay placement is not supported."),
    };
}

internal readonly record struct BzsAnchoredOverlayState(
    bool Open,
    BzsPopoverPlacement Placement,
    bool CloseOnOutsideInteraction,
    bool CloseOnEscape,
    BzsAnchoredOverlayInvocationPoint? InvocationPoint = null,
    bool RestoreFocusOnBrowserClose = true);

internal readonly record struct BzsAnchoredOverlayInvocationPoint(double ClientX, double ClientY);
