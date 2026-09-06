using System.Text.RegularExpressions;

namespace Bzs.Blazor.Tests;

/// <summary>
/// Locks where each demo route is declared (ADR-0040). Shared URLs are declared
/// once in the Catalog; the hosts declare only routes that carry host-specific
/// behavior. Each host's router merges its scan set into one routing table, so a
/// duplicate template inside one host's scan set would throw an
/// ambiguous-route exception at startup — the disjointness assertions keep that
/// from shipping.
/// </summary>
public sealed class DemoRouteOwnershipTests
{
    private const string CatalogRoutesFolder = "samples/Bzs.Blazor.Demo/Bzs.Blazor.Demo.Catalog/Routes";
    private const string CatalogPagesFolder = "samples/Bzs.Blazor.Demo/Bzs.Blazor.Demo.Catalog/Pages";
    private const string ClientRoutesFolder = "samples/Bzs.Blazor.Demo/Bzs.Blazor.Demo.Client/Routes";
    private const string ClientPagesFolder = "samples/Bzs.Blazor.Demo/Bzs.Blazor.Demo.Client/Pages";
    private const string ServerComponentsFolder = "samples/Bzs.Blazor.Demo/Bzs.Blazor.Demo/Components";
    private const string WebAssemblyRoutesFolder = "samples/Bzs.Blazor.Demo/Bzs.Blazor.Demo.WebAssembly/Routes";
    private const string WebAssemblyPagesFolder = "samples/Bzs.Blazor.Demo/Bzs.Blazor.Demo.WebAssembly/Pages";

    private static readonly Regex PageDirectivePattern = new(
        @"^@page\s+""(?<template>[^""]+)""\s*$",
        RegexOptions.CultureInvariant);

    private static readonly Regex RenderModePattern = new(
        @"^@rendermode\s",
        RegexOptions.Multiline | RegexOptions.CultureInvariant);

    /// <summary>
    /// Every razor folder the server host's router scans: its own assembly plus
    /// the client and Catalog assemblies through <c>AdditionalAssemblies</c>.
    /// </summary>
    private static readonly (string Folder, string Label)[] ServerScanSet =
    [
        (ServerComponentsFolder, "the server host"),
        (CatalogRoutesFolder, "the Catalog Routes folder"),
        (CatalogPagesFolder, "the Catalog Pages folder"),
        (ClientRoutesFolder, "the client Routes folder"),
        (ClientPagesFolder, "the client Pages folder"),
    ];

    /// <summary>
    /// Every razor folder the standalone WebAssembly host's router scans: its
    /// own assembly plus the Catalog assembly through <c>AdditionalAssemblies</c>.
    /// </summary>
    private static readonly (string Folder, string Label)[] WebAssemblyScanSet =
    [
        (CatalogRoutesFolder, "the Catalog Routes folder"),
        (CatalogPagesFolder, "the Catalog Pages folder"),
        (WebAssemblyRoutesFolder, "the WebAssembly Routes folder"),
        (WebAssemblyPagesFolder, "the WebAssembly Pages folder"),
    ];

    /// <summary>The routes both hosts serve; declared once in the Catalog (ADR-0040).</summary>
    private static readonly string[] CatalogSharedRoutes =
    [
        "/feedback",
        "/forms",
        "/foundation",
        "/layout",
        "/navigation-drawer",
        "/overlays",
        "/productivity",
        "/productivity/webassembly",
        "/releases",
        "/render-modes/webassembly",
        "/tabs",
        "/theme-foundation",
    ];

    /// <summary>The routes only the client host declares; each carries host-specific behavior.</summary>
    private static readonly string[] ClientHostOwnedRoutes =
    [
        "/",
        "/productivity/auto",
        "/test/navigation-drawer",
    ];

    private static readonly string[] WebAssemblyHostOwnedRoutes = ["/"];

    [Fact]
    public void CatalogRoutesFolderDeclaresExactlyTheSharedRoutes() => AssertExactTemplates(
        CatalogRoutesFolder,
        CatalogSharedRoutes,
        "The Catalog owns exactly the routes both hosts serve; declare a new shared route here once.");

