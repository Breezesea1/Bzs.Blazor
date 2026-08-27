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
}
