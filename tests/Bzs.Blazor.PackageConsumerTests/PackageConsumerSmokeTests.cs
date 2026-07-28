using Microsoft.Playwright;
using static Microsoft.Playwright.Assertions;

namespace Bzs.Blazor.PackageConsumerTests;

public sealed class PackageConsumerSmokeTests
{
    [Fact]
    public void PublishedPackageStaticAssetsMatchEndpointIntegrityMetadata()
    {
        var packagePath = Environment.GetEnvironmentVariable("BZS_PACKAGE_PATH");
        var endpointManifestPath = Environment.GetEnvironmentVariable(
            "BZS_PACKAGE_STATIC_WEB_ASSETS_MANIFEST");
        Assert.False(
            string.IsNullOrWhiteSpace(packagePath),
            "BZS_PACKAGE_PATH must point to the package under release verification.");
        Assert.False(
            string.IsNullOrWhiteSpace(endpointManifestPath),
            "BZS_PACKAGE_STATIC_WEB_ASSETS_MANIFEST must point to the consumer endpoint manifest.");

        StaticWebAssetIntegrityVerifier.Verify(packagePath!, endpointManifestPath!);
    }

    [Fact]
    public async Task PublishedPackageConsumerExercisesAllRenderModesAndRuntimeAssets()
    {
        var baseUrl = Environment.GetEnvironmentVariable("BZS_PACKAGE_CONSUMER_BASE_URL");
        Assert.False(
            string.IsNullOrWhiteSpace(baseUrl),
            "BZS_PACKAGE_CONSUMER_BASE_URL must point to the running published package consumer.");

        using var client = new HttpClient();
        var staticHtml = await client.GetStringAsync($"{baseUrl}/static-smoke");
        Assert.Contains("Static SSR package smoke", staticHtml, StringComparison.Ordinal);
        Assert.Contains("data-bzs-surface=\"raised\"", staticHtml, StringComparison.Ordinal);
        Assert.Contains("role=\"tab\"", staticHtml, StringComparison.Ordinal);
        Assert.Contains("data-bzs-overlay-host=\"true\"", staticHtml, StringComparison.Ordinal);

        using var playwright = await Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync();
        await using var context = await browser.NewContextAsync(new BrowserNewContextOptions
        {
            ViewportSize = new ViewportSize { Width = 1280, Height = 900 },
        });
        await using var page = await context.NewPageAsync();
        page.SetDefaultTimeout(45_000);
        Microsoft.Playwright.Assertions.SetDefaultExpectTimeout(45_000);

        var consoleErrors = new List<string>();
        var badResponses = new List<string>();
        var loadedAssets = new HashSet<string>(StringComparer.Ordinal);
        page.Console += (_, message) =>
        {
            if (string.Equals(message.Type, "error", StringComparison.OrdinalIgnoreCase))
            {
                consoleErrors.Add(message.Text);
            }
        };
        page.Response += (_, response) =>
        {
            if (response.Status >= 400)
            {
                badResponses.Add($"{response.Status} {response.Url}");
            }

            foreach (var asset in ObservedAssets)
            {
                if (MatchesRuntimeAsset(response.Url, asset))
                {
                    loadedAssets.Add(asset);
                }
            }
        };

        var requestedRuntimes = GetRequestedRuntimes();
        try
        {
            foreach (var runtime in requestedRuntimes)
            {
                var route = runtime == "aot" ? "aot/index.html" : $"{runtime}-smoke";
                await ExerciseRuntimeAsync(page, baseUrl!, runtime, route);
            }
        }
        catch (Exception exception)
        {
            Assert.Fail(
                $"{exception}{Environment.NewLine}Console errors:{Environment.NewLine}"
                + string.Join(Environment.NewLine, consoleErrors)
                + $"{Environment.NewLine}HTTP errors:{Environment.NewLine}"
                + string.Join(Environment.NewLine, badResponses));
        }

        Assert.Empty(consoleErrors);
        Assert.Empty(badResponses);
        Assert.Subset(loadedAssets, RequiredAssets);
        if (requestedRuntimes.Contains("aot", StringComparer.Ordinal))
        {
            Assert.Contains("zh-Hans/Bzs.Blazor.resources", loadedAssets);
        }
    }

    [Theory]
    [InlineData(
        "https://localhost/_content/Bzs.Blazor/bzs.blazor.css",
        "_content/Bzs.Blazor/bzs.blazor.css")]
    [InlineData(
        "https://localhost/_content/Bzs.Blazor/bzs.blazor.5fumpk2426.css",
        "_content/Bzs.Blazor/bzs.blazor.css")]
    [InlineData(
        "https://localhost/_content/Bzs.Blazor/Components/Theme/BzsThemeProvider.h77xcekwg4.razor.js",
        "Components/Theme/BzsThemeProvider.razor.js")]
    [InlineData(
        "https://localhost/aot/_framework/zh-Hans/Bzs.Blazor.resources.wasm",
        "zh-Hans/Bzs.Blazor.resources")]
    [InlineData(
        "https://localhost/aot/_framework/zh-Hans/Bzs.Blazor.resources.hgg1qq416c.wasm",
        "zh-Hans/Bzs.Blazor.resources")]
    public void RuntimeAssetMatchingAcceptsLogicalAndFingerprintedPaths(string url, string logicalAsset)
    {
        Assert.True(MatchesRuntimeAsset(url, logicalAsset));
    }

