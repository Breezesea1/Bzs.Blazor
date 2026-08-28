using System.Globalization;
using Microsoft.AspNetCore.Components.Web;

namespace Bzs.Blazor;

/// <summary>Identifies which of the two period menus in a date picker's calendar header is meant.</summary>
internal enum BzsDatePeriodMenu
{
    Month,
    Year,
}

/// <summary>Describes the side effect a period menu requires from its owning date picker after a key.</summary>
internal enum BzsDatePeriodMenuAction
{
    /// <summary>The key was not part of the period menu contract.</summary>
    None,

    /// <summary>The menu opened or its active option moved; the owner only re-renders.</summary>
    ActiveChanged,

    /// <summary>The owner moves the calendar view to the active option, which closes the menu.</summary>
    CommitActive,

    /// <summary>The menu closed itself; the owner only re-renders.</summary>
    Closed,

    /// <summary>No menu was open, so the owner closes the calendar surface instead.</summary>
    CloseSurfaceRequested,
}

/// <summary>
/// Owns which of a date picker's month and year menus is open, which option is active within it,
/// and the typeahead buffer that letter keys accumulate into. The interface is "a key or pointer
/// activation in, the action the owner must perform out"; the owning control moves the calendar
/// view, renders the options, and scrolls the active one into sight.
/// </summary>
internal sealed class BzsDatePeriodMenuState
{
    private const int TypeaheadResetMilliseconds = 2_000;
    private const int MonthPageSize = 3;
    private const int YearPageSize = 10;

    private readonly Func<BzsDatePeriodMenu, IReadOnlyList<int>> _options;
    private readonly Func<BzsDatePeriodMenu, int, string> _optionText;
    private readonly Func<CompareInfo> _compareInfo;
    private readonly Func<long> _timestamp;
    private string _typeahead = string.Empty;
    private long _typeaheadTimestamp;

