using System.Globalization;
using Bzs.Blazor.Demo.Client;

namespace Bzs.Blazor.BrowserTests;

public sealed class DemoCultureTests
{
    [Fact]
    public void ResolveDefaultsToChineseWhenCultureNameIsMissing()
    {
        Assert.Equal("zh-Hans", DemoCulture.Resolve((string?)null).Name);
        Assert.Equal("zh-Hans", DemoCulture.Resolve(string.Empty).Name);
    }

    [Fact]
    public void ResolveDefaultsToChineseWhenUriHasNoCultureParameter()
    {
        Assert.Equal("zh-Hans", DemoCulture.Resolve(new Uri("https://demo.example/")).Name);
        Assert.Equal("zh-Hans", DemoCulture.Resolve(new Uri("https://demo.example/forms")).Name);
    }

    [Fact]
    public void ResolveHonorsExplicitCultureNames()
    {
        Assert.Equal("zh-Hans", DemoCulture.Resolve("zh-Hans").Name);
        Assert.Equal("en-US", DemoCulture.Resolve("en-US").Name);
    }

    [Fact]
    public void ResolveHonorsExplicitCultureParameter()
    {
        Assert.Equal("zh-Hans", DemoCulture.Resolve(new Uri("https://demo.example/?culture=zh-Hans")).Name);
        Assert.Equal("en-US", DemoCulture.Resolve(new Uri("https://demo.example/forms?culture=en-US")).Name);
    }

    [Fact]
    public void IsChineseUsesTheDefaultForBareAndUnsupportedUris()
    {
        Assert.True(DemoCulture.IsChinese(new Uri("https://demo.example/")));
        Assert.True(DemoCulture.IsChinese(new Uri("https://demo.example/?culture=invalid")));
        Assert.True(DemoCulture.IsChinese(new Uri("https://demo.example/?culture=zh-Hans")));
        Assert.False(DemoCulture.IsChinese(new Uri("https://demo.example/?culture=en-US")));
    }

    [Fact]
    public void IsEnglishDetectsOnlyTheExplicitEnglishParameter()
    {
        Assert.True(DemoCulture.IsEnglish(new Uri("https://demo.example/?culture=en-US")));
        Assert.False(DemoCulture.IsEnglish(new Uri("https://demo.example/")));
        Assert.False(DemoCulture.IsEnglish(new Uri("https://demo.example/?culture=zh-Hans")));
    }

    [Fact]
    public void PreserveCultureKeepsExplicitChineseCulture()
    {
        var currentUri = new Uri("https://demo.example/forms?culture=zh-Hans");
        var baseUri = new Uri("https://demo.example/");

        Assert.Equal("/feedback?culture=zh-Hans", DemoCulture.PreserveCulture(currentUri, baseUri, "feedback"));
    }

    [Fact]
    public void PreserveCultureKeepsExplicitEnglishCulture()
    {
        var currentUri = new Uri("https://demo.example/forms?culture=en-US");
        var baseUri = new Uri("https://demo.example/");

        Assert.Equal("/feedback?culture=en-US", DemoCulture.PreserveCulture(currentUri, baseUri, "feedback"));
    }

    [Fact]
    public void PreserveCultureLeavesBareRelativePathWhenNoCultureParameter()
    {
        var currentUri = new Uri("https://demo.example/forms");
        var baseUri = new Uri("https://demo.example/");

        Assert.Equal("feedback", DemoCulture.PreserveCulture(currentUri, baseUri, "feedback"));
    }

    [Fact]
    public void WithCultureNormalizesToSupportedCultures()
    {
        var uri = new Uri("https://demo.example/forms?culture=fr-FR");

        Assert.Equal("/forms?culture=zh-Hans", DemoCulture.WithCulture(uri, "fr-FR"));
        Assert.Equal("/forms?culture=en-US", DemoCulture.WithCulture(uri, "en-US"));
    }
}
