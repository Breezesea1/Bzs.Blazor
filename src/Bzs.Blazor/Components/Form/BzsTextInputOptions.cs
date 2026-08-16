namespace Bzs.Blazor;

/// <summary>Specifies the native text-family type rendered by <see cref="BzsTextInput"/>.</summary>
public enum BzsTextInputType
{
    /// <summary>Renders a native text input.</summary>
    Text,

    /// <summary>Renders a native email input.</summary>
    Email,

    /// <summary>Renders a native search input.</summary>
    Search,
}

/// <summary>Specifies when a text input commits its value.</summary>
public enum BzsInputUpdateMode
{
    /// <summary>Commits when the native change event occurs.</summary>
    Change,

    /// <summary>Commits for each native input event outside IME composition.</summary>
    Input,
}
