using System.Globalization;
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
        Assert.Contains("name=\"Form.ProjectName\"", staticHtml, StringComparison.Ordinal);
        Assert.Contains("name=\"Form.Targets\"", staticHtml, StringComparison.Ordinal);

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
        var unexpectedRequestFailures = new List<string>();
        var loadedAssets = new HashSet<string>(StringComparer.Ordinal);
        page.Console += (_, message) =>
        {
            if (string.Equals(message.Type, "error", StringComparison.OrdinalIgnoreCase))
            {
                consoleErrors.Add(message.Text);
            }
        };
        page.RequestFailed += (_, request) =>
        {
            if (!IsAllowedRequestFailure(request.Failure))
            {
                unexpectedRequestFailures.Add(
                    $"{request.Method} {request.Url}: {request.Failure ?? "request failed without a reported reason"}");
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
            await ExerciseStaticPostAsync(page, baseUrl!);
            foreach (var runtime in requestedRuntimes)
            {
                var route = runtime == "aot" ? "aot/index.html" : $"{runtime}-smoke";
                await ExerciseRuntimeAsync(page, baseUrl!, runtime, route);
            }

            if (requestedRuntimes.Contains("auto", StringComparer.Ordinal))
            {
                await ExercisePrerenderHandoffAsync(page, baseUrl!);
            }
        }
        catch (Exception exception)
        {
            Assert.Fail(
                $"{exception}{Environment.NewLine}Console errors:{Environment.NewLine}"
                + string.Join(Environment.NewLine, consoleErrors)
                + $"{Environment.NewLine}HTTP errors:{Environment.NewLine}"
                + string.Join(Environment.NewLine, badResponses)
                + $"{Environment.NewLine}Request failures:{Environment.NewLine}"
                + string.Join(Environment.NewLine, unexpectedRequestFailures));
        }

        Assert.Empty(consoleErrors);
        Assert.Empty(badResponses);
        Assert.Empty(unexpectedRequestFailures);
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

    [Theory]
    [InlineData("net::ERR_ABORTED", true)]
    [InlineData("NET::ERR_ABORTED", true)]
    [InlineData("net::ERR_FAILED", false)]
    [InlineData("", false)]
    [InlineData(null, false)]
    public void RequestFailureClassificationAllowsOnlyExplicitAborts(
        string? failure,
        bool expected)
    {
        Assert.Equal(expected, IsAllowedRequestFailure(failure));
    }

    private static readonly HashSet<string> RequiredAssets = new(StringComparer.Ordinal)
    {
        "_content/Bzs.Blazor/bzs.blazor.css",
        "Components/Theme/BzsThemeProvider.razor.js",
        "Components/Tabs/BzsTabs.razor.js",
        "Components/Dialog/BzsDialog.razor.js",
        "Components/Form/BzsAutocomplete.razor.js",
        "Components/Form/BzsDateInput.razor.js",
        "Components/Form/BzsSelect.razor.js",
        "Components/Popover/BzsPopover.razor.js",
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

    private static bool IsAllowedRequestFailure(string? failure) =>
        string.Equals(failure, "net::ERR_ABORTED", StringComparison.OrdinalIgnoreCase);

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

        var dateCulture = CultureInfo.GetCultureInfo(expectedCulture);
        var selectedDate = new DateOnly(2026, 7, 18);
        var releaseDate = page.GetByRole(
            AriaRole.Combobox,
            new() { Name = "Release date", Exact = true });
        await Expect(releaseDate).ToHaveValueAsync(
            new DateOnly(2026, 7, 17).ToString("d", dateCulture));
        await releaseDate.ClickAsync();
        await Expect(releaseDate).ToHaveAttributeAsync("aria-expanded", "true");
        await page.GetByRole(
                AriaRole.Gridcell,
                new() { Name = selectedDate.ToString("D", dateCulture), Exact = true })
            .ClickAsync();
        await Expect(releaseDate).ToHaveValueAsync(selectedDate.ToString("d", dateCulture));
        await Expect(page.GetByTestId($"{runtime}-date-value")).ToHaveTextAsync("2026-07-18");

        var stage = page.GetByRole(AriaRole.Combobox, new() { Name = "Stage" });
        await stage.ClickAsync();
        await page.GetByRole(AriaRole.Option, new() { Name = "Review" }).ClickAsync();
        await Expect(stage).ToContainTextAsync("Review");

        await page.GetByRole(AriaRole.Button, new() { Name = "Open package details" }).ClickAsync();
        var packageDetails = page.GetByRole(AriaRole.Dialog, new() { Name = "Package details" });
        await Expect(packageDetails).ToContainTextAsync("Anchored package content.");
        await page.Keyboard.PressAsync("Escape");
        await Expect(packageDetails).ToHaveCountAsync(0);

        var owner = page.GetByTestId($"{runtime}-autocomplete");
        await owner.FillAsync("Ali");
        await page.GetByRole(AriaRole.Option, new() { Name = "Alicia Santos" }).ClickAsync();
        await Expect(owner).ToHaveValueAsync("Alicia Santos");
        await Expect(page.GetByTestId($"{runtime}-owner-value")).ToHaveTextAsync("alicia");

        await page.GetByTestId($"{runtime}-upload").SetInputFilesAsync(new FilePayload
        {
            Name = "package-review.txt",
            MimeType = "text/plain",
            Buffer = "Package consumer upload"u8.ToArray(),
        });
        await Expect(page.GetByText("package-review.txt", new() { Exact = true })).ToBeVisibleAsync();

        var grid = page.GetByTestId($"{runtime}-grid");
        var table = grid.GetByRole(AriaRole.Table, new() { Name = "Package work items" });
        await Expect(table.Locator("tbody tr")).ToHaveCountAsync(3);
        await table.GetByRole(AriaRole.Button, new() { Name = "Work item", Exact = true }).ClickAsync();
        await Expect(table.Locator("th[aria-sort]"))
            .ToHaveAttributeAsync("aria-sort", "ascending");

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

    private static async Task ExerciseStaticPostAsync(IPage page, string baseUrl)
    {
        var response = await page.GotoAsync($"{baseUrl}/static-smoke");
        Assert.True(response?.Ok ?? false, "Could not load the Static SSR package consumer route.");

        await page.GetByTestId("static-project-name").FillAsync(string.Empty);
        await page.GetByTestId("static-reviewer-count").FillAsync("0");
        await page.RunAndWaitForResponseAsync(
            async () =>
            {
                await page.GetByTestId("static-post-form")
                    .EvaluateAsync("form => form.submit()");
            },
            response => response.Request.Method == "POST"
                && response.Url == $"{baseUrl}/static-smoke");
        await page.WaitForLoadStateAsync(LoadState.DOMContentLoaded);
        await Expect(page.GetByTestId("static-validation"))
            .ToContainTextAsync("Project name is required.");
        await Expect(page.GetByTestId("static-validation"))
            .ToContainTextAsync("Reviewer count must be between 1 and 20.");

        await page.GetByTestId("static-project-name").FillAsync("Aurora");
        await page.GetByTestId("static-reviewer-count").FillAsync("4");
        await page.GetByTestId("static-due-date").FillAsync("2026-08-07");
        await page.GetByLabel("Notify owners", new() { Exact = true }).UncheckAsync();
        await page.Locator("input[name='Form.Priority'][value='high']")
            .CheckAsync(new() { Force = true });
        await page.GetByLabel("Stage", new() { Exact = true }).SelectOptionAsync("review");
        await page.GetByLabel("Targets", new() { Exact = true })
            .SelectOptionAsync(["accessibility", "aot"]);
        await page.GetByTestId("static-submit").ClickAsync();

        await Expect(page.GetByTestId("static-post-result")).ToHaveTextAsync(
            "Submitted | Aurora | 4 | 2026-08-07 | high | review | accessibility,aot | notify=false");
    }

    private static async Task ExercisePrerenderHandoffAsync(IPage page, string baseUrl)
    {
        const string frameworkScriptPattern = "**/_framework/blazor.web*.js";
        var scriptRequested = new TaskCompletionSource<bool>(
            TaskCreationOptions.RunContinuationsAsynchronously);
        var releaseScript = new TaskCompletionSource<bool>(
            TaskCreationOptions.RunContinuationsAsynchronously);

        await page.RouteAsync(frameworkScriptPattern, async route =>
        {
            scriptRequested.TrySetResult(true);
            await releaseScript.Task;
            await route.ContinueAsync();
        });

        Task<IResponse?>? navigation = null;
        try
        {
            navigation = page.GotoAsync($"{baseUrl}/auto-smoke");
            await scriptRequested.Task.WaitAsync(TimeSpan.FromSeconds(30));

            var prerender = await CaptureRelationshipsAsync(page, "auto");
            AssertRelationshipsResolve(prerender);
            await page.GetByTestId("auto-number").FocusAsync();
            await Expect(page.GetByTestId("auto-number")).ToBeFocusedAsync();

            releaseScript.TrySetResult(true);
            var response = await navigation;
            Assert.True(response?.Ok ?? false, "Could not activate the Auto package consumer route.");
            await Expect(page.GetByTestId("auto-ready")).ToHaveTextAsync("Interactive package ready");

            var interactive = await CaptureRelationshipsAsync(page, "auto");
            AssertRelationshipsResolve(interactive);
            await page.GetByTestId("auto-number").FocusAsync();
            await Expect(page.GetByTestId("auto-number")).ToBeFocusedAsync();
        }
        finally
        {
            releaseScript.TrySetResult(true);
            if (navigation is not null)
            {
                try
                {
                    await navigation;
                }
                catch
                {
                    // Preserve the primary assertion or navigation failure.
                }
            }
            await page.UnrouteAsync(frameworkScriptPattern);
        }
    }

    private static Task<RelationshipSnapshot> CaptureRelationshipsAsync(IPage page, string runtime) =>
        page.EvaluateAsync<RelationshipSnapshot>(
            """
            runtime => {
                const number = document.querySelector(`[data-testid='${runtime}-number']`);
                const date = document.querySelector(`[data-testid='${runtime}-date']`);
                const tab = document.querySelector(`[data-testid='${runtime}-tabs'] [role='tab']`);
                const panel = tab ? document.getElementById(tab.getAttribute('aria-controls')) : null;
                const numberLabel = number?.id
                    ? document.querySelector(`label[for='${CSS.escape(number.id)}']`)
                    : null;
                const dateLabel = date?.id
                    ? document.querySelector(`label[for='${CSS.escape(date.id)}']`)
                    : null;
                return {
                    NumberInputId: number?.id ?? null,
                    NumberLabelFor: numberLabel?.getAttribute('for') ?? null,
                    DateInputId: date?.id ?? null,
                    DateLabelFor: dateLabel?.getAttribute('for') ?? null,
                    TabId: tab?.id ?? null,
                    TabControls: tab?.getAttribute('aria-controls') ?? null,
                    PanelId: panel?.id ?? null,
                    PanelLabelledBy: panel?.getAttribute('aria-labelledby') ?? null,
                };
            }
            """,
            runtime);

    private static void AssertRelationshipsResolve(RelationshipSnapshot snapshot)
    {
        Assert.False(string.IsNullOrWhiteSpace(snapshot.NumberInputId));
        Assert.Equal(snapshot.NumberInputId, snapshot.NumberLabelFor);
        Assert.False(string.IsNullOrWhiteSpace(snapshot.DateInputId));
        Assert.Equal(snapshot.DateInputId, snapshot.DateLabelFor);
        Assert.False(string.IsNullOrWhiteSpace(snapshot.TabId));
        Assert.Equal(snapshot.TabControls, snapshot.PanelId);
        Assert.Equal(snapshot.TabId, snapshot.PanelLabelledBy);
    }

    private sealed class RelationshipSnapshot
    {
        public RelationshipSnapshot()
        {
        }

        public string? NumberInputId { get; set; }

        public string? NumberLabelFor { get; set; }

        public string? DateInputId { get; set; }

        public string? DateLabelFor { get; set; }

        public string? TabId { get; set; }

        public string? TabControls { get; set; }

        public string? PanelId { get; set; }

        public string? PanelLabelledBy { get; set; }
    }
}
