using System.Collections.Concurrent;
using Microsoft.Playwright;
using Microsoft.Playwright.Xunit;

namespace Bzs.Blazor.BrowserTests;

[Collection(DemoCollection.Name)]
public sealed class FormsAndFeedbackTests(DemoServerFixture server) : BrowserGatePageTest
{
    [Fact]
    public async Task SearchableSelectsCloseAfterOutsidePointerInteraction()
    {
        BeginBrowserGateTest();
        await Page.GotoAsync($"{server.BaseUrl}/forms");
        await Expect(Page.GetByText("Interactive runtime ready")).ToBeVisibleAsync();

        var releaseNotes = Page.GetByLabel("Release notes");
        foreach (var name in new[] { "Workspace", "Review areas" })
        {
            var select = Page.GetByRole(AriaRole.Combobox, new() { Name = name });
            await select.ClickAsync();
            await Expect(select).ToHaveAttributeAsync("aria-expanded", "true");

            await releaseNotes.ClickAsync();
            await Expect(select).ToHaveAttributeAsync("aria-expanded", "false");
        }
    }

    [Fact]
    public async Task SearchableSelectKeyboardDoesNotSubmitTheForm()
    {
        BeginBrowserGateTest();
        await Page.GotoAsync($"{server.BaseUrl}/forms");
        await Expect(Page.GetByText("Interactive runtime ready")).ToBeVisibleAsync();

        var workspace = Page.GetByRole(AriaRole.Combobox, new() { Name = "Workspace" });
        await workspace.FocusAsync();
        await workspace.PressAsync("Space");
        await Expect(workspace).ToHaveAttributeAsync("aria-expanded", "true");

        var search = Page.GetByRole(AriaRole.Searchbox, new() { Name = "Search options" });
        await search.FillAsync("final approvals");
        await search.PressAsync("Enter");

        await Expect(workspace).ToContainTextAsync("Review");
        await Expect(workspace).ToHaveAttributeAsync("aria-expanded", "false");
        await Expect(Page.GetByRole(AriaRole.Status).Last).ToHaveTextAsync("Ready to validate the profile.");
    }

    [Fact]
    public async Task SearchableSelectPanelAlignsInsideATransformedContainer()
    {
        BeginBrowserGateTest();
        var pageErrors = new ConcurrentQueue<string>();
        Page.PageError += (_, error) => pageErrors.Enqueue(error);
        await Page.GotoAsync($"{server.BaseUrl}/forms");
        await Expect(Page.GetByText("Interactive runtime ready")).ToBeVisibleAsync();

        foreach (var name in new[] { "Workspace", "Review areas" })
        {
            var select = Page.GetByRole(AriaRole.Combobox, new() { Name = name });
            var field = select.Locator("xpath=ancestor::*[contains(@class, 'bzs-field')][1]");
            await field.EvaluateAsync("element => { element.style.transform = 'scale(.8)'; element.style.transformOrigin = 'top left'; }");
            await select.ClickAsync();

            var panel = field.Locator("[data-bzs-select-panel='true']");
            await Expect(panel).ToBeVisibleAsync();
            var triggerBox = await select.BoundingBoxAsync();
            var panelBox = await panel.BoundingBoxAsync();
            Assert.NotNull(triggerBox);
            Assert.NotNull(panelBox);
            Assert.InRange(Math.Abs(triggerBox.X - panelBox.X), 0, 1);
            Assert.InRange(Math.Abs(triggerBox.Width - panelBox.Width), 0, 1);

            await Page.GetByLabel("Release notes").ClickAsync();
            await Expect(select).ToHaveAttributeAsync("aria-expanded", "false");
        }

        Assert.Empty(pageErrors);
    }

    [Fact]
    public async Task EnhancedChoiceControlsPreserveNativeRequiredAndLabelBehavior()
    {
        BeginBrowserGateTest();
        await Page.GotoAsync($"{server.BaseUrl}/forms");
        await Expect(Page.GetByText("Interactive runtime ready")).ToBeVisibleAsync();

        var constraints = Page.Locator("[data-bzs-select-constraint='true']");
        await Expect(constraints).ToHaveCountAsync(2);
        for (var index = 0; index < 2; index++)
        {
            var isValid = await constraints.Nth(index).EvaluateAsync<bool>(
                "element => { element.selectedIndex = -1; return element.checkValidity(); }");
            Assert.False(isValid);
        }

        var workspace = Page.GetByRole(AriaRole.Combobox, new() { Name = "Workspace" });
        await Page.GetByRole(AriaRole.Button, new() { Name = "Validate profile" }).ClickAsync();
        await Expect(workspace).ToBeFocusedAsync();
        await Expect(Page.GetByRole(AriaRole.Status).Last)
            .ToHaveTextAsync("Ready to validate the profile.");

        var selectedWorkflow = Page.Locator("#profile-workflow-option-2");
        var selectedWorkflowLabel =
            Page.Locator("label.bzs-radio-group__option[for='profile-workflow-option-2']");

        await selectedWorkflowLabel.ClickAsync();
        await Expect(selectedWorkflow).ToBeCheckedAsync();

        var workflowLabel = Page.Locator("#profile-workflow-label");
        await Expect(workflowLabel).ToHaveAttributeAsync("for", "profile-workflow-option-2");
        await workflowLabel.ClickAsync();
        await Expect(selectedWorkflow).ToBeFocusedAsync();
        await Expect(selectedWorkflow).ToBeCheckedAsync();
    }

