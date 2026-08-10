using AngleSharp.Html.Parser;
using Bunit;
using Bunit.JSInterop;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Bzs.Blazor.Tests;

public sealed class MenuTests
{
    [Fact]
    public void NavigationSkipsDisabledItemsAndSupportsBoundariesAndTypeahead()
    {
        var disabled = new[] { false, true, false, false };
        var labels = new[] { "Alpha", "Blocked", "Beta", "Bravo" };

        Assert.Equal(2, BzsMenuNavigation.Move(disabled, 0, 1));
        Assert.Equal(3, BzsMenuNavigation.Move(disabled, 0, -1));
        Assert.Equal(0, BzsMenuNavigation.FindBoundary(disabled, last: false));
        Assert.Equal(3, BzsMenuNavigation.FindBoundary(disabled, last: true));
        Assert.Equal(2, BzsMenuNavigation.FindTypeahead(labels, disabled, 0, "be"));
        Assert.Equal(3, BzsMenuNavigation.FindTypeahead(labels, disabled, 2, "bb"));
    }

    [Fact]
    public void MenuRendersRolesCheckedStateDisabledItemsAndSeparators()
    {
        using var context = CreateContext();
        var cut = context.Render<BzsMenu>(parameters => parameters
            .Add(component => component.Open, true)
            .Add(component => component.AccessibleName, "Actions")
            .Add(component => component.TriggerContent, "Open actions")
            .Add(component => component.ChildContent, BuildItems(
                new ItemDefinition("Pin", Checkable: true, Checked: true),
                new ItemDefinition("Unavailable", Disabled: true),
                new ItemDefinition(null, Separator: true),
                new ItemDefinition("Archive"))));

        Assert.Equal("menu", cut.Find("ul").GetAttribute("role"));
        Assert.Equal("Actions", cut.Find("ul").GetAttribute("aria-label"));
        Assert.Equal("true", cut.Find("[role='menuitemcheckbox']").GetAttribute("aria-checked"));
        Assert.True(cut.FindAll("button[role='menuitem']")[0].HasAttribute("disabled"));
        Assert.Single(cut.FindAll("[role='separator']"));
        Assert.Equal("0", cut.Find("[role='menuitemcheckbox']").GetAttribute("tabindex"));
    }

    [Fact]
    public void MenuItemsPreserveRootIdentityClassStyleAndAdditionalAttributes()
    {
        using var context = CreateContext();
        var cut = context.Render<BzsMenu>(parameters => parameters
            .Add(component => component.Open, true)
            .Add(component => component.TriggerContent, "Open actions")
            .Add(component => component.ChildContent, builder =>
            {
                builder.OpenComponent<BzsMenuItem>(0);
                builder.AddAttribute(1, nameof(BzsMenuItem.Id), "command-root");
                builder.AddAttribute(2, nameof(BzsMenuItem.Class), "consumer-command");
                builder.AddAttribute(3, nameof(BzsMenuItem.Style), "color: red");
                builder.AddAttribute(4, "data-command", "preserved");
                builder.AddAttribute(5, nameof(BzsMenuItem.Text), "Archive");
                builder.CloseComponent();

                builder.OpenComponent<BzsMenuItem>(10);
                builder.AddAttribute(11, nameof(BzsMenuItem.Id), "separator-root");
                builder.AddAttribute(12, nameof(BzsMenuItem.Class), "consumer-separator");
                builder.AddAttribute(13, nameof(BzsMenuItem.Style), "margin: 1px");
                builder.AddAttribute(14, "data-separator", "preserved");
                builder.AddAttribute(15, nameof(BzsMenuItem.Separator), true);
                builder.CloseComponent();
            }));

        var command = cut.Find("#command-root");
        Assert.Equal("none", command.GetAttribute("role"));
        Assert.Contains("bzs-menu-item", command.ClassList);
        Assert.Contains("consumer-command", command.ClassList);
        Assert.Equal("color: red;", command.GetAttribute("style"));
        Assert.Equal("preserved", command.GetAttribute("data-command"));

        var separator = cut.Find("#separator-root");
        Assert.Equal("separator", separator.GetAttribute("role"));
        Assert.Contains("bzs-menu-item--separator", separator.ClassList);
        Assert.Contains("consumer-separator", separator.ClassList);
        Assert.Equal("margin: 1px;", separator.GetAttribute("style"));
        Assert.Equal("preserved", separator.GetAttribute("data-separator"));
    }

