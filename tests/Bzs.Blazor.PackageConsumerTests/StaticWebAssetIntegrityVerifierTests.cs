using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace Bzs.Blazor.PackageConsumerTests;

public sealed class StaticWebAssetIntegrityVerifierTests
{
    [Fact]
    public void AcceptsMatchingPackageAssetsAndIgnoresCompressedEndpoints()
    {
        using var workspace = new TestWorkspace();
        var css = Encoding.UTF8.GetBytes("body { color: black; }");
        var script = Encoding.UTF8.GetBytes("export function open() {}");
        var packagePath = workspace.CreatePackage(
            ("staticwebassets/bzs.blazor.css", css),
            ("staticwebassets/Components/Dialog/BzsDialog.razor.js", script));
        var manifestPath = workspace.CreateManifest(
            Endpoint("_content/Bzs.Blazor/bzs.blazor.css", Integrity(css)),
            Endpoint(
                "_content/Bzs.Blazor/Components/Dialog/BzsDialog.razor.js",
                Integrity(script),
                route: "_content/Bzs.Blazor/Components/Dialog/BzsDialog.abc1234567.razor.js"),
            Endpoint(
                "_content/Bzs.Blazor/bzs.blazor.css.gz",
                "sha256-not-the-package-hash",
                contentEncoding: "gzip"));

        StaticWebAssetIntegrityVerifier.Verify(packagePath, manifestPath);
    }

    [Fact]
    public void IgnoresContentEncodedEndpointsThatReferenceLogicalAssetFiles()
    {
        using var workspace = new TestWorkspace();
        var css = Encoding.UTF8.GetBytes("body { color: black; }");
        var packagePath = workspace.CreatePackage(("staticwebassets/bzs.blazor.css", css));
        var manifestPath = workspace.CreateManifest(
            Endpoint("_content/Bzs.Blazor/bzs.blazor.css", Integrity(css)),
            Endpoint(
                "_content/Bzs.Blazor/bzs.blazor.css",
                "sha256-selector-compressed",
                route: "_content/Bzs.Blazor/bzs.blazor.selector.css",
                selectorContentEncoding: "br"),
            Endpoint(
                "_content/Bzs.Blazor/bzs.blazor.css",
                "sha256-header-compressed",
                route: "_content/Bzs.Blazor/bzs.blazor.header.css",
                responseHeaderContentEncoding: "gzip"));

        StaticWebAssetIntegrityVerifier.Verify(packagePath, manifestPath);
    }

    [Fact]
    public void RejectsPackageAssetWithoutEndpointMetadata()
    {
        using var workspace = new TestWorkspace();
        var packagePath = workspace.CreatePackage(
            ("staticwebassets/bzs.blazor.css", Encoding.UTF8.GetBytes("body {}")));
        var manifestPath = workspace.CreateManifest();

        var exception = Assert.Throws<InvalidDataException>(
            () => StaticWebAssetIntegrityVerifier.Verify(packagePath, manifestPath));

        Assert.Contains("has no uncompressed endpoint integrity metadata", exception.Message);
    }

    [Fact]
    public void RejectsMetadataForMissingPackageAsset()
    {
        using var workspace = new TestWorkspace();
        var css = Encoding.UTF8.GetBytes("body {}");
        var packagePath = workspace.CreatePackage(("staticwebassets/bzs.blazor.css", css));
        var manifestPath = workspace.CreateManifest(
            Endpoint("_content/Bzs.Blazor/bzs.blazor.css", Integrity(css)),
            Endpoint("_content/Bzs.Blazor/missing.js", Integrity(Encoding.UTF8.GetBytes("missing"))));

        var exception = Assert.Throws<InvalidDataException>(
            () => StaticWebAssetIntegrityVerifier.Verify(packagePath, manifestPath));

        Assert.Contains("references missing package entry", exception.Message);
    }

