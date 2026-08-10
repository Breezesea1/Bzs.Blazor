using AngleSharp.Html.Parser;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Bzs.Blazor.Tests;

public sealed class PopoverTests
{
    [Fact]
    public void PopoverRequestsControlledStateWithoutMutatingIt()
    {
        using var context = CreateContext();
        bool? requestedOpen = null;
        var cut = context.Render<BzsPopover>(parameters => parameters
            .Add(component => component.Open, false)
            .Add(component => component.OpenChanged, value => requestedOpen = value)
            .Add(component => component.TriggerContent, "Open tools")
            .Add(component => component.ChildContent, "Tools"));

        cut.Find("button").Click();

        Assert.True(requestedOpen);
        Assert.Equal("false", cut.Find("button").GetAttribute("aria-expanded"));
        Assert.Empty(cut.FindAll("[data-bzs-anchored-panel='true']"));
    }

    [Fact]
    public async Task BrowserClosureRequestsControlledState()
    {
        using var context = CreateContext();
        bool? requestedOpen = null;
        var cut = context.Render<BzsPopover>(parameters => parameters
            .Add(component => component.Open, true)
            .Add(component => component.OpenChanged, value => requestedOpen = value)
            .Add(component => component.TriggerContent, "Open tools")
            .Add(component => component.ChildContent, "Tools"));

        await cut.Instance.CloseFromBrowserAsync(restoreFocus: true);

        Assert.False(requestedOpen);
        Assert.NotEmpty(cut.FindAll("[data-bzs-anchored-panel='true']"));
    }

    [Fact]
    public async Task DisabledOpenPopoverStillRequestsBrowserDismissal()
    {
        using var context = CreateContext();
        var callbackCount = 0;
        var cut = context.Render<BzsPopover>(parameters => parameters
            .Add(component => component.Open, true)
            .Add(component => component.Disabled, true)
            .Add(component => component.OpenChanged, _ => callbackCount++)
            .Add(component => component.TriggerContent, "Open tools")
            .Add(component => component.ChildContent, "Tools"));

        await cut.Instance.CloseFromBrowserAsync();

        Assert.Equal(1, callbackCount);
        Assert.NotEmpty(cut.FindAll("[data-bzs-anchored-panel='true']"));
    }

    [Fact]
    public async Task BrowserCallbackAfterDisposalIsIgnored()
    {
        using var context = CreateContext();
        var callbackCount = 0;
        var cut = context.Render<BzsPopover>(parameters => parameters
            .Add(component => component.Open, true)
            .Add(component => component.OpenChanged, _ => callbackCount++)
            .Add(component => component.TriggerContent, "Open tools")
            .Add(component => component.ChildContent, "Tools"));

        await cut.Instance.DisposeAsync();
        await cut.Instance.CloseFromBrowserAsync();

        Assert.Equal(0, callbackCount);
    }

    [Fact]
    public void PopoverInitializesAndSynchronizesAnchoredInterop()
    {
        using var context = new BunitContext();
        context.Services.AddBzsBlazor();
        var module = context.JSInterop.SetupModule(BzsAnchoredOverlayInterop.ModulePath);
        module.SetupVoid(BzsAnchoredOverlayInterop.InitializeMethod, _ => true).SetVoidResult();
        module.SetupVoid(BzsAnchoredOverlayInterop.SetOpenMethod, _ => true).SetVoidResult();
        module.SetupVoid(BzsAnchoredOverlayInterop.DisposeMethod, _ => true).SetVoidResult();

        var cut = context.Render<BzsPopover>(parameters => parameters
            .Add(component => component.Open, true)
            .Add(component => component.Placement, BzsPopoverPlacement.TopEnd)
            .Add(component => component.TriggerContent, "Open tools")
            .Add(component => component.ChildContent, "Tools"));

        cut.WaitForAssertion(() =>
        {
            Assert.Single(module.Invocations[BzsAnchoredOverlayInterop.InitializeMethod]);
            var synchronization = Assert.Single(module.Invocations[BzsAnchoredOverlayInterop.SetOpenMethod]);
            Assert.Contains("top-end", synchronization.Arguments);
        });
    }