    [Fact]
    public void MenusUseGeneratedTriggerOrTargetIdsForDefaultAccessibleNames()
    {
        using var context = CreateContext();
        var menu = context.Render<BzsMenu>(parameters => parameters
            .Add(component => component.Open, true)
            .Add(component => component.TriggerContent, "Open actions")
            .Add(component => component.ChildContent, BuildItems(new ItemDefinition("Archive"))));

        var triggerId = menu.Find("button[data-bzs-anchor='true']").Id;
        var panel = menu.Find("[role='menu']");
        Assert.StartsWith("bzs-menu-trigger-", triggerId, StringComparison.Ordinal);
        Assert.Equal(triggerId, panel.GetAttribute("aria-labelledby"));
        Assert.False(panel.HasAttribute("aria-label"));

        var contextMenu = context.Render<BzsContextMenu>(parameters => parameters
            .Add(component => component.Open, true)
            .Add(component => component.AccessibleName, "Document actions")
            .Add(component => component.TargetContent, "Document")
            .Add(component => component.ChildContent, BuildItems(new ItemDefinition("Rename"))));

        var targetId = contextMenu.Find("[data-bzs-anchor='true']").Id;
        var contextPanel = contextMenu.Find("[role='menu']");
        Assert.StartsWith("bzs-context-menu-target-", targetId, StringComparison.Ordinal);
        Assert.Equal("Document actions", contextPanel.GetAttribute("aria-label"));
        Assert.False(contextPanel.HasAttribute("aria-labelledby"));
    }

    [Fact]
    public void ContextMenuTargetUsesSupportedGenericTargetAria()
    {
        using var context = CreateContext();
        var cut = context.Render<BzsContextMenu>(parameters => parameters
            .Add(component => component.Open, true)
            .Add(component => component.TargetAccessibleName, "Document actions")
            .Add(component => component.TargetContent, "Document")
            .Add(component => component.ChildContent, BuildItems(new ItemDefinition("Rename"))));

        var target = cut.Find("[data-bzs-anchor='true']");
        Assert.Equal("menu", target.GetAttribute("aria-haspopup"));
        Assert.Equal(cut.Find("[role='menu']").Id, target.GetAttribute("aria-controls"));
        Assert.False(target.HasAttribute("aria-expanded"));
    }

    [Fact]
    public async Task TriggerAndBrowserDismissalOnlyRequestControlledState()
    {
        using var context = CreateContext();
        var requested = new List<bool>();
        var cut = context.Render<BzsMenu>(parameters => parameters
            .Add(component => component.Open, false)
            .Add(component => component.OpenChanged, value => requested.Add(value))
            .Add(component => component.TriggerContent, "Open actions")
            .Add(component => component.ChildContent, BuildItems(new ItemDefinition("Archive"))));

        cut.Find("button[data-bzs-anchor='true']").Click();
        Assert.Equal([true], requested);
        Assert.Empty(cut.FindAll("[role='menu']"));

        cut.Render(parameters => parameters.Add(component => component.Open, true));
        await cut.Instance.CloseFromBrowserAsync(restoreFocus: true);
        Assert.Equal([true, false], requested);
        Assert.Single(cut.FindAll("[role='menu']"));
    }

