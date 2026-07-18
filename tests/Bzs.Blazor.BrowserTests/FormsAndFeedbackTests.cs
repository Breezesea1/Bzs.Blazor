using Microsoft.Playwright;
using Microsoft.Playwright.Xunit;

namespace Bzs.Blazor.BrowserTests;

[Collection(DemoCollection.Name)]
public sealed class FormsAndFeedbackTests(DemoServerFixture server) : BrowserGatePageTest
{
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
