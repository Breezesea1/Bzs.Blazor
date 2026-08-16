using Microsoft.Playwright;
using Microsoft.Playwright.Xunit;
using System.Text.RegularExpressions;

namespace Bzs.Blazor.BrowserTests;

[Collection(DemoCollection.Name)]
public sealed class DemoSmokeTests(DemoServerFixture server) : BrowserGatePageTest
{
    [Fact]
    public async Task BareVisitUsesChineseAndExplicitEnglishUpdatesDocumentLanguage()
    {
        BeginBrowserGateTest();

        await Page.GotoAsync(server.BaseUrl);
        await Expect(Page.Locator("html")).ToHaveAttributeAsync("lang", "zh-Hans");
        await Expect(Page.Locator(".demo-skip-link")).ToHaveTextAsync("跳至目录内容");

        await Page.GotoAsync($"{server.BaseUrl}?culture=en-US");
        await Expect(Page.Locator("html")).ToHaveAttributeAsync("lang", "en-US");
        await Expect(Page.Locator(".demo-skip-link")).ToHaveTextAsync("Skip to catalog content");
    }

    [Fact]
    public async Task HostShellUsesBzsLayoutAcrossViewports()
    {
        BeginBrowserGateTest();
        await Page.AddInitScriptAsync(
            "localStorage.removeItem('bzs-demo-sidebar-collapsed')");
        await Page.SetViewportSizeAsync(1280, 900);
        await Page.GotoAsync(server.BaseUrl);

        var shell = Page.Locator("#demo-app-shell");
        var drawer = Page.Locator("#demo-navigation-drawer");
        var appBar = Page.Locator("#demo-app-bar");
        var mainContent = Page.Locator("#main-content");
        await Expect(shell).ToHaveAttributeAsync("data-bzs-app-shell", "true");
        await Expect(drawer).ToHaveAttributeAsync("data-bzs-navigation-drawer", "responsive");
        await Expect(drawer).ToHaveAttributeAsync("data-bzs-open", "true");
        await Expect(appBar).ToHaveAttributeAsync("data-bzs-app-bar", "surface");
        await Expect(mainContent).ToHaveAttributeAsync("data-bzs-main-content", "landmark");
        await Expect(Page.GetByRole(
            AriaRole.Navigation,
            new() { Name = "Bzs.Blazor catalog", Exact = true }))
            .ToBeVisibleAsync();
        await Expect(appBar).ToHaveCSSAsync("margin-inline-start", "256px");
        await Expect(mainContent).ToHaveCSSAsync("margin-inline-start", "256px");

        var closeNavigation = Page.GetByRole(
            AriaRole.Button,
            new() { Name = "Close navigation", Exact = true });
        await Expect(closeNavigation).ToHaveAttributeAsync("data-bzs-variant", "secondary");
        Assert.NotEqual(
            "none",
            await closeNavigation.EvaluateAsync<string>(
                "element => getComputedStyle(element).boxShadow"));
        await closeNavigation.ClickAsync();
        await Expect(drawer).ToHaveAttributeAsync("data-bzs-open", "false");
        await Expect(drawer).ToHaveAttributeAsync("aria-hidden", "true");
        await Expect(appBar).ToHaveCSSAsync("margin-inline-start", "0px");
        await Expect(mainContent).ToHaveCSSAsync("margin-inline-start", "0px");

        var openNavigation = Page.GetByRole(
            AriaRole.Button,
            new() { Name = "Open navigation", Exact = true });
        await Expect(openNavigation).ToHaveAttributeAsync("data-bzs-variant", "secondary");
        Assert.NotEqual(
            "none",
            await openNavigation.EvaluateAsync<string>(
                "element => getComputedStyle(element).boxShadow"));
        await Expect(openNavigation).ToBeFocusedAsync();
        await openNavigation.ClickAsync();
        await Expect(drawer).ToHaveAttributeAsync("data-bzs-open", "true");
        await Expect(closeNavigation).ToBeFocusedAsync();

        await Page.SetViewportSizeAsync(390, 844);
        await Page.EvaluateAsync("localStorage.removeItem('bzs-demo-sidebar-collapsed')");
        await Page.ReloadAsync();
        await Expect(drawer).ToHaveAttributeAsync("data-bzs-open", "false");
        await Expect(appBar).ToHaveCSSAsync("margin-inline-start", "0px");
        await Expect(mainContent).ToHaveCSSAsync("margin-inline-start", "0px");

        await openNavigation.ClickAsync();
        await Expect(drawer).ToHaveAttributeAsync("data-bzs-open", "true");
        await Expect(appBar).ToHaveAttributeAsync("inert", "");
        await Expect(mainContent).ToHaveAttributeAsync("inert", "");
        await Expect(closeNavigation).ToBeFocusedAsync();
        await Expect(Page.Locator("#demo-navigation-drawer .bzs-navigation-drawer__backdrop"))
            .ToBeVisibleAsync();

        var brandLink = Page.Locator("#demo-navigation-drawer .demo-brand-link");
        var exitLink = Page.Locator("#demo-navigation-drawer .demo-user-action");
        await brandLink.FocusAsync();
        await brandLink.PressAsync("Shift+Tab");
        await Expect(exitLink).ToBeFocusedAsync();
        await exitLink.PressAsync("Tab");
        await Expect(brandLink).ToBeFocusedAsync();
        await Page.Keyboard.PressAsync("Escape");
        await Expect(drawer).ToHaveAttributeAsync("data-bzs-open", "false");
        Assert.Null(await appBar.GetAttributeAsync("inert"));
        Assert.Null(await mainContent.GetAttributeAsync("inert"));
        await Expect(openNavigation).ToBeFocusedAsync();

        await openNavigation.ClickAsync();
        await Page.Locator("#demo-navigation-drawer .bzs-navigation-drawer__backdrop")
            .DispatchEventAsync("click");
        await Expect(drawer).ToHaveAttributeAsync("data-bzs-open", "false");
        await Expect(openNavigation).ToBeFocusedAsync();
        Assert.Equal(
            0,
            await Page.EvaluateAsync<int>(
                "document.documentElement.scrollWidth - document.documentElement.clientWidth"));
    }