    [Fact]
    public void PointerAndNativeKeyboardClicksInvokeCommandAndCheckRequestExactlyOnce()
    {
        using var context = CreateContext();
        var activationCount = 0;
        var checkedRequests = new List<bool>();
        var closeRequests = 0;
        var cut = context.Render<BzsMenu>(parameters => parameters
            .Add(component => component.Open, true)
            .Add(component => component.OpenChanged, value => closeRequests += value ? 0 : 1)
            .Add(component => component.TriggerContent, "Open actions")
            .Add(component => component.ChildContent, builder =>
                AddItem(builder, 0, new ItemDefinition(
                    "Pin",
                    Checkable: true,
                    Checked: false,
                    Activated: () => activationCount++,
                    CheckedChanged: value => checkedRequests.Add(value)))));

        cut.Find("[role='menuitemcheckbox']").Click();
        Assert.Equal(1, activationCount);
        Assert.Equal([true], checkedRequests);
        Assert.Equal(1, closeRequests);

        var item = cut.Find("[role='menuitemcheckbox']");
        item.KeyDown("Enter");
        Assert.Equal(1, activationCount);
        item.Click();
        Assert.Equal(2, activationCount);
        Assert.Equal([true, true], checkedRequests);
        Assert.Equal(2, closeRequests);

        item = cut.Find("[role='menuitemcheckbox']");
        item.KeyDown(" ");
        Assert.Equal(2, activationCount);
        item.Click();
        Assert.Equal(3, activationCount);
        Assert.Equal([true, true, true], checkedRequests);
        Assert.Equal(3, closeRequests);
    }

    [Fact]
    public void ItemEscapeDoesNotDuplicateDocumentCaptureCloseHandling()
    {
        using var context = CreateContext();
        var closeRequests = 0;
        var cut = context.Render<BzsMenu>(parameters => parameters
            .Add(component => component.Open, true)
            .Add(component => component.OpenChanged, value => closeRequests += value ? 0 : 1)
            .Add(component => component.TriggerContent, "Open actions")
            .Add(component => component.ChildContent, BuildItems(new ItemDefinition("Archive"))));

        cut.Find("[role='menuitem']").KeyDown("Escape");

        Assert.Equal(0, closeRequests);
    }

    [Fact]
    public void TriggerArrowKeysChooseBoundaryAfterItemsRegister()
    {
        using var context = CreateContext();
        var requested = new List<bool>();
        var cut = context.Render<BzsMenu>(parameters => parameters
            .Add(component => component.Open, false)
            .Add(component => component.OpenChanged, value => requested.Add(value))
            .Add(component => component.TriggerContent, "Open actions")
            .Add(component => component.ChildContent, BuildItems(
                new ItemDefinition("First"),
                new ItemDefinition("Blocked", Disabled: true),
                new ItemDefinition("Last"))));

        cut.Find("button[data-bzs-anchor='true']").KeyDown("ArrowUp");
        cut.Render(parameters => parameters.Add(component => component.Open, true));
        Assert.Equal([true], requested);
        Assert.Equal(
            new[] { "-1", "-1", "0" },
            cut.FindAll("[role='menuitem']").Select(item => item.GetAttribute("tabindex")).ToArray());

        cut.Render(parameters => parameters.Add(component => component.Open, false));
        cut.Find("button[data-bzs-anchor='true']").KeyDown("ArrowDown");
        cut.Render(parameters => parameters.Add(component => component.Open, true));
        Assert.Equal([true, true], requested);
        Assert.Equal(
            new[] { "0", "-1", "-1" },
            cut.FindAll("[role='menuitem']").Select(item => item.GetAttribute("tabindex")).ToArray());
    }

    [Fact]
    public void ArrowAndTypeaheadNavigationMaintainOneRovingTabStop()
    {
        using var context = CreateContext();
        var cut = context.Render<BzsMenu>(parameters => parameters
            .Add(component => component.Open, true)
            .Add(component => component.TriggerContent, "Open actions")
            .Add(component => component.ChildContent, BuildItems(
                new ItemDefinition("Alpha"),
                new ItemDefinition("Blocked", Disabled: true),
                new ItemDefinition("Beta"),
                new ItemDefinition("Bravo"))));
        var items = cut.FindAll("[role='menuitem']");

        items[0].KeyDown("ArrowDown");
        Assert.Equal(
            new[] { "-1", "-1", "0", "-1" },
            cut.FindAll("[role='menuitem']").Select(item => item.GetAttribute("tabindex")).ToArray());

        cut.FindAll("[role='menuitem']")[2].KeyDown("b");
        Assert.Equal("0", cut.FindAll("[role='menuitem']")[3].GetAttribute("tabindex"));
    }

