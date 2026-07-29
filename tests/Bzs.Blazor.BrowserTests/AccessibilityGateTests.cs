using Deque.AxeCore.Playwright;
using Microsoft.Playwright;
using Microsoft.Playwright.Xunit;

namespace Bzs.Blazor.BrowserTests;

[Collection(DemoCollection.Name)]
public sealed class AccessibilityGateTests(DemoServerFixture server) : BrowserGatePageTest
{
    [Fact]
    public async Task AutoCatalogCompleteStateHasNoCriticalOrSeriousAxeViolations()
    {
        BeginBrowserGateTest();
        await Page.GotoAsync($"{server.BaseUrl}/render-modes/auto");
        await Expect(Page.GetByTestId("render-mode-auto-runtime-readiness"))
            .ToHaveTextAsync("Interactive runtime ready");

        await Page.GetByTestId("render-mode-auto-counter").ClickAsync();
        await Page.GetByTestId("render-mode-auto-work-item").FillAsync("Lighting review");
        await Page.GetByTestId("render-mode-auto-save").ClickAsync();
        await Expect(Page.GetByTestId("render-mode-auto-form-status"))
            .ToHaveTextAsync("Saved Lighting review.");

        await Page.GetByTestId("render-mode-auto-tabs")
            .GetByRole(AriaRole.Tab, new() { Name = "Details" })
            .ClickAsync();
        await Page.GetByTestId("render-mode-auto-open-controlled-dialog").ClickAsync();
        await Page.GetByRole(AriaRole.Dialog, new() { Name = "Controlled catalog dialog" })
            .GetByTestId("render-mode-auto-complete-controlled-dialog")
            .ClickAsync();
        await Page.GetByTestId("render-mode-auto-open-drawer").ClickAsync();
        await Page.GetByRole(AriaRole.Dialog, new() { Name = "Catalog drawer" })
            .GetByTestId("render-mode-auto-close-drawer")
            .ClickAsync();
        await Page.GetByTestId("render-mode-auto-open-service-dialog").ClickAsync();
        var serviceDialog = Page.GetByRole(AriaRole.Dialog, new() { Name = "Catalog service dialog" });
        await serviceDialog.GetByTestId("service-dialog-complete").ClickAsync();
        await Expect(serviceDialog).ToHaveCountAsync(0);
        await Expect(Page.GetByTestId("render-mode-auto-service-dialog-status"))
            .ToHaveTextAsync("Completed: true");
        await Page.GetByTestId("render-mode-auto-show-toast").ClickAsync();
        await Expect(Page.GetByTestId("render-mode-auto-overlay-host"))
            .ToContainTextAsync("Catalog toast");

        await AssertNoCriticalOrSeriousAxeViolationsAsync("Interactive Auto catalog complete state");
    }

    [Fact]
    public async Task InvalidFormStateHasNoCriticalOrSeriousAxeViolations()
    {
        BeginBrowserGateTest();
        await Page.GotoAsync($"{server.BaseUrl}/forms");
        await Expect(Page.GetByText("Interactive runtime ready")).ToBeVisibleAsync();

        var workEmail = Page.GetByLabel("Work email");
        await workEmail.FillAsync("not-an-email");
        await Page.GetByRole(AriaRole.Button, new() { Name = "Validate profile" }).ClickAsync();
        await Expect(workEmail).ToHaveAttributeAsync("aria-invalid", "true");
        await Expect(Page.GetByText("Enter a valid work email.")).ToBeVisibleAsync();

        await AssertNoCriticalOrSeriousAxeViolationsAsync("invalid form state");
    }

    [Fact]
    public async Task OpenDatePickerStateHasNoCriticalOrSeriousAxeViolations()
    {
        BeginBrowserGateTest();
        await Page.GotoAsync($"{server.BaseUrl}/forms");
        await Expect(Page.GetByText("Interactive runtime ready")).ToBeVisibleAsync();

        var input = Page.GetByRole(AriaRole.Combobox, new() { Name = "Delivery date" });
        await input.FocusAsync();
        await input.PressAsync("ArrowDown");
        await Expect(Page.GetByRole(AriaRole.Dialog, new() { Name = "Choose a date" })).ToBeVisibleAsync();

        await AssertNoCriticalOrSeriousAxeViolationsAsync("open date picker state");
    }

