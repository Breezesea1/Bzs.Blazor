using Microsoft.Playwright;
using Microsoft.Playwright.Xunit;

namespace Bzs.Blazor.BrowserTests;

[Collection(StandaloneWebAssemblyCollection.Name)]
public sealed class StandaloneReleaseAnnouncementTests(StandaloneWebAssemblyFixture server)
    : BrowserGatePageTest
{
    [Fact]
    public async Task AnnouncementAndHistoryWorkInStandaloneWebAssembly()
    {
        BeginBrowserGateTest();
        await Page.AddInitScriptAsync(
            "localStorage.removeItem('bzs.demo.announcements.read.v1')");
        await Page.SetViewportSizeAsync(390, 844);
        await Page.GotoAsync($"{server.BaseUrl}?culture=en-US");

        var trigger = Page.GetByTestId("demo-release-trigger");
        await Expect(trigger).ToHaveAttributeAsync(
            "aria-label",
            "What's new, 1 unread release announcement");
        await trigger.ClickAsync();

        var dialog = Page.GetByRole(
            AriaRole.Dialog,
            new() { Name = "What's new in Bzs.Blazor 0.2.1", Exact = true });
        await Expect(dialog).ToBeVisibleAsync();
        await dialog.GetByRole(AriaRole.Link, new() { Name = "View all releases", Exact = true })
            .ClickAsync();

        await Expect(Page).ToHaveURLAsync($"{server.BaseUrl}/releases?culture=en-US#v0.2.1");
        await Expect(dialog).ToHaveCountAsync(0);
        await Expect(Page.GetByTestId("releases-page")).ToBeVisibleAsync();
        await Expect(Page.GetByTestId("release-0.2.1")).ToBeVisibleAsync();
    }
}
