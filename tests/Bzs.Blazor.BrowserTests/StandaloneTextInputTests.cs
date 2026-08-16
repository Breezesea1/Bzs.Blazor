using Microsoft.Playwright;
using Microsoft.Playwright.Xunit;

namespace Bzs.Blazor.BrowserTests;

[Collection(StandaloneWebAssemblyCollection.Name)]
public sealed class StandaloneTextInputTests(StandaloneWebAssemblyFixture server) : BrowserGatePageTest
{
    [Fact]
    public async Task InputModeCommitsChineseImeTextOnceInStandaloneWebAssembly()
    {
        BeginBrowserGateTest();
        await Page.GotoAsync($"{server.BaseUrl}/forms?culture=en-US");
        await Expect(Page.GetByText("Interactive runtime ready", new() { Exact = true })).ToBeVisibleAsync();

        var input = Page.GetByTestId("text-input-ime-example");
        var committedValue = Page.GetByTestId("text-input-ime-value");
        await Expect(committedValue).ToHaveTextAsync("No live search text yet.");

        await input.EvaluateAsync("""
            element => {
                element.value = "n";
                element.dispatchEvent(new CompositionEvent("compositionstart", { bubbles: true }));
                element.dispatchEvent(new Event("input", { bubbles: true }));
                element.value = "你";
                element.dispatchEvent(new Event("input", { bubbles: true }));
                element.dispatchEvent(new CompositionEvent("compositionend", { bubbles: true, data: "你" }));
                element.dispatchEvent(new Event("input", { bubbles: true }));
            }
            """);

        await Expect(committedValue).ToHaveTextAsync("Committed live search: 你");
        await Expect(input).ToHaveValueAsync("你");
    }
}
