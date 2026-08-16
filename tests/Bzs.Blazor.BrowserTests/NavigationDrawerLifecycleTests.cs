using Microsoft.Playwright;
using Microsoft.Playwright.Xunit;

namespace Bzs.Blazor.BrowserTests;

[Collection(DemoCollection.Name)]
public sealed class NavigationDrawerLifecycleTests(DemoServerFixture server) : BrowserGatePageTest
{
    [Fact]
    public async Task ModalDrawerReconcilesNewAndReplacedBackgroundSiblings()
    {
        BeginBrowserGateTest();
        await Page.GotoAsync($"{server.BaseUrl}/test/navigation-drawer?culture=en-US");
        await WaitForInteractiveShellAsync();
        var open = Page.Locator("#fixture-open");
        var background = Page.Locator("#fixture-background");

        await open.ClickAsync();
        await Expect(background).ToHaveAttributeAsync("inert", "");
        await Page.EvaluateAsync("""
            () => {
                const host = document.querySelector('#navigation-drawer-fixture');
                const added = document.createElement('button');
                added.id = 'fixture-added-background';
                host.append(added);
            }
            """);
        await Expect(Page.Locator("#fixture-added-background")).ToHaveAttributeAsync("inert", "");

        await Page.EvaluateAsync("""
            () => {
                const background = document.querySelector('#fixture-background');
                const replacement = document.createElement('main');
                replacement.id = 'fixture-replaced-background';
                replacement.tabIndex = 0;
                replacement.textContent = background.textContent;
                background.replaceWith(replacement);
            }
            """);
        await Expect(Page.Locator("#fixture-replaced-background")).ToHaveAttributeAsync("inert", "");

        await Page.Keyboard.PressAsync("Escape");
        await Expect(Page.Locator("#fixture-added-background")).Not.ToHaveAttributeAsync("inert", "");
        await Expect(Page.Locator("#fixture-replaced-background")).Not.ToHaveAttributeAsync("inert", "");
    }

    [Fact]
    public async Task VariantContractControlsModalityAndPreservesNonmodalKeyBubbling()
    {
        BeginBrowserGateTest();
        await Page.SetViewportSizeAsync(1280, 900);
        await Page.GotoAsync($"{server.BaseUrl}/test/navigation-drawer?culture=en-US");
        await WaitForInteractiveShellAsync();
        var open = Page.Locator("#fixture-open");
        var background = Page.Locator("#fixture-background");
        var drawer = Page.Locator("#fixture-drawer");

        await Page.Locator("#fixture-toggle-backdrop-hidden").ClickAsync();
        await open.ClickAsync();
        await Expect(drawer).ToHaveAttributeAsync("data-bzs-open", "true");
        await Expect(background).ToHaveAttributeAsync("inert", "");
        await Page.Keyboard.PressAsync("Escape");
        await Expect(drawer).ToHaveAttributeAsync("data-bzs-open", "false");

        await Page.Locator("#fixture-toggle-backdrop-hidden").ClickAsync();
        await Page.Locator("#fixture-variant-persistent").ClickAsync();
        await Expect(drawer).ToHaveAttributeAsync("data-bzs-navigation-drawer", "persistent");
        await Page.Locator("#fixture-toggle-backdrop-override").ClickAsync();
        await open.ClickAsync();
        await Expect(drawer).ToHaveAttributeAsync("data-bzs-open", "true");
        await Expect(background).Not.ToHaveAttributeAsync("inert", "");
        await drawer.PressAsync("A");
        await Expect(Page.Locator("#fixture-ancestor-key-count")).ToHaveTextAsync("1");

        var externalClose = Page.Locator("#fixture-close-external");
        await externalClose.ClickAsync();
        await Expect(drawer).ToHaveAttributeAsync("data-bzs-open", "false");
        await Expect(externalClose).ToBeFocusedAsync();
        await open.ClickAsync();
        await Expect(drawer).ToHaveAttributeAsync("data-bzs-open", "true");

        await Page.Locator("#fixture-variant-temporary").ClickAsync();
        await Expect(drawer).ToHaveAttributeAsync("data-bzs-navigation-drawer", "temporary");
        await Expect(background).ToHaveAttributeAsync("inert", "");

        await Page.Locator("#fixture-variant-responsive").DispatchEventAsync("click");
        await Expect(drawer).ToHaveAttributeAsync("data-bzs-navigation-drawer", "responsive");
        await Expect(background).ToHaveAttributeAsync("inert", "");
        await Page.Locator("#fixture-toggle-backdrop-override").DispatchEventAsync("click");
        await Expect(background).Not.ToHaveAttributeAsync("inert", "");
    }

