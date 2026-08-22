using System.Globalization;

namespace Bzs.Blazor;

/// <summary>Declares a typed field or template column for a containing DataGrid.</summary>
/// <typeparam name="TItem">The row item type.</typeparam>
public sealed partial class BzsDataGridColumn<TItem> : BzsComponentBase, IDisposable
{
    private BzsDataGrid<TItem>? _registeredGrid;

    [CascadingParameter]
    private BzsDataGrid<TItem>? Grid { get; set; }

    /// <summary>Gets or sets the unique key used by controlled sorting.</summary>
    [Parameter, EditorRequired]
    public string? Key { get; set; }

    /// <summary>Gets or sets the visible column title.</summary>
    [Parameter]
    public string? Title { get; set; }

    /// <summary>Gets or sets custom column-header content.</summary>
    [Parameter]
    public RenderFragment? HeaderTemplate { get; set; }

    /// <summary>Gets or sets an accessible column name when the header template has no text.</summary>
    [Parameter]
    public string? AccessibleName { get; set; }

    /// <summary>Gets or sets the typed row selector used by field cells.</summary>
    [Parameter]
    public Func<TItem, object?>? ValueSelector { get; set; }

    /// <summary>Gets or sets custom cell content for each row.</summary>
    [Parameter]
    public RenderFragment<TItem>? CellTemplate { get; set; }

    /// <summary>Gets or sets an optional format string for field values.</summary>
    [Parameter]
    public string? Format { get; set; }

    /// <summary>Gets or sets the typed selector used for global client-side search.</summary>
    [Parameter]
    public Func<TItem, string?>? SearchTextSelector { get; set; }

    /// <summary>Gets or sets the typed selector used for client-side filtering.</summary>
    [Parameter]
    public Func<TItem, object?>? FilterValueSelector { get; set; }

    /// <summary>Gets or sets whether the column exposes a sort command.</summary>
    [Parameter]
    public bool Sortable { get; set; }

    /// <summary>Gets or sets the built-in provider-filter value shape exposed below this header.</summary>
    [Parameter]
    public BzsDataGridFilterKind FilterKind { get; set; }

    /// <summary>Gets or sets the selectable values offered by a <see cref="BzsDataGridFilterKind.Choice" /> filter.</summary>
    [Parameter]
    public IReadOnlyList<string> FilterChoices { get; set; } = Array.Empty<string>();

    /// <summary>Gets or sets an optional typed comparison used for client sorting.</summary>
    [Parameter]
    public Comparison<TItem>? SortComparison { get; set; }

    /// <summary>Gets or sets the inline alignment used by the header and cells.</summary>
    [Parameter]
    public BzsDataGridAlignment Align { get; set; }

    /// <summary>Gets or sets the preferred column width, such as <c>12rem</c> or <c>160px</c>.</summary>
    [Parameter]
    public string? Width { get; set; }

    /// <summary>Gets or sets the minimum column width used while the grid scrolls.</summary>
    [Parameter]
    public string? MinWidth { get; set; }

    /// <summary>Gets or sets whether the column remains visible.</summary>
    [Parameter]
    public bool Visible { get; set; } = true;

    /// <summary>Gets or sets whether users may hide this column from the column chooser.</summary>
    [Parameter]
    public bool Hideable { get; set; } = true;

    /// <summary>Gets or sets whether users may resize this column.</summary>
    [Parameter]
    public bool Resizable { get; set; } = true;

    /// <summary>Gets or sets whether the column is pinned during horizontal scrolling.</summary>
    [Parameter]
    public BzsDataGridColumnSticky Sticky { get; set; }

    /// <summary>Gets or sets the built-in footer aggregate.</summary>
    [Parameter]
    public BzsDataGridAggregate Aggregate { get; set; }

    /// <summary>Gets or sets a custom footer cell template.</summary>
    [Parameter]
    public RenderFragment<BzsDataGridFooterContext<TItem>>? FooterTemplate { get; set; }

    /// <summary>Gets or sets the typed value selector used by numeric aggregates.</summary>
    [Parameter]
    public Func<TItem, decimal?>? AggregateValueSelector { get; set; }

