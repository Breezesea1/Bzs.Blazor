namespace Bzs.Blazor;

internal static class LayoutNames
{
    public static string Spacing(BzsLayoutSpacing spacing) => spacing switch
    {
        BzsLayoutSpacing.None => "none",
        BzsLayoutSpacing.ExtraSmall => "extra-small",
        BzsLayoutSpacing.Small => "small",
        BzsLayoutSpacing.Medium => "medium",
        BzsLayoutSpacing.Large => "large",
        BzsLayoutSpacing.ExtraLarge => "extra-large",
        _ => throw new ArgumentOutOfRangeException(nameof(spacing), spacing, "The layout spacing is not supported."),
    };

    public static string Justify(BzsJustify justify) => justify switch
    {
        BzsJustify.Start => "start",
        BzsJustify.Center => "center",
        BzsJustify.End => "end",
        BzsJustify.SpaceBetween => "space-between",
        BzsJustify.SpaceAround => "space-around",
        BzsJustify.SpaceEvenly => "space-evenly",
        _ => throw new ArgumentOutOfRangeException(nameof(justify), justify, "The layout justification is not supported."),
    };

    public static string Align(BzsAlignItems align) => align switch
    {
        BzsAlignItems.Start => "start",
        BzsAlignItems.Center => "center",
        BzsAlignItems.End => "end",
        BzsAlignItems.Stretch => "stretch",
        BzsAlignItems.Baseline => "baseline",
        _ => throw new ArgumentOutOfRangeException(nameof(align), align, "The layout alignment is not supported."),
    };

    public static string Wrap(BzsStackWrap wrap) => wrap switch
    {
        BzsStackWrap.NoWrap => "no-wrap",
        BzsStackWrap.Wrap => "wrap",
        BzsStackWrap.WrapReverse => "wrap-reverse",
        _ => throw new ArgumentOutOfRangeException(nameof(wrap), wrap, "The stack wrapping behavior is not supported."),
    };
}
