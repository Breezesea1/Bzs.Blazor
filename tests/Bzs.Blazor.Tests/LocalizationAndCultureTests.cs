using System.Collections;
using System.Globalization;
using System.Resources;

namespace Bzs.Blazor.Tests;

public sealed class LocalizationAndCultureTests
{
    [Fact]
    public void NeutralAndSimplifiedChineseResourcesExposeTheSameKeys()
    {
        var manager = new ResourceManager(
            "Bzs.Blazor.Localization.BzsBlazorResources",
            typeof(BzsButton).Assembly);
        var neutral = manager.GetResourceSet(CultureInfo.InvariantCulture, true, false);
        var chinese = manager.GetResourceSet(CultureInfo.GetCultureInfo("zh-Hans"), true, false);

        Assert.NotNull(neutral);
        Assert.NotNull(chinese);
        Assert.Equal(GetKeys(neutral!), GetKeys(chinese!));
    }

    [Fact]
    public void SelectOptionDefaultValueTextIsStableAcrossCultures()
    {
        var original = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("fr-FR");
            var option = new BzsSelectOption<decimal>(1.5m, "One and a half");

            Assert.Equal("1.5", option.ValueText);
        }
        finally
        {
            CultureInfo.CurrentCulture = original;
        }
    }

    [Fact]
    public void SelectOptionRequiresExplicitTextForCustomNonFormattableValues()
    {
        Assert.Throws<ArgumentException>(() =>
            new BzsSelectOption<CustomValue>(new CustomValue(7), "Seven"));

        var option = new BzsSelectOption<CustomValue>(
            new CustomValue(7),
            "Seven",
            valueText: "custom-7");
        Assert.Equal("custom-7", option.ValueText);
    }

    private static string[] GetKeys(ResourceSet set) => set
        .Cast<DictionaryEntry>()
        .Select(static entry => (string)entry.Key)
        .Order(StringComparer.Ordinal)
        .ToArray();

    private sealed record CustomValue(int Value);
}
