using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;

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
