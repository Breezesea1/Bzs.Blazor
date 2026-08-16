using Microsoft.Playwright;
using Microsoft.Playwright.Xunit;

namespace Bzs.Blazor.BrowserTests;

[Collection(StandaloneWebAssemblyCollection.Name)]
public sealed class StandaloneTextInputTests(StandaloneWebAssemblyFixture server) : BrowserGatePageTest
{
    [Fact]
    public async Task PasswordRevealPreservesFocusAndCaretInStandaloneWebAssembly()
    {
        BeginBrowserGateTest();
        await Page.GotoAsync($"{server.BaseUrl}/forms?culture=en-US");
        await Expect(Page.GetByText("Interactive runtime ready", new() { Exact = true })).ToBeVisibleAsync();

        var input = Page.GetByTestId("password-input-example");
        var reveal = Page.GetByRole(AriaRole.Button, new() { Name = "Show password" });
        await input.FocusAsync();
        await input.EvaluateAsync("element => element.setSelectionRange(2, 5)");

        await reveal.ClickAsync();

        await Expect(input).ToHaveAttributeAsync("type", "text");
        await Expect(input).ToBeFocusedAsync();
        Assert.Equal(
            new[] { 2, 5 },
            await input.EvaluateAsync<int[]>("element => [element.selectionStart, element.selectionEnd]"));

        await Page.GetByRole(AriaRole.Button, new() { Name = "Hide password" }).ClickAsync();
        await Expect(input).ToHaveAttributeAsync("type", "password");
        await Expect(input).ToBeFocusedAsync();
        Assert.Equal(
            new[] { 2, 5 },
            await input.EvaluateAsync<int[]>("element => [element.selectionStart, element.selectionEnd]"));
        AssertNoUnexpectedBrowserErrors("standalone password reveal");
    }

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
                element.dispatchEvent(new CompositionEvent("compositionend", { bubbles: true, data: "你" }));
                element.dispatchEvent(new Event("input", { bubbles: true }));
            }
            """);

        await Expect(committedValue).ToHaveTextAsync("Committed live search: 你");
        await Expect(input).ToHaveValueAsync("你");
    }
}
