using System.Globalization;
using Bzs.Blazor.Localization;
using Microsoft.Extensions.Localization;

namespace Bzs.Blazor;

/// <summary>
/// Renders accessible determinate or indeterminate progress.
/// </summary>
public sealed partial class BzsProgress : BzsComponentBase
{
    [Inject]
    private IStringLocalizer<BzsBlazorResources> Localizer { get; set; } = default!;

    /// <summary>
    /// Gets or sets the visible and accessible description of the progress operation.
    /// A neutral <c>Progress</c> fallback is rendered when this value is empty.
    /// </summary>
    [Parameter]
    public string? Label { get; set; }

    /// <summary>
    /// Gets or sets the current progress value. A <see langword="null" /> value
    /// renders indeterminate progress.
    /// </summary>
    [Parameter]
    public double? Value { get; set; }

    /// <summary>
    /// Gets or sets the inclusive lower bound for determinate progress.
    /// </summary>
    [Parameter]
    public double Minimum { get; set; }

    /// <summary>
    /// Gets or sets the inclusive upper bound for determinate progress.
    /// </summary>
    [Parameter]
    public double Maximum { get; set; } = 100d;

    /// <summary>
    /// Gets or sets whether determinate progress renders its percentage as text.
    /// </summary>
    [Parameter]
    public bool ShowValue { get; set; } = true;

    private bool IsDeterminate => Value.HasValue;

    private string EffectiveLabel => string.IsNullOrWhiteSpace(Label)
        ? Localizer["ProgressLabel"].Value
        : Label.Trim();

    private double Percentage => IsDeterminate
        ? ((Value!.Value - Minimum) / (Maximum - Minimum)) * 100d
        : 0d;

    private string PercentText => $"{Percentage.ToString("0", CultureInfo.CurrentCulture)}%";

    private string ProgressFallbackText => IsDeterminate
        ? $"{EffectiveLabel}: {PercentText}"
        : EffectiveLabel;

    private IReadOnlyDictionary<string, object> RootAttributes
    {
        get
        {
            var attributes = new Dictionary<string, object>(
                BuildAttributes("bzs-progress"),
                StringComparer.OrdinalIgnoreCase)
            {
                ["data-bzs-progress"] = IsDeterminate ? "determinate" : "indeterminate",
            };

            return attributes;
        }
    }

    private IReadOnlyDictionary<string, object> ProgressAttributes
    {
        get
        {
            var attributes = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase)
            {
                ["class"] = "bzs-progress__track",
                ["role"] = "progressbar",
                ["aria-label"] = EffectiveLabel,
                ["aria-valuemin"] = FormatNumber(Minimum),
                ["aria-valuemax"] = FormatNumber(Maximum),
            };

            if (IsDeterminate)
            {
                attributes["value"] = FormatNumber(Value!.Value - Minimum);
                attributes["max"] = FormatNumber(Maximum - Minimum);
                attributes["aria-valuenow"] = FormatNumber(Value.Value);
                attributes["aria-valuetext"] = PercentText;
            }
            else
            {
                attributes["aria-busy"] = "true";
            }

            return attributes;
        }
    }

    /// <inheritdoc />
    protected override void OnParametersSet()
    {
        if (!double.IsFinite(Minimum))
        {
            throw new ArgumentOutOfRangeException(nameof(Minimum), Minimum, "The progress minimum must be finite.");
        }

        if (!double.IsFinite(Maximum) || Maximum <= Minimum)
        {
            throw new ArgumentOutOfRangeException(nameof(Maximum), Maximum, "The progress maximum must be finite and greater than Minimum.");
        }

        if (Value is double value && (!double.IsFinite(value) || value < Minimum || value > Maximum))
        {
            throw new ArgumentOutOfRangeException(nameof(Value), Value, "The progress value must be finite and within the configured range.");
        }
    }

    private static string FormatNumber(double value) => value.ToString("0.################", CultureInfo.InvariantCulture);
}