    [Fact]
    public async Task MobileNavigationPreservesDesktopPreferenceAcrossTheResponsiveBoundary()
    {
        BeginBrowserGateTest();
        await Page.SetViewportSizeAsync(1280, 900);
        await Page.GotoAsync(server.BaseUrl);
        await Page.EvaluateAsync("localStorage.removeItem('bzs-demo-sidebar-collapsed')");
        await Page.ReloadAsync();

        var drawer = Page.Locator("#demo-navigation-drawer");
        var appBar = Page.Locator("#demo-app-bar");
        var mainContent = Page.Locator("#main-content");
        var closeNavigation = Page.GetByRole(
            AriaRole.Button,
            new() { Name = "Close navigation", Exact = true });
        var openNavigation = Page.GetByRole(
            AriaRole.Button,
            new() { Name = "Open navigation", Exact = true });
        await Expect(drawer).ToHaveAttributeAsync("data-bzs-open", "true");

        await closeNavigation.ClickAsync();
        await Page.ReloadAsync();
        await Expect(drawer).ToHaveAttributeAsync("data-bzs-open", "false");
        Assert.Equal(
            "1",
            await Page.EvaluateAsync<string>(
                "localStorage.getItem('bzs-demo-sidebar-collapsed')"));

        await openNavigation.ClickAsync();
        await Page.ReloadAsync();
        await Expect(drawer).ToHaveAttributeAsync("data-bzs-open", "true");
        Assert.Equal(
            "0",
            await Page.EvaluateAsync<string>(
                "localStorage.getItem('bzs-demo-sidebar-collapsed')"));

        await closeNavigation.FocusAsync();
        await Page.SetViewportSizeAsync(390, 844);
        await Expect(drawer).ToHaveAttributeAsync("data-bzs-open", "false");
        await Expect(openNavigation).ToBeFocusedAsync();
        await openNavigation.ClickAsync();
        await drawer.GetByRole(AriaRole.Link, new() { Name = "Forms", Exact = true }).ClickAsync();
        await Expect(Page).ToHaveURLAsync(new Regex("/forms(?:\\?|$)"));
        await Expect(drawer).ToHaveAttributeAsync("data-bzs-open", "false");
        Assert.Equal(
            "0",
            await Page.EvaluateAsync<string>(
                "localStorage.getItem('bzs-demo-sidebar-collapsed')"));

        await Page.SetViewportSizeAsync(767, 844);
        Assert.True(await Page.EvaluateAsync<bool>("matchMedia('(width < 48rem)').matches"));
        await openNavigation.ClickAsync();
        await Expect(Page.Locator("#demo-navigation-drawer .bzs-navigation-drawer__backdrop"))
            .ToHaveCSSAsync("display", "block");
        await Page.Keyboard.PressAsync("Escape");
        await Expect(drawer).ToHaveAttributeAsync("data-bzs-open", "false");

        await Page.SetViewportSizeAsync(768, 844);
        Assert.False(await Page.EvaluateAsync<bool>("matchMedia('(width < 48rem)').matches"));
        await Expect(drawer).ToHaveAttributeAsync("data-bzs-open", "true");
        await Expect(closeNavigation).ToBeFocusedAsync();
        await Expect(appBar).ToHaveCSSAsync("margin-inline-start", "256px");
        await Expect(mainContent).ToHaveCSSAsync("margin-inline-start", "256px");
        await Expect(Page.Locator("#demo-navigation-drawer .bzs-navigation-drawer__backdrop"))
            .ToHaveCSSAsync("display", "none");

        await Page.SetViewportSizeAsync(769, 844);
        await Expect(drawer).ToHaveAttributeAsync("data-bzs-open", "true");
        await Expect(appBar).ToHaveCSSAsync("margin-inline-start", "256px");
        await Expect(mainContent).ToHaveCSSAsync("margin-inline-start", "256px");

        await closeNavigation.ClickAsync();
        await Page.SetViewportSizeAsync(767, 844);
        await openNavigation.ClickAsync();
        await Expect(closeNavigation).ToBeFocusedAsync();
        await Page.SetViewportSizeAsync(768, 844);
        await Expect(drawer).ToHaveAttributeAsync("data-bzs-open", "false");
        await Expect(openNavigation).ToBeFocusedAsync();
    }

