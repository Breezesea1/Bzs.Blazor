namespace Bzs.Blazor;

internal static class BzsSizeNames
{
    public static string Avatar(BzsAvatarSize size) => size switch
    {
        BzsAvatarSize.Small => "small",
        BzsAvatarSize.Medium => "medium",
        BzsAvatarSize.Large => "large",
        _ => throw new ArgumentOutOfRangeException(nameof(size), size, "The avatar size is not supported."),
    };

    public static string Skeleton(BzsSkeletonSize size) => size switch
    {
        BzsSkeletonSize.Small => "small",
        BzsSkeletonSize.Medium => "medium",
        BzsSkeletonSize.Large => "large",
        _ => throw new ArgumentOutOfRangeException(nameof(size), size, "The skeleton size is not supported."),
    };

    public static string Button(BzsButtonSize size) => size switch
    {
        BzsButtonSize.Small => "small",
        BzsButtonSize.Medium => "medium",
        BzsButtonSize.Large => "large",
        _ => throw new ArgumentOutOfRangeException(nameof(size), size, "The button size is not supported."),
    };
}
