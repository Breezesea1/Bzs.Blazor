using System.Xml.Linq;

namespace Bzs.Blazor.BrowserTests;

/// <summary>
/// Pins the Linux visual-baseline environment (ADR-0029) to the repository, not to
/// mutable distribution defaults. The committed baselines are only reproducible while
/// the generic fontconfig families stay pinned, so this locks the fontconfig file and
/// its application across every workflow that captures or compares a baseline.
/// </summary>
public sealed class VisualBaselineEnvironmentTests
{
    private const string FontconfigRelativePath = ".github/fontconfig/99-bzs-visual-baselines.conf";
    private const string InstallCommand =
        "sudo install -m 0644 .github/fontconfig/99-bzs-visual-baselines.conf " +
        "/etc/fonts/conf.d/99-bzs-visual-baselines.conf";

    [Fact]
    public void TheVisualBaselineFontconfigPinsMonospaceToTheInstalledFonts()
    {
        var fontconfig = LoadFontconfig();
        var preferred = PreferredFamilies(fontconfig, "monospace");

        Assert.Equal(["Noto Mono", "Noto Sans Mono", "Liberation Mono"], preferred);
    }

    [Fact]
    public void TheVisualBaselineFontconfigOnlyPinsMonospace()
    {
        var fontconfig = LoadFontconfig();
        var pinnedGenericFamilies = fontconfig
            .Descendants("alias")
            .Select(alias => alias.Element("family")?.Value)
            .OfType<string>()
            .ToArray();

        // sans-serif, system-ui, and ui-sans-serif are deliberately left to the
        // distribution defaults: they already resolve to the installed Noto families
        // and the CJK fallback, and re-declaring them measurably shifts glyph
        // selection. Only monospace needs a repository-owned pin.
        Assert.Equal(["monospace"], pinnedGenericFamilies);
    }

    [Fact]
    public void EveryBaselineWorkflowInstallsThePinnedFontsAndTheFontconfig()
    {
        foreach (var workflow in new[]
        {
            ".github/workflows/ci.yml",
            ".github/workflows/publish-nuget.yml",
            ".github/workflows/refresh-visual-baselines.yml",
        })
        {
            var text = File.ReadAllText(Path.Combine(RepositoryLayout.Root, Split(workflow)));

            Assert.Contains("fonts-liberation fonts-noto-core fonts-noto-cjk", text, StringComparison.Ordinal);
            Assert.Contains(InstallCommand, text, StringComparison.Ordinal);
        }
    }

    private static XDocument LoadFontconfig()
    {
        var path = Path.Combine(RepositoryLayout.Root, Split(FontconfigRelativePath));
        Assert.True(File.Exists(path), $"Missing visual-baseline fontconfig at {path}.");
        return XDocument.Load(path);
    }

    private static string[] PreferredFamilies(XDocument fontconfig, string genericFamily) =>
        fontconfig
            .Descendants("alias")
            .Where(alias => (string?)alias.Element("family") == genericFamily)
            .SelectMany(alias => alias.Element("prefer")?.Elements("family") ?? [])
            .Select(family => family.Value)
            .ToArray();

    private static string Split(string relativePath) =>
        Path.Combine(relativePath.Split('/'));
}
