namespace Bzs.Blazor;

/// <summary>Provides request-driven pages for <see cref="BzsDataGrid{TItem}" />.</summary>
/// <typeparam name="TItem">The row item type.</typeparam>
public interface IBzsDataGridProvider<TItem>
{
    /// <summary>Gets one page for the supplied controlled grid state.</summary>
    /// <param name="request">The immutable page, sort, and filter request.</param>
    /// <param name="cancellationToken">A token canceled when the request is superseded or the grid is disposed.</param>
    /// <returns>The page items and either a known total or explicit next-page availability.</returns>
    ValueTask<BzsDataGridResult<TItem>> GetItemsAsync(
        BzsDataGridRequest request,
        CancellationToken cancellationToken);
}

/// <summary>Describes one immutable DataGrid provider request.</summary>
public sealed class BzsDataGridRequest
{
    /// <summary>Initializes a provider request.</summary>
    /// <param name="page">The requested one-based page.</param>
    /// <param name="pageSize">The maximum number of requested items.</param>
    /// <param name="sort">The optional controlled single-column sort.</param>
    /// <param name="filters">The optional controlled filters. At most one filter is accepted per column.</param>
    public BzsDataGridRequest(
        int page,
        int pageSize,
        BzsDataGridSort? sort = null,
        IReadOnlyList<BzsDataGridFilter>? filters = null)
    {
        if (page < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(page), page, "A DataGrid provider page must be positive.");
        }
        if (pageSize < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(pageSize), pageSize, "A DataGrid provider page size must be positive.");
        }

        var snapshot = filters?.ToArray() ?? [];
        var columnKeys = new HashSet<string>(StringComparer.Ordinal);
        foreach (var filter in snapshot)
        {
            ArgumentNullException.ThrowIfNull(filter, nameof(filters));
            if (!columnKeys.Add(filter.ColumnKey))
            {
                throw new ArgumentException(
                    $"A DataGrid provider request accepts at most one filter for column '{filter.ColumnKey}'.",
                    nameof(filters));
            }
        }

        Page = page;
        PageSize = pageSize;
        Sort = sort;
        Filters = Array.AsReadOnly(snapshot);
    }

    /// <summary>Gets the requested one-based page.</summary>
    public int Page { get; }

    /// <summary>Gets the maximum number of requested items.</summary>
    public int PageSize { get; }

    /// <summary>Gets the optional controlled single-column sort.</summary>
    public BzsDataGridSort? Sort { get; }

    /// <summary>Gets the controlled filters combined with logical AND.</summary>
    public IReadOnlyList<BzsDataGridFilter> Filters { get; }
}

/// <summary>Contains one immutable DataGrid provider result.</summary>
/// <typeparam name="TItem">The row item type.</typeparam>
public sealed class BzsDataGridResult<TItem>
{
    /// <summary>Initializes a result with a known total item count.</summary>
    /// <param name="items">The items returned for the requested page.</param>
    /// <param name="totalCount">The non-negative total item count.</param>
    public BzsDataGridResult(IReadOnlyList<TItem> items, int totalCount)
    {
        ArgumentNullException.ThrowIfNull(items);
        if (totalCount < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(totalCount), totalCount, "A DataGrid total count cannot be negative.");
        }

        Items = Snapshot(items);
        TotalCount = totalCount;
    }

    /// <summary>Initializes a result whose total item count is unknown.</summary>
    /// <param name="items">The items returned for the requested page.</param>
    /// <param name="hasNextPage">Whether another page is available after this result.</param>
    public BzsDataGridResult(IReadOnlyList<TItem> items, bool hasNextPage)
    {
        ArgumentNullException.ThrowIfNull(items);
        Items = Snapshot(items);
        HasNextPage = hasNextPage;
    }

    /// <summary>Gets the snapshotted page items.</summary>
    public IReadOnlyList<TItem> Items { get; }

    /// <summary>Gets the known total count, or <see langword="null" /> when the total is unknown.</summary>
    public int? TotalCount { get; }

    /// <summary>Gets next-page availability when the total is unknown; otherwise <see langword="null" />.</summary>
    public bool? HasNextPage { get; }

    private static IReadOnlyList<TItem> Snapshot(IReadOnlyList<TItem> items) =>
        Array.AsReadOnly(items.ToArray());
}

/// <summary>Selects the built-in value shape used by a DataGrid column filter.</summary>
public enum BzsDataGridFilterKind
{
    /// <summary>The column does not expose a built-in filter.</summary>
    None,

    /// <summary>The column accepts text filter values.</summary>
    Text,

    /// <summary>The column accepts invariant decimal filter values.</summary>
    Number,

    /// <summary>The column accepts date-only filter values.</summary>
    Date,

    /// <summary>The column accepts Boolean filter values.</summary>
    Boolean,
}

