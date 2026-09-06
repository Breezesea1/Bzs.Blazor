using System.Text.RegularExpressions;

namespace Bzs.Blazor.Tests;

/// <summary>
/// Locks the per-URL route wrappers the two demo hosts declare against silent
/// drift. The wrapper folders are deliberately duplicated — the standalone
/// WebAssembly host cannot scan the client assembly — so parity between them is
/// the contract, and a one-sided route addition or rename fails here.
/// </summary>
public sealed class DemoRouteParityTests
{
    private const string ClientRoutesFolder = "samples/Bzs.Blazor.Demo/Bzs.Blazor.Demo.Client/Routes";
    private const string WebAssemblyRoutesFolder = "samples/Bzs.Blazor.Demo/Bzs.Blazor.Demo.WebAssembly/Routes";

    private static readonly Regex PageDirectivePattern = new(
        @"^@page\s+""(?<template>[^""]+)""\s*$",
        RegexOptions.Multiline | RegexOptions.CultureInvariant);

    private static readonly Regex RenderModePattern = new(
        @"^@rendermode\s",
        RegexOptions.Multiline | RegexOptions.CultureInvariant);

    private static readonly Regex ComponentPattern = new(
        @"<(?<component>Bzs\.Blazor\.Demo(?:\.[A-Za-z0-9_]+)+)",
        RegexOptions.CultureInvariant);

    /// <summary>The routes only the client host declares; each needs a host-specific reason.</summary>
    private static readonly string[] ClientOnlyRoutes =
    [
        "/productivity/auto",
        "/test/navigation-drawer",
    ];

    [Fact]
    public void WebAssemblyRoutesNeverExceedTheClientRoutes()
    {
        var client = ParseWrappers(ClientRoutesFolder);
        var webAssembly = ParseWrappers(WebAssemblyRoutesFolder);

        var orphans = GetRoutes(webAssembly)
            .Except(GetRoutes(client))
            .Order()
            .ToArray();
        Assert.True(
            orphans.Length == 0,
            $"The WebAssembly host declares routes the client host does not: {string.Join(", ", orphans)}. " +
            "Declare the route in both Routes/ folders or remove it from the WebAssembly host.");
    }

    [Fact]
    public void ClientOnlyRoutesAreTheDocumentedHostOwnedOnes()
    {
        var client = ParseWrappers(ClientRoutesFolder);
        var webAssembly = ParseWrappers(WebAssemblyRoutesFolder);

        var clientOnly = GetRoutes(client)
            .Except(GetRoutes(webAssembly))
            .Order()
            .ToArray();
        Assert.True(
            ClientOnlyRoutes.SequenceEqual(clientOnly),
            $"The client host owns exactly {string.Join(", ", ClientOnlyRoutes)}. " +
            $"It now also owns {string.Join(", ", clientOnly.Except(ClientOnlyRoutes))} and has lost " +
            $"{string.Join(", ", ClientOnlyRoutes.Except(clientOnly))}. Add a deliberate exception entry or " +
            "mirror the route into the WebAssembly Routes/ folder.");
    }

    [Fact]
    public void SharedRoutesRenderTheSameCatalogPageOnBothHosts()
    {
        var client = GetRouteComponents(ParseWrappers(ClientRoutesFolder));
        var webAssembly = GetRouteComponents(ParseWrappers(WebAssemblyRoutesFolder));

        foreach (var (template, webAssemblyComponent) in webAssembly)
        {
            var clientComponent = client[template];
            Assert.True(
                string.Equals(clientComponent, webAssemblyComponent, StringComparison.Ordinal),
                $"The {template} wrappers render different pages: the client host renders " +
                $"{clientComponent} and the WebAssembly host renders {webAssemblyComponent}.");
        }
    }

    [Fact]
    public void WebAssemblyWrappersNeverDeclareRenderModes()
    {
        foreach (var wrapper in ParseWrappers(WebAssemblyRoutesFolder))
        {
            Assert.True(
                !wrapper.DeclaresRenderMode,
                $"{wrapper.RelativePath} declares @rendermode, but the standalone WebAssembly host " +
                "already runs entirely in WebAssembly and its AGENTS.md forbids render-mode directives.");
        }
    }

    private static RouteWrapper[] ParseWrappers(string folder)
    {
        var folderPath = FindRepositoryFolder(folder);
        return Directory
            .EnumerateFiles(folderPath, "*.razor")
            .Order(StringComparer.Ordinal)
            .Select(path => ParseWrapper(folderPath, path))
            .ToArray();

        static RouteWrapper ParseWrapper(string folderPath, string path)
        {
            var markup = File.ReadAllText(path);
            return new RouteWrapper(
                Path.GetRelativePath(folderPath, path),
                [.. PageDirectivePattern.Matches(markup).Select(match => match.Groups["template"].Value)],
                ComponentPattern.Match(markup).Groups["component"].Value,
                RenderModePattern.IsMatch(markup));
        }
    }

    private static string[] GetRoutes(RouteWrapper[] wrappers) =>
        [.. wrappers.SelectMany(wrapper => wrapper.Templates)];

    private static Dictionary<string, string> GetRouteComponents(RouteWrapper[] wrappers)
    {
        var components = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var wrapper in wrappers)
        {
            foreach (var template in wrapper.Templates)
            {
                Assert.True(
                    components.TryAdd(template, wrapper.Component),
                    $"The {template} route is declared more than once on one host.");
            }
        }

        return components;
    }

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

    private sealed record RouteWrapper(
        string RelativePath,
        string[] Templates,
        string Component,
        bool DeclaresRenderMode);
}