    internal string EffectiveKey => Key!.Trim();

    internal string? EffectiveTitle => Normalize(Title);

    internal string? EffectiveAccessibleName => Normalize(AccessibleName) ?? EffectiveTitle;

    internal IReadOnlyDictionary<string, object> BuildHeaderAttributes(BzsDataGridSortDirection? sortDirection)
    {
        var attributes = new Dictionary<string, object>(
            BuildAttributes($"bzs-data-grid__header bzs-data-grid__header--{AlignName}"),
            StringComparer.OrdinalIgnoreCase)
        {
            ["scope"] = "col",
            ["data-bzs-data-grid-column"] = EffectiveKey,
            ["data-bzs-align"] = AlignName,
            ["data-bzs-sticky"] = StickyName,
        };

        AddSizingAttributes(attributes);

        attributes.Remove("role");
        attributes.Remove("aria-sort");
        attributes["scope"] = "col";
        if (!Sortable && HeaderTemplate is not null)
        {
            attributes["aria-label"] = EffectiveAccessibleName!;
        }
        if (sortDirection is not null)
        {
            attributes["aria-sort"] = sortDirection == BzsDataGridSortDirection.Ascending
                ? "ascending"
                : "descending";
        }

        return attributes;
    }

    internal RenderFragment RenderHeader() => HeaderTemplate ?? (builder => builder.AddContent(0, EffectiveTitle));

    internal RenderFragment RenderCell(TItem item) => CellTemplate is not null
        ? CellTemplate(item)
        : builder => builder.AddContent(0, FormatValue(ValueSelector!(item)));

    internal int Compare(TItem left, TItem right) => SortComparison is not null
        ? SortComparison(left, right)
        : BzsDataGridOperations.CompareValues(ValueSelector!(left), ValueSelector(right));

    internal string AlignName => Align switch
    {
        BzsDataGridAlignment.Center => "center",
        BzsDataGridAlignment.End => "end",
        _ => "start",
    };

    internal string StickyName => Sticky switch
    {
        BzsDataGridColumnSticky.Start => "start",
        BzsDataGridColumnSticky.End => "end",
        _ => "none",
    };

    internal string? EffectiveWidth => Normalize(Width);

    internal string? EffectiveMinWidth => Normalize(MinWidth);

    internal bool HasFooter => Aggregate != BzsDataGridAggregate.None || FooterTemplate is not null;

    /// <summary>Renders the footer cell content for the supplied summarized rows.</summary>
    internal RenderFragment RenderFooter(IReadOnlyList<TItem> items)
    {
        if (FooterTemplate is not null)
        {
            return FooterTemplate(new BzsDataGridFooterContext<TItem>(items));
        }

        var text = FormatAggregate(items);
        return builder => builder.AddContent(0, text);
    }

    internal IReadOnlyDictionary<string, object> BuildCellAttributes() => new Dictionary<string, object>(
        StringComparer.OrdinalIgnoreCase)
    {
        ["class"] = $"bzs-data-grid__cell bzs-data-grid__cell--{AlignName}",
        ["data-bzs-data-grid-column"] = EffectiveKey,
        ["data-bzs-align"] = AlignName,
        ["data-bzs-sticky"] = StickyName,
    };

    /// <summary>Gets the searchable text of one row, or <see langword="null" /> when the column is not searchable.</summary>
    internal string? GetSearchText(TItem item)
    {
        if (SearchTextSelector is not null)
        {
            return SearchTextSelector(item);
        }

        return ValueSelector is null ? null : FormatValue(ValueSelector(item));
    }

    /// <summary>Gets the typed filter value of one row used by client-side filtering.</summary>
    internal object? GetFilterValue(TItem item) => FilterValueSelector is not null
        ? FilterValueSelector(item)
        : ValueSelector?.Invoke(item);

