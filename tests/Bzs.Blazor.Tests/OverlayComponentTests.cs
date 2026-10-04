using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;

namespace Bzs.Blazor.Tests;

public sealed class OverlayComponentTests
{
    [Fact]
    public void DialogRequestsControlledCloseWithoutMutatingItsOpenParameter()
    {
        using var context = CreateContext();
        var requestedOpen = true;
        var reason = default(BzsDialogDismissReason?);
        var cut = context.Render<BzsDialog>(parameters => parameters
            .Add(component => component.Open, true)
            .Add(component => component.Title, "Confirm publish")
            .Add(component => component.OpenChanged, (bool value) => requestedOpen = value)
            .Add(component => component.Dismissed, (BzsDialogDismissReason value) => reason = value)
            .Add(component => component.ChildContent, "Dialog body"));

        var dialog = cut.Find("[role=dialog]");
        Assert.Equal("true", dialog.GetAttribute("aria-modal"));
        Assert.NotNull(dialog.GetAttribute("aria-labelledby"));

        cut.Find("button").Click();

        Assert.False(requestedOpen);
        Assert.Equal(BzsDialogDismissReason.CloseButton, reason);
        Assert.True(cut.Instance.Open);
        Assert.NotNull(cut.Find("[role=dialog]"));
    }

    [Fact]
    public void DialogHonorsEscapeAndBackdropPolicies()
    {
        using var context = CreateContext();
        var reasons = new List<BzsDialogDismissReason>();
        var cut = context.Render<BzsDialog>(parameters => parameters
            .Add(component => component.Open, true)
            .Add(component => component.AccessibleName, "Policy dialog")
            .Add(component => component.CloseOnEscape, false)
            .Add(component => component.CloseOnBackdropClick, false)
            .Add(component => component.Dismissed, (BzsDialogDismissReason value) => reasons.Add(value)));

        cut.Find("[role=dialog]").KeyDown(new KeyboardEventArgs { Key = "Escape" });
        cut.Find(".bzs-dialog__backdrop").Click();
        Assert.Empty(reasons);

        var enabled = context.Render<BzsDialog>(parameters => parameters
            .Add(component => component.Open, true)
            .Add(component => component.AccessibleName, "Policy dialog")
            .Add(component => component.CloseOnEscape, true)
            .Add(component => component.CloseOnBackdropClick, true)
            .Add(component => component.Dismissed, (BzsDialogDismissReason value) => reasons.Add(value)));
        enabled.Find("[role=dialog]").KeyDown(new KeyboardEventArgs { Key = "Escape" });
        enabled.Find(".bzs-dialog__backdrop").Click();

        Assert.Equal([BzsDialogDismissReason.Escape, BzsDialogDismissReason.Backdrop], reasons);
    }

    [Theory]
    [InlineData(BzsDrawerPlacement.Start, "start")]
    [InlineData(BzsDrawerPlacement.End, "end")]
    [InlineData(BzsDrawerPlacement.Top, "top")]
    [InlineData(BzsDrawerPlacement.Bottom, "bottom")]
    public void DrawerUsesLogicalPlacementAndModalSemantics(BzsDrawerPlacement placement, string expected)
    {
        using var context = CreateContext();
        var cut = context.Render<BzsDrawer>(parameters => parameters
            .Add(component => component.Open, true)
            .Add(component => component.Title, "Details")
            .Add(component => component.Placement, placement)
            .Add(component => component.Modal, false));

        var drawer = cut.Find("[role=dialog]");
        Assert.Equal(expected, drawer.GetAttribute("data-bzs-drawer"));
        Assert.Null(drawer.GetAttribute("aria-modal"));
        Assert.Empty(cut.FindAll(".bzs-drawer__backdrop"));
    }

    [Fact]
    public async Task ModalPanelLifecycleActivatesWithNormalizedFocusWhenOpened()
    {
        var runtime = new PanelLifecycleJsRuntime();
        await using var lifecycle = new BzsModalPanelLifecycle(runtime, null, "panel-1");

        lifecycle.NotifyParameters(open: true, modal: true, initialFocusSelector: "  #first  ");
        await lifecycle.SynchronizeAsync(open: true, firstRender: false, panelElement: default);

        var activation = Assert.Single(runtime.Module.Activations);
        Assert.Equal("panel-1", activation.OverlayId);
        Assert.True(activation.Modal);
        Assert.Equal("#first", activation.Selector);
        Assert.Equal(["activate"], runtime.Module.Invocations);
    }