    [Fact]
    public async Task CatalogExposesTheRenderModeRoutes()
    {
        BeginBrowserGateTest();
        await Page.GotoAsync(server.BaseUrl);

        await Expect(Page.GetByRole(AriaRole.Heading, new() { Name = "Bzs.Blazor" }))
            .ToBeVisibleAsync();
        await Page.GetByRole(AriaRole.Link, new() { Name = "Static SSR" }).First.ClickAsync();
        await Expect(Page.GetByRole(AriaRole.Heading, new() { Name = "Static SSR" }))
            .ToBeVisibleAsync();
        await Expect(Page.GetByRole(AriaRole.Button, new() { Name = "Interaction unavailable" }))
            .ToBeDisabledAsync();
    }

    [Fact]
    public async Task BrandBlockShowsLogoAndFaviconResolvesToServedAsset()
    {
        BeginBrowserGateTest();
        var response = await Page.GotoAsync(server.BaseUrl);
        Assert.NotNull(response);
        Assert.True(response.Ok);

        await AssertBrandBlockShowsLogoAndFaviconResolvesToServedAssetAsync();
    }

    [Theory]
    [InlineData("01 Theme foundation", "theme-foundation", "Light, Dark, and System")]
    [InlineData("02 Foundation", "foundation", "Icon, Surface, and Button")]
    [InlineData("03 Forms", "forms", "Profile editor")]
    [InlineData("04 Feedback", "feedback", "Status and notifications")]
    [InlineData("05 Tabs", "tabs", "Tabs, language, and direction")]
    [InlineData("06 Overlays", "overlays", "Dialog, Drawer, and Host")]
    [InlineData("07 Layout", "layout", "App Shell, Grid, and Stack")]
    public async Task CatalogComponentGroupLinksNavigateToTheirSamples(
        string linkName,
        string route,
        string pageHeading)
    {
        BeginBrowserGateTest();
        await Page.GotoAsync(server.BaseUrl);

        var componentGroups = Page.GetByRole(AriaRole.Region, new() { Name = "Component groups" });
        await componentGroups.GetByRole(AriaRole.Link, new() { Name = linkName, Exact = true }).ClickAsync();

        await Expect(Page).ToHaveURLAsync(new Regex($"/{Regex.Escape(route)}$"));
        await Expect(Page.GetByRole(AriaRole.Heading, new() { Name = pageHeading }))
            .ToBeVisibleAsync();
    }

