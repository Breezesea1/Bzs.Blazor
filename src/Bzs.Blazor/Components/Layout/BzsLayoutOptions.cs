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