/// <summary>Selects a text-filter operation.</summary>
public enum BzsDataGridTextOperator
{
    /// <summary>Matches values containing the supplied text.</summary>
    Contains,

    /// <summary>Matches values beginning with the supplied text.</summary>
    StartsWith,

    /// <summary>Matches values ending with the supplied text.</summary>
    EndsWith,

    /// <summary>Matches values equal to the supplied text.</summary>
    Equals,
}

/// <summary>Selects a number- or date-filter comparison.</summary>
public enum BzsDataGridComparisonOperator
{
    /// <summary>Matches equal values.</summary>
    Equals,

    /// <summary>Matches unequal values.</summary>
    NotEquals,

    /// <summary>Matches values less than the supplied value.</summary>
    LessThan,

    /// <summary>Matches values less than or equal to the supplied value.</summary>
    LessThanOrEqual,

    /// <summary>Matches values greater than the supplied value.</summary>
    GreaterThan,

    /// <summary>Matches values greater than or equal to the supplied value.</summary>
    GreaterThanOrEqual,
}

/// <summary>Provides the closed base contract for a typed DataGrid filter.</summary>
public abstract class BzsDataGridFilter
{
    private protected BzsDataGridFilter(string columnKey)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(columnKey);
        ColumnKey = columnKey.Trim();
    }

    /// <summary>Gets the opaque unique key of the filtered column.</summary>
    public string ColumnKey { get; }
}

/// <summary>Describes a text filter for one DataGrid column.</summary>
public sealed class BzsDataGridTextFilter : BzsDataGridFilter
{
    /// <summary>Initializes a text filter.</summary>
    public BzsDataGridTextFilter(
        string columnKey,
        string value,
        BzsDataGridTextOperator @operator = BzsDataGridTextOperator.Contains,
        bool caseSensitive = false)
        : base(columnKey)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        if (!Enum.IsDefined(@operator))
        {
            throw new ArgumentOutOfRangeException(nameof(@operator), @operator, "The DataGrid text operator is not supported.");
        }

        Value = value.Trim();
        Operator = @operator;
        CaseSensitive = caseSensitive;
    }

    /// <summary>Gets the non-empty text value.</summary>
    public string Value { get; }

    /// <summary>Gets the text operation.</summary>
    public BzsDataGridTextOperator Operator { get; }

    /// <summary>Gets whether matching is case-sensitive.</summary>
    public bool CaseSensitive { get; }
}

/// <summary>Describes an invariant decimal filter for one DataGrid column.</summary>
public sealed class BzsDataGridNumberFilter : BzsDataGridFilter
{
    /// <summary>Initializes a number filter.</summary>
    public BzsDataGridNumberFilter(
        string columnKey,
        decimal value,
        BzsDataGridComparisonOperator @operator = BzsDataGridComparisonOperator.Equals)
        : base(columnKey)
    {
        ValidateComparisonOperator(@operator);
        Value = value;
        Operator = @operator;
    }

    /// <summary>Gets the invariant decimal value.</summary>
    public decimal Value { get; }

    /// <summary>Gets the comparison operation.</summary>
    public BzsDataGridComparisonOperator Operator { get; }

    private static void ValidateComparisonOperator(BzsDataGridComparisonOperator @operator)
    {
        if (!Enum.IsDefined(@operator))
        {
            throw new ArgumentOutOfRangeException(nameof(@operator), @operator, "The DataGrid comparison operator is not supported.");
        }
    }
}

/// <summary>Describes a date-only filter for one DataGrid column.</summary>
public sealed class BzsDataGridDateFilter : BzsDataGridFilter
{
    /// <summary>Initializes a date filter.</summary>
    public BzsDataGridDateFilter(
        string columnKey,
        DateOnly value,
        BzsDataGridComparisonOperator @operator = BzsDataGridComparisonOperator.Equals)
        : base(columnKey)
    {
        if (!Enum.IsDefined(@operator))
        {
            throw new ArgumentOutOfRangeException(nameof(@operator), @operator, "The DataGrid comparison operator is not supported.");
        }

        Value = value;
        Operator = @operator;
    }

    /// <summary>Gets the date-only value.</summary>
    public DateOnly Value { get; }

    /// <summary>Gets the comparison operation.</summary>
    public BzsDataGridComparisonOperator Operator { get; }
}

/// <summary>Describes a Boolean equality filter for one DataGrid column.</summary>
public sealed class BzsDataGridBooleanFilter : BzsDataGridFilter
{
    /// <summary>Initializes a Boolean filter.</summary>
    public BzsDataGridBooleanFilter(string columnKey, bool value)
        : base(columnKey)
    {
        Value = value;
    }

    /// <summary>Gets the Boolean value to match.</summary>
    public bool Value { get; }
}
