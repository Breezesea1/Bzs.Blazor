using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using Bzs.Blazor.Demo.Client;
using Microsoft.Playwright;
using Microsoft.Playwright.Xunit;

namespace Bzs.Blazor.BrowserTests;

public abstract class BrowserGatePageTest : PageTest
{
    private readonly BrowserObservation _observation = new();
    private string _artifactTestName = "unattributed";

    public override async Task InitializeAsync()
    {
        await base.InitializeAsync();
        Microsoft.Playwright.Assertions.SetDefaultExpectTimeout(30_000);
        await _observation.ObserveAsync(Context);
        _observation.PrimaryContext = Context;
        _observation.PrimaryPage = Page;
    }

    public override async Task DisposeAsync()
    {
        try
        {
            await _observation.CaptureAsync(
                TestOk ? null : BrowserObservation.TryPrepareArtifactDirectory(_artifactTestName));
        }
        finally
        {
            await base.DisposeAsync();
        }
    }

    protected void BeginBrowserGateTest(
        string? caseSuffix = null,
        [CallerMemberName] string testName = "")
    {
        var name = string.IsNullOrWhiteSpace(caseSuffix)
            ? testName
            : $"{testName}-{caseSuffix}";
        _artifactTestName = $"{GetType().Name}.{name}";
    }

    protected async Task<IPage> NewObservedPageAsync(BrowserNewContextOptions options)
    {
        var context = await NewContext(options);
        await _observation.ObserveAsync(context);
        var page = await context.NewPageAsync();

        _observation.Observe(page);
        _observation.PrimaryContext = context;
        _observation.PrimaryPage = page;
        return page;
    }

    protected async Task AssertBrandBlockShowsLogoAndFaviconResolvesToServedAssetAsync()
    {
        var navigation = Page.GetByRole(
            AriaRole.Navigation,
            new() { Name = "Bzs.Blazor 目录", Exact = true });
        await Expect(navigation).ToBeVisibleAsync();

        var brandLink = navigation.GetByRole(
            AriaRole.Link,
            new() { Name = "Bzs.Blazor", Exact = false });
        await Expect(brandLink).ToBeVisibleAsync();
        var logo = brandLink.Locator("img");
        var logoHref = await logo.GetAttributeAsync("src");
        Assert.False(string.IsNullOrWhiteSpace(logoHref), "The brand logo source is missing.");
        Assert.False(
            logoHref.StartsWith("data:", StringComparison.Ordinal),
            "The brand logo still uses an inline data URL.");
        await logo.EvaluateAsync(
            """
            image => image.complete && image.naturalWidth > 0
                ? Promise.resolve()
                : new Promise((resolve, reject) => {
                    image.addEventListener('load', resolve, { once: true });
                    image.addEventListener('error', () => reject(new Error('The brand logo failed to load.')), { once: true });
                })
            """);

        var icon = Page.Locator("head link[rel='icon']");
        var iconHref = await icon.GetAttributeAsync("href");
        Assert.False(string.IsNullOrWhiteSpace(iconHref), "The favicon link is missing.");
        Assert.False(
            iconHref.StartsWith("data:", StringComparison.Ordinal),
            "The favicon link still uses the empty data: placeholder.");
        var resolvedIconHref = await icon.EvaluateAsync<string>("element => element.href");
        var iconResponse = await Page.Context.APIRequest.GetAsync(resolvedIconHref);
        Assert.True(iconResponse.Ok, $"The favicon '{resolvedIconHref}' was not served successfully.");
    }