    [Fact]
    public async Task ThemeFoundationHonorsCspSystemModeAndReducedMotion()
    {
        BeginBrowserGateTest();
        await Page.EmulateMediaAsync(new()
        {
            ColorScheme = ColorScheme.Dark,
            ReducedMotion = ReducedMotion.Reduce,
        });

        var response = await Page.GotoAsync($"{server.BaseUrl}/theme-foundation");
        Assert.NotNull(response);
        var styleDirective = response.Headers["content-security-policy"]
            .Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Single(directive => directive.StartsWith("style-src", StringComparison.Ordinal));
        Assert.Equal("style-src 'self'", styleDirective);

        var provider = Page.Locator(".bzs-theme-provider");
        await Expect(Page.GetByRole(AriaRole.Heading, new() { Name = "Light, Dark, and System" }))
            .ToBeVisibleAsync();
        await Expect(provider).ToHaveAttributeAsync("data-bzs-theme", "light");
        Assert.Equal(0, await provider.Locator("style").CountAsync());
        await Expect(Page.GetByRole(AriaRole.Status))
            .ToContainTextAsync("Interactive runtime ready");

        await Page.GetByRole(AriaRole.Button, new() { Name = "Dark" }).ClickAsync();
        await Expect(provider).ToHaveAttributeAsync("data-bzs-theme", "dark");

        await Page.GetByRole(AriaRole.Button, new() { Name = "Light" }).ClickAsync();
        await Expect(provider).ToHaveAttributeAsync("data-bzs-theme", "light");

        await Page.GetByRole(AriaRole.Button, new() { Name = "System" }).ClickAsync();
        await Expect(provider).ToHaveAttributeAsync("data-bzs-theme", "dark");

        var primary = await provider.EvaluateAsync<string>(
            "element => getComputedStyle(element).getPropertyValue('--bzs-primary').trim()");
        var motion = await provider.EvaluateAsync<string>(
            "element => getComputedStyle(element).getPropertyValue('--bzs-motion-normal').trim()");
        Assert.Equal("#0f766e", primary);
        Assert.Equal("0ms", motion);
    }

    [Fact]
    public async Task FoundationComponentsActivateAfterAutoHydration()
    {
        BeginBrowserGateTest();
        await Page.GotoAsync($"{server.BaseUrl}/foundation");
        await Expect(Page.GetByRole(AriaRole.Status))
            .ToContainTextAsync("Interactive runtime ready");

        var action = Page.GetByRole(AriaRole.Button, new() { Name = "Primary action 0" });
        await action.PressAsync("Enter");
        await Expect(Page.GetByRole(AriaRole.Button, new() { Name = "Primary action 1" }))
            .ToBeVisibleAsync();
        await Page.GetByRole(AriaRole.Button, new() { Name = "Primary action 1" }).PressAsync("Space");
        await Expect(Page.GetByRole(AriaRole.Button, new() { Name = "Primary action 2" }))
            .ToBeVisibleAsync();
        var compactHeight = await Page.GetByRole(AriaRole.Button, new() { Name = "Primary action 2" })
            .EvaluateAsync<double>("element => element.getBoundingClientRect().height");
        await Page.GetByRole(AriaRole.Button, new() { Name = "Comfortable density" }).ClickAsync();
        var provider = Page.Locator(".bzs-theme-provider");
        await Expect(provider).ToHaveAttributeAsync("data-bzs-density", "comfortable");
        var comfortableHeight = await Page.GetByRole(AriaRole.Button, new() { Name = "Primary action 2" })
            .EvaluateAsync<double>("element => element.getBoundingClientRect().height");
        Assert.True(comfortableHeight > compactHeight);
        await Expect(Page.GetByRole(AriaRole.Button, new() { Name = "Saving" }))
            .ToBeDisabledAsync();
        await Expect(Page.GetByRole(AriaRole.Button, new() { Name = "Close example" }))
            .ToBeVisibleAsync();
    }

