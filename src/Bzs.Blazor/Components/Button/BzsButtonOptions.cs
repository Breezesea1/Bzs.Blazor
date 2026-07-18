namespace Bzs.Blazor;

/// <summary>
/// Selects the semantic visual treatment of a <see cref="BzsButton" />.
/// </summary>
public enum BzsButtonVariant
{
    /// <summary>Uses the primary action treatment.</summary>
    Primary,

    /// <summary>Uses a raised neutral treatment.</summary>
    Secondary,

    /// <summary>Uses a bordered, low-emphasis treatment.</summary>
    Outline,

    /// <summary>Uses a borderless, low-emphasis treatment.</summary>
    Ghost,

    /// <summary>Uses the error semantic color for a destructive action.</summary>
    Danger,
}

/// <summary>
/// Selects the size adjustment applied on top of the active density tokens.
/// </summary>
public enum BzsButtonSize
{
    /// <summary>Uses the compact size adjustment.</summary>
    Small,

    /// <summary>Uses the default size for the active density.</summary>
    Medium,

    /// <summary>Uses the spacious size adjustment.</summary>
    Large,
}

/// <summary>
/// Selects the native HTML button behavior.
/// </summary>
public enum BzsButtonType
{
    /// <summary>Renders a command button without form submission behavior.</summary>
    Button,

    /// <summary>Renders a button that submits its owning form.</summary>
    Submit,

    /// <summary>Renders a button that resets its owning form.</summary>
    Reset,
}