    protected async Task AssertDemoChromeAsync(bool isChinese, bool includesServerRenderModes, string hostStatus)
    {
        var chrome = DemoChrome.Read(isChinese);

        await Expect(Page.Locator("a[href='#main-content']")).ToHaveTextAsync(chrome.SkipLink);
        var navigation = Page.GetByRole(
            AriaRole.Navigation,
            new() { Name = chrome.NavigationAccessibleName, Exact = true });
        await Expect(navigation).ToBeVisibleAsync();
        await Expect(navigation.GetByRole(AriaRole.Link, new() { Name = $"Bzs.Blazor {chrome.BrandTagline}", Exact = true }))
            .ToBeVisibleAsync();
        await Expect(Page.GetByRole(AriaRole.Button, new() { Name = chrome.CloseNavigation, Exact = true }))
            .ToBeVisibleAsync();

        foreach (var section in DemoChrome.ReadSectionNames(isChinese, includesServerRenderModes))
        {
            await Expect(navigation.GetByText(section, new() { Exact = true })).ToBeVisibleAsync();
        }

        foreach (var link in DemoChrome.ReadNavigationLinkNames(isChinese, includesServerRenderModes))
        {
            await Expect(navigation.GetByRole(AriaRole.Link, new() { Name = link, Exact = true })).ToBeVisibleAsync();
        }

        await Expect(navigation.GetByText(chrome.DemoUserAvatarInitial, new() { Exact = true })).ToBeVisibleAsync();
        await Expect(navigation.GetByText(chrome.DemoUser, new() { Exact = true })).ToBeVisibleAsync();
        await Expect(navigation.GetByRole(AriaRole.Separator, new() { Name = chrome.ResizeNavigationDrawer, Exact = true }))
            .ToBeVisibleAsync();
        await Expect(navigation.GetByRole(AriaRole.Link, new() { Name = chrome.SignOutAccessibleName, Exact = true }))
            .ToBeVisibleAsync();
        await Expect(Page.GetByText(chrome.ComponentWorkbench, new() { Exact = true })).ToBeVisibleAsync();
        await Expect(Page.GetByText(hostStatus, new() { Exact = true })).ToBeVisibleAsync();

        var language = Page.GetByRole(
            AriaRole.Radiogroup,
            new() { Name = chrome.LanguageSwitcherAccessibleName, Exact = true });
        await Expect(language).ToBeVisibleAsync();
        await Expect(language.GetByRole(AriaRole.Radio, new() { Name = "English", Exact = true })).ToBeVisibleAsync();
        await Expect(language.GetByRole(AriaRole.Radio, new() { Name = "中文", Exact = true })).ToBeVisibleAsync();
    }

    protected async Task AssertGlobalThemeSwitchPersistsAndFollowsSystemPreferenceAsync(
        DemoDestinationUrls urls,
        string? culture,
        bool isChinese)
    {
        var chrome = DemoChrome.Read(isChinese);
        var accessibleName = chrome.ThemeSwitcherAccessibleName;
        var lightLabel = chrome.ThemeLight;
        var darkLabel = chrome.ThemeDark;
        var systemLabel = chrome.ThemeSystem;
        var foundationLinkLabel = chrome.FoundationComponents;
        await Page.AddInitScriptAsync(
            """
            if (!sessionStorage.getItem('bzs-demo-theme-mode-test-initialized')) {
                localStorage.removeItem('bzs-demo-theme-mode');
                sessionStorage.setItem('bzs-demo-theme-mode-test-initialized', 'true');
            }
            """);
        await Page.EmulateMediaAsync(new() { ColorScheme = ColorScheme.Light });

        await Page.GotoAsync(urls.Root(culture));

        var provider = Page.GetByTestId("demo-global-theme-provider");
        await Expect(provider).ToHaveAttributeAsync("data-bzs-demo-theme-mode", "light");
        var themeSwitch = Page.GetByRole(AriaRole.Group, new() { Name = accessibleName, Exact = true });
        await Expect(themeSwitch).ToBeVisibleAsync();
        await Expect(themeSwitch.GetByRole(AriaRole.Button, new() { Name = lightLabel, Exact = true }))
            .ToBeVisibleAsync();
        var dark = themeSwitch.GetByRole(AriaRole.Button, new() { Name = darkLabel, Exact = true });
        await Expect(dark).ToBeVisibleAsync();
        var system = themeSwitch.GetByRole(AriaRole.Button, new() { Name = systemLabel, Exact = true });
        await Expect(system).ToBeVisibleAsync();

        await dark.ClickAsync();
        await Expect(provider).ToHaveAttributeAsync("data-bzs-theme", "dark");

        await Page.GetByRole(AriaRole.Navigation).GetByRole(
            AriaRole.Link,
            new() { Name = foundationLinkLabel, Exact = true }).ClickAsync();
        await Expect(provider).ToHaveAttributeAsync("data-bzs-theme", "dark");

        await Page.ReloadAsync();
        await Expect(provider).ToHaveAttributeAsync("data-bzs-theme", "dark");

        themeSwitch = Page.GetByRole(AriaRole.Group, new() { Name = accessibleName, Exact = true });
        await themeSwitch.GetByRole(AriaRole.Button, new() { Name = systemLabel, Exact = true }).ClickAsync();
        await Expect(Page.GetByTestId("demo-global-theme-provider"))
            .ToHaveAttributeAsync("data-bzs-theme", "light");

        await Page.GotoAsync(urls.To(DemoCatalogDestinations.Forms, culture));
        await Expect(Page.GetByTestId("demo-global-theme-provider"))
            .ToHaveAttributeAsync("data-bzs-theme", "light");

        await Page.EmulateMediaAsync(new() { ColorScheme = ColorScheme.Dark });
        await Expect(Page.GetByTestId("demo-global-theme-provider"))
            .ToHaveAttributeAsync("data-bzs-theme", "dark");
    }

