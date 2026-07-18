namespace Bzs.Blazor;

/// <summary>
/// Represents the SVG path data and view box for a single icon.
/// </summary>
/// <remarks>
/// Consumers can create instances for application-specific icons without a
/// provider or a host asset dependency. Path data is rendered as an SVG
/// attribute and is not interpreted as markup.
/// </remarks>
public sealed record BzsIconData
{
    /// <summary>
    /// Gets the standard view box used by the embedded Lucide geometry.
    /// </summary>
    public const string DefaultViewBox = "0 0 24 24";

    /// <summary>
    /// Initializes a new icon definition.
    /// </summary>
    /// <param name="pathData">The SVG path <c>d</c> attribute value.</param>
    /// <param name="viewBox">The SVG view box for the path data.</param>
    public BzsIconData(string pathData, string viewBox = DefaultViewBox)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(pathData);
        ArgumentException.ThrowIfNullOrWhiteSpace(viewBox);

        PathData = pathData;
        ViewBox = viewBox;
    }

    /// <summary>
    /// Gets the SVG path <c>d</c> attribute value.
    /// </summary>
    public string PathData { get; }

    /// <summary>
    /// Gets the SVG view box for the icon.
    /// </summary>
    public string ViewBox { get; }
}
