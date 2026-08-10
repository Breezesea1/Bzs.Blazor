namespace Bzs.Blazor;

/// <summary>Describes one item rendered by <see cref="BzsBreadcrumbs" />.</summary>
public sealed record BzsBreadcrumbItem
{
    /// <summary>Initializes a breadcrumb item.</summary>
    /// <param name="label">The visible breadcrumb label.</param>
    /// <param name="href">The optional navigation destination.</param>
    /// <param name="current">An optional explicit current-page state.</param>
    public BzsBreadcrumbItem(string label, string? href = null, bool? current = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(label);
        Label = label.Trim();
        Href = string.IsNullOrWhiteSpace(href) ? null : href.Trim();
        Current = current;
    }

    /// <summary>Gets the visible breadcrumb label.</summary>
    public string Label { get; }

    /// <summary>Gets the optional navigation destination.</summary>
    public string? Href { get; }

    /// <summary>Gets the optional explicit current-page state.</summary>
    public bool? Current { get; }
}
