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
