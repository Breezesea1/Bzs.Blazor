using System.Collections.ObjectModel;

namespace Bzs.Blazor;

/// <summary>
/// Provides the attributes that connect one consumer-rendered trigger element
/// to a containing <see cref="BzsTooltip" />.
/// </summary>
public sealed class BzsTooltipTriggerContext
{
    internal BzsTooltipTriggerContext(IReadOnlyDictionary<string, object> attributes)
    {
        Attributes = new ReadOnlyDictionary<string, object>(
            new Dictionary<string, object>(attributes, StringComparer.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Gets the event and accessibility attributes that must be applied to the
    /// single interactive root element rendered by the trigger template.
    /// </summary>
    public IReadOnlyDictionary<string, object> Attributes { get; }
}
