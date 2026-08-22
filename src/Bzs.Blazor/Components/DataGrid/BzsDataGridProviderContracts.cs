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
    /// <param name="sorts">
    /// The optional precedence-ordered controlled sorts. When supplied together with
    /// <paramref name="sort" />, the first entry must equal <paramref name="sort" />.
    /// </param>
    /// <param name="searchText">The optional global search text.</param>
    public BzsDataGridRequest(
        int page,
        int pageSize,
        BzsDataGridSort? sort = null,
        IReadOnlyList<BzsDataGridFilter>? filters = null,
        IReadOnlyList<BzsDataGridSort>? sorts = null,
        string? searchText = null)
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
        Sorts = CreateSorts(sort, sorts);
        SearchText = string.IsNullOrWhiteSpace(searchText) ? null : searchText.Trim();
    }

    /// <summary>Gets the requested one-based page.</summary>
    public int Page { get; }

    /// <summary>Gets the maximum number of requested items.</summary>
    public int PageSize { get; }

    /// <summary>
    /// Gets the primary controlled sort, which is the first entry of <see cref="Sorts" />.
    /// </summary>
    public BzsDataGridSort? Sort { get; }

    /// <summary>
    /// Gets the precedence-ordered controlled sorts. Providers that support only one sort
    /// column may use <see cref="Sort" /> instead.
    /// </summary>
    public IReadOnlyList<BzsDataGridSort> Sorts { get; }

    /// <summary>Gets the trimmed global search text, or <see langword="null" /> when no search is active.</summary>
    public string? SearchText { get; }

    /// <summary>Gets the controlled filters combined with logical AND.</summary>
    public IReadOnlyList<BzsDataGridFilter> Filters { get; }

    private static IReadOnlyList<BzsDataGridSort> CreateSorts(
        BzsDataGridSort? sort,
        IReadOnlyList<BzsDataGridSort>? sorts)
    {
        if (sorts is null)
        {
            return sort is null ? Array.Empty<BzsDataGridSort>() : Array.AsReadOnly(new[] { sort });
        }

        var snapshot = sorts.ToArray();
        var columnKeys = new HashSet<string>(StringComparer.Ordinal);
        foreach (var entry in snapshot)
        {
            ArgumentNullException.ThrowIfNull(entry, nameof(sorts));
            if (!columnKeys.Add(entry.ColumnKey))
            {
                throw new ArgumentException(
                    $"A DataGrid provider request accepts at most one sort for column '{entry.ColumnKey}'.",
                    nameof(sorts));
            }
        }

        if (sort is not null
            && (snapshot.Length == 0
                || !string.Equals(snapshot[0].ColumnKey, sort.ColumnKey, StringComparison.Ordinal)
                || snapshot[0].Direction != sort.Direction))
        {
            throw new ArgumentException(
                "A DataGrid provider request requires its primary sort to be the first entry of sorts.",
                nameof(sorts));
        }

        return Array.AsReadOnly(snapshot);
    }
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

    /// <summary>The column accepts a set of selected values chosen from declared choices.</summary>
    Choice,
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

    /// <summary>Matches values that do not contain the supplied text.</summary>
    NotContains,

    /// <summary>Matches empty values.</summary>
    IsEmpty,

    /// <summary>Matches non-empty values.</summary>
    IsNotEmpty,
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
        if (!Enum.IsDefined(@operator))
        {
            throw new ArgumentOutOfRangeException(nameof(@operator), @operator, "The DataGrid text operator is not supported.");
        }

        if (RequiresValue(@operator))
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(value);
        }
        else
        {
            ArgumentNullException.ThrowIfNull(value);
        }

        Value = value.Trim();
        Operator = @operator;
        CaseSensitive = caseSensitive;
    }

    /// <summary>Initializes a presence text filter that carries no comparison value.</summary>
    /// <param name="columnKey">The unique key of the filtered column.</param>
    /// <param name="operator">The presence operator, either <see cref="BzsDataGridTextOperator.IsEmpty" /> or <see cref="BzsDataGridTextOperator.IsNotEmpty" />.</param>
    public BzsDataGridTextFilter(string columnKey, BzsDataGridTextOperator @operator)
        : base(columnKey)
    {
        if (RequiresValue(@operator))
        {
            throw new ArgumentOutOfRangeException(
                nameof(@operator),
                @operator,
                "The DataGrid text operator requires a comparison value.");
        }

        Value = string.Empty;
        Operator = @operator;
    }

    /// <summary>Gets the text value, which is empty for presence operators.</summary>
    public string Value { get; }

    /// <summary>Gets the text operation.</summary>
    public BzsDataGridTextOperator Operator { get; }

    /// <summary>Gets whether matching is case-sensitive.</summary>
    public bool CaseSensitive { get; }

    /// <summary>Determines whether the supplied operator compares against a value.</summary>
    /// <param name="operator">The text operator.</param>
    /// <returns><see langword="true" /> when the operator needs a non-empty value.</returns>
    public static bool RequiresValue(BzsDataGridTextOperator @operator) =>
        @operator is not (BzsDataGridTextOperator.IsEmpty or BzsDataGridTextOperator.IsNotEmpty);
}

/// <summary>Describes a multi-value choice filter for one DataGrid column.</summary>
public sealed class BzsDataGridChoiceFilter : BzsDataGridFilter
{
    /// <summary>Initializes a choice filter.</summary>
    /// <param name="columnKey">The unique key of the filtered column.</param>
    /// <param name="values">The selected non-empty choice values combined with logical OR.</param>
    public BzsDataGridChoiceFilter(string columnKey, IReadOnlyList<string> values)
        : base(columnKey)
    {
        ArgumentNullException.ThrowIfNull(values);
        if (values.Count == 0)
        {
            throw new ArgumentException("A DataGrid choice filter requires at least one value.", nameof(values));
        }

        var snapshot = new List<string>(values.Count);
        var seen = new HashSet<string>(StringComparer.Ordinal);
        foreach (var value in values)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(value, nameof(values));
            var trimmed = value.Trim();
            if (seen.Add(trimmed))
            {
                snapshot.Add(trimmed);
            }
        }

        Values = snapshot.AsReadOnly();
    }

    /// <summary>Gets the selected choice values combined with logical OR.</summary>
    public IReadOnlyList<string> Values { get; }
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
