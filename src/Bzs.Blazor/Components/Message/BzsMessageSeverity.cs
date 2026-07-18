namespace Bzs.Blazor;

/// <summary>
/// Selects the semantic severity presented by a <see cref="BzsMessage" />.
/// </summary>
public enum BzsMessageSeverity
{
    /// <summary>Reports neutral informational feedback.</summary>
    Information,

    /// <summary>Reports successful completion.</summary>
    Success,

    /// <summary>Reports feedback that needs attention without interrupting the user.</summary>
    Warning,

    /// <summary>Reports feedback that prevents the requested operation.</summary>
    Error,
}
