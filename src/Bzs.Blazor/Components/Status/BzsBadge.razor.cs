using System.Globalization;

namespace Bzs.Blazor;

/// <summary>
/// Renders a compact semantic label or bounded count.
/// </summary>
public sealed partial class BzsBadge : BzsComponentBase
{
    /// <summary>
    /// Gets or sets the semantic treatment of the badge.
    /// </summary>
    [Parameter]
    public BzsMessageSeverity Severity { get; set; } = BzsMessageSeverity.Information;

    /// <summary>
    /// Gets or sets the count displayed by the badge. Values greater than
    /// <see cref="Maximum" /> are displayed using the configured upper bound.
    /// </summary>
    [Parameter]
    public int? Count { get; set; }

    /// <summary>
    /// Gets or sets the greatest count displayed without an overflow suffix.
    /// </summary>
    [Parameter]
    public int Maximum { get; set; } = 99;

    /// <summary>
    /// Gets or sets whether a zero count remains visible.
    /// </summary>
    [Parameter]
    public bool ShowZero { get; set; }

    /// <summary>
    /// Gets or sets the non-count content displayed by the badge.
    /// </summary>
    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    /// <summary>
    /// Gets or sets an explicit accessible name for the badge.
    /// </summary>
    [Parameter]
    public string? AccessibleName { get; set; }

    private bool IsVisible => Count is not 0 || ShowZero;

    private string SeverityName => Severity switch
    {
        BzsMessageSeverity.Information => "information",
        BzsMessageSeverity.Success => "success",
        BzsMessageSeverity.Warning => "warning",
        BzsMessageSeverity.Error => "error",
        _ => throw new ArgumentOutOfRangeException(nameof(Severity), Severity, "The badge severity is not supported."),
    };

    private BzsIconData SeverityIcon => Severity switch
    {
        BzsMessageSeverity.Information => BzsIcons.Info,
        BzsMessageSeverity.Success => BzsIcons.Success,
        BzsMessageSeverity.Warning => BzsIcons.Warning,
        BzsMessageSeverity.Error => BzsIcons.Error,
        _ => throw new ArgumentOutOfRangeException(nameof(Severity), Severity, "The badge severity is not supported."),
    };

    private IReadOnlyDictionary<string, object> RootAttributes
    {
        get
        {
            var attributes = new Dictionary<string, object>(
                BuildAttributes($"bzs-badge bzs-badge--{SeverityName}"),
                StringComparer.OrdinalIgnoreCase)
            {
                ["data-bzs-badge-severity"] = SeverityName,
            };

            if (!string.IsNullOrWhiteSpace(AccessibleName))
            {
                attributes["aria-label"] = AccessibleName.Trim();
            }

            return attributes;
        }
    }

    /// <inheritdoc />
    protected override void OnParametersSet()
    {
        if (!Enum.IsDefined(Severity))
        {
            throw new ArgumentOutOfRangeException(nameof(Severity), Severity, "The badge severity is not supported.");
        }

        if (Maximum < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(Maximum), Maximum, "The badge maximum must be at least one.");
        }

        if (Count < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(Count), Count, "The badge count cannot be negative.");
        }

        if (Count.HasValue == (ChildContent is not null))
        {
            throw new InvalidOperationException("BzsBadge requires exactly one of Count or ChildContent.");
        }
    }

    private string FormatCount(int count) => count > Maximum
        ? $"{Maximum.ToString(CultureInfo.CurrentCulture)}+"
        : count.ToString(CultureInfo.CurrentCulture);
}