    private string FormatAggregate(IReadOnlyList<TItem> items)
    {
        // Count summarizes rows, not column values, so it never uses the value Format.
        if (Aggregate == BzsDataGridAggregate.Count)
        {
            return items.Count.ToString(CultureInfo.CurrentCulture);
        }

        var value = BzsDataGridOperations.Aggregate(items, AggregateValueSelector, Aggregate);
        return value is null
            ? string.Empty
            : value.Value.ToString(Format, CultureInfo.CurrentCulture);
    }

    /// <inheritdoc />
    protected override void OnParametersSet()
    {
        if (Grid is null)
        {
            throw new InvalidOperationException("BzsDataGridColumn must be rendered inside BzsDataGrid.");
        }

        if (string.IsNullOrWhiteSpace(Key))
        {
            throw new ArgumentException("BzsDataGridColumn requires a non-empty Key.", nameof(Key));
        }

        if (HeaderTemplate is null && string.IsNullOrWhiteSpace(Title))
        {
            throw new InvalidOperationException("BzsDataGridColumn requires Title or HeaderTemplate.");
        }

        if (HeaderTemplate is not null && EffectiveAccessibleName is null)
        {
            throw new InvalidOperationException(
                "A templated BzsDataGridColumn requires Title or AccessibleName.");
        }

        if (ValueSelector is null && CellTemplate is null)
        {
            throw new InvalidOperationException("BzsDataGridColumn requires ValueSelector or CellTemplate.");
        }

        if (Sortable && ValueSelector is null && SortComparison is null)
        {
            throw new InvalidOperationException(
                "A sortable BzsDataGridColumn requires ValueSelector or SortComparison.");
        }

        if (!Enum.IsDefined(FilterKind))
        {
            throw new ArgumentOutOfRangeException(
                nameof(FilterKind),
                FilterKind,
                "The DataGrid filter kind is not supported.");
        }

        if (!Enum.IsDefined(Align))
        {
            throw new ArgumentOutOfRangeException(nameof(Align), Align, "The DataGrid column alignment is not supported.");
        }

        if (FilterChoices is null)
        {
            throw new InvalidOperationException("BzsDataGridColumn FilterChoices cannot be null.");
        }

        if (FilterKind == BzsDataGridFilterKind.Choice && FilterChoices.Count == 0)
        {
            throw new InvalidOperationException("A choice-filtered BzsDataGridColumn requires FilterChoices.");
        }

        if (!Enum.IsDefined(Sticky))
        {
            throw new ArgumentOutOfRangeException(nameof(Sticky), Sticky, "The DataGrid column sticky mode is not supported.");
        }

        if (!Enum.IsDefined(Aggregate))
        {
            throw new ArgumentOutOfRangeException(nameof(Aggregate), Aggregate, "The DataGrid column aggregate is not supported.");
        }

        if (Aggregate != BzsDataGridAggregate.None && Aggregate != BzsDataGridAggregate.Count
            && AggregateValueSelector is null)
        {
            throw new InvalidOperationException(
                $"The DataGrid aggregate '{Aggregate}' requires AggregateValueSelector.");
        }

        if (!ReferenceEquals(_registeredGrid, Grid))
        {
            _registeredGrid?.Unregister(this);
            _registeredGrid = Grid;
        }

        _registeredGrid.RegisterOrUpdate(this);
    }

    /// <inheritdoc />
    public void Dispose()
    {
        _registeredGrid?.Unregister(this);
        _registeredGrid = null;
    }

    private string FormatValue(object? value)
    {
        if (value is null)
        {
            return string.Empty;
        }

        return value is IFormattable formattable
            ? formattable.ToString(Format, CultureInfo.CurrentCulture) ?? string.Empty
            : value.ToString() ?? string.Empty;
    }

    private void AddSizingAttributes(Dictionary<string, object> attributes)
    {
        if (EffectiveWidth is { } width)
        {
            attributes["data-bzs-width"] = width;
        }

        if (EffectiveMinWidth is { } minWidth)
        {
            attributes["data-bzs-min-width"] = minWidth;
        }

        if (Resizable)
        {
            attributes["data-bzs-resizable"] = "true";
        }
    }

    private static string? Normalize(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
