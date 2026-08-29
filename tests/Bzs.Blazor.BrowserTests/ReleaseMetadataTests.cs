using System.Xml.Linq;
using Bzs.Blazor.Demo.Client;

namespace Bzs.Blazor.BrowserTests;

/// <summary>
/// Pins the three places a release version has to agree: the package the publish workflow validates
/// against the tag, the catalog entry the Demo announces and the install snippet quotes, and the
/// release notes the package points readers at.
/// </summary>
public sealed class ReleaseMetadataTests
{
    [Fact]
    public void ThePackageVersionMatchesTheNewestAnnouncedRelease()
    {
        var project = XDocument.Load(
            Path.Combine(RepositoryLayout.Root, "src", "Bzs.Blazor", "Bzs.Blazor.csproj"));
        var version = project.Descendants("Version").Single().Value.Trim();

        Assert.Equal(version, DemoReleaseCatalog.Latest.Version);
        Assert.Equal($"v{version}", DemoReleaseCatalog.Latest.Id);
    }

    [Fact]
    public void EveryAnnouncedReleaseHasNotesInTheRepository()
    {
        var releaseDirectory = Path.Combine(RepositoryLayout.Root, "docs", "releases");

        foreach (var release in DemoReleaseCatalog.All)
        {
            var notesPath = Path.Combine(releaseDirectory, $"{release.Version}.md");
            Assert.True(File.Exists(notesPath), $"Missing release notes {notesPath}.");
        }
    }

    [Fact]
    public void ThePackageReleaseNotesPointAtTheCurrentVersionsNotes()
    {
        var project = XDocument.Load(
            Path.Combine(RepositoryLayout.Root, "src", "Bzs.Blazor", "Bzs.Blazor.csproj"));
        var version = project.Descendants("Version").Single().Value.Trim();
        var notes = project.Descendants("PackageReleaseNotes").Single().Value;

        Assert.Contains($"docs/releases/{version}.md", notes, StringComparison.Ordinal);
    }
}
