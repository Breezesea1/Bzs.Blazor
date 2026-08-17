using System.Text.RegularExpressions;
using Microsoft.Playwright;
using Microsoft.Playwright.Xunit;

namespace Bzs.Blazor.BrowserTests;

[Collection(DemoCollection.Name)]
public sealed class ProductivityDemoTests(DemoServerFixture server) : BrowserGatePageTest
{
    [Fact]
    public async Task StaticSsrReturnsPassiveProductivityMarkupWithoutLoadingTheProvider()
    {
        BeginBrowserGateTest("static");
        using var client = new HttpClient();
        var response = await client.GetAsync($"{server.BaseUrl}/productivity/static?culture=en-US");
        var html = await response.Content.ReadAsStringAsync();

        response.EnsureSuccessStatusCode();
        Assert.Contains("data-testid=\"productivity-workbench\"", html, StringComparison.Ordinal);
        Assert.Contains("data-bzs-interactive=\"false\"", html, StringComparison.Ordinal);
        Assert.Contains("data-bzs-tooltip=\"true\"", html, StringComparison.Ordinal);
        Assert.Contains("data-bzs-popover=\"true\"", html, StringComparison.Ordinal);
        Assert.Contains("<nav", html, StringComparison.Ordinal);
        Assert.Contains("role=\"combobox\"", html, StringComparison.Ordinal);
        Assert.Contains("type=\"file\"", html, StringComparison.Ordinal);
        Assert.Contains("aria-busy=\"true\"", html, StringComparison.Ordinal);
        Assert.Contains("Loading data", html, StringComparison.Ordinal);
        Assert.Contains("Review queue", html, StringComparison.Ordinal);
        Assert.DoesNotContain("role=\"tooltip\"", html, StringComparison.Ordinal);
    }

