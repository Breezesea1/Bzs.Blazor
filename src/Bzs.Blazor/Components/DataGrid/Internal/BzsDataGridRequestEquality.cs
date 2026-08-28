namespace Bzs.Blazor;

/// <summary>
/// Decides when two DataGrid requests, sorts, or filters describe the same query. The request
/// lifecycle uses it to recognize a request it has already started, and the grid uses it to
/// recognize controlled state that has not actually changed.
/// </summary>
internal static class BzsDataGridRequestEquality
{
    internal static bool RequestsEqual(BzsDataGridRequest? left, BzsDataGridRequest right) =>
        left is not null
        && left.Page == right.Page
        && left.PageSize == right.PageSize
        && SortListsEqual(left.Sorts, right.Sorts)
        && string.Equals(left.SearchText, right.SearchText, StringComparison.Ordinal)
        && FilterListsEqual(left.Filters, right.Filters);

    internal static bool SortsEqual(BzsDataGridSort? left, BzsDataGridSort? right) =>
        ReferenceEquals(left, right)
        || left is not null
            && right is not null
            && left.Direction == right.Direction
            && string.Equals(left.ColumnKey, right.ColumnKey, StringComparison.Ordinal);

    internal static bool SortListsEqual(
        IReadOnlyList<BzsDataGridSort> left,
        IReadOnlyList<BzsDataGridSort> right)
    {
        if (left.Count != right.Count)
        {
            return false;
        }

        for (var index = 0; index < left.Count; index++)
        {
            if (!SortsEqual(left[index], right[index]))
            {
                return false;
            }
        }
        return true;
    }

    internal static bool FilterListsEqual(
        IReadOnlyList<BzsDataGridFilter> left,
        IReadOnlyList<BzsDataGridFilter> right)
    {
        if (left.Count != right.Count)
        {
            return false;
        }

        var orderedLeft = left.OrderBy(static filter => filter.ColumnKey, StringComparer.Ordinal).ToArray();
        var orderedRight = right.OrderBy(static filter => filter.ColumnKey, StringComparer.Ordinal).ToArray();
        for (var index = 0; index < orderedLeft.Length; index++)
        {
            if (!FiltersEqual(orderedLeft[index], orderedRight[index]))
            {
                return false;
            }
        }
        return true;
    }

    internal static bool FiltersEqual(BzsDataGridFilter left, BzsDataGridFilter right) =>
        string.Equals(left.ColumnKey, right.ColumnKey, StringComparison.Ordinal)
        && (left, right) switch
        {
            (BzsDataGridTextFilter first, BzsDataGridTextFilter second) =>
                first.Operator == second.Operator
                && first.CaseSensitive == second.CaseSensitive
                && string.Equals(first.Value, second.Value, StringComparison.Ordinal),
            (BzsDataGridNumberFilter first, BzsDataGridNumberFilter second) =>
                first.Operator == second.Operator && first.Value == second.Value,
            (BzsDataGridDateFilter first, BzsDataGridDateFilter second) =>
                first.Operator == second.Operator && first.Value == second.Value,
            (BzsDataGridBooleanFilter first, BzsDataGridBooleanFilter second) =>
                first.Value == second.Value,
            (BzsDataGridChoiceFilter first, BzsDataGridChoiceFilter second) =>
                first.Values.SequenceEqual(second.Values, StringComparer.Ordinal),
            _ => false,
        };
}
