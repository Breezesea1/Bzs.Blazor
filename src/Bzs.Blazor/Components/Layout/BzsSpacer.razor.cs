namespace Bzs.Blazor;

/// <summary>
/// Consumes available space along the main axis of a flexible layout.
/// </summary>
public sealed partial class BzsSpacer : BzsComponentBase
{
    private IReadOnlyDictionary<string, object> RootAttributes
    {
        get
        {
            var attributes = new Dictionary<string, object>(
                BuildAttributes("bzs-spacer"),
                StringComparer.OrdinalIgnoreCase)
            {
                ["aria-hidden"] = "true",
                ["data-bzs-spacer"] = string.Empty,
            };

            return attributes;
        }
    }
}