    [Fact]
    public void ContextInvocationRequestsOpenAndKeepsCoordinatesInsideInterop()
    {
        using var context = new BunitContext();
        context.Services.AddBzsBlazor();
        context.JSInterop.Mode = JSRuntimeMode.Loose;
        var module = context.JSInterop.SetupModule(BzsAnchoredOverlayInterop.ModulePath);
        module.SetupVoid(BzsAnchoredOverlayInterop.InitializeMethod, _ => true).SetVoidResult();
        module.SetupVoid(BzsAnchoredOverlayInterop.SetOpenAtMethod, _ => true).SetVoidResult();
        module.SetupVoid(BzsAnchoredOverlayInterop.DisposeMethod, _ => true).SetVoidResult();
        var openRequestCount = 0;
        var cut = context.Render<BzsContextMenu>(parameters => parameters
            .Add(component => component.Open, false)
            .Add(component => component.OpenChanged, value => openRequestCount += value ? 1 : 0)
            .Add(component => component.TargetContent, "Document")
            .Add(component => component.ChildContent, BuildItems(new ItemDefinition("Rename"))));

        var target = cut.Find("[data-bzs-anchor='true']");
        target.ContextMenu(new MouseEventArgs
        {
            ClientX = 120,
            ClientY = 80,
        });
        Assert.Equal(1, openRequestCount);
        Assert.Empty(cut.FindAll("[role='menu']"));

        cut.Render(parameters => parameters.Add(component => component.Open, true));
        Assert.Equal("0", cut.Find("[role='menuitem']").GetAttribute("tabindex"));
        var invocation = module.Invocations[BzsAnchoredOverlayInterop.SetOpenAtMethod].Last();
        Assert.Contains(120d, invocation.Arguments);
        Assert.Contains(80d, invocation.Arguments);
    }

    [Fact]
    public async Task IgnoredControlledCloseCanBeRequestedAgainWithoutStaleFocusRestoration()
    {
        using var context = new BunitContext();
        context.Services.AddBzsBlazor();
        var module = context.JSInterop.SetupModule(BzsAnchoredOverlayInterop.ModulePath);
        module.SetupVoid(BzsAnchoredOverlayInterop.InitializeMethod, _ => true).SetVoidResult();
        module.SetupVoid(BzsAnchoredOverlayInterop.SetOpenMethod, _ => true).SetVoidResult();
        module.SetupVoid(BzsAnchoredOverlayInterop.DisposeMethod, _ => true).SetVoidResult();
        var closeRequestCount = 0;
        var cut = context.Render<BzsMenu>(parameters => parameters
            .Add(component => component.Open, true)
            .Add(component => component.OpenChanged, value => closeRequestCount += value ? 0 : 1)
            .Add(component => component.TriggerContent, "Open actions")
            .Add(component => component.ChildContent, BuildItems(new ItemDefinition("Archive"))));

        await cut.Instance.CloseFromBrowserAsync(restoreFocus: true);
        await cut.Instance.CloseFromBrowserAsync(restoreFocus: true);
        Assert.Equal(2, closeRequestCount);

        cut.Render(parameters => parameters.Add(component => component.Open, false));
        var closeSynchronization = module.Invocations[BzsAnchoredOverlayInterop.SetOpenMethod].Last();
        Assert.Contains(false, closeSynchronization.Arguments);
        Assert.DoesNotContain(true, closeSynchronization.Arguments.Skip(5));
    }

