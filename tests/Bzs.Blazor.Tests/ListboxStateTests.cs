namespace Bzs.Blazor.Tests;

public sealed class ListboxStateTests
{
    private static readonly IReadOnlyList<BzsSelectOption<string>> Options =
    [
        new("alpha", "Alpha"),
        new("beta", "Beta", disabled: true) { Description = "Second choice" },
        new("gamma", "Gamma") { Description = "Final approval" },
        new("delta", "Delta", disabled: true),
    ];

    private static BzsListboxState<string> CreateState(
        bool searchEnabled = false,
        string? selectedValue = null,
        bool commitOnSpaceWhenOpen = false,
        IReadOnlyList<BzsSelectOption<string>>? options = null) => new(
            () => options ?? Options,
            () => searchEnabled,
            selectedValue is null ? null : () => selectedValue,
            commitOnSpaceWhenOpen);

    [Fact]
    public void ClosedStateHasNoActiveOption()
    {
        var state = CreateState();

        Assert.False(state.IsOpen);
        Assert.Equal(-1, state.ActiveIndex);
        Assert.Null(state.ActiveOption);
    }

    [Fact]
    public void OpeningActivatesTheFirstEnabledOption()
    {
        var state = CreateState();

        state.Open();

        Assert.True(state.IsOpen);
        Assert.Equal("alpha", state.ActiveOption?.Value);
    }

    [Fact]
    public void OpeningPrefersTheSelectedEnabledOption()
    {
        var state = CreateState(selectedValue: "gamma");

        state.Open();

        Assert.Equal("gamma", state.ActiveOption?.Value);
    }

    [Theory]
    [InlineData("beta")]
    [InlineData("missing")]
    public void OpeningFallsBackToTheFirstEnabledOptionWhenTheSelectionIsUnreachable(string selectedValue)
    {
        var state = CreateState(selectedValue: selectedValue);

        state.Open();

        Assert.Equal("alpha", state.ActiveOption?.Value);
    }

    [Theory]
    [InlineData("ArrowDown", "gamma")]
    [InlineData("ArrowUp", "gamma")]
    public void ArrowKeysSkipDisabledOptionsAndWrap(string key, string expectedValue)
    {
        var state = CreateState();
        state.Open();

        var action = state.HandleKey(key);

        Assert.Equal(BzsListboxAction.ActiveChanged, action);
        Assert.Equal(expectedValue, state.ActiveOption?.Value);
    }

    [Fact]
    public void ArrowKeysOpenTheClosedPanelInsteadOfMoving()
    {
        var state = CreateState();

        var action = state.HandleKey("ArrowDown");

        Assert.Equal(BzsListboxAction.Opened, action);
        Assert.True(state.IsOpen);
        Assert.Equal("alpha", state.ActiveOption?.Value);
    }

    [Fact]
    public void HomeAndEndReachTheEnabledBoundaries()
    {
        var state = CreateState();
        state.Open();

        Assert.Equal(BzsListboxAction.ActiveChanged, state.HandleKey("End"));
        Assert.Equal("gamma", state.ActiveOption?.Value);
        Assert.Equal(BzsListboxAction.ActiveChanged, state.HandleKey("Home"));
        Assert.Equal("alpha", state.ActiveOption?.Value);
    }

    [Fact]
    public void HomeAndEndAreIgnoredWhileClosed()
    {
        var state = CreateState();

        Assert.Equal(BzsListboxAction.None, state.HandleKey("Home"));
        Assert.Equal(BzsListboxAction.None, state.HandleKey("End"));
        Assert.False(state.IsOpen);
    }

    [Fact]
    public void EnterCommitsTheActiveOptionWithoutClosing()
    {
        var state = CreateState();
        state.Open();

        var action = state.HandleKey("Enter");

        Assert.Equal(BzsListboxAction.CommitActive, action);
        Assert.True(state.IsOpen);
        Assert.Equal("alpha", state.ActiveOption?.Value);
    }

    [Fact]
    public void SpaceOpensTheClosedPanel()
    {
        var state = CreateState();

        Assert.Equal(BzsListboxAction.Opened, state.HandleKey(" "));
        Assert.True(state.IsOpen);
    }

    [Fact]
    public void SpaceCommitsInsideAnOpenPanelOnlyWhenTheOwnerOptsIn()
    {
        var committing = CreateState(commitOnSpaceWhenOpen: true);
        committing.Open();
        Assert.Equal(BzsListboxAction.CommitActive, committing.HandleKey(" "));

        var plain = CreateState();
        plain.Open();
        Assert.Equal(BzsListboxAction.None, plain.HandleKey(" "));
    }

