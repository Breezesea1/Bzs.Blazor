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

    /// <summary>Gets or sets whether the column exposes a sort command.</summary>
    [Parameter]
    public bool Sortable { get; set; }

    /// <summary>Gets or sets the built-in provider-filter value shape exposed below this header.</summary>
    [Parameter]
    public BzsDataGridFilterKind FilterKind { get; set; }

    /// <summary>Gets or sets an optional typed comparison used for client sorting.</summary>
    [Parameter]
    public Comparison<TItem>? SortComparison { get; set; }

    internal string EffectiveKey => Key!.Trim();

    internal string? EffectiveTitle => Normalize(Title);

    internal string? EffectiveAccessibleName => Normalize(AccessibleName) ?? EffectiveTitle;

    internal IReadOnlyDictionary<string, object> BuildHeaderAttributes(BzsDataGridSort? sort)
    {
        var attributes = new Dictionary<string, object>(
            BuildAttributes("bzs-data-grid__header"),
            StringComparer.OrdinalIgnoreCase)
        {
            ["scope"] = "col",
            ["data-bzs-data-grid-column"] = EffectiveKey,
        };

        attributes.Remove("role");
        attributes.Remove("aria-sort");
        attributes["scope"] = "col";
        if (!Sortable && HeaderTemplate is not null)
        {
            attributes["aria-label"] = EffectiveAccessibleName!;
        }
        if (sort is not null && string.Equals(sort.ColumnKey, EffectiveKey, StringComparison.Ordinal))
        {
            attributes["aria-sort"] = sort.Direction == BzsDataGridSortDirection.Ascending
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

    private static string? Normalize(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