    [Fact]
    public async Task ModalDrawerContainsFocusAndRestoresOnlyConnectedOpeners()
    {
        BeginBrowserGateTest();
        await Page.GotoAsync($"{server.BaseUrl}/test/navigation-drawer?culture=en-US");
        await WaitForInteractiveShellAsync();
        var open = Page.Locator("#fixture-open");
        var drawer = Page.Locator("#fixture-drawer");

        await Page.Locator("#fixture-open-without-focusable").ClickAsync();
        await AssertFocusIsInsideDrawerAsync(drawer);
        await Page.Keyboard.PressAsync("Tab");
        await AssertFocusIsInsideDrawerAsync(drawer);

        await Page.Keyboard.PressAsync("Escape");
        await Expect(Page.Locator("#fixture-open-without-focusable")).ToBeFocusedAsync();
        await open.ClickAsync();
        await Expect(drawer).ToHaveAttributeAsync("data-bzs-open", "true");
        await Page.EvaluateAsync("() => document.querySelector('#fixture-open')?.remove()");
        await Page.Keyboard.PressAsync("Escape");
        await Expect(drawer).ToHaveAttributeAsync("data-bzs-open", "false");
    }

    [Fact]
    public async Task ControlledDismissalCanRejectEscapeAndBackdropRequests()
    {
        BeginBrowserGateTest();
        await Page.GotoAsync($"{server.BaseUrl}/test/navigation-drawer?culture=en-US");
        await WaitForInteractiveShellAsync();
        var drawer = Page.Locator("#fixture-drawer");

        await Page.Locator("#fixture-reject-close").ClickAsync();
        await Page.Locator("#fixture-open").ClickAsync();
        await Expect(drawer).ToHaveAttributeAsync("data-bzs-open", "true");
        await Page.Keyboard.PressAsync("Escape");
        await Expect(drawer).ToHaveAttributeAsync("data-bzs-open", "true");
        await ClickBackdropAsync(drawer);
        await Expect(drawer).ToHaveAttributeAsync("data-bzs-open", "true");
    }

    [Fact]
    public async Task InitialFocusSelectorReceivesFocusWhenTheDrawerOpens()
    {
        BeginBrowserGateTest();
        await Page.GotoAsync($"{server.BaseUrl}/test/navigation-drawer?culture=en-US");
        await WaitForInteractiveShellAsync();

        await Page.Locator("#fixture-open-with-initial-focus").ClickAsync();

        await Expect(Page.Locator("#fixture-initial-focus-target")).ToBeFocusedAsync();
    }

    [Fact]
    public async Task DisabledBackdropDismissalDoesNotRequestAControlledClose()
    {
        BeginBrowserGateTest();
        await Page.GotoAsync($"{server.BaseUrl}/test/navigation-drawer?culture=en-US");
        await WaitForInteractiveShellAsync();
        var drawer = Page.Locator("#fixture-drawer");

        await Page.Locator("#fixture-toggle-close-on-backdrop-click").ClickAsync();
        await Page.Locator("#fixture-open").ClickAsync();
        await ClickBackdropAsync(drawer);

        await Expect(drawer).ToHaveAttributeAsync("data-bzs-open", "true");
    }