    [Fact]
    public async Task OpenDialogStateHasNoCriticalOrSeriousAxeViolations()
    {
        BeginBrowserGateTest();
        await Page.GotoAsync($"{server.BaseUrl}/overlays");
        await Expect(Page.GetByTestId("overlays-runtime-status"))
            .ToHaveTextAsync("Interactive runtime ready");

        await Page.GetByTestId("open-controlled-dialog").ClickAsync();
        await Expect(Page.GetByRole(AriaRole.Dialog, new() { Name = "Controlled dialog" }))
            .ToBeVisibleAsync();

        await AssertNoCriticalOrSeriousAxeViolationsAsync("open controlled dialog state");
    }

    [Fact]
    public async Task TabsStateHasNoCriticalOrSeriousAxeViolations()
    {
        BeginBrowserGateTest();
        await Page.GotoAsync($"{server.BaseUrl}/tabs");
        await Expect(Page.GetByTestId("tabs-runtime-status"))
            .ToHaveTextAsync("Interactive runtime ready");

        await Page.GetByTestId("automatic-tabs")
            .GetByRole(AriaRole.Tab, new() { Name = "Activity" })
            .ClickAsync();
        await Expect(Page.GetByTestId("automatic-selection")).ToHaveTextAsync("Selected: activity");

        await AssertNoCriticalOrSeriousAxeViolationsAsync("tabs state");
    }

    [Fact]
    public async Task ForcedColorsPreservesFocusBordersAndPageWidth()
    {
        BeginBrowserGateTest();
        var page = await NewObservedPageAsync(new BrowserNewContextOptions
        {
            ForcedColors = ForcedColors.Active,
            ViewportSize = new ViewportSize { Width = 1280, Height = 900 },
        });

        await page.GotoAsync($"{server.BaseUrl}/forms");
        await page.GetByText("Interactive runtime ready").WaitForAsync();
        Assert.True(await page.EvaluateAsync<bool>("() => matchMedia('(forced-colors: active)').matches"));

        var workEmail = page.GetByLabel("Work email");
        await workEmail.FocusAsync();
        await Expect(workEmail).ToBeFocusedAsync();
        Assert.True(await HasVisibleFocusAsync(workEmail));
        Assert.True(await HasBorderAsync(workEmail));
        await AssertNoHorizontalPageOverflowAsync(page, "forced-colors forms");

        await page.GotoAsync($"{server.BaseUrl}/tabs");
        await page.GetByTestId("tabs-runtime-status").WaitForAsync();
        var overview = page.GetByTestId("automatic-tabs")
            .GetByRole(AriaRole.Tab, new() { Name = "Overview" });
        await overview.FocusAsync();
        await overview.PressAsync("ArrowRight");
        var activity = page.GetByTestId("automatic-tabs")
            .GetByRole(AriaRole.Tab, new() { Name = "Activity" });
        await Expect(activity).ToBeFocusedAsync();
        Assert.True(await HasVisibleFocusAsync(activity));
        Assert.True(await HasBorderAsync(activity));
        await AssertNoHorizontalPageOverflowAsync(page, "forced-colors tabs");
    }

    [Fact]
    public async Task TwoHundredPercentReflowEquivalentAvoidsOverflowAndInteractiveControlOverlap()
    {
        BeginBrowserGateTest();
        await SetTwoHundredPercentReflowEquivalentViewportAsync(Page);

        await Page.GotoAsync($"{server.BaseUrl}/forms");
        await Expect(Page.GetByText("Interactive runtime ready")).ToBeVisibleAsync();
        await AssertFitsAtTwoHundredPercentReflowEquivalentAsync(Page, "forms");

        await Page.GotoAsync($"{server.BaseUrl}/tabs");
        await Expect(Page.GetByTestId("tabs-runtime-status"))
            .ToHaveTextAsync("Interactive runtime ready");
        await AssertFitsAtTwoHundredPercentReflowEquivalentAsync(Page, "tabs");

        await Page.GotoAsync($"{server.BaseUrl}/overlays");
        await Expect(Page.GetByTestId("overlays-runtime-status"))
            .ToHaveTextAsync("Interactive runtime ready");
        await AssertFitsAtTwoHundredPercentReflowEquivalentAsync(Page, "overlays");
    }