    [Fact]
    public void RejectsMissingIntegrityMetadata()
    {
        using var workspace = new TestWorkspace();
        var css = Encoding.UTF8.GetBytes("body {}");
        var packagePath = workspace.CreatePackage(("staticwebassets/bzs.blazor.css", css));
        var manifestPath = workspace.CreateManifest(
            Endpoint("_content/Bzs.Blazor/bzs.blazor.css", integrity: null));

        var exception = Assert.Throws<InvalidDataException>(
            () => StaticWebAssetIntegrityVerifier.Verify(packagePath, manifestPath));

        Assert.Contains("must declare exactly one integrity value", exception.Message);
    }

    [Fact]
    public void RejectsIntegrityThatDoesNotMatchPackageBytes()
    {
        using var workspace = new TestWorkspace();
        var css = Encoding.UTF8.GetBytes("body {}");
        var packagePath = workspace.CreatePackage(("staticwebassets/bzs.blazor.css", css));
        var manifestPath = workspace.CreateManifest(
            Endpoint("_content/Bzs.Blazor/bzs.blazor.css", "sha256-invalid"));

        var exception = Assert.Throws<InvalidDataException>(
            () => StaticWebAssetIntegrityVerifier.Verify(packagePath, manifestPath));

        Assert.Contains("does not match endpoint integrity metadata", exception.Message);
    }

    [Fact]
    public void RejectsConflictingIntegrityValuesForOnePackageAsset()
    {
        using var workspace = new TestWorkspace();
        var css = Encoding.UTF8.GetBytes("body {}");
        var packagePath = workspace.CreatePackage(("staticwebassets/bzs.blazor.css", css));
        var manifestPath = workspace.CreateManifest(
            Endpoint("_content/Bzs.Blazor/bzs.blazor.css", Integrity(css)),
            Endpoint(
                "_content/Bzs.Blazor/bzs.blazor.css",
                "sha256-conflicting",
                route: "_content/Bzs.Blazor/bzs.blazor.abc1234567.css"));

        var exception = Assert.Throws<InvalidDataException>(
            () => StaticWebAssetIntegrityVerifier.Verify(packagePath, manifestPath));

        Assert.Contains("conflicting endpoint integrity values", exception.Message);
    }

    private static object Endpoint(
        string assetFile,
        string? integrity,
        string? route = null,
        string? contentEncoding = null,
        string? selectorContentEncoding = null,
        string? responseHeaderContentEncoding = null) => new
        {
            Route = route ?? assetFile,
            AssetFile = assetFile,
            Selectors = contentEncoding is null && selectorContentEncoding is null
                ? Array.Empty<object>()
                : [new { Name = "Content-Encoding", Value = contentEncoding ?? selectorContentEncoding }],
            ResponseHeaders = responseHeaderContentEncoding is null
                ? Array.Empty<object>()
                : [new { Name = "Content-Encoding", Value = responseHeaderContentEncoding }],
            EndpointProperties = integrity is null
                ? Array.Empty<object>()
                : [new { Name = "integrity", Value = integrity }],
        };

    private static string Integrity(byte[] content) =>
        "sha256-" + Convert.ToBase64String(SHA256.HashData(content));

    private sealed class TestWorkspace : IDisposable
    {
        private readonly string root = Path.Combine(
            Path.GetTempPath(),
            $"bzs-static-assets-{Guid.NewGuid():N}");

        public TestWorkspace()
        {
            Directory.CreateDirectory(root);
        }

        public string CreatePackage(params (string Name, byte[] Content)[] entries)
        {
            var path = Path.Combine(root, $"package-{Guid.NewGuid():N}.nupkg");
            using var archive = ZipFile.Open(path, ZipArchiveMode.Create);
            foreach (var (name, content) in entries)
            {
                var entry = archive.CreateEntry(name);
                using var stream = entry.Open();
                stream.Write(content);
            }

            return path;
        }

        public string CreateManifest(params object[] endpoints)
        {
            var path = Path.Combine(root, $"endpoints-{Guid.NewGuid():N}.json");
            File.WriteAllText(path, JsonSerializer.Serialize(new { Endpoints = endpoints }));
            return path;
        }

        public void Dispose()
        {
            Directory.Delete(root, recursive: true);
        }
    }
}
