using System.Collections;

namespace Bzs.Blazor;

internal static class BzsDataGridOperations
{
    internal static IReadOnlyList<TItem> Apply<TItem>(
        IReadOnlyList<TItem> items,
        Comparison<TItem>? comparison,
        BzsDataGridSortDirection? direction,
        int page,
        int pageSize)
    {
        IEnumerable<IndexedItem<TItem>> query = items.Select(
            static (item, index) => new IndexedItem<TItem>(item, index));

        if (comparison is not null && direction is not null)
        {
            var directionMultiplier = direction == BzsDataGridSortDirection.Ascending ? 1 : -1;
            query = query.OrderBy(
                static item => item,
                Comparer<IndexedItem<TItem>>.Create((left, right) =>
                {
                    var result = comparison(left.Item, right.Item);
                    if (directionMultiplier < 0)
                    {
                        result = result > 0 ? -1 : result < 0 ? 1 : 0;
                    }
                    return result != 0 ? result : left.Index.CompareTo(right.Index);
                }));
        }

        return query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(static item => item.Item)
            .ToArray();
    }

    internal static int CompareValues(object? left, object? right)
    {
        if (ReferenceEquals(left, right))
        {
            return 0;
        }

        if (left is null)
        {
            return -1;
        }

        if (right is null)
        {
            return 1;
        }

        if (left is string leftText && right is string rightText)
        {
            return StringComparer.CurrentCulture.Compare(leftText, rightText);
        }

        if (left is IComparable comparable)
        {
            return comparable.CompareTo(right);
        }

        throw new InvalidOperationException(
            $"Values of type '{left.GetType().Name}' are not comparable. Supply SortComparison on the DataGrid column.");
    }

    private readonly record struct IndexedItem<TItem>(TItem Item, int Index);
}