    [Fact]
    public void PopoverRetriesTransientInitializationAndRecoversOnALaterRender()
    {
        using var context = new BunitContext();
        context.Services.AddBzsBlazor();
        var module = context.JSInterop.SetupModule(BzsAnchoredOverlayInterop.ModulePath);
        var initialize = module.SetupVoid(BzsAnchoredOverlayInterop.InitializeMethod, _ => true)
            .SetException(new TaskCanceledException("Anchored overlay initialization was interrupted."));
        var setOpen = module.SetupVoid(BzsAnchoredOverlayInterop.SetOpenMethod, _ => true).SetVoidResult();
        module.SetupVoid(BzsAnchoredOverlayInterop.DisposeMethod, _ => true).SetVoidResult();

        var cut = context.Render<BzsPopover>(parameters => parameters
            .Add(component => component.Open, true)
            .Add(component => component.TriggerContent, "Open tools")
            .Add(component => component.ChildContent, "Tools"));

        initialize.VerifyInvoke(BzsAnchoredOverlayInterop.InitializeMethod, 2);
        initialize.SetVoidResult();
        cut.Render();

        initialize.VerifyInvoke(BzsAnchoredOverlayInterop.InitializeMethod, 3);
        setOpen.VerifyInvoke(BzsAnchoredOverlayInterop.SetOpenMethod, 1);
    }

    [Fact]
    public void PopoverRetriesTransientSynchronizationAndRecoversOnALaterRender()
    {
        using var context = new BunitContext();
        context.Services.AddBzsBlazor();
        var module = context.JSInterop.SetupModule(BzsAnchoredOverlayInterop.ModulePath);
        module.SetupVoid(BzsAnchoredOverlayInterop.InitializeMethod, _ => true).SetVoidResult();
        var setOpen = module.SetupVoid(BzsAnchoredOverlayInterop.SetOpenMethod, _ => true)
            .SetException(new TaskCanceledException("Anchored overlay synchronization was interrupted."));
        module.SetupVoid(BzsAnchoredOverlayInterop.DisposeMethod, _ => true).SetVoidResult();

        var cut = context.Render<BzsPopover>(parameters => parameters
            .Add(component => component.Open, true)
            .Add(component => component.TriggerContent, "Open tools")
            .Add(component => component.ChildContent, "Tools"));

        setOpen.VerifyInvoke(BzsAnchoredOverlayInterop.SetOpenMethod, 2);
        setOpen.SetVoidResult();
        cut.Render();
        setOpen.VerifyInvoke(BzsAnchoredOverlayInterop.SetOpenMethod, 3);
        cut.Render();
        setOpen.VerifyInvoke(BzsAnchoredOverlayInterop.SetOpenMethod, 3);
    }

    [Fact]
    public async Task OpenPopoverRendersMeaningfulStaticMarkup()
    {
        using var context = new BunitContext();
        context.Services.AddBzsBlazor();
        await using var renderer = new HtmlRenderer(
            context.Services,
            context.Services.GetRequiredService<ILoggerFactory>());
        var parameters = ParameterView.FromDictionary(new Dictionary<string, object?>
        {
            [nameof(BzsPopover.Open)] = true,
            [nameof(BzsPopover.TriggerContent)] = (RenderFragment)(builder => builder.AddContent(0, "Open tools")),
            [nameof(BzsPopover.ChildContent)] = (RenderFragment)(builder => builder.AddContent(0, "Tool content")),
        });

        var html = await renderer.Dispatcher.InvokeAsync(async () =>
        {
            var output = await renderer.RenderComponentAsync<BzsPopover>(parameters);
            return output.ToHtmlString();
        });
        var document = await new HtmlParser().ParseDocumentAsync(html);

        Assert.Equal("true", document.QuerySelector("button")?.GetAttribute("aria-expanded"));
        Assert.Contains("Tool content", document.Body?.TextContent, StringComparison.Ordinal);
    }

    [Fact]
    public void PopoverRejectsUnsupportedPlacement()
    {
        using var context = CreateContext();

        Assert.Throws<ArgumentOutOfRangeException>(() => context.Render<BzsPopover>(parameters => parameters
            .Add(component => component.Placement, (BzsPopoverPlacement)999)
            .Add(component => component.TriggerContent, "Open tools")
            .Add(component => component.ChildContent, "Tools")));
    }

    private static BunitContext CreateContext()
    {
        var context = new BunitContext();
        context.Services.AddBzsBlazor();
        var module = context.JSInterop.SetupModule(BzsAnchoredOverlayInterop.ModulePath);
        module.SetupVoid(BzsAnchoredOverlayInterop.InitializeMethod, _ => true).SetVoidResult();
        module.SetupVoid(BzsAnchoredOverlayInterop.SetOpenMethod, _ => true).SetVoidResult();
        module.SetupVoid(BzsAnchoredOverlayInterop.DisposeMethod, _ => true).SetVoidResult();
        return context;
    }
}
