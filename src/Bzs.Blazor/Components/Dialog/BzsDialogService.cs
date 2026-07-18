namespace Bzs.Blazor;

using System.Diagnostics.CodeAnalysis;

/// <summary>
/// Implements command-driven dialogs within one Blazor service scope.
/// </summary>
public sealed class BzsDialogService : IBzsDialogService
{
    private readonly BzsOverlayCoordinator _coordinator;
    private readonly BzsOverlayHostRegistry _hostRegistry;

    internal BzsDialogService(
        BzsOverlayCoordinator coordinator,
        BzsOverlayHostRegistry hostRegistry)
    {
        _coordinator = coordinator ?? throw new ArgumentNullException(nameof(coordinator));
        _hostRegistry = hostRegistry ?? throw new ArgumentNullException(nameof(hostRegistry));
    }

    /// <inheritdoc />
    public Task<BzsDialogResult<TResult>> ShowAsync<
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] TComponent,
        TResult>(
        Action<BzsDialogParameterBuilder<TComponent>>? parameters = null,
        BzsDialogOptions? options = null,
        CancellationToken ct = default)
        where TComponent : IComponent
    {
        var parameterBuilder = new BzsDialogParameterBuilder<TComponent>();
        parameters?.Invoke(parameterBuilder);

        switch (_hostRegistry.State)
        {
            case BzsOverlayHostState.Missing:
                throw new InvalidOperationException(
                    "No BzsOverlayHost is registered for this service scope. Place exactly one <BzsOverlayHost /> inside each interactive application root that calls IBzsDialogService.");

            case BzsOverlayHostState.PresentStatic:
                return Task.FromResult(BzsDialogResult<TResult>.Unavailable());

            case BzsOverlayHostState.Disposed:
                return Task.FromResult(BzsDialogResult<TResult>.HostDisposed());

            case BzsOverlayHostState.ActiveInteractive:
                break;

            default:
                throw new InvalidOperationException("The overlay host lifecycle state is not supported.");
        }

        var request = _coordinator.CreateDialogRequest<TResult>(options ?? new BzsDialogOptions());
        var dialogContext = new BzsDialogContext<TResult>(
            result => request.TryComplete(BzsDialogResult<TResult>.Completed(result)),
            () => request.TryComplete(BzsDialogResult<TResult>.Cancelled()),
            reason => request.TryComplete(BzsDialogResult<TResult>.Dismissed(reason)));

        request.SetContent(CreateContent<TComponent, TResult>(parameterBuilder.Build(), dialogContext));
        return _coordinator.Enqueue(request, ct);
    }

    private static RenderFragment CreateContent<
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] TComponent,
        TResult>(
        IReadOnlyList<BzsDialogParameter> parameters,
        BzsDialogContext<TResult> dialogContext)
        where TComponent : IComponent
    {
        return builder =>
        {
            builder.OpenComponent<CascadingValue<BzsDialogContext<TResult>>>(0);
            builder.AddAttribute(1, nameof(CascadingValue<BzsDialogContext<TResult>>.Value), dialogContext);
            builder.AddAttribute(2, nameof(CascadingValue<BzsDialogContext<TResult>>.IsFixed), true);
            builder.AddAttribute(3, nameof(CascadingValue<BzsDialogContext<TResult>>.ChildContent),
                (RenderFragment)(contentBuilder =>
                {
                    contentBuilder.OpenComponent<TComponent>(0);
                    var sequence = 1;
                    foreach (var parameter in parameters)
                    {
                        contentBuilder.AddAttribute(sequence++, parameter.Name, parameter.Value);
                    }

                    contentBuilder.CloseComponent();
                }));
            builder.CloseComponent();
        };
    }
}
