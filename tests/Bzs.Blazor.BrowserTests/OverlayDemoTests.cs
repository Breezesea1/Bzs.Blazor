using Microsoft.Playwright;
using Microsoft.Playwright.Xunit;

namespace Bzs.Blazor.BrowserTests;

[Collection(DemoCollection.Name)]
public sealed class OverlayDemoTests(DemoServerFixture server) : BrowserGatePageTest
{
    [Fact]
    public async Task ControlledDialogSupportsKeyboardOnlyActivationFocusTrapEscapeAndRestoration()
    {
        BeginBrowserGateTest();
        await GoToOverlaysAsync();

        var trigger = Page.GetByTestId("open-controlled-dialog");
        await Page.Keyboard.PressAsync("Tab");
        await Expect(trigger).ToBeFocusedAsync();
        await Page.Keyboard.PressAsync("Space");

        var dialog = Page.GetByRole(AriaRole.Dialog, new() { Name = "Controlled dialog" });
        var primary = dialog.GetByTestId("controlled-dialog-primary");
        var cancel = dialog.GetByTestId("controlled-dialog-cancel");
        var nested = dialog.GetByTestId("open-nested-service-dialog");

        await Expect(dialog).ToBeVisibleAsync();
        await Expect(primary).ToBeFocusedAsync();

        await Page.Keyboard.PressAsync("Tab");
        await Expect(cancel).ToBeFocusedAsync();
        await Page.Keyboard.PressAsync("Tab");
        await Expect(nested).ToBeFocusedAsync();
        await Page.Keyboard.PressAsync("Tab");
        await Expect(primary).ToBeFocusedAsync();
        await Page.Keyboard.PressAsync("Shift+Tab");
        await Expect(nested).ToBeFocusedAsync();

        await Page.Keyboard.PressAsync("Escape");
        await Expect(dialog).ToHaveCountAsync(0);
        await Expect(trigger).ToBeFocusedAsync();

        await Page.Keyboard.PressAsync("Enter");
        await Expect(dialog).ToBeVisibleAsync();
        await Page.Keyboard.PressAsync("Escape");
        await Expect(dialog).ToHaveCountAsync(0);
        await Expect(trigger).ToBeFocusedAsync();
    }

    [Fact]
    public async Task ControlledDialogFocusTrapSkipsDynamicallyInsertedNonTabbableControls()
    {
        BeginBrowserGateTest();
        await GoToOverlaysAsync();

        await Page.GetByTestId("open-controlled-dialog").ClickAsync();
        var dialog = Page.GetByRole(AriaRole.Dialog, new() { Name = "Controlled dialog" });
        var primary = dialog.GetByTestId("controlled-dialog-primary");
        var nested = dialog.GetByTestId("open-nested-service-dialog");
        await Expect(dialog).ToBeVisibleAsync();

        await dialog.EvaluateAsync(
            """
            panel => {
                const programmaticOnly = document.createElement('button');
                programmaticOnly.type = 'button';
                programmaticOnly.setAttribute('tabindex', '-1');
                programmaticOnly.dataset.testid = 'dynamic-programmatic-only-control';

                const disabledFieldset = document.createElement('fieldset');
                disabledFieldset.disabled = true;
                const disabledFieldsetControl = document.createElement('button');
                disabledFieldsetControl.type = 'button';
                disabledFieldsetControl.dataset.testid = 'dynamic-fieldset-disabled-control';
                disabledFieldset.append(disabledFieldsetControl);

                panel.querySelector('.bzs-dialog__content').append(programmaticOnly, disabledFieldset);
            }
            """);

        await Expect(dialog.GetByTestId("dynamic-programmatic-only-control"))
            .ToHaveAttributeAsync("tabindex", "-1");
        await Expect(dialog.GetByTestId("dynamic-fieldset-disabled-control")).ToBeDisabledAsync();

        await nested.FocusAsync();
        await Page.Keyboard.PressAsync("Tab");
        await Expect(primary).ToBeFocusedAsync();
        await Page.Keyboard.PressAsync("Shift+Tab");
        await Expect(nested).ToBeFocusedAsync();
    }

    [Fact]
    public async Task ControlledDialogHonorsTheConfiguredBackdropPolicy()
    {
        BeginBrowserGateTest();
        await GoToOverlaysAsync();

        var trigger = Page.GetByTestId("open-controlled-dialog");
        await trigger.ClickAsync();

        var dialog = Page.GetByRole(AriaRole.Dialog, new() { Name = "Controlled dialog" });
        await Expect(dialog).ToBeVisibleAsync();
        await GetDialogBackdrop(dialog).ClickAsync(new() { Position = new Position { X = 4, Y = 4 } });
        await Expect(dialog).ToBeVisibleAsync();

        await dialog.PressAsync("Escape");
        await Expect(dialog).ToHaveCountAsync(0);

        await Page.GetByLabel("Allow dialog backdrop dismissal").CheckAsync();
        await trigger.ClickAsync();
        await Expect(dialog).ToBeVisibleAsync();
        await GetDialogBackdrop(dialog).ClickAsync(new() { Position = new Position { X = 4, Y = 4 } });
        await Expect(dialog).ToHaveCountAsync(0);
    }