    [Fact]
    public async Task ModalPanelLifecycleDeactivatesWhenClosed()
    {
        var runtime = new PanelLifecycleJsRuntime();
        await using var lifecycle = new BzsModalPanelLifecycle(runtime, null, "panel-1");

        lifecycle.NotifyParameters(open: true, modal: true, initialFocusSelector: null);
        await lifecycle.SynchronizeAsync(open: true, firstRender: false, panelElement: default);

        lifecycle.NotifyParameters(open: false, modal: true, initialFocusSelector: null);
        await lifecycle.SynchronizeAsync(open: false, firstRender: false, panelElement: default);

        Assert.Equal(["activate", "deactivate"], runtime.Module.Invocations);
    }

    [Fact]
    public async Task ModalPanelLifecycleResynchronizesWhenModalityChanges()
    {
        var runtime = new PanelLifecycleJsRuntime();
        await using var lifecycle = new BzsModalPanelLifecycle(runtime, null, "panel-1");

        lifecycle.NotifyParameters(open: true, modal: true, initialFocusSelector: null);
        await lifecycle.SynchronizeAsync(open: true, firstRender: false, panelElement: default);

        lifecycle.NotifyParameters(open: true, modal: false, initialFocusSelector: null);
        await lifecycle.SynchronizeAsync(open: true, firstRender: false, panelElement: default);

        Assert.Equal(["activate", "activate"], runtime.Module.Invocations);
        Assert.Equal([true, false], runtime.Module.Activations.Select(activation => activation.Modal));
    }

    [Fact]
    public async Task ModalPanelLifecycleSkipsSynchronizationWhenNothingChanged()
    {
        var runtime = new PanelLifecycleJsRuntime();
        await using var lifecycle = new BzsModalPanelLifecycle(runtime, null, "panel-1");

        lifecycle.NotifyParameters(open: true, modal: true, initialFocusSelector: "#first");
        await lifecycle.SynchronizeAsync(open: true, firstRender: false, panelElement: default);
        await lifecycle.SynchronizeAsync(open: true, firstRender: false, panelElement: default);

        Assert.Equal(["activate"], runtime.Module.Invocations);
    }

    [Fact]
    public async Task DisposedModalPanelLifecycleDoesNotSynchronizeOrDeactivate()
    {
        var runtime = new PanelLifecycleJsRuntime();
        var lifecycle = new BzsModalPanelLifecycle(runtime, null, "panel-1");

        await lifecycle.DisposeAsync();

        lifecycle.NotifyParameters(open: true, modal: true, initialFocusSelector: null);
        await lifecycle.SynchronizeAsync(open: true, firstRender: true, panelElement: default);

        Assert.Empty(runtime.Module.Invocations);
    }

    [Fact]
    public async Task ModalPanelLifecycleDisposeDeactivatesItsLoadedModule()
    {
        var runtime = new PanelLifecycleJsRuntime();
        var lifecycle = new BzsModalPanelLifecycle(runtime, null, "panel-1");

        lifecycle.NotifyParameters(open: true, modal: true, initialFocusSelector: null);
        await lifecycle.SynchronizeAsync(open: true, firstRender: false, panelElement: default);

        await lifecycle.DisposeAsync();

        Assert.Equal(["activate", "deactivate"], runtime.Module.Invocations);
    }

    [Fact]
    public async Task OverlayHostRendersServiceDialogsAndCompletesTheirTypedContext()
    {
        using var context = CreateContext();
        var host = context.Render<BzsOverlayHost>();
        var dialogs = context.Services.GetRequiredService<IBzsDialogService>();

        var resultTask = dialogs.ShowAsync<HostedDialogContent, string>(
            parameters => parameters.Add(component => component.Message, "Hosted content"),
            new BzsDialogOptions { Title = "Hosted dialog" });

        host.WaitForAssertion(() => Assert.Contains("Hosted content", host.Markup, StringComparison.Ordinal));
        var content = host.FindComponent<HostedDialogContent>().Instance;
        Assert.NotNull(content.Dialog);
        Assert.True(content.Dialog!.Complete("accepted"));

        var result = await resultTask;
        Assert.Equal(BzsDialogResultKind.Completed, result.Kind);
        Assert.Equal("accepted", result.Value);
        host.WaitForAssertion(() => Assert.Empty(host.FindAll("[role=dialog]")));
    }

    [Fact]
    public void OverlayHostRendersAndDismissesScopedToasts()
    {
        using var context = CreateContext();
        var host = context.Render<BzsOverlayHost>();
        var toasts = context.Services.GetRequiredService<IBzsToastService>();

        toasts.Show(new BzsToastOptions
        {
            Message = "Saved",
            Duration = Timeout.InfiniteTimeSpan,
        });

        host.WaitForAssertion(() => Assert.Contains("Saved", host.Markup, StringComparison.Ordinal));
        host.Find(".bzs-toast button").Click();
        host.WaitForAssertion(() => Assert.Empty(toasts.Snapshot));
    }

