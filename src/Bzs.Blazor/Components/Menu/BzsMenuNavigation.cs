namespace Bzs.Blazor;

internal static class BzsMenuNavigation
{
    internal static int Move(IReadOnlyList<bool> disabled, int currentIndex, int offset)
    {
        if (disabled.Count == 0 || disabled.All(static value => value))
        {
            return -1;
        }

        var index = currentIndex < 0
            ? (offset >= 0 ? -1 : 0)
            : currentIndex;
        for (var attempt = 0; attempt < disabled.Count; attempt++)
        {
            index = (index + offset + disabled.Count) % disabled.Count;
            if (!disabled[index])
            {
                return index;
            }
        }

        return -1;
    }

    internal static int FindBoundary(IReadOnlyList<bool> disabled, bool last) =>
        Move(disabled, last ? 0 : -1, last ? -1 : 1);

    internal static int FindTypeahead(
        IReadOnlyList<string> labels,
        IReadOnlyList<bool> disabled,
        int currentIndex,
        string query)
    {
        if (labels.Count != disabled.Count || string.IsNullOrWhiteSpace(query))
        {
            return -1;
        }

        var normalized = query.Trim();
        if (normalized.Length > 1 && normalized.All(character => character == normalized[0]))
        {
            normalized = normalized[..1];
        }

        for (var offset = 1; offset <= labels.Count; offset++)
        {
            var index = (Math.Max(currentIndex, -1) + offset) % labels.Count;
            if (!disabled[index] && labels[index].StartsWith(normalized, StringComparison.CurrentCultureIgnoreCase))
            {
                return index;
            }
        }

        return -1;
    }
}

internal interface IBzsMenuOwner
{
    void RegisterOrUpdate(BzsMenuItem item);
    void Unregister(BzsMenuItem item);
    int GetTabIndex(BzsMenuItem item);
    Task ActivateItemAsync(BzsMenuItem item);
    Task HandleItemKeyDownAsync(BzsMenuItem item, Microsoft.AspNetCore.Components.Web.KeyboardEventArgs args);
}

internal sealed class BzsMenuState
{
    private readonly List<BzsMenuItem> _items = [];
    private BzsMenuItem? _focusedItem;

    internal IReadOnlyList<BzsMenuItem> Items => _items;

    internal BzsMenuItem? FocusedItem => EnsureFocusedItem();

    internal void RegisterOrUpdate(BzsMenuItem item)
    {
        if (!_items.Contains(item))
        {
            _items.Add(item);
        }

        EnsureFocusedItem();
    }

    internal void Unregister(BzsMenuItem item)
    {
        _items.Remove(item);
        if (ReferenceEquals(_focusedItem, item))
        {
            _focusedItem = null;
        }
        EnsureFocusedItem();
    }

    internal int GetTabIndex(BzsMenuItem item) =>
        !item.Separator && !item.Disabled && ReferenceEquals(EnsureFocusedItem(), item) ? 0 : -1;

    internal BzsMenuItem? SetBoundary(bool last)
    {
        var index = BzsMenuNavigation.FindBoundary(DisabledStates(), last);
        _focusedItem = index < 0 ? null : _items[index];
        return _focusedItem;
    }

    internal BzsMenuItem? Move(BzsMenuItem current, int offset)
    {
        var index = BzsMenuNavigation.Move(DisabledStates(), _items.IndexOf(current), offset);
        _focusedItem = index < 0 ? null : _items[index];
        return _focusedItem;
    }

    internal BzsMenuItem? FindTypeahead(BzsMenuItem current, string query)
    {
        var index = BzsMenuNavigation.FindTypeahead(
            _items.Select(static item => item.EffectiveText).ToArray(),
            DisabledStates(),
            _items.IndexOf(current),
            query);
        if (index >= 0)
        {
            _focusedItem = _items[index];
        }
        return index < 0 ? null : _focusedItem;
    }

    internal void ClearFocus() => _focusedItem = null;

    private BzsMenuItem? EnsureFocusedItem()
    {
        if (_focusedItem is { Disabled: false, Separator: false } && _items.Contains(_focusedItem))
        {
            return _focusedItem;
        }

        _focusedItem = _items.FirstOrDefault(static item => !item.Disabled && !item.Separator);
        return _focusedItem;
    }

    private IReadOnlyList<bool> DisabledStates() =>
        _items.Select(static item => item.Disabled || item.Separator).ToArray();
}