    [Fact]
    public async Task AcceptedBrowserEscapeCloseSynchronizesFocusRestorationOnce()
    {
        using var context = new BunitContext();
        context.Services.AddBzsBlazor();
        var module = context.JSInterop.SetupModule(BzsAnchoredOverlayInterop.ModulePath);
        module.SetupVoid(BzsAnchoredOverlayInterop.InitializeMethod, _ => true).SetVoidResult();
        module.SetupVoid(BzsAnchoredOverlayInterop.SetOpenMethod, _ => true).SetVoidResult();
        module.SetupVoid(BzsAnchoredOverlayInterop.DisposeMethod, _ => true).SetVoidResult();
        var closeRequestCount = 0;
        IRenderedComponent<BzsMenu>? cut = null;
        cut = context.Render<BzsMenu>(parameters => parameters
            .Add(component => component.Open, true)
            .Add(component => component.OpenChanged, value =>
            {
                closeRequestCount += value ? 0 : 1;
                cut!.Render(updated => updated.Add(component => component.Open, value));
            })
            .Add(component => component.TriggerContent, "Open actions")
            .Add(component => component.ChildContent, BuildItems(new ItemDefinition("Archive"))));

        await cut.Instance.CloseFromBrowserAsync(restoreFocus: true);

        Assert.Equal(1, closeRequestCount);
        Assert.Empty(cut.FindAll("[role='menu']"));
        var closeSynchronizations = module.Invocations[BzsAnchoredOverlayInterop.SetOpenMethod]
            .Where(invocation => invocation.Arguments.ElementAt(1) is false)
            .ToArray();
        var closeSynchronization = Assert.Single(closeSynchronizations);
        Assert.Contains(true, closeSynchronization.Arguments.Skip(5));
    }

    [Fact]
    public void MenuRetriesTransientInitializationAndRecoversOnALaterRender()
    {
        using var context = new BunitContext();
        context.Services.AddBzsBlazor();
        var module = context.JSInterop.SetupModule(BzsAnchoredOverlayInterop.ModulePath);
        var initialize = module.SetupVoid(BzsAnchoredOverlayInterop.InitializeMethod, _ => true)
            .SetException(new TaskCanceledException("Menu initialization was interrupted."));
        var setOpen = module.SetupVoid(BzsAnchoredOverlayInterop.SetOpenMethod, _ => true).SetVoidResult();
        module.SetupVoid(BzsAnchoredOverlayInterop.DisposeMethod, _ => true).SetVoidResult();

        var cut = context.Render<BzsMenu>(parameters => parameters
            .Add(component => component.Open, true)
            .Add(component => component.TriggerContent, "Open actions")
            .Add(component => component.ChildContent, BuildItems(new ItemDefinition("Archive"))));

        initialize.VerifyInvoke(BzsAnchoredOverlayInterop.InitializeMethod, 2);
        initialize.SetVoidResult();
        cut.Render();

        initialize.VerifyInvoke(BzsAnchoredOverlayInterop.InitializeMethod, 3);
        setOpen.VerifyInvoke(BzsAnchoredOverlayInterop.SetOpenMethod, 1);
    }

    [Fact]
    public void ContextMenuRetriesTransientSynchronizationAndRecoversOnALaterRender()
    {
        using var context = new BunitContext();
        context.Services.AddBzsBlazor();
        var module = context.JSInterop.SetupModule(BzsAnchoredOverlayInterop.ModulePath);
        module.SetupVoid(BzsAnchoredOverlayInterop.InitializeMethod, _ => true).SetVoidResult();
        var setOpen = module.SetupVoid(BzsAnchoredOverlayInterop.SetOpenAtMethod, _ => true)
            .SetException(new TaskCanceledException("Context menu synchronization was interrupted."));
        module.SetupVoid(BzsAnchoredOverlayInterop.DisposeMethod, _ => true).SetVoidResult();

        var cut = context.Render<BzsContextMenu>(parameters => parameters
            .Add(component => component.Open, true)
            .Add(component => component.TargetContent, "Document")
            .Add(component => component.ChildContent, BuildItems(new ItemDefinition("Rename"))));

        setOpen.VerifyInvoke(BzsAnchoredOverlayInterop.SetOpenAtMethod, 2);
        setOpen.SetVoidResult();
        cut.Render();
        setOpen.VerifyInvoke(BzsAnchoredOverlayInterop.SetOpenAtMethod, 3);
        cut.Render();
        setOpen.VerifyInvoke(BzsAnchoredOverlayInterop.SetOpenAtMethod, 3);
    }

