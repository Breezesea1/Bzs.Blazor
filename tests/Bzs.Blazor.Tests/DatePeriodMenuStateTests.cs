using System.Globalization;
using Microsoft.AspNetCore.Components.Web;

namespace Bzs.Blazor.Tests;

/// <summary>
/// Drives <see cref="BzsDatePeriodMenuState" /> directly, so the month and year menu keyboard and
/// typeahead contract is asserted as "a key in, an action and an active option out" instead of
/// through a rendered calendar.
/// </summary>
public sealed class DatePeriodMenuStateTests
{
    private static readonly DateOnly ViewMonth = new(2026, 6, 1);

    [Fact]
    public void ArrowOnAClosedMenuOpensItOnTheViewedPeriod()
    {
        var state = CreateState();

        var action = state.HandleKey(BzsDatePeriodMenu.Month, Key("ArrowDown"), ViewMonth);

        Assert.Equal(BzsDatePeriodMenuAction.ActiveChanged, action);
        Assert.Equal(BzsDatePeriodMenu.Month, state.OpenMenu);
        Assert.Equal(6, state.ActiveMonth);
        Assert.Equal(2026, state.ActiveYear);
        Assert.True(state.ConsumeScrollPending());
        Assert.False(state.ConsumeScrollPending());
    }

    [Theory]
    [InlineData("ArrowDown", 7)]
    [InlineData("ArrowUp", 5)]
    [InlineData("PageDown", 9)]
    [InlineData("PageUp", 3)]
    [InlineData("Home", 1)]
    [InlineData("End", 12)]
    public void MovementKeysWalkTheMonthOptions(string key, int expectedMonth)
    {
        var state = CreateState();
        state.Open(BzsDatePeriodMenu.Month, ViewMonth);

        var action = state.HandleKey(BzsDatePeriodMenu.Month, Key(key), ViewMonth);

        Assert.Equal(BzsDatePeriodMenuAction.ActiveChanged, action);
        Assert.Equal(expectedMonth, state.ActiveMonth);
    }

    [Theory]
    [InlineData("PageDown", 2036)]
    [InlineData("PageUp", 2016)]
    public void PagingUsesADifferentStrideForYears(string key, int expectedYear)
    {
        var state = CreateState();
        state.Open(BzsDatePeriodMenu.Year, ViewMonth);

        state.HandleKey(BzsDatePeriodMenu.Year, Key(key), ViewMonth);

        Assert.Equal(expectedYear, state.ActiveYear);
    }

    [Fact]
    public void MovementClampsAtTheOptionBoundaries()
    {
        var state = CreateState();
        state.Open(BzsDatePeriodMenu.Month, new DateOnly(2026, 1, 1));

        state.HandleKey(BzsDatePeriodMenu.Month, Key("PageUp"), ViewMonth);
        Assert.Equal(1, state.ActiveMonth);

        state.HandleKey(BzsDatePeriodMenu.Month, Key("End"), ViewMonth);
        state.HandleKey(BzsDatePeriodMenu.Month, Key("ArrowDown"), ViewMonth);
        Assert.Equal(12, state.ActiveMonth);
    }

    [Theory]
    [InlineData("Enter")]
    [InlineData(" ")]
    [InlineData("Tab")]
    public void CommitKeysAskTheOwnerToMoveTheView(string key)
    {
        var state = CreateState();
        state.Open(BzsDatePeriodMenu.Month, ViewMonth);
        state.HandleKey(BzsDatePeriodMenu.Month, Key("ArrowDown"), ViewMonth);

        var action = state.HandleKey(BzsDatePeriodMenu.Month, Key(key), ViewMonth);

        Assert.Equal(BzsDatePeriodMenuAction.CommitActive, action);
        Assert.Equal(7, state.ActiveOption(BzsDatePeriodMenu.Month));
    }

    [Theory]
    [InlineData("Enter")]
    [InlineData(" ")]
    public void CommitKeysOnAClosedMenuOpenItInstead(string key)
    {
        var state = CreateState();

        var action = state.HandleKey(BzsDatePeriodMenu.Month, Key(key), ViewMonth);

        Assert.Equal(BzsDatePeriodMenuAction.ActiveChanged, action);
        Assert.Equal(BzsDatePeriodMenu.Month, state.OpenMenu);
    }