    [Fact]
    public void ClientRoutesFolderDeclaresExactlyTheHostOwnedRoutes() => AssertExactTemplates(
        ClientRoutesFolder,
        ClientHostOwnedRoutes,
        "The client host owns only the home route (host-supplied IncludesServerRenderModes), the " +
        "server-only navigation-drawer lifecycle harness, and the productivity auto variant.");

    [Fact]
    public void WebAssemblyRoutesFolderDeclaresExactlyTheHomeRoute() => AssertExactTemplates(
        WebAssemblyRoutesFolder,
        WebAssemblyHostOwnedRoutes,
        "The standalone WebAssembly host owns only the home route; shared routes come from the Catalog.");

    [Fact]
    public void ServerHostScanSetDeclaresNoRouteTemplateTwice() => AssertNoDuplicateTemplates(
        ServerScanSet,
        "the server host's router scan set");

    [Fact]
    public void WebAssemblyHostScanSetDeclaresNoRouteTemplateTwice() => AssertNoDuplicateTemplates(
        WebAssemblyScanSet,
        "the WebAssembly host's router scan set");

    [Fact]
    public void WebAssemblyWrappersNeverDeclareRenderModes()
    {
        foreach (var wrapper in Directory.EnumerateFiles(FindRepositoryFolder(WebAssemblyRoutesFolder), "*.razor"))
        {
            Assert.True(
                !RenderModePattern.IsMatch(File.ReadAllText(wrapper)),
                $"{Path.GetFileName(wrapper)} declares @rendermode, but the standalone WebAssembly host " +
                "already runs entirely in WebAssembly and its AGENTS.md forbids render-mode directives.");
        }
    }

    [Fact]
    public void ServerEndpointRoutingRegistersEveryAssemblyThatDeclaresRoutes()
    {
        var program = File.ReadAllText(FindRepositoryFile(
            "samples", "Bzs.Blazor.Demo", "Bzs.Blazor.Demo", "Program.cs"));
        Assert.True(
            program.Contains("AddAdditionalAssemblies(", StringComparison.Ordinal)
            && program.Contains("Bzs.Blazor.Demo.Client._Imports", StringComparison.Ordinal)
            && program.Contains("Bzs.Blazor.Demo.Catalog._Imports", StringComparison.Ordinal),
            "MapRazorComponents in Program.cs must register both the client and Catalog assemblies " +
            "through AddAdditionalAssemblies; a missing assembly makes its @page components 404 on " +
            "the server even though the Router component scans them for client-side navigation.");
    }

    private static void AssertExactTemplates(string folder, string[] expected, string rationale)
    {
        var actual = ParseTemplates(folder).Order().ToArray();
        Assert.True(
            expected.Order().SequenceEqual(actual),
            $"{folder} must declare exactly {string.Join(", ", expected.Order())}, but declares " +
            $"{string.Join(", ", actual)}. {rationale}");
    }

    private static void AssertNoDuplicateTemplates((string Folder, string Label)[] scanSet, string scanSetName)
    {
        var declarations = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var (folder, label) in scanSet)
        {
            foreach (var template in ParseTemplates(folder))
            {
                Assert.True(
                    declarations.TryAdd(template, label),
                    $"The {template} route is declared in both {declarations[template]} and {label} inside " +
                    $"{scanSetName}; the router would throw an ambiguous-route exception.");
            }
        }
    }

    private static string[] ParseTemplates(string folder) =>
        [.. Directory
            .EnumerateFiles(FindRepositoryFolder(folder), "*.razor", SearchOption.AllDirectories)
            .SelectMany(File.ReadAllLines)
            .Select(line => PageDirectivePattern.Match(line))
            .Where(match => match.Success)
            .Select(match => match.Groups["template"].Value)];

    private static string FindRepositoryFolder(string relativePath)
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "Bzs.Blazor.slnx")))
            {
                return Path.Combine(directory.FullName, relativePath.Replace('/', Path.DirectorySeparatorChar));
            }

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException("Could not locate the Bzs.Blazor repository root.");
    }

    private static string FindRepositoryFile(params string[] segments) =>
        FindRepositoryFolder(Path.Combine([.. segments]));
}