    [Fact]
    public async Task OpenMenuHasMeaningfulStaticSsrMarkup()
    {
        using var context = CreateContext();
        await using var renderer = new HtmlRenderer(
            context.Services,
            context.Services.GetRequiredService<ILoggerFactory>());
        var parameters = ParameterView.FromDictionary(new Dictionary<string, object?>
        {
            [nameof(BzsMenu.Open)] = true,
            [nameof(BzsMenu.TriggerContent)] = (RenderFragment)(builder => builder.AddContent(0, "Actions")),
            [nameof(BzsMenu.ChildContent)] = BuildItems(new ItemDefinition("Archive")),
        });

        var html = await renderer.Dispatcher.InvokeAsync(async () =>
        {
            var output = await renderer.RenderComponentAsync<BzsMenu>(parameters);
            return output.ToHtmlString();
        });
        var document = await new HtmlParser().ParseDocumentAsync(html);

        Assert.Equal("menu", document.QuerySelector("ul")?.GetAttribute("role"));
        Assert.Equal("menuitem", document.QuerySelector("ul button")?.GetAttribute("role"));
        Assert.Equal("true", document.QuerySelector("button[aria-haspopup='menu']")?.GetAttribute("aria-expanded"));
    }

    private static BunitContext CreateContext()
    {
        var context = new BunitContext();
        context.Services.AddBzsBlazor();
        context.JSInterop.Mode = JSRuntimeMode.Loose;
        var module = context.JSInterop.SetupModule(BzsAnchoredOverlayInterop.ModulePath);
        module.SetupVoid(BzsAnchoredOverlayInterop.InitializeMethod, _ => true).SetVoidResult();
        module.SetupVoid(BzsAnchoredOverlayInterop.SetOpenMethod, _ => true).SetVoidResult();
        module.SetupVoid(BzsAnchoredOverlayInterop.SetOpenAtMethod, _ => true).SetVoidResult();
        module.SetupVoid(BzsAnchoredOverlayInterop.DisposeMethod, _ => true).SetVoidResult();
        return context;
    }

    private static RenderFragment BuildItems(params ItemDefinition[] items) => builder =>
    {
        for (var index = 0; index < items.Length; index++)
        {
            AddItem(builder, index * 10, items[index]);
        }
    };

    private static void AddItem(RenderTreeBuilder builder, int sequence, ItemDefinition item)
    {
        builder.OpenComponent<BzsMenuItem>(sequence);
        builder.AddAttribute(sequence + 1, nameof(BzsMenuItem.Text), item.Text);
        builder.AddAttribute(sequence + 2, nameof(BzsMenuItem.Disabled), item.Disabled);
        builder.AddAttribute(sequence + 3, nameof(BzsMenuItem.Separator), item.Separator);
        builder.AddAttribute(sequence + 4, nameof(BzsMenuItem.Checkable), item.Checkable);
        builder.AddAttribute(sequence + 5, nameof(BzsMenuItem.Checked), item.Checked);
        if (item.Activated is not null)
        {
            builder.AddAttribute(
                sequence + 6,
                nameof(BzsMenuItem.Activated),
                EventCallback.Factory.Create(new object(), item.Activated));
        }
        if (item.CheckedChanged is not null)
        {
            builder.AddAttribute(
                sequence + 7,
                nameof(BzsMenuItem.CheckedChanged),
                EventCallback.Factory.Create<bool>(new object(), item.CheckedChanged));
        }
        builder.CloseComponent();
    }

    private sealed record ItemDefinition(
        string? Text,
        bool Disabled = false,
        bool Separator = false,
        bool Checkable = false,
        bool Checked = false,
        Action? Activated = null,
        Action<bool>? CheckedChanged = null);
}