    [Fact]
    public void TabOnAClosedMenuIsNotPartOfTheContract()
    {
        var state = CreateState();

        var action = state.HandleKey(BzsDatePeriodMenu.Month, Key("Tab"), ViewMonth);

        Assert.Equal(BzsDatePeriodMenuAction.None, action);
        Assert.Null(state.OpenMenu);
    }

    [Fact]
    public void EscapeClosesTheMenuFirstAndTheSurfaceOnlyOnceNoMenuIsOpen()
    {
        var state = CreateState();
        state.Open(BzsDatePeriodMenu.Year, ViewMonth);

        Assert.Equal(
            BzsDatePeriodMenuAction.Closed,
            state.HandleKey(BzsDatePeriodMenu.Year, Key("Escape"), ViewMonth));
        Assert.Null(state.OpenMenu);

        Assert.Equal(
            BzsDatePeriodMenuAction.CloseSurfaceRequested,
            state.HandleKey(BzsDatePeriodMenu.Year, Key("Escape"), ViewMonth));
    }

    [Fact]
    public void AKeyAimedAtTheOtherMenuOpensThatMenuInstead()
    {
        var state = CreateState();
        state.Open(BzsDatePeriodMenu.Month, ViewMonth);

        var action = state.HandleKey(BzsDatePeriodMenu.Year, Key("ArrowDown"), ViewMonth);

        Assert.Equal(BzsDatePeriodMenuAction.ActiveChanged, action);
        Assert.Equal(BzsDatePeriodMenu.Year, state.OpenMenu);
    }

    [Fact]
    public void TypeaheadJumpsToTheFirstMonthWithThatPrefix()
    {
        var state = CreateState();
        state.Open(BzsDatePeriodMenu.Month, new DateOnly(2026, 1, 1));

        var action = state.HandleKey(BzsDatePeriodMenu.Month, Key("j"), ViewMonth);

        Assert.Equal(BzsDatePeriodMenuAction.ActiveChanged, action);
        Assert.Equal(6, state.ActiveMonth);
    }

    [Fact]
    public void RepeatingOneLetterCyclesThroughTheMonthsSharingIt()
    {
        var state = CreateState();
        state.Open(BzsDatePeriodMenu.Month, new DateOnly(2026, 1, 1));

        state.HandleKey(BzsDatePeriodMenu.Month, Key("j"), ViewMonth);
        Assert.Equal(6, state.ActiveMonth);

        state.HandleKey(BzsDatePeriodMenu.Month, Key("j"), ViewMonth);
        Assert.Equal(7, state.ActiveMonth);

        state.HandleKey(BzsDatePeriodMenu.Month, Key("j"), ViewMonth);
        Assert.Equal(1, state.ActiveMonth);
    }

    [Fact]
    public void AMultiLetterPrefixNarrowsInsteadOfCycling()
    {
        var state = CreateState();
        state.Open(BzsDatePeriodMenu.Month, new DateOnly(2026, 1, 1));

        state.HandleKey(BzsDatePeriodMenu.Month, Key("j"), ViewMonth);
        state.HandleKey(BzsDatePeriodMenu.Month, Key("u"), ViewMonth);
        Assert.Equal(6, state.ActiveMonth);

        state.HandleKey(BzsDatePeriodMenu.Month, Key("l"), ViewMonth);
        Assert.Equal(7, state.ActiveMonth);
    }

    [Fact]
    public void AnUnmatchedLetterRestartsTheBufferFromThatLetter()
    {
        var state = CreateState();
        state.Open(BzsDatePeriodMenu.Month, new DateOnly(2026, 1, 1));

        state.HandleKey(BzsDatePeriodMenu.Month, Key("j"), ViewMonth);
        state.HandleKey(BzsDatePeriodMenu.Month, Key("m"), ViewMonth);

        Assert.Equal(3, state.ActiveMonth);
    }

    [Fact]
    public void ALetterMatchingNothingLeavesTheActiveOptionAlone()
    {
        var state = CreateState();
        state.Open(BzsDatePeriodMenu.Month, ViewMonth);

        var action = state.HandleKey(BzsDatePeriodMenu.Month, Key("q"), ViewMonth);

        Assert.Equal(BzsDatePeriodMenuAction.None, action);
        Assert.Equal(6, state.ActiveMonth);
    }