    [Theory]
    [InlineData("Pixel 5")]
    [InlineData("iPhone 13")]
    public async Task MobileCatalogFormsAndTabsNavigateAndRemainInteractive(string deviceName)
    {
        BeginBrowserGateTest(deviceName);
        var page = await NewObservedPageAsync(Playwright.Devices[deviceName]);

        await page.GotoAsync($"{server.BaseUrl}/render-modes/auto");
        await page.GetByTestId("render-mode-auto-runtime-readiness").WaitForAsync();
        await page.GetByTestId("render-mode-auto-counter").ClickAsync();
        await Expect(page.GetByTestId("render-mode-auto-counter"))
            .ToHaveTextAsync("Interaction count: 1");
        await AssertNoHorizontalPageOverflowAsync(page, $"{deviceName} Interactive Auto catalog");

        await page.GotoAsync($"{server.BaseUrl}/forms");
        await page.GetByText("Interactive runtime ready").WaitForAsync();
        var workEmail = page.GetByLabel("Work email");
        await workEmail.FillAsync("not-an-email");
        await page.GetByRole(AriaRole.Button, new() { Name = "Validate profile" }).ClickAsync();
        await Expect(workEmail).ToHaveAttributeAsync("aria-invalid", "true");
        await AssertNoHorizontalPageOverflowAsync(page, $"{deviceName} forms");

        await page.GotoAsync($"{server.BaseUrl}/tabs");
        await page.GetByTestId("tabs-runtime-status").WaitForAsync();
        var overview = page.GetByTestId("automatic-tabs")
            .GetByRole(AriaRole.Tab, new() { Name = "Overview" });
        await Expect(overview).ToHaveAttributeAsync("aria-selected", "true");
        await page.GetByTestId("automatic-tabs")
            .GetByRole(AriaRole.Tab, new() { Name = "Activity" })
            .ClickAsync();
        await Expect(page.GetByTestId("automatic-selection")).ToHaveTextAsync("Selected: activity");
        await AssertNoHorizontalPageOverflowAsync(page, $"{deviceName} tabs");
    }

    private async Task AssertNoCriticalOrSeriousAxeViolationsAsync(string state)
    {
        var axeResult = await Page.RunAxe();
        var violations = axeResult.Violations
            .Where(violation => string.Equals(violation.Impact, "critical", StringComparison.OrdinalIgnoreCase)
                || string.Equals(violation.Impact, "serious", StringComparison.OrdinalIgnoreCase))
            .ToArray();

        Assert.True(
            violations.Length == 0,
            $"axe found critical or serious violations for {state}:{Environment.NewLine}{FormatAxeViolations(violations)}");
    }

    private static async Task SetTwoHundredPercentReflowEquivalentViewportAsync(IPage page)
    {
        // Playwright cannot control browser-level zoom. A 640 CSS-pixel viewport has the
        // same effective layout width as a 1280-pixel viewport at 200% zoom, exercising
        // the reflow behavior without relying on non-user CSS zoom.
        const int desktopViewportWidth = 1280;
        const int reflowEquivalentViewportWidth = desktopViewportWidth / 2;
        const int viewportHeight = 900;

        await page.SetViewportSizeAsync(reflowEquivalentViewportWidth, viewportHeight);
        var viewportWidth = await page.EvaluateAsync<int>("() => window.innerWidth");

        Assert.Equal(reflowEquivalentViewportWidth, viewportWidth);
    }

    private static async Task AssertFitsAtTwoHundredPercentReflowEquivalentAsync(IPage page, string state)
    {
        await AssertNoHorizontalPageOverflowAsync(page, $"{state} at 200% reflow-equivalent viewport");
        var overlaps = await GetUnexpectedInteractiveOverlapsAsync(page);
        Assert.True(
            overlaps.Length == 0,
            $"Unexpected interactive control overlap in {state} at 200% reflow-equivalent viewport:{Environment.NewLine}{string.Join(Environment.NewLine, overlaps)}");
    }

    private static async Task AssertNoHorizontalPageOverflowAsync(IPage page, string state)
    {
        var hasHorizontalOverflow = await page.EvaluateAsync<bool>(
            "() => document.documentElement.scrollWidth > document.documentElement.clientWidth");

        Assert.False(hasHorizontalOverflow, $"{state} has horizontal page overflow.");
    }

