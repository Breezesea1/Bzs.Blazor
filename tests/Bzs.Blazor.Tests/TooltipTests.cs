using AngleSharp.Html.Parser;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Bzs.Blazor.Tests;

public sealed class TooltipTests
{
    [Fact]
    public async Task FocusRevealsAndBlurHidesTooltipWithoutAddingATabStop()
    {
        using var context = CreateContext();
        var cut = context.Render<BzsTooltip>(parameters => parameters
            .Add(component => component.Text, "Creates a report")
            .Add(component => component.ShowDelay, TimeSpan.Zero)
            .Add(component => component.HideDelay, TimeSpan.Zero)
            .Add(component => component.TriggerContent, BuildTrigger("Report")));
        var trigger = cut.Find("button[data-bzs-anchor='true']");

        Assert.False(trigger.HasAttribute("tabindex"));
        Assert.Single(cut.FindAll("button"));

        await trigger.FocusInAsync(new FocusEventArgs());

        var tooltip = cut.Find("[role='tooltip']");
        Assert.Equal(tooltip.Id, trigger.GetAttribute("aria-describedby"));
        Assert.Equal("Creates a report", tooltip.TextContent);

        await trigger.FocusOutAsync(new FocusEventArgs());
        Assert.Empty(cut.FindAll("[role='tooltip']"));
    }

    [Fact]
    public void TriggerAttributesPreserveNaturalSemanticsAndAccessibleNames()
    {
        using var context = CreateContext();
        var cut = context.Render<BzsTooltip>(parameters => parameters
            .Add(component => component.Text, "Downloads the report")
            .Add(component => component.TriggerContent, BuildTrigger("", "a", "Download report")));

        var trigger = cut.Find("a[data-bzs-anchor='true']");
        Assert.Equal("Download report", trigger.GetAttribute("aria-label"));
        Assert.Equal("/report", trigger.GetAttribute("href"));
        Assert.Empty(cut.FindAll("[data-bzs-anchor='true'] [tabindex]"));
    }

    [Fact]
    public async Task SupersededShowDelayCannotRevealStaleTooltip()
    {
        using var context = CreateContext();
        var cut = context.Render<BzsTooltip>(parameters => parameters
            .Add(component => component.Text, "Delayed")
            .Add(component => component.ShowDelay, TimeSpan.FromSeconds(1))
            .Add(component => component.HideDelay, TimeSpan.Zero)
            .Add(component => component.TriggerContent, BuildTrigger("Target")));
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        cut.Instance.DelayAsync = (_, cancellationToken) => release.Task.WaitAsync(cancellationToken);
        var trigger = cut.Find("[data-bzs-anchor='true']");

        var entering = trigger.PointerEnterAsync(new PointerEventArgs { PointerType = "mouse" });
        await trigger.PointerLeaveAsync(new PointerEventArgs { PointerType = "mouse" });
        release.TrySetResult();
        await entering;

        Assert.Empty(cut.FindAll("[role='tooltip']"));
    }

    [Fact]
    public async Task DisablingCancelsPendingShowEvenWhenReenabledBeforeDelayCompletes()
    {
        using var context = CreateContext();
        var cut = context.Render<BzsTooltip>(parameters => parameters
            .Add(component => component.Text, "Delayed")
            .Add(component => component.ShowDelay, TimeSpan.FromSeconds(1))
            .Add(component => component.TriggerContent, BuildTrigger("Target")));
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        cut.Instance.DelayAsync = (_, cancellationToken) => release.Task.WaitAsync(cancellationToken);

        var pending = cut.Find("[data-bzs-anchor='true']").FocusInAsync(new FocusEventArgs());
        cut.Render(parameters => parameters.Add(component => component.Disabled, true));
        cut.Render(parameters => parameters.Add(component => component.Disabled, false));
        release.TrySetResult();
        await pending;

        Assert.Empty(cut.FindAll("[role='tooltip']"));
    }

    [Fact]
    public async Task TouchRequiresACompletedTapAndBrowserDismissalClosesTooltip()
    {
        using var context = CreateContext();
        var cut = context.Render<BzsTooltip>(parameters => parameters
            .Add(component => component.Text, "Touch help")
            .Add(component => component.TriggerContent, BuildTrigger("Target")));
        var trigger = cut.Find("[data-bzs-anchor='true']");

        await trigger.TriggerEventAsync(
            "onpointerdown",
            new PointerEventArgs { PointerType = "touch", PointerId = 1 });
        Assert.Empty(cut.FindAll("[role='tooltip']"));

        await trigger.TriggerEventAsync(
            "onpointercancel",
            new PointerEventArgs { PointerType = "touch", PointerId = 1 });
        await trigger.TriggerEventAsync(
            "onpointerup",
            new PointerEventArgs { PointerType = "touch", PointerId = 1 });
        Assert.Empty(cut.FindAll("[role='tooltip']"));

        await trigger.TriggerEventAsync(
            "onpointerdown",
            new PointerEventArgs { PointerType = "touch", PointerId = 2 });
        await trigger.TriggerEventAsync(
            "onpointerup",
            new PointerEventArgs { PointerType = "touch", PointerId = 2 });
        Assert.Single(cut.FindAll("[role='tooltip']"));

        await cut.Instance.CloseFromBrowserAsync();
        Assert.Empty(cut.FindAll("[role='tooltip']"));
    }