    [Fact]
    public void TheTypeaheadBufferExpiresSoTheNextLetterStartsOver()
    {
        var now = 0L;
        var state = CreateState(timestamp: () => now);
        state.Open(BzsDatePeriodMenu.Month, new DateOnly(2026, 1, 1));

        state.HandleKey(BzsDatePeriodMenu.Month, Key("j"), ViewMonth);
        state.HandleKey(BzsDatePeriodMenu.Month, Key("u"), ViewMonth);
        Assert.Equal(6, state.ActiveMonth);

        now = 5_000;
        state.HandleKey(BzsDatePeriodMenu.Month, Key("n"), ViewMonth);

        // A surviving buffer would spell "jun" and stay on June; an expired one searches for "n"
        // alone and finds November.
        Assert.Equal(11, state.ActiveMonth);
    }

    [Theory]
    [InlineData("ArrowDown")]
    [InlineData("ArrowUp")]
    [InlineData("PageDown")]
    [InlineData("End")]
    public void EveryMovementKeyDiscardsTheTypeaheadBuffer(string movementKey)
    {
        var state = CreateState();
        state.Open(BzsDatePeriodMenu.Month, new DateOnly(2026, 1, 1));

        state.HandleKey(BzsDatePeriodMenu.Month, Key("j"), ViewMonth);
        var afterFirstLetter = state.ActiveMonth;
        state.HandleKey(BzsDatePeriodMenu.Month, Key(movementKey), ViewMonth);
        var afterMovement = state.ActiveMonth;
        state.HandleKey(BzsDatePeriodMenu.Month, Key("u"), ViewMonth);

        // A surviving buffer would spell "ju" and land on June; a discarded one leaves "u", which
        // matches no month name, so wherever the movement key landed stands.
        Assert.NotEqual(afterFirstLetter, afterMovement);
        Assert.Equal(afterMovement, state.ActiveMonth);
    }

    [Theory]
    [InlineData("Home")]
    [InlineData("End")]
    [InlineData("PageUp")]
    [InlineData("PageDown")]
    public void MovementKeysOnAClosedMenuAreNotPartOfTheContract(string key)
    {
        var state = CreateState();

        var action = state.HandleKey(BzsDatePeriodMenu.Month, Key(key), ViewMonth);

        Assert.Equal(BzsDatePeriodMenuAction.None, action);
        Assert.Null(state.OpenMenu);
    }

    [Fact]
    public void TypeaheadIsIgnoredWhileNoMenuIsOpen()
    {
        var state = CreateState();

        var action = state.HandleKey(BzsDatePeriodMenu.Month, Key("j"), ViewMonth);

        Assert.Equal(BzsDatePeriodMenuAction.None, action);
        Assert.Null(state.OpenMenu);
    }

    [Fact]
    public void YearTypeaheadNarrowsOnDigitsAndCyclesOnASingleOne()
    {
        var state = CreateState();
        state.Open(BzsDatePeriodMenu.Year, ViewMonth);
        Assert.Equal(2026, state.ActiveYear);

        state.HandleKey(BzsDatePeriodMenu.Year, Key("2"), ViewMonth);
        Assert.Equal(2027, state.ActiveYear);

        state.HandleKey(BzsDatePeriodMenu.Year, Key("2"), ViewMonth);
        Assert.Equal(2028, state.ActiveYear);

        state.HandleKey(BzsDatePeriodMenu.Year, Key("0"), ViewMonth);
        Assert.Equal(2016, state.ActiveYear);

        state.HandleKey(BzsDatePeriodMenu.Year, Key("3"), ViewMonth);
        state.HandleKey(BzsDatePeriodMenu.Year, Key("0"), ViewMonth);
        Assert.Equal(2030, state.ActiveYear);
    }

    [Fact]
    public void ModifiedAndControlKeysAreNotTypeahead()
    {
        var state = CreateState();
        state.Open(BzsDatePeriodMenu.Month, new DateOnly(2026, 1, 1));

        Assert.Equal(
            BzsDatePeriodMenuAction.None,
            state.HandleKey(
                BzsDatePeriodMenu.Month,
                BzsDatePeriodMenuKey.From(new KeyboardEventArgs { Key = "j", CtrlKey = true }),
                ViewMonth));
        Assert.Equal(
            BzsDatePeriodMenuAction.None,
            state.HandleKey(
                BzsDatePeriodMenu.Month,
                BzsDatePeriodMenuKey.From(new KeyboardEventArgs { Key = "\t" }),
                ViewMonth));
        Assert.Equal(1, state.ActiveMonth);
    }

