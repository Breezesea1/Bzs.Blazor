namespace Bzs.Blazor;

/// <summary>
/// Defines the maximum inline size of a <see cref="BzsContainer"/>.
/// </summary>
public enum BzsContainerMaxWidth
{
    /// <summary>Constrains content to an extra-small reading width.</summary>
    ExtraSmall,
    /// <summary>Constrains content to a small page width.</summary>
    Small,
    /// <summary>Constrains content to a medium page width.</summary>
    Medium,
    /// <summary>Constrains content to the default large page width.</summary>
    Large,
    /// <summary>Constrains content to an extra-large page width.</summary>
    ExtraLarge,
    /// <summary>Constrains content to the widest built-in page width.</summary>
    ExtraExtraLarge,
    /// <summary>Leaves the maximum inline size unconstrained.</summary>
    None,
}

/// <summary>
/// Defines the token-driven gap between layout children.
/// </summary>
public enum BzsLayoutSpacing
{
    /// <summary>Removes the gap between children.</summary>
    None,
    /// <summary>Uses the extra-small layout spacing token.</summary>
    ExtraSmall,
    /// <summary>Uses the small layout spacing token.</summary>
    Small,
    /// <summary>Uses the medium layout spacing token.</summary>
    Medium,
    /// <summary>Uses the large layout spacing token.</summary>
    Large,
    /// <summary>Uses the extra-large layout spacing token.</summary>
    ExtraLarge,
}

/// <summary>
/// Defines how children are distributed along a layout's main axis.
/// </summary>
public enum BzsJustify
{
    /// <summary>Packs children at the start of the main axis.</summary>
    Start,
    /// <summary>Centers children on the main axis.</summary>
    Center,
    /// <summary>Packs children at the end of the main axis.</summary>
    End,
    /// <summary>Distributes remaining space between children.</summary>
    SpaceBetween,
    /// <summary>Distributes remaining space around children.</summary>
    SpaceAround,
    /// <summary>Distributes remaining space evenly around children.</summary>
    SpaceEvenly,
}

/// <summary>
/// Defines how children are aligned along a layout's cross axis.
/// </summary>
public enum BzsAlignItems
{
    /// <summary>Aligns children at the start of the cross axis.</summary>
    Start,
    /// <summary>Centers children on the cross axis.</summary>
    Center,
    /// <summary>Aligns children at the end of the cross axis.</summary>
    End,
    /// <summary>Stretches children across the available cross axis.</summary>
    Stretch,
    /// <summary>Aligns children on their text baselines.</summary>
    Baseline,
}

/// <summary>
/// Defines how stack children wrap when they exceed the available space.
/// </summary>
public enum BzsStackWrap
{
    /// <summary>Keeps all children on one line.</summary>
    NoWrap,
    /// <summary>Wraps children onto additional lines.</summary>
    Wrap,
    /// <summary>Wraps children with the cross axis reversed.</summary>
    WrapReverse,
}

/// <summary>
/// Defines the inset applied to a divider.
/// </summary>
public enum BzsDividerInset
{
    /// <summary>Spans the full available cross axis.</summary>
    None,
    /// <summary>Adds space at the logical start edge.</summary>
    Start,
    /// <summary>Adds equal space at both logical edges.</summary>
    Both,
}

/// <summary>
/// Defines the semantic color treatment of a <see cref="BzsAppBar" />.
/// </summary>
public enum BzsAppBarColor
{
    /// <summary>Uses the active theme surface.</summary>
    Surface,
    /// <summary>Uses the primary theme color.</summary>
    Primary,
    /// <summary>Uses an informational accent.</summary>
    Info,
    /// <summary>Uses a success accent.</summary>
    Success,
    /// <summary>Uses a warning accent.</summary>
    Warning,
    /// <summary>Uses an error accent.</summary>
    Error,
}

/// <summary>
/// Defines how a <see cref="BzsNavigationDrawer" /> participates in an app shell.
/// </summary>
public enum BzsNavigationDrawerVariant
{
    /// <summary>Reserves space for open navigation at every viewport size.</summary>
    Persistent,
    /// <summary>Overlays main content at every viewport size.</summary>
    Temporary,
    /// <summary>Reserves space from 48rem and overlays content below that width.</summary>
    Responsive,
}

/// <summary>
/// Defines the logical edge of a <see cref="BzsNavigationDrawer" />.
/// </summary>
public enum BzsNavigationDrawerPosition
{
    /// <summary>Anchors navigation to the logical start edge.</summary>
    Start,
    /// <summary>Anchors navigation to the logical end edge.</summary>
    End,
}
