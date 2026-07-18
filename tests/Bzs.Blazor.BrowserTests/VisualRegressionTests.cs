using System.Security.Cryptography;
using Microsoft.Playwright;
using Microsoft.Playwright.Xunit;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace Bzs.Blazor.BrowserTests;

[Collection(DemoCollection.Name)]
public sealed class VisualRegressionTests(DemoServerFixture server) : BrowserGatePageTest
{
    [Fact]
    public async Task FoundationLightDesktopMatchesBaseline()
    {
        BeginBrowserGateTest();
        await Page.SetViewportSizeAsync(1440, 900);
        await Page.GotoAsync($"{server.BaseUrl}/foundation");
        await Expect(Page.GetByRole(AriaRole.Status)).ToContainTextAsync("Interactive runtime ready");

        await AssertMatchesBaselineAsync("foundation-light-desktop.png");
    }

    [Fact]
    public async Task FoundationDarkDesktopMatchesBaseline()
    {
        BeginBrowserGateTest();
        await Page.SetViewportSizeAsync(1440, 900);
        await Page.GotoAsync($"{server.BaseUrl}/foundation");
        await Expect(Page.GetByRole(AriaRole.Status)).ToContainTextAsync("Interactive runtime ready");
        await Page.GetByRole(AriaRole.Button, new() { Name = "Dark" }).ClickAsync();

        await AssertMatchesBaselineAsync("foundation-dark-desktop.png");
    }

    [Fact]
    public async Task TabsLightMobileMatchesBaseline()
    {
        BeginBrowserGateTest();
        await Page.SetViewportSizeAsync(390, 844);
        await Page.GotoAsync($"{server.BaseUrl}/tabs");
        await Expect(Page.GetByTestId("tabs-runtime-status")).ToHaveTextAsync("Interactive runtime ready");

        await AssertMatchesBaselineAsync("tabs-light-mobile.png");
    }

    [Fact]
    public async Task AutoCatalogDarkMobileMatchesBaseline()
    {
        BeginBrowserGateTest();
        await Page.SetViewportSizeAsync(390, 844);
        await Page.GotoAsync($"{server.BaseUrl}/render-modes/auto");
        await Expect(Page.GetByTestId("render-mode-auto-runtime-readiness"))
            .ToHaveTextAsync("Interactive runtime ready");
        await Page.GetByRole(AriaRole.Button, new() { Name = "Dark" }).ClickAsync();

        await AssertMatchesBaselineAsync("auto-dark-mobile.png");
    }

    private async Task AssertMatchesBaselineAsync(string fileName)
    {
        await Page.EvaluateAsync("() => document.activeElement instanceof HTMLElement && document.activeElement.blur()");
        var repositoryRoot = FindRepositoryRoot();
        var baselineDirectory = Path.Combine(
            repositoryRoot,
            "tests",
            "Bzs.Blazor.BrowserTests",
            "VisualBaselines");
        var baselinePath = Path.Combine(baselineDirectory, fileName);
        var actual = await Page.ScreenshotAsync(new PageScreenshotOptions
        {
            FullPage = true,
            Animations = ScreenshotAnimations.Disabled,
            Caret = ScreenshotCaret.Hide,
            Scale = ScreenshotScale.Css,
        });

        if (string.Equals(
            Environment.GetEnvironmentVariable("BZS_UPDATE_VISUAL_BASELINES"),
            "1",
            StringComparison.Ordinal))
        {
            Directory.CreateDirectory(baselineDirectory);
            await File.WriteAllBytesAsync(baselinePath, actual);
            return;
        }

        Assert.True(
            File.Exists(baselinePath),
            $"Missing visual baseline {baselinePath}. Run with BZS_UPDATE_VISUAL_BASELINES=1 to approve it.");
        var expected = await File.ReadAllBytesAsync(baselinePath);
        var comparison = ComparePixels(expected, actual);
        if (comparison.IsMatch)
        {
            return;
        }

        var actualDirectory = Path.Combine(repositoryRoot, "TestResults", "visual-regression");
        Directory.CreateDirectory(actualDirectory);
        var actualPath = Path.Combine(actualDirectory, fileName);
        await File.WriteAllBytesAsync(actualPath, actual);

        Assert.Fail(
            $"Visual baseline mismatch for {fileName}. "
            + $"Expected SHA256 {Convert.ToHexString(SHA256.HashData(expected))}, "
            + $"actual SHA256 {Convert.ToHexString(SHA256.HashData(actual))}. "
            + $"Different pixels: {comparison.DifferentPixels} of {comparison.TotalPixels}; "
            + $"maximum channel delta: {comparison.MaximumChannelDelta}. "
            + $"Actual screenshot: {actualPath}");
    }

    private static PixelComparison ComparePixels(byte[] expected, byte[] actual)
    {
        using var expectedImage = Image.Load<Rgba32>(expected);
        using var actualImage = Image.Load<Rgba32>(actual);
        if (expectedImage.Width != actualImage.Width || expectedImage.Height != actualImage.Height)
        {
            return new PixelComparison(false, int.MaxValue, 0, 0);
        }

        const int channelTolerance = 8;
        const double maximumDifferentPixelRatio = 0.005;
        var differentPixels = 0;
        var maximumChannelDelta = 0;
        var totalPixels = expectedImage.Width * expectedImage.Height;
        for (var y = 0; y < expectedImage.Height; y++)
        {
            for (var x = 0; x < expectedImage.Width; x++)
            {
                var expectedPixel = expectedImage[x, y];
                var actualPixel = actualImage[x, y];
                var delta = Math.Max(
                    Math.Max(Math.Abs(expectedPixel.R - actualPixel.R), Math.Abs(expectedPixel.G - actualPixel.G)),
                    Math.Max(Math.Abs(expectedPixel.B - actualPixel.B), Math.Abs(expectedPixel.A - actualPixel.A)));
                maximumChannelDelta = Math.Max(maximumChannelDelta, delta);
                if (delta > channelTolerance)
                {
                    differentPixels++;
                }
            }
        }

        return new PixelComparison(
            differentPixels <= totalPixels * maximumDifferentPixelRatio,
            differentPixels,
            totalPixels,
            maximumChannelDelta);
    }

    private readonly record struct PixelComparison(
        bool IsMatch,
        int DifferentPixels,
        int TotalPixels,
        int MaximumChannelDelta);

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "Bzs.Blazor.slnx")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException("Could not locate the Bzs.Blazor repository root.");
    }
}
