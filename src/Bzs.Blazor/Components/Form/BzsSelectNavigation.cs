namespace Bzs.Blazor;

internal static class BzsSelectNavigation
{
    internal static IReadOnlyList<BzsSelectOption<TValue>> Filter<TValue>(
        IReadOnlyList<BzsSelectOption<TValue>> options,
        string? searchText)
    {
        if (string.IsNullOrWhiteSpace(searchText))
        {
            return options;
        }

        return options.Where(option =>
                option.Label.Contains(searchText, StringComparison.OrdinalIgnoreCase)
                || option.Description?.Contains(searchText, StringComparison.OrdinalIgnoreCase) == true)
            .ToArray();
    }

    internal static int FindFirstEnabledIndex<TValue>(IReadOnlyList<BzsSelectOption<TValue>> options)
    {
        for (var index = 0; index < options.Count; index++)
        {
            if (!options[index].Disabled)
            {
                return index;
            }
        }

        return -1;
    }

    internal static int FindLastEnabledIndex<TValue>(IReadOnlyList<BzsSelectOption<TValue>> options)
    {
        for (var index = options.Count - 1; index >= 0; index--)
        {
            if (!options[index].Disabled)
            {
                return index;
            }
        }

        return -1;
    }

    internal static int FindInitialActiveIndex<TValue>(
        IReadOnlyList<BzsSelectOption<TValue>> options,
        TValue? selectedValue)
    {
        for (var index = 0; index < options.Count; index++)
        {
            var option = options[index];
            if (!option.Disabled
                && EqualityComparer<TValue>.Default.Equals(option.Value, selectedValue))
            {
                return index;
            }
        }

        return FindFirstEnabledIndex(options);
    }

    internal static int MoveActiveIndex<TValue>(
        IReadOnlyList<BzsSelectOption<TValue>> options,
        int activeIndex,
        int delta)
    {
        if (options.Count == 0)
        {
            return -1;
        }

        for (var offset = 1; offset <= options.Count; offset++)
        {
            var candidate = (activeIndex + delta * offset) % options.Count;
            if (candidate < 0)
            {
                candidate += options.Count;
            }

            if (!options[candidate].Disabled)
            {
                return candidate;
            }
        }

        return activeIndex;
    }
}
