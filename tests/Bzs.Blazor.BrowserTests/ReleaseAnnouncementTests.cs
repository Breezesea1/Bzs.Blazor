using Microsoft.Net.Http.Headers;
using Microsoft.Playwright;
using Microsoft.Playwright.Xunit;

namespace Bzs.Blazor.BrowserTests;

[Collection(DemoCollection.Name)]
public sealed class ReleaseAnnouncementTests(DemoServerFixture server) : BrowserGatePageTest
{
    private const string StorageKey = "bzs.demo.announcements.read.v1";

    [Fact]
    public async Task ReleasesRouteProvidesAStaticDocumentFallback()
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, $"{server.BaseUrl}/releases?culture=en-US");
        request.Headers.TryAddWithoutValidation(HeaderNames.Accept, "text/html");
        using var client = new HttpClient();
        using var response = await client.SendAsync(request);
        var html = await response.Content.ReadAsStringAsync();

        response.EnsureSuccessStatusCode();
        Assert.Contains("data-testid=\"releases-page\"", html, StringComparison.Ordinal);
        Assert.Contains("Release announcements", html, StringComparison.Ordinal);
        Assert.Contains("0.4.0", html, StringComparison.Ordinal);
        Assert.Contains("0.3.0", html, StringComparison.Ordinal);
        Assert.Contains("0.2.3", html, StringComparison.Ordinal);
        Assert.Contains("0.2.2", html, StringComparison.Ordinal);
        Assert.Contains("Forms and data workflows", html, StringComparison.Ordinal);
        Assert.Contains("Demo experience", html, StringComparison.Ordinal);
        Assert.Contains("Fixes and accessibility", html, StringComparison.Ordinal);
        Assert.Contains("New public contracts", html, StringComparison.Ordinal);
        Assert.Contains("Deliberately deferred", html, StringComparison.Ordinal);
        Assert.Contains("data-testid=\"demo-release-fallback\"", html, StringComparison.Ordinal);
        Assert.Contains("href=\"/releases?culture=en-US\"", html, StringComparison.Ordinal);
        Assert.Contains("title=\"What&#x27;s new\"", html, StringComparison.Ordinal);
        var release040Index = html.IndexOf(
            "Productivity workflows, resizable navigation, and identity",
            StringComparison.Ordinal);
        var release030Index = html.IndexOf(
            "Forms, data workflows, and navigation drawers",
            StringComparison.Ordinal);
        var release023Index = html.IndexOf(
            "Bilingual Demo and shared landing page",
            StringComparison.Ordinal);
        var release022Index = html.IndexOf(
            "Anchored overlay lifecycle hardening",
            StringComparison.Ordinal);
        Assert.True(
            release040Index >= 0
                && release030Index >= 0
                && release023Index >= 0
                && release022Index >= 0
                && release040Index < release030Index
                && release030Index < release023Index
                && release023Index < release022Index);
    }

    [Fact]
    public async Task AnnouncementRequiresExplicitAcknowledgementAndPersistsIt()
    {
        BeginBrowserGateTest();
        await Page.SetViewportSizeAsync(1280, 900);
        await Page.GotoAsync($"{server.BaseUrl}?culture=en-US");
        await Page.EvaluateAsync($"localStorage.removeItem('{StorageKey}')");
        await Page.ReloadAsync();

        var trigger = Page.GetByTestId("demo-release-trigger");
        await Expect(trigger).ToHaveAttributeAsync(
            "aria-label",
            "What's new, 1 unread release announcement");
        Assert.Null(await trigger.GetAttributeAsync("title"));
        await Expect(Page.Locator(".demo-release-unread")).ToHaveTextAsync("1");

        await trigger.HoverAsync();
        var tooltip = Page.GetByRole(AriaRole.Tooltip);
        await Expect(tooltip).ToHaveTextAsync("What's new");
        await Page.GetByRole(AriaRole.Heading, new() { Name = "Bzs.Blazor" }).HoverAsync();
        await Expect(tooltip).ToHaveCountAsync(0);
        await trigger.FocusAsync();
        await Expect(tooltip).ToHaveTextAsync("What's new");

        await trigger.ClickAsync();
        var dialog = Page.GetByRole(
            AriaRole.Dialog,
            new() { Name = "What's new in Bzs.Blazor 0.4.0", Exact = true });
        await Expect(dialog).ToBeVisibleAsync();
        await Expect(dialog.GetByText("Productivity workflows, resizable navigation, and identity", new() { Exact = true }))
            .ToBeVisibleAsync();

        await Page.Keyboard.PressAsync("Escape");
        await Expect(dialog).ToHaveCountAsync(0);
        await Expect(trigger).ToHaveAttributeAsync(
            "aria-label",
            "What's new, 1 unread release announcement");

        await trigger.ClickAsync();
        await Page.GetByTestId("demo-release-acknowledge").ClickAsync();
        await Expect(dialog).ToHaveCountAsync(0);
        await Expect(trigger).ToHaveAttributeAsync("aria-label", "What's new");
        await Expect(Page.Locator(".demo-release-unread")).ToHaveCountAsync(0);

        var storedIds = await Page.EvaluateAsync<string[]>(
            $"JSON.parse(localStorage.getItem('{StorageKey}') ?? '[]')");
        Assert.Equal(["v0.4.0"], storedIds);

        await Page.ReloadAsync();
        trigger = Page.GetByTestId("demo-release-trigger");
        await Expect(trigger).ToHaveAttributeAsync("aria-label", "What's new");
        await Expect(Page.Locator(".demo-release-unread")).ToHaveCountAsync(0);

        await Page.GetByRole(AriaRole.Link, new() { Name = "Releases", Exact = true }).ClickAsync();
        await Expect(Page).ToHaveURLAsync($"{server.BaseUrl}/releases?culture=en-US");
        await Expect(Page.GetByRole(
            AriaRole.Heading,
            new() { Name = "Release announcements", Exact = true }))
            .ToBeVisibleAsync();
        await AssertLatestReleaseHistoryAsync();
        await Expect(Page.GetByRole(
            AriaRole.Heading,
            new() { Name = "New public contracts", Exact = true }))
            .ToBeVisibleAsync();
        await Expect(Page.GetByRole(
            AriaRole.Heading,
            new() { Name = "Deliberately deferred", Exact = true }))
            .ToBeVisibleAsync();
    }

    [Fact]
    public async Task AnnouncementAndHistoryUseTheSelectedChineseCulture()
    {
        BeginBrowserGateTest();
        await Page.AddInitScriptAsync($"localStorage.removeItem('{StorageKey}')");
        await Page.GotoAsync($"{server.BaseUrl}/?culture=zh-Hans");

        var trigger = Page.GetByTestId("demo-release-trigger");
        await Expect(trigger).ToHaveAttributeAsync("aria-label", "更新公告，1 个未读版本");
        await trigger.ClickAsync();
        await Expect(Page.GetByRole(
            AriaRole.Dialog,
            new() { Name = "Bzs.Blazor 0.4.0 更新内容", Exact = true }))
            .ToBeVisibleAsync();
        await Expect(Page.GetByRole(AriaRole.Button, new() { Name = "标为已读", Exact = true }))
            .ToBeVisibleAsync();

        await Page.GetByRole(
            AriaRole.Dialog,
            new() { Name = "Bzs.Blazor 0.4.0 更新内容", Exact = true })
            .GetByRole(AriaRole.Link, new() { Name = "查看所有版本", Exact = true })
            .ClickAsync();
        await Expect(Page).ToHaveURLAsync($"{server.BaseUrl}/releases?culture=zh-Hans#v0.4.0");
        await Expect(Page.GetByRole(
            AriaRole.Dialog,
            new() { Name = "Bzs.Blazor 0.4.0 更新内容", Exact = true }))
            .ToHaveCountAsync(0);
        await Expect(Page.GetByRole(
            AriaRole.Heading,
            new() { Name = "版本公告", Exact = true }))
            .ToBeVisibleAsync();
        await Expect(Page.GetByRole(
            AriaRole.Heading,
            new() { Name = "表单与数据工作流", Exact = true }))
            .ToBeVisibleAsync();
    }
}