    private static Task<bool> HasVisibleFocusAsync(ILocator locator) =>
        locator.EvaluateAsync<bool>(
            "element => { const style = getComputedStyle(element); return style.outlineStyle !== 'none' && parseFloat(style.outlineWidth) > 0; }");

    private static Task<bool> HasBorderAsync(ILocator locator) =>
        locator.EvaluateAsync<bool>(
            "element => { const style = getComputedStyle(element); return parseFloat(style.borderTopWidth) > 0 || parseFloat(style.borderInlineEndWidth) > 0; }");

    private static Task<string[]> GetUnexpectedInteractiveOverlapsAsync(IPage page) =>
        page.EvaluateAsync<string[]>(
            """
            () => {
                const controls = [...document.querySelectorAll('button, input:not([type="hidden"]), select, textarea, [role="tab"]')]
                    .filter(element => {
                        const style = getComputedStyle(element);
                        const rect = element.getBoundingClientRect();
                        return style.display !== 'none'
                            && style.visibility !== 'hidden'
                            && rect.width > 0
                            && rect.height > 0;
                    })
                    .map(element => ({
                        element,
                        rect: element.getBoundingClientRect(),
                        label: element.getAttribute('aria-label')
                            || element.textContent.trim().replace(/\s+/g, ' ').slice(0, 80)
                            || element.id
                            || element.tagName.toLowerCase(),
                    }));

                const overlaps = [];
                const isReservedInlineOverlay = (owner, overlay) => {
                    const ownerId = owner.element.getAttribute('data-bzs-inline-overlay-owner');
                    if (!ownerId || overlay.element.getAttribute('data-bzs-inline-overlay-for') !== ownerId) {
                        return false;
                    }

                    if (owner.element.parentElement !== overlay.element.parentElement) {
                        return false;
                    }

                    const ownerStyle = getComputedStyle(owner.element);
                    const overlayStyle = getComputedStyle(overlay.element);
                    if (overlayStyle.position !== 'absolute') {
                        return false;
                    }

                    const reservedInlineEnd = parseFloat(ownerStyle.paddingInlineEnd) || 0;
                    if (reservedInlineEnd + 1 < overlay.rect.width) {
                        return false;
                    }

                    const fitsBlock = overlay.rect.top >= owner.rect.top - 1
                        && overlay.rect.bottom <= owner.rect.bottom + 1;
                    const fitsInlineEnd = ownerStyle.direction === 'rtl'
                        ? overlay.rect.left >= owner.rect.left - 1
                            && overlay.rect.right <= owner.rect.left + reservedInlineEnd + 1
                        : overlay.rect.left >= owner.rect.right - reservedInlineEnd - 1
                            && overlay.rect.right <= owner.rect.right + 1;
                    return fitsBlock && fitsInlineEnd;
                };

                for (let first = 0; first < controls.length; first += 1) {
                    for (let second = first + 1; second < controls.length; second += 1) {
                        const a = controls[first];
                        const b = controls[second];
                        if (a.element.contains(b.element) || b.element.contains(a.element)) {
                            continue;
                        }

                        if (isReservedInlineOverlay(a, b) || isReservedInlineOverlay(b, a)) {
                            continue;
                        }

                        const overlapWidth = Math.min(a.rect.right, b.rect.right) - Math.max(a.rect.left, b.rect.left);
                        const overlapHeight = Math.min(a.rect.bottom, b.rect.bottom) - Math.max(a.rect.top, b.rect.top);
                        if (overlapWidth > 1 && overlapHeight > 1) {
                            overlaps.push(`${a.label} overlaps ${b.label}`);
                        }
                    }
                }

                return overlaps.slice(0, 10);
            }
            """);

    private static string FormatAxeViolations(IEnumerable<Deque.AxeCore.Commons.AxeResultItem> violations) =>
        string.Join(
            Environment.NewLine,
            violations.Select(violation =>
                $"{violation.Impact} {violation.Id}: {violation.Help}{Environment.NewLine}"
                + string.Join(
                    Environment.NewLine,
                    violation.Nodes.Select(node => $"  node: {string.Join(", ", node.Target)}{Environment.NewLine}  html: {node.Html}"))));
}
