namespace Bzs.Blazor;

/// <summary>Describes the side effect an option list requires from its owning control after a key.</summary>
internal enum BzsOptionListAction
{
    /// <summary>The key was not part of the option list contract.</summary>
    None,

    /// <summary>The active option moved within the open panel.</summary>
    ActiveChanged,

    /// <summary>The owner applies its own selection semantics to the active option.</summary>
    CommitActive,

    /// <summary>The owner closes the panel and restores focus to its trigger.</summary>
    CloseRequested,
}

/// <summary>
/// Owns the open state, current visible-option snapshot, active option, and shared open-panel
/// keyboard transitions. The owning control keeps filtering, selection, and rendered interface.
/// </summary>
internal sealed class BzsOptionListState<TOption>
{
    private readonly Func<TOption, bool> _isDisabled;
    private readonly Func<TOption, TOption, bool> _sameIdentity;
    private IReadOnlyList<TOption> _options = Array.Empty<TOption>();

    internal BzsOptionListState(
        Func<TOption, bool> isDisabled,
        Func<TOption, TOption, bool> sameIdentity)
    {
        ArgumentNullException.ThrowIfNull(isDisabled);
        ArgumentNullException.ThrowIfNull(sameIdentity);
        _isDisabled = isDisabled;
        _sameIdentity = sameIdentity;
    }

    internal bool IsOpen { get; private set; }

    internal int ActiveIndex { get; private set; } = -1;

    internal TOption? ActiveOption => ActiveIndex >= 0 && ActiveIndex < _options.Count
        ? _options[ActiveIndex]
        : default;

    /// <summary>Gets the current visible-option snapshot.</summary>
    internal IReadOnlyList<TOption> Options => _options;

    /// <summary>Replaces the visible-option snapshot and reconciles the active identity when open.</summary>
    internal void SetOptions(IReadOnlyList<TOption> options)
    {
        ArgumentNullException.ThrowIfNull(options);

        var hadActiveOption = IsOpen && ActiveIndex >= 0 && ActiveIndex < _options.Count;
        var previousActiveOption = hadActiveOption ? _options[ActiveIndex] : default;
        _options = options.ToArray();

        if (!IsOpen)
        {
            ActiveIndex = -1;
            return;
        }

        ActiveIndex = hadActiveOption
            ? FindEnabledIdentity(previousActiveOption!)
            : -1;
        if (ActiveIndex < 0)
        {
            ActiveIndex = FindFirstEnabled();
        }
    }

    /// <summary>Opens the panel and activates the first enabled option.</summary>
    internal void Open()
    {
        IsOpen = true;
        ActiveIndex = FindFirstEnabled();
    }

    /// <summary>Opens the panel, preferring an enabled option with the supplied identity.</summary>
    internal void Open(TOption preferredOption)
    {
        IsOpen = true;
        ActiveIndex = FindEnabledIdentity(preferredOption);
        if (ActiveIndex < 0)
        {
            ActiveIndex = FindFirstEnabled();
        }
    }

    /// <summary>Closes the panel and clears its active option.</summary>
    internal void Close()
    {
        IsOpen = false;
        ActiveIndex = -1;
    }

    /// <summary>Activates an enabled option by visible-snapshot index.</summary>
    internal void Activate(int index)
    {
        if (index >= 0 && index < _options.Count && !_isDisabled(_options[index]))
        {
            ActiveIndex = index;
        }
    }

    /// <summary>Applies an open-panel key and reports the side effect the owner must perform.</summary>
    internal BzsOptionListAction HandleKey(string? key, bool commitOnSpaceWhenOpen = false)
    {
        if (!IsOpen)
        {
            return BzsOptionListAction.None;
        }

        switch (key)
        {
            case "ArrowDown":
                MoveActive(1);
                return BzsOptionListAction.ActiveChanged;
            case "ArrowUp":
                MoveActive(-1);
                return BzsOptionListAction.ActiveChanged;
            case "Home":
                ActiveIndex = FindFirstEnabled();
                return BzsOptionListAction.ActiveChanged;
            case "End":
                ActiveIndex = FindLastEnabled();
                return BzsOptionListAction.ActiveChanged;
            case "Enter":
                return BzsOptionListAction.CommitActive;
            case " " when commitOnSpaceWhenOpen:
                return BzsOptionListAction.CommitActive;
            case "Escape":
                return BzsOptionListAction.CloseRequested;
            default:
                return BzsOptionListAction.None;
        }
    }

    private int FindFirstEnabled() => BzsOptionListNavigation.FindFirstEnabled(_options, _isDisabled);

    private int FindLastEnabled() => BzsOptionListNavigation.FindLastEnabled(_options, _isDisabled);

    private void MoveActive(int delta) => ActiveIndex = BzsOptionListNavigation.Move(
        _options,
        _isDisabled,
        ActiveIndex,
        delta);

    private int FindEnabledIdentity(TOption option)
    {
        for (var index = 0; index < _options.Count; index++)
        {
            if (!_isDisabled(_options[index]) && _sameIdentity(_options[index], option))
            {
                return index;
            }
        }

        return -1;
    }
}
