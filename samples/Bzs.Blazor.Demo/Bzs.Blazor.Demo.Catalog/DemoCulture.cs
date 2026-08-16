using System.Globalization;

namespace Bzs.Blazor.Demo.Client;

public static class DemoCulture
{
    private const string ChineseCultureName = "zh-Hans";
    private const string EnglishCultureName = "en-US";

    private static readonly CultureInfo EnglishCulture = CultureInfo.GetCultureInfo(EnglishCultureName);
    private static readonly CultureInfo SimplifiedChineseCulture = CultureInfo.GetCultureInfo(ChineseCultureName);

    public static CultureInfo Resolve(string? cultureName) =>
        IsEnglish(cultureName) ? EnglishCulture : SimplifiedChineseCulture;

    public static CultureInfo Resolve(Uri uri) =>
        IsEnglish(uri) ? EnglishCulture : SimplifiedChineseCulture;

    public static void ApplyCurrentCulture(Uri uri)
    {
        var culture = Resolve(uri);
        CultureInfo.DefaultThreadCurrentCulture = culture;
        CultureInfo.DefaultThreadCurrentUICulture = culture;
        CultureInfo.CurrentCulture = culture;
        CultureInfo.CurrentUICulture = culture;
    }

    public static bool IsChinese(Uri uri) =>
        !IsEnglish(uri);

    public static bool IsEnglish(Uri uri) =>
        IsEnglish(GetCultureName(uri));

    public static bool IsChinese(string? cultureName) =>
        string.Equals(cultureName, ChineseCultureName, StringComparison.OrdinalIgnoreCase);

    public static bool IsEnglish(string? cultureName) =>
        string.Equals(cultureName, EnglishCultureName, StringComparison.OrdinalIgnoreCase);

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

    public static string PreserveCulture(Uri currentUri, Uri baseUri, string relativePath)
    {
        var cultureName = GetCultureName(currentUri);
        return cultureName is null
            ? relativePath
            : WithCulture(new Uri(baseUri, relativePath), cultureName);
    }

    private static string? GetCultureName(Uri uri)
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
                return null;
            }

            return Uri.UnescapeDataString(queryPart[(separatorIndex + 1)..].Replace('+', ' '));
        }

        return null;
    }

    private static bool IsCultureQueryPart(string queryPart)
    {
        var separatorIndex = queryPart.IndexOf('=');
        var encodedName = separatorIndex < 0 ? queryPart : queryPart[..separatorIndex];
        var name = Uri.UnescapeDataString(encodedName.Replace('+', ' '));
        return string.Equals(name, "culture", StringComparison.OrdinalIgnoreCase);
    }
}