    [Fact]
    public void ASyntheticClickOnTheOpenTriggerCommitsAndARealOneCloses()
    {
        var state = CreateState();
        state.Open(BzsDatePeriodMenu.Month, ViewMonth);

        Assert.Equal(
            BzsDatePeriodMenuAction.CommitActive,
            state.Activate(BzsDatePeriodMenu.Month, pointerDetail: 0, ViewMonth));

        Assert.Equal(
            BzsDatePeriodMenuAction.Closed,
            state.Activate(BzsDatePeriodMenu.Month, pointerDetail: 1, ViewMonth));
        Assert.Null(state.OpenMenu);

        Assert.Equal(
            BzsDatePeriodMenuAction.ActiveChanged,
            state.Activate(BzsDatePeriodMenu.Month, pointerDetail: 1, ViewMonth));
        Assert.Equal(BzsDatePeriodMenu.Month, state.OpenMenu);
    }

    [Fact]
    public void ClosingForgetsTheTypeaheadBufferAndThePendingScroll()
    {
        var state = CreateState();
        state.Open(BzsDatePeriodMenu.Month, new DateOnly(2026, 1, 1));
        state.HandleKey(BzsDatePeriodMenu.Month, Key("j"), ViewMonth);

        state.Close();

        Assert.Null(state.OpenMenu);
        Assert.False(state.ConsumeScrollPending());

        state.Open(BzsDatePeriodMenu.Month, new DateOnly(2026, 1, 1));
        state.HandleKey(BzsDatePeriodMenu.Month, Key("u"), ViewMonth);

        Assert.Equal(1, state.ActiveMonth);
    }

    [Fact]
    public void ANarrowedOptionListReactivatesTheViewedPeriod()
    {
        var months = new List<int> { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12 };
        var state = CreateState(months: months);
        state.Open(BzsDatePeriodMenu.Month, ViewMonth);
        state.HandleKey(BzsDatePeriodMenu.Month, Key("End"), ViewMonth);
        Assert.Equal(12, state.ActiveMonth);

        months.RemoveRange(3, 9);
        state.SynchronizeWithOptions(new DateOnly(2026, 2, 1));

        Assert.Equal(2, state.ActiveMonth);
        Assert.True(state.ConsumeScrollPending());
    }

    [Fact]
    public void SynchronizingKeepsAnActiveOptionTheListStillOffers()
    {
        var state = CreateState();
        state.Open(BzsDatePeriodMenu.Month, ViewMonth);
        state.HandleKey(BzsDatePeriodMenu.Month, Key("End"), ViewMonth);
        state.ConsumeScrollPending();

        state.SynchronizeWithOptions(new DateOnly(2026, 2, 1));

        Assert.Equal(12, state.ActiveMonth);
        Assert.True(state.ConsumeScrollPending());
    }

    [Fact]
    public void SynchronizingDoesNothingWhileNoMenuIsOpen()
    {
        var state = CreateState();

        state.SynchronizeWithOptions(ViewMonth);

        Assert.False(state.ConsumeScrollPending());
    }

    [Fact]
    public void PointerHoverActivatesAnOptionWithoutTouchingTheOpenMenu()
    {
        var state = CreateState();
        state.Open(BzsDatePeriodMenu.Month, ViewMonth);

        state.Activate(BzsDatePeriodMenu.Month, 9);
        state.Activate(BzsDatePeriodMenu.Year, 2030);

        Assert.Equal(9, state.ActiveMonth);
        Assert.Equal(2030, state.ActiveYear);
        Assert.Equal(BzsDatePeriodMenu.Month, state.OpenMenu);
    }

    private static BzsDatePeriodMenuKey Key(string key) =>
        BzsDatePeriodMenuKey.From(new KeyboardEventArgs { Key = key });

    private static BzsDatePeriodMenuState CreateState(
        IReadOnlyList<int>? months = null,
        IReadOnlyList<int>? years = null,
        Func<long>? timestamp = null)
    {
        var culture = CultureInfo.GetCultureInfo("en-US");
        var monthOptions = months ?? [1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12];
        var yearOptions = years ?? Enumerable.Range(2016, 21).ToArray();
        return new BzsDatePeriodMenuState(
            menu => menu == BzsDatePeriodMenu.Month ? monthOptions : yearOptions,
            (menu, option) => menu == BzsDatePeriodMenu.Month
                ? culture.DateTimeFormat.GetMonthName(option)
                : option.ToString(culture),
            () => culture.CompareInfo,
            timestamp);
    }
}