    [Fact]
    public async Task ResponsiveDrawerStaysOpenWhileViewportChangesReconcileModalConstraints()
    {
        BeginBrowserGateTest();
        await Page.SetViewportSizeAsync(1280, 900);
        await Page.GotoAsync($"{server.BaseUrl}/test/navigation-drawer?culture=en-US");
        await WaitForInteractiveShellAsync();
        var drawer = Page.Locator("#fixture-drawer");
        var background = Page.Locator("#fixture-background");

        await Page.Locator("#fixture-variant-responsive").ClickAsync();
        await Page.Locator("#fixture-open").ClickAsync();
        await Expect(drawer).ToHaveAttributeAsync("data-bzs-open", "true");
        await Expect(background).Not.ToHaveAttributeAsync("inert", "");
        Assert.Equal(string.Empty, await Page.EvaluateAsync<string>("() => document.body.style.overflow"));

        await Page.SetViewportSizeAsync(390, 844);
        await Expect(drawer).ToHaveAttributeAsync("data-bzs-open", "true");
        await Expect(background).ToHaveAttributeAsync("inert", "");
        Assert.Equal("hidden", await Page.EvaluateAsync<string>("() => document.body.style.overflow"));

        await Page.SetViewportSizeAsync(1280, 900);
        await Expect(drawer).ToHaveAttributeAsync("data-bzs-open", "true");
        await Expect(background).Not.ToHaveAttributeAsync("inert", "");
        Assert.Equal(string.Empty, await Page.EvaluateAsync<string>("() => document.body.style.overflow"));
    }

    [Fact]
    public async Task PersistentDrawerDoesNotTrapTabFocus()
    {
        BeginBrowserGateTest();
        await Page.GotoAsync($"{server.BaseUrl}/test/navigation-drawer?culture=en-US");
        await WaitForInteractiveShellAsync();
        var drawer = Page.Locator("#fixture-drawer");

        await Page.Locator("#fixture-variant-persistent").ClickAsync();
        await Page.Locator("#fixture-open").ClickAsync();
        await Page.Locator("#fixture-enhanced-navigation").FocusAsync();
        await Page.Keyboard.PressAsync("Tab");

        Assert.False(await drawer.EvaluateAsync<bool>(
            "element => element.contains(document.activeElement)"));
    }

    [Fact]
    public async Task EnhancedNavigationDisposesAnOpenModalDrawer()
    {
        BeginBrowserGateTest();
        await Page.GotoAsync($"{server.BaseUrl}/test/navigation-drawer?culture=en-US");
        await WaitForInteractiveShellAsync();
        var drawer = Page.Locator("#fixture-drawer");

        await Page.Locator("#fixture-open").ClickAsync();
        await Expect(Page.Locator("#fixture-background")).ToHaveAttributeAsync("inert", "");
        await Page.Locator("#fixture-enhanced-navigation").EvaluateAsync("element => element.click()");

        await Expect(Page).ToHaveURLAsync(new System.Text.RegularExpressions.Regex("/layout(?:\\?|$)"));
        await Expect(drawer).ToHaveCountAsync(0);
        Assert.Equal(string.Empty, await Page.EvaluateAsync<string>("() => document.body.style.overflow"));
    }

    private Task WaitForInteractiveShellAsync() =>
        Expect(Page.Locator("#navigation-drawer-fixture"))
            .ToHaveAttributeAsync("data-fixture-interactive", "true");

    private async Task ClickBackdropAsync(ILocator drawer)
    {
        var bounds = await drawer.BoundingBoxAsync();
        Assert.NotNull(bounds);
        await Page.Mouse.ClickAsync(
            bounds.X + bounds.Width - 8,
            bounds.Y + (bounds.Height / 2));
    }

    private static async Task AssertFocusIsInsideDrawerAsync(ILocator drawer)
    {
        Assert.True(await drawer.EvaluateAsync<bool>(
            "element => element.contains(document.activeElement)"));
    }
}