    internal BzsDatePeriodMenuState(
        Func<BzsDatePeriodMenu, IReadOnlyList<int>> options,
        Func<BzsDatePeriodMenu, int, string> optionText,
        Func<CompareInfo> compareInfo,
        Func<long>? timestamp = null)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(optionText);
        ArgumentNullException.ThrowIfNull(compareInfo);
        _options = options;
        _optionText = optionText;
        _compareInfo = compareInfo;
        _timestamp = timestamp ?? (static () => Environment.TickCount64);
    }

    /// <summary>Gets the open menu, or null while neither menu is open.</summary>
    internal BzsDatePeriodMenu? OpenMenu { get; private set; }

    internal int ActiveMonth { get; private set; }

    internal int ActiveYear { get; private set; }

    /// <summary>
    /// Gets whether the active option still has to be scrolled into sight. The owner clears it once
    /// it has done so, because only the owner knows when the menu has actually rendered.
    /// </summary>
    internal bool ScrollPending { get; private set; }

    internal int ActiveOption(BzsDatePeriodMenu menu) =>
        menu == BzsDatePeriodMenu.Month ? ActiveMonth : ActiveYear;

    /// <summary>Opens a menu with the option matching the calendar's current view active.</summary>
    internal void Open(BzsDatePeriodMenu menu, DateOnly viewMonth)
    {
        OpenMenu = menu;
        ActiveMonth = viewMonth.Month;
        ActiveYear = viewMonth.Year;
        ResetTypeahead();
        ScrollPending = true;
    }

    internal void Close()
    {
        OpenMenu = null;
        ScrollPending = false;
        ResetTypeahead();
    }

    /// <summary>Activates an option directly, as pointer hover does.</summary>
    internal void Activate(BzsDatePeriodMenu menu, int option)
    {
        if (menu == BzsDatePeriodMenu.Month)
        {
            ActiveMonth = option;
        }
        else
        {
            ActiveYear = option;
        }
    }

    internal bool ConsumeScrollPending()
    {
        if (!ScrollPending)
        {
            return false;
        }

        ScrollPending = false;
        return true;
    }

    /// <summary>
    /// Re-activates the option matching the calendar view when the available options no longer
    /// contain the active one, which happens once a bound changes underneath an open menu.
    /// </summary>
    internal void SynchronizeWithOptions(DateOnly viewMonth)
    {
        if (OpenMenu is not { } menu)
        {
            return;
        }

        if (!_options(menu).Contains(ActiveOption(menu)))
        {
            Activate(menu, menu == BzsDatePeriodMenu.Month ? viewMonth.Month : viewMonth.Year);
        }
        ScrollPending = true;
    }

    /// <summary>
    /// Applies a pointer activation of a menu trigger. A keyboard-synthesized click on the open
    /// menu's own trigger commits the active option; a real click on it closes the menu.
    /// </summary>
    internal BzsDatePeriodMenuAction Activate(
        BzsDatePeriodMenu menu,
        long pointerDetail,
        DateOnly viewMonth)
    {
        if (OpenMenu != menu)
        {
            Open(menu, viewMonth);
            return BzsDatePeriodMenuAction.ActiveChanged;
        }

        if (pointerDetail == 0)
        {
            return BzsDatePeriodMenuAction.CommitActive;
        }

        OpenMenu = null;
        return BzsDatePeriodMenuAction.Closed;
    }

    /// <summary>Applies a key to a menu and reports the action its owner must perform.</summary>
    internal BzsDatePeriodMenuAction HandleKey(
        BzsDatePeriodMenu menu,
        BzsDatePeriodMenuKey key,
        DateOnly viewMonth)
    {
        var isOpen = OpenMenu == menu;
        switch (key.Key)
        {
            case "ArrowDown":
            case "ArrowUp":
                ResetTypeahead();
                if (!isOpen)
                {
                    Open(menu, viewMonth);
                }
                else
                {
                    MoveActive(menu, key.Key == "ArrowDown" ? 1 : -1);
                }
                return BzsDatePeriodMenuAction.ActiveChanged;
            case "Home" when isOpen:
                ResetTypeahead();
                ActivateBoundary(menu, first: true);
                return BzsDatePeriodMenuAction.ActiveChanged;
            case "End" when isOpen:
                ResetTypeahead();
                ActivateBoundary(menu, first: false);
                return BzsDatePeriodMenuAction.ActiveChanged;
            case "PageUp" when isOpen:
                ResetTypeahead();
                MoveActive(menu, -PageSize(menu));
                return BzsDatePeriodMenuAction.ActiveChanged;
            case "PageDown" when isOpen:
                ResetTypeahead();
                MoveActive(menu, PageSize(menu));
                return BzsDatePeriodMenuAction.ActiveChanged;
            case "Enter":
            case " ":
                if (isOpen)
                {
                    return BzsDatePeriodMenuAction.CommitActive;
                }

                Open(menu, viewMonth);
                return BzsDatePeriodMenuAction.ActiveChanged;
            case "Escape":
                if (!isOpen)
                {
                    return BzsDatePeriodMenuAction.CloseSurfaceRequested;
                }

                OpenMenu = null;
                return BzsDatePeriodMenuAction.Closed;
            case "Tab" when isOpen:
                return BzsDatePeriodMenuAction.CommitActive;
            default:
                return isOpen && key.IsTypeahead
                    ? ActivateByTypeahead(menu, key.Key)
                    : BzsDatePeriodMenuAction.None;
        }
    }

    private BzsDatePeriodMenuAction ActivateByTypeahead(BzsDatePeriodMenu menu, string key)
    {
        var now = _timestamp();
        if (now - _typeaheadTimestamp > TypeaheadResetMilliseconds)
        {
            _typeahead = string.Empty;
        }

        var compareInfo = _compareInfo();
        var repeatedSingleCharacter = menu == BzsDatePeriodMenu.Month
            && _typeahead.Length == 1
            && compareInfo.Compare(_typeahead, key, TypeaheadCompareOptions) == 0;
        _typeahead = repeatedSingleCharacter ? key : _typeahead + key;
        _typeaheadTimestamp = now;

        if (TryActivateByPrefix(menu, _typeahead))
        {
            return BzsDatePeriodMenuAction.ActiveChanged;
        }

        _typeahead = key;
        return TryActivateByPrefix(menu, key)
            ? BzsDatePeriodMenuAction.ActiveChanged
            : BzsDatePeriodMenuAction.None;
    }

    private bool TryActivateByPrefix(BzsDatePeriodMenu menu, string prefix)
    {
        var options = _options(menu);
        var activeIndex = IndexOf(options, ActiveOption(menu));
        var startIndex = prefix.Length == 1 ? activeIndex + 1 : 0;
        var compareInfo = _compareInfo();

        for (var offset = 0; offset < options.Count; offset++)
        {
            var index = (startIndex + offset) % options.Count;
            var option = options[index];
            if (!compareInfo.IsPrefix(_optionText(menu, option), prefix, TypeaheadCompareOptions))
            {
                continue;
            }

            Activate(menu, option);
            ScrollPending = true;
            return true;
        }

        return false;
    }

    private void MoveActive(BzsDatePeriodMenu menu, int offset)
    {
        var options = _options(menu);
        var index = IndexOf(options, ActiveOption(menu));
        Activate(menu, options[Math.Clamp(index + offset, 0, options.Count - 1)]);
        ScrollPending = true;
    }

    private void ActivateBoundary(BzsDatePeriodMenu menu, bool first)
    {
        var options = _options(menu);
        Activate(menu, first ? options[0] : options[^1]);
        ScrollPending = true;
    }

    private void ResetTypeahead()
    {
        _typeahead = string.Empty;
        _typeaheadTimestamp = 0;
    }

    private static CompareOptions TypeaheadCompareOptions =>
        CompareOptions.IgnoreCase | CompareOptions.IgnoreNonSpace;

    private static int PageSize(BzsDatePeriodMenu menu) =>
        menu == BzsDatePeriodMenu.Month ? MonthPageSize : YearPageSize;

    private static int IndexOf(IReadOnlyList<int> options, int active)
    {
        for (var index = 0; index < options.Count; index++)
        {
            if (options[index] == active)
            {
                return index;
            }
        }

        return 0;
    }
}

/// <summary>A key pressed on a period menu, reduced to what the menu contract depends on.</summary>
internal readonly record struct BzsDatePeriodMenuKey(string Key, bool IsTypeahead)
{
    internal static BzsDatePeriodMenuKey From(KeyboardEventArgs args) => new(
        args.Key,
        args.Key.Length == 1
            && !char.IsControl(args.Key[0])
            && !args.AltKey
            && !args.CtrlKey
            && !args.MetaKey);
}
