using System.Globalization;

namespace Bzs.Blazor.Demo.Client;

/// <summary>
/// Typed bilingual text catalog for visitor-facing demo copy. Entries resolve zh-Hans or
/// en-US from <see cref="CultureInfo.CurrentUICulture"/>, which each host applies from the
/// <c>?culture=</c> URL parameter; zh-Hans is the default when no parameter is present.
/// </summary>
public static class DemoText
{
    public static class Chrome
    {
        public static string SkipLink => Get("跳至目录内容", "Skip to catalog content");
    }

    private static string Get(string chinese, string english) =>
        DemoCulture.IsChinese(CultureInfo.CurrentUICulture.Name) ? chinese : english;
}