    [Fact]
    public void SearchSuppressesSpaceCommitSoTheQueryCanContainSpaces()
    {
        var state = CreateState(searchEnabled: true, commitOnSpaceWhenOpen: true);
        state.Open();

        Assert.Equal(BzsListboxAction.None, state.HandleKey(" "));
    }

    [Fact]
    public void EscapeAsksTheOwnerToCloseWithoutClosingTheStateItself()
    {
        var state = CreateState();
        state.Open();

        var action = state.HandleKey("Escape");

        // The owner drives the close so the overlay observes the open state before it flips.
        Assert.Equal(BzsListboxAction.CloseRequested, action);
        Assert.True(state.IsOpen);
    }

    [Fact]
    public void ClosingClearsTheSearchAndTheActiveOption()
    {
        var state = CreateState(searchEnabled: true);
        state.Open();
        state.Search("gamma");

        state.Close();

        Assert.False(state.IsOpen);
        Assert.Null(state.ActiveOption);
        Assert.Equal(string.Empty, state.SearchText);
    }

    [Fact]
    public void UnknownKeysLeaveTheStateUntouched()
    {
        var state = CreateState();
        state.Open();

        Assert.Equal(BzsListboxAction.None, state.HandleKey("a"));
        Assert.Equal(BzsListboxAction.None, state.HandleKey(null));
        Assert.Equal("alpha", state.ActiveOption?.Value);
    }

    [Fact]
    public void SearchNarrowsVisibleOptionsAndActivatesTheFirstEnabledMatch()
    {
        var state = CreateState(searchEnabled: true);
        state.Open();

        state.Search("a");

        Assert.Equal(["alpha", "beta", "gamma", "delta"], state.VisibleOptions.Select(static option => option.Value));
        Assert.Equal("alpha", state.ActiveOption?.Value);

        state.Search("approval");

        Assert.Equal("gamma", Assert.Single(state.VisibleOptions).Value);
        Assert.Equal("gamma", state.ActiveOption?.Value);
    }

    [Fact]
    public void SearchMatchingOnlyDisabledOptionsLeavesNothingActive()
    {
        var state = CreateState(searchEnabled: true);
        state.Open();

        state.Search("Second choice");

        Assert.Equal("beta", Assert.Single(state.VisibleOptions).Value);
        Assert.Null(state.ActiveOption);
    }

    [Fact]
    public void ActiveIndexTracksTheVisibleOptionsNotTheFullCollection()
    {
        var state = CreateState(searchEnabled: true);
        state.Open();
        state.Search("gamma");

        Assert.Equal(0, state.ActiveIndex);
        Assert.Equal("gamma", state.ActiveOption?.Value);
    }

    [Fact]
    public void ActivateIgnoresOutOfRangeAndDisabledOptions()
    {
        var state = CreateState();
        state.Open();

        state.Activate(1);
        Assert.Equal("alpha", state.ActiveOption?.Value);

        state.Activate(99);
        Assert.Equal("alpha", state.ActiveOption?.Value);

        state.Activate(2);
        Assert.Equal("gamma", state.ActiveOption?.Value);
    }

    [Fact]
    public void OpeningClearsAnEarlierSearch()
    {
        var state = CreateState(searchEnabled: true);
        state.Open();
        state.Search("gamma");

        state.Close();
        state.Open();

        Assert.Equal(string.Empty, state.SearchText);
        Assert.Equal(4, state.VisibleOptions.Count);
    }

    [Fact]
    public void AllDisabledOptionsLeaveNothingActiveAndMovementIsInert()
    {
        BzsSelectOption<string>[] options =
        [
            new("alpha", "Alpha", disabled: true),
            new("beta", "Beta", disabled: true),
        ];
        var state = CreateState(options: options);

        state.Open();
        Assert.Null(state.ActiveOption);
        Assert.Equal(BzsListboxAction.ActiveChanged, state.HandleKey("ArrowDown"));
        Assert.Null(state.ActiveOption);
    }

    [Fact]
    public void EmptyOptionsLeaveNothingActive()
    {
        var state = CreateState(options: []);

        state.Open();

        Assert.Equal(-1, state.ActiveIndex);
        Assert.Null(state.ActiveOption);
        Assert.Empty(state.VisibleOptions);
    }
}
