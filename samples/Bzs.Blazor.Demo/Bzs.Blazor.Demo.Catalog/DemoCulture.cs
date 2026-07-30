using System.Globalization;

namespace Bzs.Blazor.Demo.Client;

public static class DemoCulture
{
    private const string ChineseCultureName = "zh-Hans";

    private static readonly CultureInfo EnglishCulture = CultureInfo.GetCultureInfo("en-US");
    private static readonly CultureInfo SimplifiedChineseCulture = CultureInfo.GetCultureInfo(ChineseCultureName);

    public static CultureInfo Resolve(string? cultureName) =>
        IsChinese(cultureName) ? SimplifiedChineseCulture : EnglishCulture;

    public static bool IsChinese(Uri uri)
    {
        foreach (var queryPart in uri.Query.TrimStart('?').Split('&', StringSplitOptions.RemoveEmptyEntries))
        {
            if (!IsCultureQueryPart(queryPart))
            {
                continue;
            }

            var separatorIndex = queryPart.IndexOf('=');
            if (separatorIndex < 0)
            {
                return false;
            }

            var value = Uri.UnescapeDataString(queryPart[(separatorIndex + 1)..].Replace('+', ' '));
            return IsChinese(value);
        }

        return false;
    }

    public static string WithCulture(Uri uri, string cultureName)
    {
        var queryParts = uri.Query.TrimStart('?')
            .Split('&', StringSplitOptions.RemoveEmptyEntries)
            .Where(queryPart => !IsCultureQueryPart(queryPart))
            .ToList();
        queryParts.Add($"culture={Uri.EscapeDataString(Resolve(cultureName).Name)}");

        var builder = new UriBuilder(uri)
        {
            Query = string.Join('&', queryParts),
        };
        return $"{builder.Uri.AbsolutePath}{builder.Uri.Query}{builder.Uri.Fragment}";
    }

    public static string PreserveCulture(Uri currentUri, Uri baseUri, string relativePath) =>
        IsChinese(currentUri)
            ? WithCulture(new Uri(baseUri, relativePath), ChineseCultureName)
            : relativePath;

    public static bool IsChinese(string? cultureName) =>
        string.Equals(cultureName, ChineseCultureName, StringComparison.OrdinalIgnoreCase);

    private static bool IsCultureQueryPart(string queryPart)
    {
        var separatorIndex = queryPart.IndexOf('=');
        var encodedName = separatorIndex < 0 ? queryPart : queryPart[..separatorIndex];
        var name = Uri.UnescapeDataString(encodedName.Replace('+', ' '));
        return string.Equals(name, "culture", StringComparison.OrdinalIgnoreCase);
    }
}
