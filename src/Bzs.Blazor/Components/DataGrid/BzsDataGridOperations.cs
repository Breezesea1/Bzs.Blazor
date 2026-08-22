using System.Collections;
using System.Globalization;

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
        var sorted = comparison is not null && direction is not null
            ? Sort(items, [new SortStep<TItem>(comparison, direction.Value)])
            : items;
        return Paginate(sorted, page, pageSize);
    }

    internal static IReadOnlyList<TItem> Paginate<TItem>(
        IReadOnlyList<TItem> items,
        int page,
        int pageSize)
    {
        var offset = (page - 1L) * pageSize;
        if (offset >= items.Count)
        {
            return Array.Empty<TItem>();
        }

        var start = (int)offset;
        var length = Math.Min(pageSize, items.Count - start);
        var window = new TItem[length];
        for (var index = 0; index < length; index++)
        {
            window[index] = items[start + index];
        }
        return window;
    }

    /// <summary>Orders items by the supplied precedence-ordered steps, keeping equal items in source order.</summary>
    internal static IReadOnlyList<TItem> Sort<TItem>(
        IReadOnlyList<TItem> items,
        IReadOnlyList<SortStep<TItem>> steps)
    {
        if (steps.Count == 0 || items.Count < 2)
        {
            return items;
        }

        var indexed = new IndexedItem<TItem>[items.Count];
        for (var index = 0; index < items.Count; index++)
        {
            indexed[index] = new IndexedItem<TItem>(items[index], index);
        }

        Array.Sort(indexed, (left, right) =>
        {
            foreach (var step in steps)
            {
                var result = step.Comparison(left.Item, right.Item);
                if (result == 0)
                {
                    continue;
                }

                return step.Direction == BzsDataGridSortDirection.Ascending
                    ? result < 0 ? -1 : 1
                    : result < 0 ? 1 : -1;
            }

            return left.Index.CompareTo(right.Index);
        });

        var sorted = new TItem[indexed.Length];
        for (var index = 0; index < indexed.Length; index++)
        {
            sorted[index] = indexed[index].Item;
        }
        return sorted;
    }

    /// <summary>Keeps the items that satisfy every supplied predicate.</summary>
    internal static IReadOnlyList<TItem> Filter<TItem>(
        IReadOnlyList<TItem> items,
        IReadOnlyList<Func<TItem, bool>> predicates)
    {
        if (predicates.Count == 0)
        {
            return items;
        }

        var matches = new List<TItem>(items.Count);
        foreach (var item in items)
        {
            var included = true;
            foreach (var predicate in predicates)
            {
                if (!predicate(item))
                {
                    included = false;
                    break;
                }
            }

            if (included)
            {
                matches.Add(item);
            }
        }
        return matches;
    }

    /// <summary>Evaluates one typed filter against one already-selected column value.</summary>
    internal static bool Matches(BzsDataGridFilter filter, object? value) => filter switch
    {
        BzsDataGridTextFilter text => MatchesText(text, value),
        BzsDataGridNumberFilter number => TryConvertDecimal(value, out var candidate)
            && Compare(number.Operator, decimal.Compare(candidate, number.Value)),
        BzsDataGridDateFilter date => TryConvertDate(value, out var candidate)
            && Compare(date.Operator, candidate.CompareTo(date.Value)),
        BzsDataGridBooleanFilter boolean => value is bool candidate && candidate == boolean.Value,
        BzsDataGridChoiceFilter choice => MatchesChoice(choice, value),
        _ => true,
    };

    /// <summary>Determines whether the supplied text occurs in any searchable value.</summary>
    internal static bool MatchesSearch(IReadOnlyList<string?> searchTexts, string search)
    {
        foreach (var text in searchTexts)
        {
            if (!string.IsNullOrEmpty(text)
                && text.Contains(search, StringComparison.CurrentCultureIgnoreCase))
            {
                return true;
            }
        }
        return false;
    }

    /// <summary>Computes one numeric aggregate over the supplied items.</summary>
    internal static decimal? Aggregate<TItem>(
        IReadOnlyList<TItem> items,
        Func<TItem, decimal?>? selector,
        BzsDataGridAggregate aggregate)
    {
        if (aggregate == BzsDataGridAggregate.None || selector is null)
        {
            return null;
        }

        decimal total = 0;
        decimal? minimum = null;
        decimal? maximum = null;
        var count = 0;
        foreach (var item in items)
        {
            if (selector(item) is not decimal value)
            {
                continue;
            }

            count++;
            total += value;
            minimum = minimum is null || value < minimum ? value : minimum;
            maximum = maximum is null || value > maximum ? value : maximum;
        }

        return aggregate switch
        {
            BzsDataGridAggregate.Sum => total,
            BzsDataGridAggregate.Average => count == 0 ? null : total / count,
            BzsDataGridAggregate.Minimum => minimum,
            BzsDataGridAggregate.Maximum => maximum,
            _ => count,
        };
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

    private static bool MatchesText(BzsDataGridTextFilter filter, object? value)
    {
        var text = value as string ?? FormatInvariant(value);
        if (text is null)
        {
            return false;
        }

        var comparison = filter.CaseSensitive
            ? StringComparison.CurrentCulture
            : StringComparison.CurrentCultureIgnoreCase;
        return filter.Operator switch
        {
            BzsDataGridTextOperator.StartsWith => text.StartsWith(filter.Value, comparison),
            BzsDataGridTextOperator.EndsWith => text.EndsWith(filter.Value, comparison),
            BzsDataGridTextOperator.Equals => string.Equals(text, filter.Value, comparison),
            BzsDataGridTextOperator.NotContains => !text.Contains(filter.Value, comparison),
            BzsDataGridTextOperator.IsEmpty => text.Length == 0,
            BzsDataGridTextOperator.IsNotEmpty => text.Length != 0,
            _ => text.Contains(filter.Value, comparison),
        };
    }

    private static bool MatchesChoice(BzsDataGridChoiceFilter filter, object? value)
    {
        var text = value as string ?? FormatInvariant(value);
        if (text is null)
        {
            return false;
        }

        foreach (var choice in filter.Values)
        {
            if (string.Equals(choice, text, StringComparison.CurrentCulture))
            {
                return true;
            }
        }
        return false;
    }

    private static bool Compare(BzsDataGridComparisonOperator @operator, int comparison) => @operator switch
    {
        BzsDataGridComparisonOperator.NotEquals => comparison != 0,
        BzsDataGridComparisonOperator.LessThan => comparison < 0,
        BzsDataGridComparisonOperator.LessThanOrEqual => comparison <= 0,
        BzsDataGridComparisonOperator.GreaterThan => comparison > 0,
        BzsDataGridComparisonOperator.GreaterThanOrEqual => comparison >= 0,
        _ => comparison == 0,
    };

    private static bool TryConvertDecimal(object? value, out decimal result)
    {
        switch (value)
        {
            case decimal candidate:
                result = candidate;
                return true;
            case int candidate:
                result = candidate;
                return true;
            case long candidate:
                result = candidate;
                return true;
            case short candidate:
                result = candidate;
                return true;
            case byte candidate:
                result = candidate;
                return true;
            case float candidate:
                result = (decimal)candidate;
                return true;
            case double candidate:
                result = (decimal)candidate;
                return true;
            case string candidate:
                return decimal.TryParse(
                    candidate,
                    NumberStyles.Number,
                    CultureInfo.CurrentCulture,
                    out result);
            default:
                result = 0;
                return false;
        }
    }

    private static bool TryConvertDate(object? value, out DateOnly result)
    {
        switch (value)
        {
            case DateOnly candidate:
                result = candidate;
                return true;
            case DateTime candidate:
                result = DateOnly.FromDateTime(candidate);
                return true;
            case DateTimeOffset candidate:
                result = DateOnly.FromDateTime(candidate.DateTime);
                return true;
            case string candidate:
                return DateOnly.TryParse(candidate, CultureInfo.CurrentCulture, out result);
            default:
                result = default;
                return false;
        }
    }

    private static string? FormatInvariant(object? value) => value switch
    {
        null => null,
        IFormattable formattable => formattable.ToString(null, CultureInfo.CurrentCulture),
        _ => value.ToString(),
    };

    internal readonly record struct SortStep<TItem>(
        Comparison<TItem> Comparison,
        BzsDataGridSortDirection Direction);

    private readonly record struct IndexedItem<TItem>(TItem Item, int Index);
}