    protected async Task AssertLandingPageSectionsAsync(
        DemoDestinationUrls urls,
        string? culture,
        bool includesServerRenderModes)
    {
        await Page.GotoAsync(urls.Root(culture));

        var expectedSections = new[]
        {
            "landing-hero",
            "landing-demo-strip",
            "landing-install",
            "landing-features",
            "landing-component-groups",
            "landing-release",
            "landing-routes",
            "landing-footer",
        };

        foreach (var section in expectedSections)
        {
            await Expect(Page.GetByTestId(section)).ToBeVisibleAsync();
        }

        var order = await Page.GetByTestId("landing-page").EvaluateAsync<string[]>(
            "root => [...root.querySelectorAll(':scope > [data-testid]')].map(element => element.getAttribute('data-testid'))");
        Assert.Equal(expectedSections, order);

        var isChinese = culture is not DemoDestinationUrls.English;
        var expectedRuntimeLinks = includesServerRenderModes
            ? isChinese
                ? new[] { "静态 SSR", "交互式服务器", "交互式 WebAssembly", "交互式自动" }
                : ["Static SSR", "Interactive Server", "Interactive WebAssembly", "Interactive Auto"]
            : isChinese
                ? ["交互式 WebAssembly"]
                : ["Interactive WebAssembly"];
        var actualRuntimeLinks = await Page.GetByTestId("landing-routes")
            .GetByRole(AriaRole.Link)
            .AllTextContentsAsync();
        Assert.Equal(expectedRuntimeLinks, actualRuntimeLinks.Select(text => text.Trim()));
    }

    protected async Task AssertLandingPageCopyFollowsCultureAsync(DemoDestinationUrls urls)
    {
        await Page.GotoAsync(urls.Root());
        await Expect(Page.GetByRole(AriaRole.Heading, new() { Level = 1 }))
            .ToHaveTextAsync("为 Blazor 而生的紧凑组件库");
        await Expect(Page.GetByTestId("landing-install").GetByRole(
            AriaRole.Heading, new() { Name = "安装", Exact = true })).ToBeVisibleAsync();
        await Expect(Page.GetByTestId("landing-features").GetByRole(
            AriaRole.Heading, new() { Name = "为什么是 Bzs.Blazor", Exact = true })).ToBeVisibleAsync();
        await Expect(Page.GetByTestId("landing-release").GetByRole(
            AriaRole.Heading, new() { Name = "最新版本", Exact = true })).ToBeVisibleAsync();
        await Expect(Page.GetByTestId("landing-footer")).ToContainTextAsync("基于 MIT 许可证发布。");

        await Page.GotoAsync(urls.Root(DemoDestinationUrls.English));
        await Expect(Page.GetByRole(AriaRole.Heading, new() { Level = 1 }))
            .ToHaveTextAsync("A compact component library for Blazor");
        await Expect(Page.GetByTestId("landing-install").GetByRole(
            AriaRole.Heading, new() { Name = "Installation", Exact = true })).ToBeVisibleAsync();
        await Expect(Page.GetByTestId("landing-features").GetByRole(
            AriaRole.Heading, new() { Name = "Why Bzs.Blazor", Exact = true })).ToBeVisibleAsync();
        await Expect(Page.GetByTestId("landing-release").GetByRole(
            AriaRole.Heading, new() { Name = "Latest release", Exact = true })).ToBeVisibleAsync();
        await Expect(Page.GetByTestId("landing-footer")).ToContainTextAsync("Released under the MIT license.");
    }

    protected async Task AssertLandingHeroCtasReachTheirSectionsAsync(
        DemoDestinationUrls urls,
        string? culture)
    {
        await Page.GotoAsync(urls.Root(culture));
        await Expect(Page.GetByTestId("landing-page")).ToHaveAttributeAsync("data-interactive", "true");

        await Page.GetByTestId("landing-cta-install").ClickAsync();
        await Expect(Page).ToHaveURLAsync(new Regex("#landing-install$"));
        await Expect(Page.GetByTestId("landing-install")).ToBeInViewportAsync();

        await Page.GetByTestId("landing-cta-groups").ClickAsync();
        await Expect(Page).ToHaveURLAsync(new Regex("#landing-component-groups$"));
        await Expect(Page.GetByTestId("landing-component-groups")).ToBeInViewportAsync();
    }