    [Fact]
    public void OverlayHostKeepsTheToastRegionMountedBeforeAnyToastExists()
    {
        using var context = CreateContext();
        var host = context.Render<BzsOverlayHost>();

        var region = host.Find(".bzs-overlay-host__toasts");
        Assert.NotNull(region.GetAttribute("aria-label"));
        Assert.Empty(region.Children);
    }

    [Fact]
    public void OverlayHostRejectsDuplicatesInOneScope()
    {
        using var context = CreateContext();
        _ = context.Render<BzsOverlayHost>();

        var exception = Assert.Throws<InvalidOperationException>(() => context.Render<BzsOverlayHost>());

        Assert.Contains("Only one BzsOverlayHost", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task DisposingTheHostCompletesPendingDialogsAsHostDisposed()
    {
        using var context = CreateContext();
        var host = context.Render<BzsOverlayHost>();
        var dialogs = context.Services.GetRequiredService<IBzsDialogService>();
        var resultTask = dialogs.ShowAsync<HostedDialogContent, string>();

        await host.Instance.DisposeAsync();

        var result = await resultTask;
        Assert.Equal(BzsDialogResultKind.HostDisposed, result.Kind);
    }

    [Fact]
    public async Task DisposedOverlayHostCanBeReplacedWithinTheSameScope()
    {
        using var context = CreateContext();
        var firstHost = context.Render<BzsOverlayHost>();
        var dialogs = context.Services.GetRequiredService<IBzsDialogService>();
        var firstResultTask = dialogs.ShowAsync<HostedDialogContent, string>();

        await firstHost.Instance.DisposeAsync();

        Assert.Equal(BzsDialogResultKind.HostDisposed, (await firstResultTask).Kind);
        var inactiveResult = await dialogs.ShowAsync<HostedDialogContent, string>();
        Assert.Equal(BzsDialogResultKind.HostDisposed, inactiveResult.Kind);

        var secondHost = context.Render<BzsOverlayHost>();
        var secondResultTask = dialogs.ShowAsync<HostedDialogContent, string>(
            parameters => parameters.Add(component => component.Message, "Replacement host content"));

        secondHost.WaitForAssertion(
            () => Assert.Contains("Replacement host content", secondHost.Markup, StringComparison.Ordinal));
        var content = secondHost.FindComponent<HostedDialogContent>().Instance;
        Assert.NotNull(content.Dialog);
        Assert.True(content.Dialog!.Complete("accepted"));

        var secondResult = await secondResultTask;
        Assert.Equal(BzsDialogResultKind.Completed, secondResult.Kind);
        Assert.Equal("accepted", secondResult.Value);
    }

    private sealed class PanelLifecycleJsRuntime : IJSRuntime
    {
        internal PanelLifecycleJsModule Module { get; } = new();

        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object?[]? args) =>
            InvokeAsync<TValue>(identifier, CancellationToken.None, args);

        public ValueTask<TValue> InvokeAsync<TValue>(
            string identifier,
            CancellationToken cancellationToken,
            object?[]? args)
        {
            Assert.Equal("import", identifier);
            return ValueTask.FromResult((TValue)(object)Module);
        }
    }

    private sealed class PanelLifecycleJsModule : IJSObjectReference
    {
        internal List<PanelActivation> Activations { get; } = [];

        internal List<string> Invocations { get; } = [];

        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object?[]? args)
        {
            Invocations.Add(identifier);
            if (identifier == BzsOverlayInterop.ActivateMethod)
            {
                Activations.Add(new PanelActivation(
                    (string)args![0]!,
                    (bool)args[2]!,
                    (string?)args[3]));
            }

            return ValueTask.FromResult(default(TValue)!);
        }

        public ValueTask<TValue> InvokeAsync<TValue>(
            string identifier,
            CancellationToken cancellationToken,
            object?[]? args) =>
            InvokeAsync<TValue>(identifier, args);

        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }

    private sealed record PanelActivation(string OverlayId, bool Modal, string? Selector);

    private static BunitContext CreateContext()
    {
        var context = new BunitContext();
        context.JSInterop.Mode = JSRuntimeMode.Loose;
        context.Services.AddBzsBlazor();
        return context;
    }

    private sealed class HostedDialogContent : ComponentBase
    {
        [Parameter]
        public string Message { get; set; } = string.Empty;

        [CascadingParameter]
        public BzsDialogContext<string>? Dialog { get; set; }

        protected override void BuildRenderTree(RenderTreeBuilder builder) => builder.AddContent(0, Message);
    }
}
