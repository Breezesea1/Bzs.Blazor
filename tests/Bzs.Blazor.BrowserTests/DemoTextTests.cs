using System.Globalization;
using System.Reflection;
using Bzs.Blazor.Demo.Client;

namespace Bzs.Blazor.BrowserTests;

public sealed class DemoTextTests
{
    [Fact]
    public void ChromeEntriesProvideTheBilingualDemoShellCatalog()
    {
        var expectedEntries = new[]
        {
            "SkipLink",
            "NavigationAccessibleName",
            "BrandTagline",
            "CloseNavigation",
            "CatalogSection",
            "Overview",
            "ThemeFoundation",
            "FoundationComponents",
            "Forms",
            "Productivity",
            "Feedback",
            "Tabs",
            "Overlays",
            "Layout",
            "ProjectSection",
            "Releases",
            "RenderModesSection",
            "RuntimeSection",
            "StaticSsr",
            "InteractiveServer",
            "InteractiveWebAssembly",
            "InteractiveAuto",
            "DemoUser",
            "Administrator",
            "Exit",
            "SignOutAccessibleName",
            "OpenNavigation",
            "ComponentWorkbench",
            "AspireDemoHost",
            "StaticWebAssemblyHost",
            "LanguageSwitcherAccessibleName",
            "InteractionError",
            "Reload",
            "WhatsNew",
            "ViewAllReleases",
            "MarkAsRead",
        };

        var actualEntries = typeof(DemoText.Chrome)
            .GetProperties(BindingFlags.Public | BindingFlags.Static)
            .Where(property => property.PropertyType == typeof(string))
            .Select(property => property.Name)
            .Order()
            .ToArray();

        Assert.Equal(expectedEntries.Order(), actualEntries);
    }

    [Fact]
    public void EveryCatalogEntryHasNonEmptyDistinctTranslations()
    {
        var properties = typeof(DemoText).GetNestedTypes()
            .SelectMany(nestedType => nestedType.GetProperties(BindingFlags.Public | BindingFlags.Static))
            .Where(property => property.PropertyType == typeof(string))
            .ToArray();

        Assert.NotEmpty(properties);

        foreach (var property in properties)
        {
            var chinese = EvaluateUnderCulture("zh-Hans", () => (string?)property.GetValue(null));
            var english = EvaluateUnderCulture("en-US", () => (string?)property.GetValue(null));

            Assert.False(string.IsNullOrWhiteSpace(chinese), $"{property.Name} must not be empty under the default culture.");
            Assert.False(string.IsNullOrWhiteSpace(english), $"{property.Name} must not be empty under en-US.");
            Assert.NotEqual(chinese, english);
        }
    }

    private static T EvaluateUnderCulture<T>(string cultureName, Func<T> evaluate)
    {
        var previousCulture = CultureInfo.CurrentUICulture;
        try
        {
            CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo(cultureName);
            return evaluate();
        }
        finally
        {
            CultureInfo.CurrentUICulture = previousCulture;
        }
    }
}
