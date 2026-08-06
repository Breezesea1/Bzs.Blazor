using System.Globalization;

namespace Bzs.Blazor;

/// <summary>
/// Occupies a responsive number of columns within a <see cref="BzsGrid"/>.
/// </summary>
public sealed partial class BzsGridItem : BzsComponentBase
{
    /// <summary>Gets or sets the 1-12 column span at all viewport sizes.</summary>
    [Parameter]
    public int? Xs { get; set; }

    /// <summary>Gets or sets the 1-12 column span from the small breakpoint.</summary>
    [Parameter]
    public int? Sm { get; set; }

    /// <summary>Gets or sets the 1-12 column span from the medium breakpoint.</summary>
    [Parameter]
    public int? Md { get; set; }

    /// <summary>Gets or sets the 1-12 column span from the large breakpoint.</summary>
    [Parameter]
    public int? Lg { get; set; }

    /// <summary>Gets or sets the 1-12 column span from the extra-large breakpoint.</summary>
    [Parameter]
    public int? Xl { get; set; }

    /// <summary>Gets or sets the 1-12 column span from the widest breakpoint.</summary>
    [Parameter]
    public int? Xxl { get; set; }

    /// <summary>
    /// Gets or sets the content rendered inside the grid item.
    /// </summary>
    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    private IReadOnlyDictionary<string, object> RootAttributes
    {
        get
        {
            var attributes = new Dictionary<string, object>(
                BuildAttributes("bzs-grid-item"),
                StringComparer.OrdinalIgnoreCase)
            {
                ["data-bzs-grid-item"] = string.Empty,
            };

            AddSpan(attributes, "xs", Xs);
            AddSpan(attributes, "sm", Sm);
            AddSpan(attributes, "md", Md);
            AddSpan(attributes, "lg", Lg);
            AddSpan(attributes, "xl", Xl);
            AddSpan(attributes, "xxl", Xxl);
            return attributes;
        }
    }

    /// <inheritdoc />
    protected override void OnParametersSet()
    {
        ValidateSpan(Xs, nameof(Xs));
        ValidateSpan(Sm, nameof(Sm));
        ValidateSpan(Md, nameof(Md));
        ValidateSpan(Lg, nameof(Lg));
        ValidateSpan(Xl, nameof(Xl));
        ValidateSpan(Xxl, nameof(Xxl));
    }

    private static void AddSpan(IDictionary<string, object> attributes, string breakpoint, int? span)
    {
        if (span is not null)
        {
            attributes[$"data-bzs-{breakpoint}"] = span.Value.ToString(CultureInfo.InvariantCulture);
        }
    }

    private static void ValidateSpan(int? span, string parameterName)
    {
        if (span is < 1 or > 12)
        {
            throw new ArgumentOutOfRangeException(parameterName, span, "Grid spans must be between 1 and 12.");
        }
    }
}
