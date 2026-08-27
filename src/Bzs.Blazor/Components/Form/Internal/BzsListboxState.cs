namespace Bzs.Blazor;

/// <summary>Describes the side effect an option list requires from its owning control after a key.</summary>
internal enum BzsListboxAction
{
    /// <summary>The key was not part of the option list contract.</summary>
    None,

    /// <summary>The panel opened and a position request is pending.</summary>
    Opened,

    /// <summary>The active option moved within the open panel.</summary>
    ActiveChanged,

    /// <summary>The owner applies its own selection semantics to the active option.</summary>
    CommitActive,

    /// <summary>The panel closed and the owner restores focus to its trigger.</summary>
    Closed,
}

/// <summary>
/// Owns the open state, search text, and active option of an option list presented in an anchored
/// overlay. The owning control keeps its own selection cardinality and rendered interface.
/// </summary>
internal sealed class BzsListboxState<TValue>
{
    private readonly Func<IReadOnlyList<BzsSelectOption<TValue>>> _options;
    private readonly Func<bool> _searchEnabled;
    private readonly Func<TValue?>? _preferredActiveValue;
    private readonly bool _commitOnSpaceWhenOpen;
    private bool _positionPending;
    private bool _focusSearchPending;

    internal BzsListboxState(
        Func<IReadOnlyList<BzsSelectOption<TValue>>> options,
        Func<bool> searchEnabled,
        Func<TValue?>? preferredActiveValue = null,
        bool commitOnSpaceWhenOpen = false)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(searchEnabled);
        _options = options;
        _searchEnabled = searchEnabled;
        _preferredActiveValue = preferredActiveValue;
        _commitOnSpaceWhenOpen = commitOnSpaceWhenOpen;
    }

    internal bool IsOpen { get; private set; }

    internal int ActiveIndex { get; private set; } = -1;

    internal string SearchText { get; private set; } = string.Empty;

    /// <summary>Gets the options surviving the current search text.</summary>
    internal IReadOnlyList<BzsSelectOption<TValue>> VisibleOptions =>
        BzsSelectNavigation.Filter(_options(), SearchText);

    /// <summary>Gets the active option, or null when no enabled option is active.</summary>
    internal BzsSelectOption<TValue>? ActiveOption
    {
        get
        {
            var options = VisibleOptions;
            return ActiveIndex >= 0 && ActiveIndex < options.Count ? options[ActiveIndex] : null;
        }
    }

    internal void Open()
    {
        IsOpen = true;
        SearchText = string.Empty;
        ActiveIndex = FindInitialActive();
        _positionPending = true;
        _focusSearchPending = _searchEnabled();
    }

    internal void Close()
    {
        IsOpen = false;
        SearchText = string.Empty;
        ActiveIndex = -1;
        _positionPending = false;
        _focusSearchPending = false;
    }

    /// <summary>Activates an option by index, ignoring out-of-range and disabled options.</summary>
    internal void Activate(int index)
    {
        var options = VisibleOptions;
        if (index >= 0 && index < options.Count && !options[index].Disabled)
        {
            ActiveIndex = index;
        }
    }

    /// <summary>Replaces the search text and activates the first enabled match.</summary>
    internal void Search(string? searchText)
    {
        SearchText = searchText ?? string.Empty;
        ActiveIndex = BzsListboxNavigation.FindFirstEnabled(VisibleOptions, static option => option.Disabled);
    }

    /// <summary>
    /// Reports whether the owner owes the browser a position call, and clears the request. Returns
    /// true at most once per open.
    /// </summary>
    internal bool TakePositionRequest(out bool focusSearch)
    {
        focusSearch = _focusSearchPending;
        var pending = _positionPending;
        _positionPending = false;
        _focusSearchPending = false;
        return pending;
    }

    /// <summary>Applies a key to the option list and reports the side effect the owner must perform.</summary>
    internal BzsListboxAction HandleKey(string? key)
    {
        switch (key)
        {
            case "ArrowDown":
            case "ArrowUp":
                if (!IsOpen)
                {
                    Open();
                    return BzsListboxAction.Opened;
                }

                MoveActive(key == "ArrowDown" ? 1 : -1);
                return BzsListboxAction.ActiveChanged;
            case "Home" when IsOpen:
                ActiveIndex = BzsListboxNavigation.FindFirstEnabled(VisibleOptions, static option => option.Disabled);
                return BzsListboxAction.ActiveChanged;
            case "End" when IsOpen:
                ActiveIndex = BzsListboxNavigation.FindLastEnabled(VisibleOptions, static option => option.Disabled);
                return BzsListboxAction.ActiveChanged;
            case "Enter" when IsOpen:
                return BzsListboxAction.CommitActive;
            case " " when !IsOpen:
                Open();
                return BzsListboxAction.Opened;
            case " " when _commitOnSpaceWhenOpen && !_searchEnabled():
                return BzsListboxAction.CommitActive;
            case "Escape" when IsOpen:
                Close();
                return BzsListboxAction.Closed;
            default:
                return BzsListboxAction.None;
        }
    }

    private void MoveActive(int delta) => ActiveIndex = BzsListboxNavigation.Move(
        VisibleOptions,
        static option => option.Disabled,
        ActiveIndex,
        delta);

    private int FindInitialActive()
    {
        var options = VisibleOptions;
        if (_preferredActiveValue is not null)
        {
            var preferred = _preferredActiveValue();
            for (var index = 0; index < options.Count; index++)
            {
                if (!options[index].Disabled
                    && EqualityComparer<TValue>.Default.Equals(options[index].Value, preferred))
                {
                    return index;
                }
            }
        }

        return BzsListboxNavigation.FindFirstEnabled(options, static option => option.Disabled);
    }
}
