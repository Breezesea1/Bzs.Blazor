using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.Extensions.DependencyInjection;

namespace Bzs.Blazor.Tests;

public sealed class DialogServiceTests
{
    [Fact]
    public void ShowAsyncWithoutAnOverlayHostFailsWithActionableConfigurationGuidance()
    {
        var services = new ServiceCollection();
        services.AddBzsBlazor();
        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();
        var dialogs = scope.ServiceProvider.GetRequiredService<IBzsDialogService>();

        var exception = Assert.Throws<InvalidOperationException>(() =>
        {
            _ = dialogs.ShowAsync<TestDialogContent, bool>();
        });

        Assert.Contains("BzsOverlayHost", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ShowAsyncWithAPresentStaticHostReturnsUnavailableWithoutQueuingARequest()
    {
        using var coordinator = new BzsOverlayCoordinator();
        var registry = new BzsOverlayHostRegistry(coordinator);
        var dialogs = new BzsDialogService(coordinator, registry);
        registry.RegisterStaticHost();

        var result = await dialogs.ShowAsync<TestDialogContent, string>();

        Assert.Equal(BzsDialogResultKind.Unavailable, result.Kind);
        Assert.True(result.IsUnavailable);
        Assert.Empty(coordinator.Snapshot);
    }

    [Fact]
    public void StartupCallAfterHostRegistrationReturnsUnavailableBeforeInteractiveActivation()
    {
        using var context = new BunitContext();
        context.JSInterop.Mode = JSRuntimeMode.Loose;
        context.Services.AddBzsBlazor();

        var rendered = context.Render<HostThenStartupDialogProbe>();
        var probe = rendered.FindComponent<StartupDialogProbe>().Instance;
        var coordinator = context.Services.GetRequiredService<BzsOverlayCoordinator>();

        Assert.NotNull(probe.Result);
        Assert.Equal(BzsDialogResultKind.Unavailable, probe.Result.Kind);
        Assert.Empty(coordinator.Snapshot);
    }

    [Fact]
    public async Task ActiveHostEnqueuesARenderableRequestDeliversParametersAndCompletesThroughItsContext()
    {
        using var coordinator = new BzsOverlayCoordinator();
        var registry = new BzsOverlayHostRegistry(coordinator);
        var dialogs = new BzsDialogService(coordinator, registry);
        registry.RegisterStaticHost();
        registry.ActivateInteractiveHost();

        var resultTask = dialogs.ShowAsync<ParameterizedDialogContent, string>(
            parameters => parameters.Add(component => component.Message, "Changes are ready."));
        var snapshot = Assert.Single(coordinator.Snapshot);

        using var context = new BunitContext();
        var rendered = context.Render(snapshot.Content);
        var content = rendered.FindComponent<ParameterizedDialogContent>().Instance;

        Assert.Equal("Changes are ready.", content.Message);
        Assert.NotNull(content.Dialog);
        Assert.True(content.Dialog!.Complete("accepted"));

        var result = await resultTask;
        Assert.Equal(BzsDialogResultKind.Completed, result.Kind);
        Assert.Equal("accepted", result.Value);
        Assert.Empty(coordinator.Snapshot);
        Assert.False(content.Dialog.Cancel());
    }

    [Fact]
    public async Task ShowAsyncWithADisposedHostReturnsHostDisposed()
    {
        using var coordinator = new BzsOverlayCoordinator();
        var registry = new BzsOverlayHostRegistry(coordinator);
        var dialogs = new BzsDialogService(coordinator, registry);
        registry.RegisterStaticHost();
        registry.ActivateInteractiveHost();
        registry.DisposeHost();

        var result = await dialogs.ShowAsync<TestDialogContent, string>();

        Assert.Equal(BzsDialogResultKind.HostDisposed, result.Kind);
        Assert.True(result.IsHostDisposed);
        Assert.Empty(coordinator.Snapshot);
    }

    [Fact]
    public void HostRegistryRejectsDuplicateRegistrationAndActivation()
    {
        using var coordinator = new BzsOverlayCoordinator();
        var registry = new BzsOverlayHostRegistry(coordinator);
        registry.RegisterStaticHost();

        var duplicateRegistration = Assert.Throws<InvalidOperationException>(registry.RegisterStaticHost);
        Assert.Contains("Only one BzsOverlayHost", duplicateRegistration.Message, StringComparison.Ordinal);

        registry.ActivateInteractiveHost();
        var duplicateActivation = Assert.Throws<InvalidOperationException>(registry.ActivateInteractiveHost);
        Assert.Contains("Only one BzsOverlayHost", duplicateActivation.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task DialogRequestsAreIsolatedBetweenDependencyInjectionScopes()
    {
        var services = new ServiceCollection();
        services.AddBzsBlazor();
        using var provider = services.BuildServiceProvider();
        using var firstScope = provider.CreateScope();
        using var secondScope = provider.CreateScope();
        var firstProvider = firstScope.ServiceProvider;
        var secondProvider = secondScope.ServiceProvider;
        var firstRegistry = firstProvider.GetRequiredService<BzsOverlayHostRegistry>();
        var secondRegistry = secondProvider.GetRequiredService<BzsOverlayHostRegistry>();
        var firstCoordinator = firstProvider.GetRequiredService<BzsOverlayCoordinator>();
        var secondCoordinator = secondProvider.GetRequiredService<BzsOverlayCoordinator>();
        var firstDialogs = firstProvider.GetRequiredService<IBzsDialogService>();
        var secondDialogs = secondProvider.GetRequiredService<IBzsDialogService>();
        firstRegistry.RegisterStaticHost();
        firstRegistry.ActivateInteractiveHost();
        secondRegistry.RegisterStaticHost();
        secondRegistry.ActivateInteractiveHost();

        var firstTask = firstDialogs.ShowAsync<TestDialogContent, string>();
        var secondTask = secondDialogs.ShowAsync<TestDialogContent, string>();
        var firstRequest = Assert.Single(firstCoordinator.Snapshot);
        var secondRequest = Assert.Single(secondCoordinator.Snapshot);

        Assert.NotSame(firstDialogs, secondDialogs);
        Assert.NotSame(firstCoordinator, secondCoordinator);
        Assert.True(firstCoordinator.Cancel(firstRequest.Id));

        Assert.Equal(BzsDialogResultKind.Cancelled, (await firstTask).Kind);
        Assert.Empty(firstCoordinator.Snapshot);
        Assert.Single(secondCoordinator.Snapshot);

        Assert.True(secondCoordinator.Dismiss(secondRequest.Id, BzsDialogDismissReason.Escape));
        var secondResult = await secondTask;
        Assert.Equal(BzsDialogResultKind.Dismissed, secondResult.Kind);
        Assert.Equal(BzsDialogDismissReason.Escape, secondResult.DismissReason);
        Assert.Empty(secondCoordinator.Snapshot);
    }

    [Fact]
    public void DialogParameterBuilderRejectsNonDirectNonParameterReadOnlyAndDuplicateProperties()
    {
        var nonDirect = new BzsDialogParameterBuilder<ParameterizedDialogContent>();
        var nonParameter = new BzsDialogParameterBuilder<ParameterizedDialogContent>();
        var readOnly = new BzsDialogParameterBuilder<ParameterizedDialogContent>();
        var duplicate = new BzsDialogParameterBuilder<ParameterizedDialogContent>();

        Assert.Throws<ArgumentException>(() =>
            nonDirect.Add(component => component.Metadata.Name, "nested"));
        Assert.Throws<ArgumentException>(() =>
            nonParameter.Add(component => component.NotAParameter, "unmarked"));
        Assert.Throws<ArgumentException>(() =>
            readOnly.Add(component => component.ReadOnlyMessage, "readonly"));

        duplicate.Add(component => component.Message, "first");
        Assert.Throws<InvalidOperationException>(() =>
            duplicate.Add(component => component.Message, "second"));
    }

    [Fact]
    public async Task DisposingAnActiveHostCompletesItsPendingDialogWithHostDisposed()
    {
        using var coordinator = new BzsOverlayCoordinator();
        var registry = new BzsOverlayHostRegistry(coordinator);
        var dialogs = new BzsDialogService(coordinator, registry);
        registry.RegisterStaticHost();
        registry.ActivateInteractiveHost();

        var resultTask = dialogs.ShowAsync<TestDialogContent, string>();
        Assert.Single(coordinator.Snapshot);

        registry.DisposeHost();

        Assert.Equal(BzsDialogResultKind.HostDisposed, (await resultTask).Kind);
        Assert.Empty(coordinator.Snapshot);
    }

    [Fact]
    public async Task CoordinatorSnapshotsAreStableReadOnlyAndDoNotLetSubscriberFailuresBlockNotifications()
    {
        using var coordinator = new BzsOverlayCoordinator();
        var observedChanges = new List<BzsOverlayChangedEventArgs>();
        coordinator.Changed += (_, _) => throw new InvalidOperationException("subscriber failure");
        coordinator.Changed += (_, args) => observedChanges.Add(args);
        var request = CreateRequest<string>(coordinator);

        var resultTask = coordinator.Enqueue(request, CancellationToken.None);
        var snapshot = coordinator.Snapshot;
        var snapshotItems = Assert.IsAssignableFrom<IList<BzsOverlayDialogSnapshot>>(snapshot);
        Assert.Throws<NotSupportedException>(() => snapshotItems[0] = snapshotItems[0]);
        var firstChanged = Assert.Single(observedChanges);
        var changedItems = Assert.IsAssignableFrom<IList<BzsOverlayDialogSnapshot>>(firstChanged.Snapshot);
        Assert.Throws<NotSupportedException>(() => changedItems[0] = changedItems[0]);

        Assert.True(request.TryComplete(BzsDialogResult<string>.Completed("done")));

        Assert.Equal(BzsDialogResultKind.Completed, (await resultTask).Kind);
        Assert.Equal(2, observedChanges.Count);
        Assert.Single(snapshot);
        Assert.Empty(coordinator.Snapshot);
    }

    [Fact]
    public async Task ConcurrentCompletionOperationsCompleteOneRequestExactlyOnce()
    {
        using var coordinator = new BzsOverlayCoordinator();
        var request = CreateRequest<string>(coordinator);
        var resultTask = coordinator.Enqueue(request, CancellationToken.None);

        var completions = await Task.WhenAll(
            Task.Run(() => request.TryComplete(BzsDialogResult<string>.Completed("done"))),
            Task.Run(request.TryCancel),
            Task.Run(() => request.TryDismiss(BzsDialogDismissReason.Backdrop)),
            Task.Run(request.TryHostDisposed));

        Assert.Equal(1, completions.Count(static completed => completed));
        var result = await resultTask;
        Assert.True(result.Kind is BzsDialogResultKind.Completed
            or BzsDialogResultKind.Cancelled
            or BzsDialogResultKind.Dismissed
            or BzsDialogResultKind.HostDisposed);
        Assert.Empty(coordinator.Snapshot);
        Assert.False(request.TryCancel());
        Assert.False(request.TryHostDisposed());
    }

    [Fact]
    public async Task CancellationTokenAndHostDisposalRaceLeaveNoPendingRequest()
    {
        using var coordinator = new BzsOverlayCoordinator();
        using var cancellationSource = new CancellationTokenSource();
        var request = CreateRequest<string>(coordinator);
        var resultTask = coordinator.Enqueue(request, cancellationSource.Token);

        await Task.WhenAll(
            Task.Run(cancellationSource.Cancel),
            Task.Run(coordinator.Dispose));

        var result = await resultTask;
        Assert.True(result.Kind is BzsDialogResultKind.Cancelled or BzsDialogResultKind.HostDisposed);
        Assert.Empty(coordinator.Snapshot);
        Assert.False(request.TryCancel());
        Assert.False(request.TryHostDisposed());
    }

    private static BzsOverlayDialogRequest<TResult> CreateRequest<TResult>(BzsOverlayCoordinator coordinator)
    {
        var request = coordinator.CreateDialogRequest<TResult>(new BzsDialogOptions());
        request.SetContent(static _ => { });
        return request;
    }

    private sealed class TestDialogContent : ComponentBase
    {
    }

    private sealed class HostThenStartupDialogProbe : ComponentBase
    {
        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {
            builder.OpenComponent<BzsOverlayHost>(0);
            builder.CloseComponent();
            builder.OpenComponent<StartupDialogProbe>(1);
            builder.CloseComponent();
        }
    }

    private sealed class StartupDialogProbe : ComponentBase
    {
        [Inject]
        private IBzsDialogService Dialogs { get; set; } = default!;

        internal BzsDialogResult<bool>? Result { get; private set; }

        protected override async Task OnInitializedAsync()
        {
            Result = await Dialogs.ShowAsync<TestDialogContent, bool>();
        }
    }

    private sealed class ParameterizedDialogContent : ComponentBase
    {
        [Parameter]
        public string Message { get; set; } = string.Empty;

        public string NotAParameter { get; set; } = string.Empty;

        public string ReadOnlyMessage { get; } = string.Empty;

        public DialogMetadata Metadata { get; } = new();

        [CascadingParameter]
        public BzsDialogContext<string>? Dialog { get; set; }

        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {
            builder.AddContent(0, Message);
        }
    }

    private sealed class DialogMetadata
    {
        public string Name { get; set; } = string.Empty;
    }
}
