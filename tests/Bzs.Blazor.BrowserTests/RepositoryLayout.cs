namespace Bzs.Blazor.BrowserTests;

/// <summary>Locates the repository root from a test assembly's output directory.</summary>
internal static class RepositoryLayout
{
    private const string SolutionFileName = "Bzs.Blazor.slnx";

    internal static string Root { get; } = FindRoot();

    /// <summary>Gets the directory a browser gate writes failure artifacts to.</summary>
    internal static string GetBrowserGateArtifactDirectory(string testName) => Path.Combine(
        Root,
        "TestResults",
        "browser-gates",
        SanitizePathSegment(testName));

    internal static string SanitizePathSegment(string value)
    {
        var invalidCharacters = Path.GetInvalidFileNameChars();
        return string.Concat(value.Select(character =>
            invalidCharacters.Contains(character) ? '_' : character));
    }

    private static string FindRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, SolutionFileName)))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException("Could not locate the Bzs.Blazor repository root.");
    }
}