    [Fact]
    public async Task DrawersExposePlacementModalSemanticsAndScrollLocking()
    {
        BeginBrowserGateTest();
        await GoToOverlaysAsync();

        await Page.GetByTestId("open-modal-drawer").ClickAsync();
        var modalDrawer = Page.GetByRole(AriaRole.Dialog, new() { Name = "Modal drawer" });
        await Expect(modalDrawer).ToBeVisibleAsync();
        await Expect(modalDrawer).ToHaveAttributeAsync("data-bzs-drawer", "end");
        await Expect(modalDrawer).ToHaveAttributeAsync("aria-modal", "true");
        await Page.WaitForFunctionAsync("() => document.body.style.overflow === 'hidden'");

        await modalDrawer.PressAsync("Escape");
        await Expect(modalDrawer).ToHaveCountAsync(0);
        await Page.WaitForFunctionAsync("() => document.body.style.overflow === ''");

        await Page.GetByTestId("open-nonmodal-drawer").ClickAsync();
        var nonmodalDrawer = Page.GetByRole(AriaRole.Dialog, new() { Name = "Nonmodal drawer" });
        await Expect(nonmodalDrawer).ToBeVisibleAsync();
        await Expect(nonmodalDrawer).ToHaveAttributeAsync("data-bzs-drawer", "start");
        await Expect(nonmodalDrawer).Not.ToHaveAttributeAsync("aria-modal", "true");
        await Page.WaitForFunctionAsync("() => document.body.style.overflow === ''");
        await Expect(nonmodalDrawer.GetByTestId("close-nonmodal-drawer")).ToBeFocusedAsync();
        await Page.Keyboard.PressAsync("Shift+Tab");
        await Expect(Page.GetByTestId("show-host-toast")).ToBeFocusedAsync();
        await Expect(nonmodalDrawer).ToBeVisibleAsync();

        await Page.Keyboard.PressAsync("Escape");
        await Expect(nonmodalDrawer).ToHaveCountAsync(0);
    }

    [Fact]
    public async Task EscapeFromALowerDialogOnlyClosesTheTopNonmodalDrawer()
    {
        BeginBrowserGateTest();
        await GoToOverlaysAsync();

        await Page.GetByTestId("open-controlled-dialog").ClickAsync();
        var controlledDialog = Page.GetByRole(AriaRole.Dialog, new() { Name = "Controlled dialog" });
        await Expect(controlledDialog).ToBeVisibleAsync();

        await Page.GetByTestId("open-nonmodal-drawer").DispatchEventAsync("click");
        var nonmodalDrawer = Page.GetByRole(AriaRole.Dialog, new() { Name = "Nonmodal drawer" });
        await Expect(nonmodalDrawer).ToBeVisibleAsync();
        await Expect(nonmodalDrawer.GetByTestId("close-nonmodal-drawer")).ToBeFocusedAsync();

        var lowerDialogFocus = controlledDialog.GetByTestId("controlled-dialog-primary");
        await lowerDialogFocus.FocusAsync();
        await Expect(lowerDialogFocus).ToBeFocusedAsync();

        await Page.Keyboard.PressAsync("Escape");
        await Expect(nonmodalDrawer).ToHaveCountAsync(0);
        await Expect(controlledDialog).ToBeVisibleAsync();
    }

    [Fact]
    public async Task ServiceDialogReturnsTypedResultAndTheHostRendersToasts()
    {
        BeginBrowserGateTest();
        await GoToOverlaysAsync();

        await Page.GetByTestId("open-service-dialog").ClickAsync();
        var dialog = Page.GetByRole(AriaRole.Dialog, new() { Name = "Service dialog" });
        await Expect(dialog).ToBeVisibleAsync();
        await Expect(dialog.GetByTestId("service-dialog-content"))
            .ToContainTextAsync("Approve the staged overlay workflow?");
        await dialog.GetByTestId("service-dialog-complete").ClickAsync();
        await Expect(dialog).ToHaveCountAsync(0);
        await Expect(Page.GetByTestId("service-dialog-result")).ToHaveTextAsync("Completed: true");

        await Page.GetByTestId("show-host-toast").ClickAsync();
        var toast = Page.GetByRole(AriaRole.Status, new() { Name = "Overlay host toast" });
        await Expect(toast).ToBeVisibleAsync();
        await Expect(Page.GetByTestId("overlays-host")).ToContainTextAsync("Host toast");
    }

    [Fact]
    public async Task NestedServiceDialogClosesBeforeTheControlledDialog()
    {
        BeginBrowserGateTest();
        await GoToOverlaysAsync();

        await Page.GetByTestId("open-controlled-dialog").ClickAsync();
        var controlledDialog = Page.GetByRole(AriaRole.Dialog, new() { Name = "Controlled dialog" });
        await Expect(controlledDialog).ToBeVisibleAsync();

        await controlledDialog.GetByTestId("open-nested-service-dialog").ClickAsync();
        var serviceDialog = Page.GetByRole(AriaRole.Dialog, new() { Name = "Service dialog" });
        await Expect(serviceDialog).ToBeVisibleAsync();
        await Expect(Page.GetByRole(AriaRole.Dialog)).ToHaveCountAsync(2);

        await serviceDialog.PressAsync("Escape");
        await Expect(serviceDialog).ToHaveCountAsync(0);
        await Expect(controlledDialog).ToBeVisibleAsync();
        await Expect(controlledDialog.GetByTestId("open-nested-service-dialog")).ToBeFocusedAsync();

        await controlledDialog.PressAsync("Escape");
        await Expect(controlledDialog).ToHaveCountAsync(0);
    }

    private async Task GoToOverlaysAsync()
    {
        await Page.GotoAsync($"{server.BaseUrl}/overlays");
        await Expect(Page.GetByTestId("overlays-runtime-status"))
            .ToHaveTextAsync("Interactive runtime ready");
    }

    private static ILocator GetDialogBackdrop(ILocator dialog) =>
        dialog.Locator("xpath=..").Locator(".bzs-dialog__backdrop");
}