    [Fact]
    public async Task LayoutPrimitivesRespondAcrossViewports()
    {
        BeginBrowserGateTest();
        await Page.SetViewportSizeAsync(390, 844);
        await Page.GotoAsync($"{server.BaseUrl}/layout");
        await Expect(Page.GetByRole(AriaRole.Heading, new() { Name = "App Shell, Grid, and Stack" }))
            .ToBeVisibleAsync();
        await Expect(Page.Locator("#layout-intro"))
            .ToContainTextAsync("Each live preview is paired with the Razor that produces it.");
        await Expect(Page.GetByRole(AriaRole.Heading, new() { Name = "Dividers" }))
            .ToBeVisibleAsync();
        await Expect(Page.Locator("#layout-container-code"))
            .ToContainTextAsync("BzsContainer Fixed=\"true\"");
        await Expect(Page.Locator("#layout-grid-code"))
            .ToContainTextAsync("BzsGridItem Xs=\"12\" Md=\"6\" Lg=\"4\"");
        await Expect(Page.Locator("#layout-stack-code"))
            .ToContainTextAsync("BzsSpacer");
        await Expect(Page.Locator("#layout-divider-code"))
            .ToContainTextAsync("BzsDivider Vertical=\"true\" Absolute=\"true\"");
        await Expect(Page.Locator("#layout-divider-css-code"))
            .ToContainTextAsync("position: relative");
        await Expect(Page.Locator("#layout-app-shell-code"))
            .ToContainTextAsync("BzsNavigationDrawerVariant.Responsive");
        await Expect(Page.Locator("#layout-app-shell-css-code"))
            .ToContainTextAsync("--bzs-navigation-drawer-width: 13rem");
        var palette = Page.Locator("#layout-theme-palette");
        var layoutPage = Page.Locator("#layout-workbench");
        await Expect(palette).ToBeVisibleAsync();
        var lightPaletteColors = new[]
        {
            (Tone: "primary", Color: "rgb(87, 86, 216)"),
            (Tone: "info", Color: "rgb(11, 116, 148)"),
            (Tone: "success", Color: "rgb(20, 122, 99)"),
            (Tone: "warning", Color: "rgb(164, 95, 8)"),
            (Tone: "error", Color: "rgb(193, 63, 89)"),
        };
        foreach (var (tone, color) in lightPaletteColors)
        {
            await Expect(Page.Locator($"#layout-tone-{tone} .demo-layout-swatch"))
                .ToHaveCSSAsync("background-color", color);
        }

        Assert.Equal(
            "#edf1f5",
            await layoutPage.EvaluateAsync<string>(
                "element => getComputedStyle(element).getPropertyValue('--layout-canvas').trim()"));
        Assert.Equal(
            "#f6f8fb",
            await layoutPage.EvaluateAsync<string>(
                "element => getComputedStyle(element).getPropertyValue('--layout-surface').trim()"));
        var swatchColors = await palette.Locator(".demo-layout-swatch")
            .EvaluateAllAsync<string[]>("elements => elements.map(element => getComputedStyle(element).backgroundColor)");
        Assert.Equal(5, swatchColors.Distinct(StringComparer.Ordinal).Count());

        await layoutPage.EvaluateAsync("element => element.setAttribute('data-bzs-theme', 'dark')");
        var darkPaletteColors = new[]
        {
            (Tone: "primary", Color: "rgb(154, 156, 241)"),
            (Tone: "info", Color: "rgb(104, 204, 225)"),
            (Tone: "success", Color: "rgb(96, 208, 168)"),
            (Tone: "warning", Color: "rgb(239, 183, 98)"),
            (Tone: "error", Color: "rgb(255, 141, 163)"),
        };
        foreach (var (tone, color) in darkPaletteColors)
        {
            await Expect(Page.Locator($"#layout-tone-{tone} .demo-layout-swatch"))
                .ToHaveCSSAsync("background-color", color);
        }

        Assert.Equal(
            "#15181e",
            await layoutPage.EvaluateAsync<string>(
                "element => getComputedStyle(element).getPropertyValue('--layout-canvas').trim()"));
        Assert.Equal(
            "#1b1f27",
            await layoutPage.EvaluateAsync<string>(
                "element => getComputedStyle(element).getPropertyValue('--layout-surface').trim()"));
        await layoutPage.EvaluateAsync("element => element.removeAttribute('data-bzs-theme')");

        var container = Page.Locator("#layout-container");
        Assert.NotEqual(
            "none",
            await container.EvaluateAsync<string>("element => getComputedStyle(element).boxShadow"));
        Assert.Contains(
            "inset",
            await Page.Locator("#layout-container-code")
                .EvaluateAsync<string>("element => getComputedStyle(element.parentElement).boxShadow"));

        var appShell = Page.Locator("#layout-app-shell");
        var appBar = Page.Locator("#layout-app-bar");
        var drawerClose = Page.Locator("#layout-drawer-close");
        var navigationDrawer = Page.Locator("#layout-navigation-drawer");
        var navigationLandmark = Page.GetByRole(AriaRole.Navigation, new() { Name = "Workspace navigation" });
        var shellMainContent = Page.Locator("#layout-main-content");
        await Expect(appShell).ToBeVisibleAsync();
        await Expect(appBar).ToHaveAttributeAsync("data-bzs-app-bar", "surface");
        await Expect(drawerClose).ToBeVisibleAsync();
        await Expect(navigationLandmark.GetByRole(AriaRole.Link, new() { Name = "Overview", Exact = true }))
            .ToBeVisibleAsync();
        Assert.Equal(
            "0px",
            await shellMainContent.EvaluateAsync<string>("element => getComputedStyle(element).marginInlineStart"));
        Assert.Equal(
            "0px",
            await appBar.EvaluateAsync<string>("element => getComputedStyle(element).marginInlineStart"));

        await drawerClose.ClickAsync();
        await Expect(navigationDrawer).ToHaveAttributeAsync("aria-hidden", "true");
        var drawerToggle = Page.Locator("#layout-drawer-toggle");
        await Expect(drawerToggle).ToBeVisibleAsync();
        await Expect(drawerToggle).ToHaveAttributeAsync("aria-expanded", "false");
        await drawerToggle.ClickAsync();
        await Expect(drawerClose).ToBeVisibleAsync();
        await Expect(navigationLandmark.GetByRole(AriaRole.Link, new() { Name = "Overview", Exact = true }))
            .ToBeVisibleAsync();

        var grid = Page.Locator("#layout-responsive-grid");
        var production = Page.Locator("#layout-production");
        var review = Page.Locator("#layout-review");
        var archive = Page.Locator("#layout-archive");
        await Expect(production).ToBeVisibleAsync();
        await Expect(review).ToBeVisibleAsync();
        await Expect(archive).ToBeVisibleAsync();
        var productionMobile = await GetVisibleBoxAsync(production);
        var reviewMobile = await GetVisibleBoxAsync(review);
        var archiveMobile = await GetVisibleBoxAsync(archive);

        Assert.True(reviewMobile.Y > productionMobile.Y);
        Assert.True(archiveMobile.Y > reviewMobile.Y);
        Assert.InRange(Math.Abs(productionMobile.Width - reviewMobile.Width), 0, 1);

        await Page.SetViewportSizeAsync(1280, 900);
        await Expect(appBar).ToHaveCSSAsync("margin-inline-start", "208px");
        await Expect(shellMainContent).ToHaveCSSAsync("margin-inline-start", "208px");
        var productionDesktop = await GetVisibleBoxAsync(production);
        var reviewDesktop = await GetVisibleBoxAsync(review);
        var archiveDesktop = await GetVisibleBoxAsync(archive);

        Assert.InRange(Math.Abs(productionDesktop.Y - reviewDesktop.Y), 0, 1);
        Assert.InRange(Math.Abs(reviewDesktop.Y - archiveDesktop.Y), 0, 1);
        Assert.InRange(Math.Abs(productionDesktop.Width - reviewDesktop.Width), 0, 1);
        Assert.True(productionDesktop.Width < productionMobile.Width);
        Assert.Equal("12px", await grid.EvaluateAsync<string>("element => getComputedStyle(element).gap"));

        var shellDesktop = await GetVisibleBoxAsync(appShell);
        var shellAppBarDesktop = await GetVisibleBoxAsync(appBar);
        var shellNavigationDesktop = await GetVisibleBoxAsync(navigationDrawer);
        var shellMainDesktop = await GetVisibleBoxAsync(shellMainContent);
        Assert.True(shellAppBarDesktop.X - shellDesktop.X > 150);
        Assert.True(shellMainDesktop.X - shellDesktop.X > 150);
        Assert.InRange(Math.Abs(shellAppBarDesktop.X - shellMainDesktop.X), 0, 1);
        Assert.InRange(Math.Abs(shellNavigationDesktop.Y - shellDesktop.Y), 0, 1);
        Assert.InRange(Math.Abs(shellNavigationDesktop.Height - shellDesktop.Height), 0, 2);

        var queueStack = Page.Locator("#layout-queue-stack");
        var queue = await GetVisibleBoxAsync(queueStack.GetByText("Queue", new() { Exact = true }));
        var itemCountLabel = queueStack.GetByText("12 items", new() { Exact = true });
        var itemCount = await GetVisibleBoxAsync(itemCountLabel);
        Assert.True(itemCount.X - (queue.X + queue.Width) > 100);
        var itemCountShadow = await itemCountLabel
            .EvaluateAsync<string>("element => getComputedStyle(element).boxShadow");
        Assert.NotEqual("none", itemCountShadow);
        Assert.DoesNotContain("inset", itemCountShadow, StringComparison.Ordinal);

        var flexDivider = Page.Locator("#layout-flex-divider");
        var naturalDivider = Page.Locator("#layout-natural-divider");
        Assert.Equal(
            "stretch",
            await flexDivider.EvaluateAsync<string>("element => getComputedStyle(element).alignSelf"));
        Assert.NotEqual(
            "stretch",
            await naturalDivider.EvaluateAsync<string>("element => getComputedStyle(element).alignSelf"));
        var flexDividerBox = await GetVisibleBoxAsync(flexDivider);
        var naturalDividerBox = await GetVisibleBoxAsync(naturalDivider);
        Assert.True(flexDividerBox.Height > naturalDividerBox.Height + 8);

        var absoluteBoundary = await GetVisibleBoxAsync(Page.Locator("#layout-absolute-boundary"));
        var absoluteDivider = await GetVisibleBoxAsync(Page.Locator("#layout-absolute-divider"));
        var blockStartInset = absoluteDivider.Y - absoluteBoundary.Y;
        var blockEndInset = absoluteBoundary.Y + absoluteBoundary.Height
            - absoluteDivider.Y - absoluteDivider.Height;
        Assert.True(blockStartInset > 0);
        Assert.True(blockEndInset > 0);
        Assert.InRange(Math.Abs(blockStartInset - blockEndInset), 0, 1);
    }

    private async Task<(double X, double Y, double Width, double Height)> GetVisibleBoxAsync(
        ILocator locator)
    {
        for (var attempt = 0; attempt < 4; attempt++)
        {
            await Expect(locator).ToBeVisibleAsync();
            var box = await locator.BoundingBoxAsync();
            if (box is not null)
            {
                return (box.X, box.Y, box.Width, box.Height);
            }

            await Task.Delay(25);
        }

        throw new InvalidOperationException("The visible element did not expose a bounding box.");
    }
}