    protected async Task AssertLandingLiveStripAsync(DemoDestinationUrls urls, string? culture)
    {
        await Page.GotoAsync(urls.Root(culture));
        await Expect(Page.GetByTestId("landing-page")).ToHaveAttributeAsync("data-interactive", "true");

        var strip = Page.GetByTestId("landing-demo-strip");

        var name = strip.Locator("#landing-demo-name");
        await name.FillAsync("Mei");
        await Expect(name).ToHaveValueAsync("Mei");

        var workspace = strip.Locator("#landing-demo-workspace");
        await workspace.ClickAsync();
        await strip.Locator("#landing-demo-workspace-option-1").ClickAsync();
        await workspace.ClickAsync();
        await Expect(strip.Locator("#landing-demo-workspace-option-1"))
            .ToHaveAttributeAsync("aria-selected", "true");
        await Page.Keyboard.PressAsync("Escape");
        await Expect(workspace).ToHaveAttributeAsync("aria-expanded", "false");
        await Expect(workspace).ToBeFocusedAsync();

        var notifications = strip.Locator("#landing-demo-notifications");
        var initiallyChecked = await notifications.IsCheckedAsync();
        await notifications.PressAsync("Space");
        await Expect(notifications).ToBeCheckedAsync(new() { Checked = !initiallyChecked });

        await strip.GetByTestId("landing-show-toast").ClickAsync();
        var toast = Page.GetByTestId("landing-overlay-host").GetByRole(AriaRole.Status);
        await Expect(toast).ToBeVisibleAsync();
        await toast.GetByRole(AriaRole.Button).ClickAsync();
        await Expect(toast).ToHaveCountAsync(0);

        await strip.GetByTestId("landing-open-dialog").ClickAsync();
        var dialog = Page.GetByRole(AriaRole.Dialog);
        await Expect(dialog).ToBeVisibleAsync();
        await Expect(Page.GetByTestId("landing-dialog-close")).ToBeFocusedAsync();
        await Page.Keyboard.PressAsync("Escape");
        await Expect(dialog).ToHaveCountAsync(0);
    }

    protected async Task AssertLandingInstallSnippetAsync(DemoDestinationUrls urls, string? culture)
    {
        await Page.GotoAsync(urls.Root(culture));

        var snippet = Page.GetByTestId("landing-install-snippet");
        await Expect(snippet).ToContainTextAsync("dotnet add package Bzs.Blazor");
        await Expect(snippet).ToContainTextAsync($"--version {DemoReleases.LatestVersion}");
        await Expect(snippet).ToContainTextAsync("AddBzsBlazor()");

        await Expect(Page.GetByTestId("landing-page")).ToHaveAttributeAsync("data-interactive", "true");
        await Page.GetByTestId("landing-copy").ClickAsync();
        await Expect(Page.GetByTestId("landing-copy-status")).ToHaveTextAsync(new Regex(".+"));
    }

    protected async Task AssertLandingReleaseSummaryAsync(DemoDestinationUrls urls, string? culture)
    {
        await Page.GotoAsync(urls.Root(culture));

        var release = Page.GetByTestId("landing-release");
        await Expect(release.GetByTestId("landing-release-version"))
            .ToHaveTextAsync(new Regex(@"^\d+\.\d+\.\d+$"));

        await release.GetByTestId("landing-release-more").ClickAsync();
        await Expect(Page).ToHaveURLAsync(urls.To(DemoCatalogDestinations.Releases, culture));
        await Expect(Page.GetByTestId("releases-page")).ToBeVisibleAsync();
    }

    protected async Task AssertLatestReleaseHistoryAsync()
    {
        var releaseHeadings = await Page.GetByRole(
            AriaRole.Heading,
            new() { Level = 2 }).AllTextContentsAsync();

        Assert.Equal(
            DemoReleases.TitlesInOrder(isChinese: false, 3),
            releaseHeadings.Take(3));
    }

    protected async Task AssertLandingFooterAsync(DemoDestinationUrls urls, string? culture)
    {
        await Page.GotoAsync(urls.Root(culture));

        var footer = Page.GetByTestId("landing-footer");
        await Expect(footer.Locator("a[href^='https://www.nuget.org/packages/']")).ToBeVisibleAsync();
        await Expect(footer.Locator("a[href='https://github.com/Breezesea1/Bzs.Blazor']")).ToBeVisibleAsync();
        await Expect(footer.Locator("a[href$='/LICENSE']")).ToBeVisibleAsync();
    }

    protected void AssertNoUnexpectedBrowserErrors(string? context = null)
    {
        var errors = _observation.GetUnexpectedErrors();
        var message = context is null
            ? "Unexpected browser errors were reported."
            : $"Unexpected browser errors were reported during {context}.";
        Assert.True(errors.Count == 0, $"{message}{Environment.NewLine}{string.Join(Environment.NewLine, errors)}");
    }
}