    [Theory]
    [InlineData(
        "https://localhost/_content/Bzs.Blazor/bzs.blazor.css.map",
        "_content/Bzs.Blazor/bzs.blazor.css")]
    [InlineData(
        "https://localhost/_content/Bzs.Blazor/Components/Theme/BzsThemeProvider.razor.js.map",
        "Components/Theme/BzsThemeProvider.razor.js")]
    [InlineData(
        "https://localhost/_content/Bzs.Blazor/NotComponents/Theme/BzsThemeProvider.razor.js",
        "Components/Theme/BzsThemeProvider.razor.js")]
    [InlineData(
        "https://localhost/_content/Bzs.Blazor/bzs.blazor.short.css",
        "_content/Bzs.Blazor/bzs.blazor.css")]
    [InlineData(
        "https://localhost/_content/Bzs.Blazor/bzs.blazor.invalid-id.css",
        "_content/Bzs.Blazor/bzs.blazor.css")]
    public void RuntimeAssetMatchingRejectsNeighboringAndMalformedPaths(string url, string logicalAsset)
    {
        Assert.False(MatchesRuntimeAsset(url, logicalAsset));
    }

    private static readonly HashSet<string> RequiredAssets = new(StringComparer.Ordinal)
    {
        "_content/Bzs.Blazor/bzs.blazor.css",
        "Components/Theme/BzsThemeProvider.razor.js",
        "Components/Tabs/BzsTabs.razor.js",
        "Components/Dialog/BzsDialog.razor.js",
        "Components/Form/BzsSelect.razor.js",
    };

    private static readonly IReadOnlyList<string> ObservedAssets =
        [.. RequiredAssets, "zh-Hans/Bzs.Blazor.resources"];

    private static IReadOnlyList<string> GetRequestedRuntimes()
    {
        var configured = Environment.GetEnvironmentVariable("BZS_PACKAGE_CONSUMER_RUNTIMES");
        return string.IsNullOrWhiteSpace(configured)
            ? ["server", "wasm", "auto", "aot"]
            : configured.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
    }

    private static bool MatchesRuntimeAsset(string url, string logicalAsset)
    {
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
        {
            return false;
        }

        var path = Uri.UnescapeDataString(uri.AbsolutePath);
        string extension;
        string stem;
        if (logicalAsset.EndsWith(".razor.js", StringComparison.Ordinal))
        {
            extension = ".razor.js";
            stem = logicalAsset[..^extension.Length];
        }
        else if (logicalAsset.EndsWith(".resources", StringComparison.Ordinal))
        {
            extension = ".wasm";
            stem = logicalAsset;
        }
        else
        {
            extension = Path.GetExtension(logicalAsset);
            stem = logicalAsset[..^extension.Length];
        }

        if (string.IsNullOrEmpty(extension) || !path.EndsWith(extension, StringComparison.Ordinal))
        {
            return false;
        }

        var expectedStemSuffix = $"/{stem}";
        var pathWithoutExtension = path[..^extension.Length];
        if (pathWithoutExtension.EndsWith(expectedStemSuffix, StringComparison.Ordinal))
        {
            return true;
        }

        const int fingerprintSegmentLength = 11;
        if (pathWithoutExtension.Length < expectedStemSuffix.Length + fingerprintSegmentLength)
        {
            return false;
        }

        var fingerprint = pathWithoutExtension[^fingerprintSegmentLength..];
        var pathBeforeFingerprint = pathWithoutExtension[..^fingerprintSegmentLength];
        return pathBeforeFingerprint.EndsWith(expectedStemSuffix, StringComparison.Ordinal)
            && fingerprint[0] == '.'
            && fingerprint[1..].All(char.IsAsciiLetterOrDigit);
    }

    private static async Task ExerciseRuntimeAsync(
        IPage page,
        string baseUrl,
        string runtime,
        string route)
    {
        var response = await page.GotoAsync($"{baseUrl}/{route}");
        Assert.True(response?.Ok ?? false, $"Could not load the {runtime} package consumer route.");
        await Expect(page.GetByTestId($"{runtime}-ready")).ToHaveTextAsync("Interactive package ready");
        var expectedCulture = runtime is "server" or "aot" ? "zh-Hans" : "en-US";
        await Expect(page.GetByTestId($"{runtime}-culture")).ToHaveTextAsync(expectedCulture);

        await page.GetByRole(AriaRole.Button, new() { Name = "Use dark theme" }).ClickAsync();
        await Expect(page.GetByTestId($"{runtime}-theme")).ToHaveAttributeAsync("data-bzs-theme", "dark");

        await page.GetByLabel("Reviewer count").FillAsync("4");
        await Expect(page.GetByLabel("Reviewer count")).ToHaveValueAsync("4");
        var stage = page.GetByRole(AriaRole.Combobox, new() { Name = "Stage" });
        await stage.ClickAsync();
        await page.GetByRole(AriaRole.Option, new() { Name = "Review" }).ClickAsync();
        await Expect(stage).ToContainTextAsync("Review");

        var tabs = page.GetByTestId($"{runtime}-tabs");
        await tabs.GetByRole(AriaRole.Tab, new() { Name = "Details" }).ClickAsync();
        await Expect(tabs.GetByRole(AriaRole.Tab, new() { Name = "Details" }))
            .ToHaveAttributeAsync("aria-selected", "true");

        await page.GetByTestId($"{runtime}-open-dialog").ClickAsync();
        var dialog = page.GetByRole(AriaRole.Dialog, new() { Name = "Package dialog" });
        await Expect(dialog).ToBeVisibleAsync();
        var closeLabel = runtime is "server" or "aot" ? "关闭对话框" : "Close dialog";
        await Expect(dialog.GetByRole(AriaRole.Button, new() { Name = closeLabel })).ToBeVisibleAsync();
        await dialog.GetByRole(AriaRole.Button, new() { Name = "Complete package dialog" }).ClickAsync();
        await Expect(dialog).ToHaveCountAsync(0);
        await Expect(page.GetByTestId($"{runtime}-dialog-result")).ToHaveTextAsync("Completed: true");
    }
}
