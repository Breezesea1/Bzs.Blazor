namespace Bzs.Blazor;

/// <summary>Selects the direction of a DataGrid sort.</summary>
public enum BzsDataGridSortDirection
{
    /// <summary>Sorts lower values before higher values.</summary>
    Ascending,

    /// <summary>Sorts higher values before lower values.</summary>
    Descending,
}

/// <summary>Describes the controlled single-column sort applied to a DataGrid.</summary>
public sealed record BzsDataGridSort
{
    /// <summary>Initializes a DataGrid sort.</summary>
    /// <param name="columnKey">The unique key of the sorted column.</param>
    /// <param name="direction">The sort direction.</param>
    public BzsDataGridSort(string columnKey, BzsDataGridSortDirection direction)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(columnKey);
        if (!Enum.IsDefined(direction))
        {
            throw new ArgumentOutOfRangeException(nameof(direction), direction, "The DataGrid sort direction is not supported.");
        }

        ColumnKey = columnKey.Trim();
        Direction = direction;
    }

    /// <summary>Gets the unique key of the sorted column.</summary>
    public string ColumnKey { get; }

    /// <summary>Gets the sort direction.</summary>
    public BzsDataGridSortDirection Direction { get; }
}

/// <summary>Selects the controlled row-selection behavior of a DataGrid.</summary>
public enum BzsDataGridSelectionMode
{
    /// <summary>Does not render row-selection controls.</summary>
    None,

    /// <summary>Allows one selected row.</summary>
    Single,

    /// <summary>Allows multiple selected rows.</summary>
    Multiple,
}

/// <summary>Selects the inline size of DataGrid rows and controls.</summary>
public enum BzsDataGridDensity
{
    /// <summary>Uses the productivity-oriented default row height.</summary>
    Compact,

    /// <summary>Uses a taller row height with more breathing room.</summary>
    Comfortable,
}

/// <summary>Selects the inline alignment of a DataGrid column.</summary>
public enum BzsDataGridAlignment
{
    /// <summary>Aligns content to the inline start edge.</summary>
    Start,

    /// <summary>Centers content along the inline axis.</summary>
    Center,

    /// <summary>Aligns content to the inline end edge.</summary>
    End,
}

/// <summary>Selects whether a DataGrid column stays pinned while the viewport scrolls.</summary>
public enum BzsDataGridColumnSticky
{
    /// <summary>Scrolls with the remaining columns.</summary>
    None,

    /// <summary>Pins the column to the inline start edge.</summary>
    Start,

    /// <summary>Pins the column to the inline end edge.</summary>
    End,
}

/// <summary>Selects the built-in footer aggregate computed for a DataGrid column.</summary>
public enum BzsDataGridAggregate
{
    /// <summary>Computes no aggregate.</summary>
    None,

    /// <summary>Counts the aggregated rows.</summary>
    Count,

    /// <summary>Sums the selected numeric values.</summary>
    Sum,

    /// <summary>Averages the selected numeric values.</summary>
    Average,

    /// <summary>Selects the smallest numeric value.</summary>
    Minimum,

    /// <summary>Selects the largest numeric value.</summary>
    Maximum,
}

/// <summary>Describes the rows available to a DataGrid footer template.</summary>
/// <typeparam name="TItem">The row item type.</typeparam>
public sealed class BzsDataGridFooterContext<TItem>
{
    /// <summary>Initializes a footer context.</summary>
    /// <param name="items">The rows the footer summarizes.</param>
    public BzsDataGridFooterContext(IReadOnlyList<TItem> items)
    {
        ArgumentNullException.ThrowIfNull(items);
        Items = items;
    }

    /// <summary>Gets the rows the footer summarizes.</summary>
    public IReadOnlyList<TItem> Items { get; }
}

/// <summary>Describes a column resize committed by the browser.</summary>
public sealed class BzsDataGridColumnResizeEventArgs
{
    /// <summary>Initializes column resize event arguments.</summary>
    /// <param name="columnKey">The resized column key.</param>
    /// <param name="width">The committed CSS pixel width.</param>
    public BzsDataGridColumnResizeEventArgs(string columnKey, double width)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(columnKey);
        if (double.IsNaN(width) || double.IsInfinity(width) || width <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(width), width, "A DataGrid column width must be positive and finite.");
        }

        ColumnKey = columnKey.Trim();
        Width = width;
    }

    /// <summary>Gets the resized column key.</summary>
    public string ColumnKey { get; }

    /// <summary>Gets the committed CSS pixel width.</summary>
    public double Width { get; }
}

/// <summary>Describes one row activation requested by a DataGrid.</summary>
/// <typeparam name="TItem">The row item type.</typeparam>
public sealed class BzsDataGridRowEventArgs<TItem>
{
    /// <summary>Initializes row event arguments.</summary>
    /// <param name="item">The activated row item.</param>
    /// <param name="rowIndex">The zero-based index of the row within the rendered page.</param>
    public BzsDataGridRowEventArgs(TItem item, int rowIndex)
    {
        if (rowIndex < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(rowIndex), rowIndex, "A DataGrid row index cannot be negative.");
        }

        Item = item;
        RowIndex = rowIndex;
    }

    /// <summary>Gets the activated row item.</summary>
    public TItem Item { get; }

    /// <summary>Gets the zero-based index of the row within the rendered page.</summary>
    public int RowIndex { get; }
}