    [Fact]
    public async Task CompactGridRowsUseTheSelectedCatalogLanguage()
    {
        BeginBrowserGateTest("static-zh-Hans");
        using var client = new HttpClient();
        var response = await client.GetAsync(
            $"{server.BaseUrl}/productivity/static?culture=zh-Hans");
        var html = System.Net.WebUtility.HtmlDecode(
            await response.Content.ReadAsStringAsync());

        response.EnsureSuccessStatusCode();
        Assert.Contains("发布说明", html, StringComparison.Ordinal);
        Assert.Contains("键盘审计", html, StringComparison.Ordinal);
        Assert.DoesNotContain("Release notes", html, StringComparison.Ordinal);
        Assert.DoesNotContain("Keyboard audit", html, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("server")]
    [InlineData("webassembly")]
    [InlineData("auto")]
    public async Task InteractiveRenderModesExerciseTheNewComponentWorkflow(string renderMode)
    {
        BeginBrowserGateTest(renderMode);
        await Page.SetViewportSizeAsync(1280, 900);
        var response = await Page.GotoAsync($"{server.BaseUrl}/productivity/{renderMode}?culture=en-US");
        Assert.True(response?.Ok ?? false);

        await Expect(Page.GetByTestId("productivity-workbench"))
            .ToHaveAttributeAsync("data-bzs-interactive", "true");
        await Expect(Page.GetByRole(AriaRole.Heading, new() { Name = "Operational workbench" }))
            .ToBeVisibleAsync();
        await Expect(Page.GetByRole(AriaRole.Navigation, new() { Name = "Productivity breadcrumb" }))
            .ToBeVisibleAsync();
        var workbenchNavigation = Page.GetByRole(
            AriaRole.Navigation,
            new() { Name = "Workbench navigation" });
        await Expect(workbenchNavigation).ToBeVisibleAsync();
        await Expect(Page.GetByRole(AriaRole.Img, new() { Name = "Alicia Santos" }))
            .ToBeVisibleAsync();
        await Expect(Page.GetByText("7", new() { Exact = true })).ToBeVisibleAsync();

        var reviewQueueDisclosure = workbenchNavigation.Locator("details");
        await Expect(reviewQueueDisclosure).ToHaveAttributeAsync("data-bzs-open", "true");
        await reviewQueueDisclosure.Locator("summary").ClickAsync();
        await Expect(reviewQueueDisclosure).ToHaveAttributeAsync("data-bzs-open", "false");

        var reviewFilter = Page.GetByRole(
            AriaRole.Button,
            new() { Name = "Needs review", Exact = true });
        var selectedReviewFilterBox = await reviewFilter.BoundingBoxAsync();
        Assert.NotNull(selectedReviewFilterBox);
        await reviewFilter.ClickAsync();
        await Expect(reviewFilter).ToHaveAttributeAsync("aria-pressed", "false");
        var unselectedReviewFilterBox = await reviewFilter.BoundingBoxAsync();
        Assert.NotNull(unselectedReviewFilterBox);
        Assert.InRange(
            Math.Abs(selectedReviewFilterBox.Width - unselectedReviewFilterBox.Width),
            0,
            0.5f);

        var removeReviewFilter = Page.GetByRole(
            AriaRole.Button,
            new() { Name = "Remove review filter", Exact = true });
        await removeReviewFilter.ClickAsync();
        await Expect(removeReviewFilter).ToHaveCountAsync(0);
        await Expect(Page.GetByText("Needs review", new() { Exact = true })).ToHaveCountAsync(0);

        var reviewGrid = Page.GetByRole(AriaRole.Table, new() { Name = "Review queue" });
        await Expect(reviewGrid).ToBeVisibleAsync();
        await Expect(reviewGrid.Locator("tbody tr")).ToHaveCountAsync(5);
        await Expect(Page.GetByTestId("productivity-grid-refresh-status"))
            .ToHaveTextAsync("The DataGrid is ready to refresh.");
        await Page.GetByRole(
            AriaRole.Button,
            new() { Name = "Refresh the current DataGrid request", Exact = true }).ClickAsync();
        await Expect(Page.GetByTestId("productivity-grid-refresh-status"))
            .ToHaveTextAsync("The DataGrid refreshed with the same provider request.");
        await Expect(reviewGrid.GetByRole(AriaRole.Combobox, new() { Name = "Rows per page" }))
            .ToHaveCountAsync(0);
        var selectAllRows = reviewGrid.GetByRole(
            AriaRole.Checkbox,
            new() { Name = "Select all rows on this page", Exact = true });
        await Expect(selectAllRows).Not.ToBeCheckedAsync();
        await reviewGrid.GetByRole(
            AriaRole.Checkbox,
            new() { Name = "Select row 1", Exact = true }).ClickAsync();
        await Expect(selectAllRows).ToHaveAttributeAsync("aria-checked", "mixed");
        await Expect(selectAllRows).ToHaveJSPropertyAsync("indeterminate", true);
        await selectAllRows.FocusAsync();
        await selectAllRows.PressAsync("Space");
        await Expect(selectAllRows).ToBeCheckedAsync();

        var tooltipTrigger = Page.GetByRole(AriaRole.Button, new() { Name = "Focus me" });
        await tooltipTrigger.FocusAsync();
        await Expect(Page.GetByRole(AriaRole.Tooltip)).ToContainTextAsync("keyboard and pointer");

        await Page.GetByRole(AriaRole.Button, new() { Name = "Open review details" }).ClickAsync();
        await Expect(Page.GetByRole(AriaRole.Dialog, new() { Name = "Review details" }))
            .ToContainTextAsync("Two reviewers");
        await Page.Keyboard.PressAsync("Escape");

        await Page.GetByRole(AriaRole.Button, new() { Name = "Open review actions" }).ClickAsync();
        await Page.GetByRole(AriaRole.Menuitem, new() { Name = "Mark ready" }).ClickAsync();
        await Expect(Page.GetByText("Review marked ready.", new() { Exact = true })).ToBeVisibleAsync();

        await Page.GetByText("Right-click this review", new() { Exact = true })
            .ClickAsync(new() { Button = MouseButton.Right });
        await Page.GetByRole(AriaRole.Menuitem, new() { Name = "Copy link" }).ClickAsync();
        await Expect(Page.GetByText("Review link copied.", new() { Exact = true })).ToBeVisibleAsync();

        var owner = Page.GetByRole(AriaRole.Combobox, new() { Name = "Review owner" });
        await owner.FillAsync("Alicia");
        await Expect(Page.GetByRole(AriaRole.Option, new() { Name = "Alicia Santos" }))
            .ToBeVisibleAsync();
        await Page.GetByRole(AriaRole.Option, new() { Name = "Alicia Santos" }).ClickAsync();
        await Expect(owner).ToHaveValueAsync("Alicia Santos");

        await Page.GetByTestId("productivity-upload").SetInputFilesAsync(new FilePayload
        {
            Name = "review.pdf",
            MimeType = "application/pdf",
            Buffer = "Productivity upload"u8.ToArray(),
        });
        await Expect(Page.GetByText("review.pdf", new() { Exact = true })).ToBeVisibleAsync();

        await Page.GetByRole(AriaRole.Button, new() { Name = "Review", Exact = true }).ClickAsync();
        await Expect(reviewGrid.Locator("th[aria-sort]"))
            .ToHaveAttributeAsync("aria-sort", "ascending");
        await Page.GetByRole(AriaRole.Button, new() { Name = "Go to next page" }).Last.ClickAsync();
        await Expect(Page).ToHaveURLAsync(new Regex($"/productivity/{renderMode}(?:\\?|$)"));
        await Expect(reviewGrid.Locator("tbody tr")).ToHaveCountAsync(3);
        await Expect(selectAllRows).Not.ToBeCheckedAsync();
        AssertNoUnexpectedBrowserErrors($"productivity {renderMode} workflow");
    }

    [Theory]
    [InlineData("server")]
    [InlineData("webassembly")]
    [InlineData("auto")]
    public async Task ControlledSelectAllRestoresStateWhenTheParentRejectsTheChange(string renderMode)
    {
        BeginBrowserGateTest(renderMode);
        var response = await Page.GotoAsync(
            $"{server.BaseUrl}/productivity/{renderMode}?culture=en-US&rejectGridSelection=true");
        Assert.True(response?.Ok ?? false);

        await Expect(Page.GetByTestId("productivity-workbench"))
            .ToHaveAttributeAsync("data-bzs-interactive", "true");
        var selectAllRows = Page.GetByRole(
            AriaRole.Table,
            new() { Name = "Review queue" }).GetByRole(
                AriaRole.Checkbox,
                new() { Name = "Select all rows on this page", Exact = true });

        await selectAllRows.ClickAsync();

        await Expect(selectAllRows).Not.ToBeCheckedAsync();
        await Expect(selectAllRows).ToHaveJSPropertyAsync("indeterminate", false);
    }

    [Fact]
    public async Task MenuKeyboardNavigationDoesNotScrollTheDocument()
    {
        BeginBrowserGateTest();
        await Page.SetViewportSizeAsync(800, 420);
        await Page.GotoAsync($"{server.BaseUrl}/productivity/server?culture=en-US");
        await Expect(Page.GetByTestId("productivity-workbench"))
            .ToHaveAttributeAsync("data-bzs-interactive", "true");

        var trigger = Page.GetByRole(AriaRole.Button, new() { Name = "Open review actions" });
        await trigger.ScrollIntoViewIfNeededAsync();
        await trigger.FocusAsync();
        var scrollRegion = Page.Locator("#main-content");
        var initialScrollTop = await scrollRegion.EvaluateAsync<double>("element => element.scrollTop");
        Assert.True(initialScrollTop > 0);

        await trigger.PressAsync("ArrowDown");
        var firstItem = Page.GetByRole(AriaRole.Menuitem, new() { Name = "Mark ready" });
        var lastItem = Page.GetByRole(AriaRole.Menuitem, new() { Name = "Assign reviewer" });
        await Expect(firstItem).ToBeFocusedAsync();
        await AssertScrollPositionAsync(scrollRegion, initialScrollTop);

        await firstItem.PressAsync("End");
        await Expect(lastItem).ToBeFocusedAsync();
        await AssertScrollPositionAsync(scrollRegion, initialScrollTop);

        await lastItem.PressAsync("Home");
        await Expect(firstItem).ToBeFocusedAsync();
        await AssertScrollPositionAsync(scrollRegion, initialScrollTop);

        await firstItem.PressAsync("ArrowDown");
        await Expect(lastItem).ToBeFocusedAsync();
        await AssertScrollPositionAsync(scrollRegion, initialScrollTop);
        AssertNoUnexpectedBrowserErrors("menu keyboard scroll suppression");
    }

    private static async Task AssertScrollPositionAsync(
        ILocator scrollRegion,
        double expectedScrollTop)
    {
        var actualScrollTop = await scrollRegion.EvaluateAsync<double>("element => element.scrollTop");
        Assert.Equal(expectedScrollTop, actualScrollTop);
    }
}