    [Fact]
    public async Task DisabledTooltipCannotBeRevealedAndDisposalCancelsActiveDelay()
    {
        using var context = CreateContext();
        var cut = context.Render<BzsTooltip>(parameters => parameters
            .Add(component => component.Text, "Unavailable")
            .Add(component => component.Disabled, true)
            .Add(component => component.TriggerContent, BuildTrigger("Target")));

        await cut.Find("[data-bzs-anchor='true']").FocusInAsync(new FocusEventArgs());
        Assert.Empty(cut.FindAll("[role='tooltip']"));

        cut.Render(parameters => parameters
            .Add(component => component.Disabled, false)
            .Add(component => component.ShowDelay, TimeSpan.FromSeconds(1)));
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        cut.Instance.DelayAsync = (_, cancellationToken) => release.Task.WaitAsync(cancellationToken);
        var pending = cut.Find("[data-bzs-anchor='true']").FocusInAsync(new FocusEventArgs());

        await cut.Instance.DisposeAsync();
        release.TrySetResult();
        await pending;
        await cut.Instance.CloseFromBrowserAsync();
    }

    [Fact]
    public async Task TooltipSupportsRichContentAndRejectsAmbiguousContent()
    {
        using var context = CreateContext();
        var cut = context.Render<BzsTooltip>(parameters => parameters
            .Add(component => component.TooltipContent, builder =>
            {
                builder.OpenElement(0, "strong");
                builder.AddContent(1, "Rich help");
                builder.CloseElement();
            })
            .Add(component => component.ShowDelay, TimeSpan.Zero)
            .Add(component => component.TriggerContent, BuildTrigger("Target")));

        await cut.Find("[data-bzs-anchor='true']").FocusInAsync(new FocusEventArgs());
        Assert.Equal("Rich help", cut.Find("[role='tooltip'] strong").TextContent);

        Assert.Throws<InvalidOperationException>(() => context.Render<BzsTooltip>(parameters => parameters
            .Add(component => component.Text, "Text")
            .Add(component => component.TooltipContent, "Content")
            .Add(component => component.TriggerContent, BuildTrigger("Target"))));
    }

    [Fact]
    public void TooltipMakesThreeBoundedImmediateSynchronizationAttempts()
    {
        using var context = new BunitContext();
        context.Services.AddBzsBlazor();
        var module = context.JSInterop.SetupModule(BzsAnchoredOverlayInterop.ModulePath);
        module.SetupVoid(BzsAnchoredOverlayInterop.InitializeMethod, _ => true).SetVoidResult();
        var setOpen = module.SetupVoid(BzsAnchoredOverlayInterop.SetOpenMethod, _ => true)
            .SetException(new TaskCanceledException("Tooltip synchronization was interrupted."));
        module.SetupVoid(BzsAnchoredOverlayInterop.DisposeMethod, _ => true).SetVoidResult();

        context.Render<BzsTooltip>(parameters => parameters
            .Add(component => component.Text, "Retry")
            .Add(component => component.TriggerContent, BuildTrigger("Target")));

        setOpen.VerifyInvoke(BzsAnchoredOverlayInterop.SetOpenMethod, 3);
    }

    [Fact]
    public async Task StaticRenderingKeepsTheConsumerTriggerSemanticAndInert()
    {
        using var context = CreateContext();
        await using var renderer = new HtmlRenderer(
            context.Services,
            context.Services.GetRequiredService<ILoggerFactory>());
        var parameters = ParameterView.FromDictionary(new Dictionary<string, object?>
        {
            [nameof(BzsTooltip.Text)] = "Creates a report",
            [nameof(BzsTooltip.TriggerContent)] = BuildTrigger("Report"),
        });

        var html = await renderer.Dispatcher.InvokeAsync(async () =>
        {
            var output = await renderer.RenderComponentAsync<BzsTooltip>(parameters);
            return output.ToHtmlString();
        });
        var document = await new HtmlParser().ParseDocumentAsync(html);

        Assert.NotNull(document.QuerySelector("button[data-bzs-anchor='true']"));
        Assert.Null(document.QuerySelector("[role='tooltip']"));
        Assert.Equal(1, document.QuerySelectorAll("button").Length);
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

    private static RenderFragment<BzsTooltipTriggerContext> BuildTrigger(
        string text,
        string element = "button",
        string? accessibleName = null) => trigger => builder =>
    {
        builder.OpenElement(0, element);
        builder.AddMultipleAttributes(1, trigger.Attributes);
        if (element == "button")
        {
            builder.AddAttribute(2, "type", "button");
        }
        else if (element == "a")
        {
            builder.AddAttribute(2, "href", "/report");
        }
        if (accessibleName is not null)
        {
            builder.AddAttribute(3, "aria-label", accessibleName);
        }
        builder.AddContent(4, text);
        builder.CloseElement();
    };
}
