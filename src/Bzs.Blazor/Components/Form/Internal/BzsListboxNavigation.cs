namespace Bzs.Blazor;

/// <summary>Locates active option indices within an option list, skipping disabled options.</summary>
internal static class BzsListboxNavigation
{
    internal static int FindFirstEnabled<TOption>(
        IReadOnlyList<TOption> options,
        Func<TOption, bool> isDisabled)
    {
        for (var index = 0; index < options.Count; index++)
        {
            if (!isDisabled(options[index]))
            {
                return index;
            }
        }

        return -1;
    }

    internal static int FindLastEnabled<TOption>(
        IReadOnlyList<TOption> options,
        Func<TOption, bool> isDisabled)
    {
        for (var index = options.Count - 1; index >= 0; index--)
        {
            if (!isDisabled(options[index]))
            {
                return index;
            }
        }

        return -1;
    }

    internal static int Move<TOption>(
        IReadOnlyList<TOption> options,
        Func<TOption, bool> isDisabled,
        int activeIndex,
        int delta)
    {
        if (options.Count == 0)
        {
            return -1;
        }

        for (var offset = 1; offset <= options.Count; offset++)
        {
            var candidate = (activeIndex + (delta * offset)) % options.Count;
            if (candidate < 0)
            {
                candidate += options.Count;
            }

            if (!isDisabled(options[candidate]))
            {
                return candidate;
            }
        }

        return activeIndex;
    }
}
