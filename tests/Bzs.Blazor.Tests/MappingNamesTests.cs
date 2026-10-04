namespace Bzs.Blazor.Tests;

public sealed class MappingNamesTests
{
    public static TheoryData<BzsMessageSeverity, string, BzsIconData> MessageSeverityMappings => new()
    {
        { BzsMessageSeverity.Information, "information", BzsIcons.Info },
        { BzsMessageSeverity.Success, "success", BzsIcons.Success },
        { BzsMessageSeverity.Warning, "warning", BzsIcons.Warning },
        { BzsMessageSeverity.Error, "error", BzsIcons.Error },
    };

    public static TheoryData<BzsToastSeverity, string, BzsIconData> ToastSeverityMappings => new()
    {
        { BzsToastSeverity.Information, "information", BzsIcons.Info },
        { BzsToastSeverity.Success, "success", BzsIcons.Success },
        { BzsToastSeverity.Warning, "warning", BzsIcons.Warning },
        { BzsToastSeverity.Error, "error", BzsIcons.Error },
    };

    [Theory]
    [MemberData(nameof(MessageSeverityMappings))]
    public void MessageSeverityMapsToItsNameAndIcon(
        BzsMessageSeverity severity,
        string expectedName,
        BzsIconData expectedIcon)
    {
        Assert.Equal(expectedName, BzsSeverityNames.Name(severity));
        Assert.Same(expectedIcon, BzsSeverityNames.Icon(severity));
    }

    [Theory]
    [MemberData(nameof(ToastSeverityMappings))]
    public void ToastSeverityMapsToItsNameAndIcon(
        BzsToastSeverity severity,
        string expectedName,
        BzsIconData expectedIcon)
    {
        Assert.Equal(expectedName, BzsSeverityNames.Name(severity));
        Assert.Same(expectedIcon, BzsSeverityNames.Icon(severity));
    }

    [Theory]
    [InlineData(BzsMessageSeverity.Information, false)]
    [InlineData(BzsMessageSeverity.Success, false)]
    [InlineData(BzsMessageSeverity.Warning, false)]
    [InlineData(BzsMessageSeverity.Error, true)]
    public void MessageSeverityIsAssertiveOnlyForError(BzsMessageSeverity severity, bool expected)
    {
        Assert.Equal(expected, BzsSeverityNames.IsAssertive(severity));
    }

    [Theory]
    [InlineData(BzsToastSeverity.Information, false)]
    [InlineData(BzsToastSeverity.Success, false)]
    [InlineData(BzsToastSeverity.Warning, false)]
    [InlineData(BzsToastSeverity.Error, true)]
    public void ToastSeverityIsAssertiveOnlyForError(BzsToastSeverity severity, bool expected)
    {
        Assert.Equal(expected, BzsSeverityNames.IsAssertive(severity));
    }

    [Theory]
    [InlineData(BzsSurfaceLevel.Base, "base")]
    [InlineData(BzsSurfaceLevel.Raised, "raised")]
    [InlineData(BzsSurfaceLevel.Inset, "inset")]
    [InlineData(BzsSurfaceLevel.Overlay, "overlay")]
    public void SurfaceLevelMapsToItsName(BzsSurfaceLevel level, string expected)
    {
        Assert.Equal(expected, BzsSurfaceLevelNames.Name(level));
    }

    [Theory]
    [InlineData(BzsAvatarSize.Small, "small")]
    [InlineData(BzsAvatarSize.Medium, "medium")]
    [InlineData(BzsAvatarSize.Large, "large")]
    public void AvatarSizeMapsToItsName(BzsAvatarSize size, string expected)
    {
        Assert.Equal(expected, BzsSizeNames.Avatar(size));
    }

    [Theory]
    [InlineData(BzsSkeletonSize.Small, "small")]
    [InlineData(BzsSkeletonSize.Medium, "medium")]
    [InlineData(BzsSkeletonSize.Large, "large")]
    public void SkeletonSizeMapsToItsName(BzsSkeletonSize size, string expected)
    {
        Assert.Equal(expected, BzsSizeNames.Skeleton(size));
    }

    [Theory]
    [InlineData(BzsButtonSize.Small, "small")]
    [InlineData(BzsButtonSize.Medium, "medium")]
    [InlineData(BzsButtonSize.Large, "large")]
    public void ButtonSizeMapsToItsName(BzsButtonSize size, string expected)
    {
        Assert.Equal(expected, BzsSizeNames.Button(size));
    }

    [Fact]
    public void UndefinedEnumValuesAreRejected()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => BzsSeverityNames.Name((BzsMessageSeverity)999));
        Assert.Throws<ArgumentOutOfRangeException>(() => BzsSeverityNames.Icon((BzsMessageSeverity)999));
        Assert.Throws<ArgumentOutOfRangeException>(() => BzsSeverityNames.Name((BzsToastSeverity)999));
        Assert.Throws<ArgumentOutOfRangeException>(() => BzsSeverityNames.Icon((BzsToastSeverity)999));
        Assert.Throws<ArgumentOutOfRangeException>(() => BzsSurfaceLevelNames.Name((BzsSurfaceLevel)999));
        Assert.Throws<ArgumentOutOfRangeException>(() => BzsSizeNames.Avatar((BzsAvatarSize)999));
        Assert.Throws<ArgumentOutOfRangeException>(() => BzsSizeNames.Skeleton((BzsSkeletonSize)999));
        Assert.Throws<ArgumentOutOfRangeException>(() => BzsSizeNames.Button((BzsButtonSize)999));
    }
}