    [Fact]
    public async Task FormsAcceptKeyboardInputValidateInlineAndFitAtTwoHundredPercentZoom()
    {
        BeginBrowserGateTest();
        await Page.SetViewportSizeAsync(640, 720);
        await Page.GotoAsync($"{server.BaseUrl}/forms");

        await Expect(Page.GetByText("Interactive runtime ready")).ToBeVisibleAsync();

        var fullName = Page.GetByLabel("Full name");
        await fullName.FocusAsync();
        await fullName.PressAsync("Control+A");
        await fullName.PressSequentiallyAsync("Alicia Santos");
        await fullName.PressAsync("Tab");

        var workEmail = Page.GetByLabel("Work email");
        await workEmail.FocusAsync();
        await workEmail.PressAsync("Control+A");
        await workEmail.PressSequentiallyAsync("not-an-email");
        await workEmail.PressAsync("Tab");

        await Page.GetByRole(AriaRole.Button, new() { Name = "Validate profile" }).PressAsync("Enter");
        await Expect(Page.GetByText("Enter a valid work email.")).ToBeVisibleAsync();
        await Expect(workEmail).ToHaveAttributeAsync("aria-invalid", "true");
        await Expect(Page.GetByLabel("Disabled assignment")).ToBeDisabledAsync();
        await Expect(Page.GetByLabel("Read-only identifier")).ToHaveAttributeAsync("readonly", "readonly");

        var hasHorizontalOverflow = await Page.EvaluateAsync<bool>(
            "() => document.documentElement.scrollWidth > document.documentElement.clientWidth");
        Assert.False(hasHorizontalOverflow);
    }

    [Fact]
    public async Task TimedToastPausesForHoverAndKeyboardFocus()
    {
        BeginBrowserGateTest();
        await Page.GotoAsync($"{server.BaseUrl}/feedback");
        await Expect(Page.GetByText("Interactive runtime ready")).ToBeVisibleAsync();

        var showTimedToast = Page.GetByRole(AriaRole.Button, new() { Name = "Show timed toast" });
        var toast = Page.GetByRole(AriaRole.Status, new() { Name = "Timed toast" });

        await showTimedToast.ClickAsync();
        await Expect(toast).ToBeVisibleAsync();
        await toast.HoverAsync();
        await Page.WaitForTimeoutAsync(1800);
        await Expect(toast).ToBeVisibleAsync();
        await Page.Mouse.MoveAsync(0, 0);
        await Expect(toast).ToHaveCountAsync(0);

        await showTimedToast.ClickAsync();
        await Expect(toast).ToBeVisibleAsync();
        await toast.GetByRole(AriaRole.Button, new() { Name = "Dismiss notification" }).FocusAsync();
        await Page.WaitForTimeoutAsync(1800);
        await Expect(toast).ToBeVisibleAsync();
        await showTimedToast.FocusAsync();
        await Expect(toast).ToHaveCountAsync(0);
    }

    [Fact]
    public async Task ToastCloseLiveRegionAndReducedMotionRemainAccessible()
    {
        BeginBrowserGateTest();
        await Page.EmulateMediaAsync(new() { ReducedMotion = ReducedMotion.Reduce });
        await Page.GotoAsync($"{server.BaseUrl}/feedback");
        await Expect(Page.GetByText("Interactive runtime ready")).ToBeVisibleAsync();

        var persistentToastButton = Page.GetByRole(AriaRole.Button, new() { Name = "Show persistent toast" });
        var transitionDurations = await persistentToastButton.EvaluateAsync<string[]>(
            "element => getComputedStyle(element).transitionDuration.split(',').map(value => value.trim())");
        Assert.NotEmpty(transitionDurations);
        Assert.All(transitionDurations, duration => Assert.True(IsZeroDuration(duration), $"Expected zero transition duration, got {duration}."));

        await Page.GetByRole(AriaRole.Button, new() { Name = "Show persistent toast" }).ClickAsync();
        var persistentToast = Page.GetByRole(AriaRole.Status, new() { Name = "Persistent toast" });
        await Expect(persistentToast).ToHaveAttributeAsync("aria-live", "polite");
        await persistentToast.GetByRole(AriaRole.Button, new() { Name = "Dismiss notification" }).ClickAsync();
        await Expect(persistentToast).ToHaveCountAsync(0);

        await Page.GetByRole(AriaRole.Button, new() { Name = "Show error toast" }).ClickAsync();
        var errorToast = Page.GetByRole(AriaRole.Alert, new() { Name = "Save failure toast" });
        await Expect(errorToast).ToHaveAttributeAsync("aria-live", "assertive");

        await Page.GotoAsync($"{server.BaseUrl}/foundation");
        await Expect(Page.GetByRole(AriaRole.Status)).ToContainTextAsync("Interactive runtime ready");
        var loadingIcon = Page.GetByRole(AriaRole.Button, new() { Name = "Saving" })
            .Locator(".bzs-button__loading-icon");
        await Expect(loadingIcon).ToBeVisibleAsync();
        Assert.Equal(
            "none",
            await loadingIcon.EvaluateAsync<string>("element => getComputedStyle(element).animationName"));
    }

    private static bool IsZeroDuration(string duration) =>
        string.Equals(duration, "0s", StringComparison.Ordinal)
        || string.Equals(duration, "0ms", StringComparison.Ordinal);
}
