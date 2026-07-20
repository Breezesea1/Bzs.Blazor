using System.IO.Compression;
using System.Security.Cryptography;
using System.Text.Json;

namespace Bzs.Blazor.PackageConsumerTests;

internal static class StaticWebAssetIntegrityVerifier
{
    private const string PackageAssetPrefix = "staticwebassets/";
    private const string RuntimeAssetPrefix = "_content/Bzs.Blazor/";

    public static void Verify(string packagePath, string endpointManifestPath)
    {
        if (!File.Exists(packagePath))
        {
            throw new InvalidDataException($"Package was not found at '{packagePath}'.");
        }

        if (!File.Exists(endpointManifestPath))
        {
            throw new InvalidDataException(
                $"Static web asset endpoint manifest was not found at '{endpointManifestPath}'.");
        }

        using var archive = ZipFile.OpenRead(packagePath);
        var packagedAssets = archive.Entries
            .Where(entry => entry.FullName.StartsWith(PackageAssetPrefix, StringComparison.Ordinal)
                && IsCssOrJavaScript(entry.FullName))
            .ToDictionary(entry => entry.FullName, StringComparer.Ordinal);

        if (packagedAssets.Count == 0)
        {
            throw new InvalidDataException("Package contains no CSS or JavaScript static web assets.");
        }

        using var manifestStream = File.OpenRead(endpointManifestPath);
        using var manifest = JsonDocument.Parse(manifestStream);
        if (!manifest.RootElement.TryGetProperty("Endpoints", out var endpoints)
            || endpoints.ValueKind != JsonValueKind.Array)
        {
            throw new InvalidDataException("Static web asset endpoint manifest has no Endpoints array.");
        }

        var integrityByPackageEntry = new Dictionary<string, HashSet<string>>(StringComparer.Ordinal);
        foreach (var endpoint in endpoints.EnumerateArray())
        {
            if (!TryGetString(endpoint, "AssetFile", out var assetFile)
                || !assetFile.StartsWith(RuntimeAssetPrefix, StringComparison.Ordinal)
                || !IsCssOrJavaScript(assetFile)
                || assetFile.EndsWith(".gz", StringComparison.Ordinal)
                || HasContentEncoding(endpoint))
            {
                continue;
            }

            var packageEntry = PackageAssetPrefix + assetFile[RuntimeAssetPrefix.Length..];
            if (!packagedAssets.ContainsKey(packageEntry))
            {
                throw new InvalidDataException(
                    $"Static web asset metadata references missing package entry '{packageEntry}'.");
            }

            var integrityValues = GetEndpointPropertyValues(endpoint, "integrity");
            if (integrityValues.Count != 1 || string.IsNullOrWhiteSpace(integrityValues[0]))
            {
                throw new InvalidDataException(
                    $"Static web asset metadata for '{packageEntry}' must declare exactly one integrity value.");
            }

            if (!integrityByPackageEntry.TryGetValue(packageEntry, out var values))
            {
                values = new HashSet<string>(StringComparer.Ordinal);
                integrityByPackageEntry.Add(packageEntry, values);
            }

            values.Add(integrityValues[0]);
        }

        foreach (var (entryName, entry) in packagedAssets)
        {
            if (!integrityByPackageEntry.TryGetValue(entryName, out var integrityValues))
            {
                throw new InvalidDataException(
                    $"Package entry '{entryName}' has no uncompressed endpoint integrity metadata.");
            }

            if (integrityValues.Count != 1)
            {
                throw new InvalidDataException(
                    $"Package entry '{entryName}' has conflicting endpoint integrity values.");
            }

            using var entryStream = entry.Open();
            var expectedIntegrity = "sha256-" + Convert.ToBase64String(SHA256.HashData(entryStream));
            var actualIntegrity = integrityValues.Single();
            if (!string.Equals(actualIntegrity, expectedIntegrity, StringComparison.Ordinal))
            {
                throw new InvalidDataException(
                    $"Package entry '{entryName}' does not match endpoint integrity metadata. "
                    + $"Expected '{expectedIntegrity}', found '{actualIntegrity}'.");
            }
        }
    }

    private static bool IsCssOrJavaScript(string path) =>
        path.EndsWith(".css", StringComparison.Ordinal)
        || path.EndsWith(".js", StringComparison.Ordinal);

    private static bool HasContentEncoding(JsonElement endpoint) =>
        HasNamedItem(endpoint, "Selectors", "Content-Encoding")
        || HasNamedItem(endpoint, "ResponseHeaders", "Content-Encoding");

    private static bool HasNamedItem(JsonElement endpoint, string collectionName, string expectedName)
    {
        if (!endpoint.TryGetProperty(collectionName, out var items)
            || items.ValueKind != JsonValueKind.Array)
        {
            return false;
        }

        return items.EnumerateArray().Any(item =>
            TryGetString(item, "Name", out var name)
            && string.Equals(name, expectedName, StringComparison.OrdinalIgnoreCase));
    }

    private static List<string> GetEndpointPropertyValues(
        JsonElement endpoint,
        string expectedName)
    {
        if (!endpoint.TryGetProperty("EndpointProperties", out var properties)
            || properties.ValueKind != JsonValueKind.Array)
        {
            return [];
        }

        return properties.EnumerateArray()
            .Where(property =>
                TryGetString(property, "Name", out var name)
                && string.Equals(name, expectedName, StringComparison.Ordinal))
            .Select(property =>
                TryGetString(property, "Value", out var value) ? value : string.Empty)
            .ToList();
    }

    private static bool TryGetString(JsonElement element, string propertyName, out string value)
    {
        value = string.Empty;
        if (!element.TryGetProperty(propertyName, out var property)
            || property.ValueKind != JsonValueKind.String)
        {
            return false;
        }

        value = property.GetString() ?? string.Empty;
        return true;
    }
}
