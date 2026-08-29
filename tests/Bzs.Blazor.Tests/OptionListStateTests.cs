namespace Bzs.Blazor.Tests;

public sealed class OptionListStateTests
{
    private static readonly IReadOnlyList<TestOption> Options =
    [
        new("alpha"),
        new("beta", Disabled: true),
        new("gamma"),
        new("delta", Disabled: true),
    ];

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
        state.SetOptions(Options);

        state.Open();

        Assert.True(state.IsOpen);
        Assert.Equal("alpha", state.ActiveOption?.Id);
    }

    [Fact]
    public void OpeningPrefersTheSelectedEnabledOption()
    {
        var state = CreateState();
        state.SetOptions(Options);

        state.Open(new TestOption("gamma"));

        Assert.Equal("gamma", state.ActiveOption?.Id);
    }

    [Theory]
    [InlineData("beta")]
    [InlineData("missing")]
    public void OpeningFallsBackToTheFirstEnabledOptionWhenThePreferredOptionIsUnreachable(string preferredId)
    {
        var state = CreateState();
        state.SetOptions(Options);

        state.Open(new TestOption(preferredId));

        Assert.Equal("alpha", state.ActiveOption?.Id);
    }

    [Theory]
    [InlineData("ArrowDown", "gamma")]
    [InlineData("ArrowUp", "gamma")]
    public void ArrowKeysSkipDisabledOptionsAndWrap(string key, string expectedId)
    {
        var state = CreateState();
        state.SetOptions(Options);
        state.Open();

        var action = state.HandleKey(key);

        Assert.Equal(BzsOptionListAction.ActiveChanged, action);
        Assert.Equal(expectedId, state.ActiveOption?.Id);
    }

    [Fact]
    public void ClosedPanelKeysRemainWithTheOwner()
    {
        var state = CreateState();
        state.SetOptions(Options);

        Assert.Equal(BzsOptionListAction.None, state.HandleKey("ArrowDown"));
        Assert.Equal(BzsOptionListAction.None, state.HandleKey(" "));
        Assert.False(state.IsOpen);
    }

    [Fact]
    public void HomeAndEndReachTheEnabledBoundaries()
    {
        var state = CreateState();
        state.SetOptions(Options);
        state.Open();

        Assert.Equal(BzsOptionListAction.ActiveChanged, state.HandleKey("End"));
        Assert.Equal("gamma", state.ActiveOption?.Id);
        Assert.Equal(BzsOptionListAction.ActiveChanged, state.HandleKey("Home"));
        Assert.Equal("alpha", state.ActiveOption?.Id);
    }

    [Fact]
    public void HomeAndEndAreIgnoredWhileClosed()
    {
        var state = CreateState();
        state.SetOptions(Options);

        Assert.Equal(BzsOptionListAction.None, state.HandleKey("Home"));
        Assert.Equal(BzsOptionListAction.None, state.HandleKey("End"));
    }

    [Fact]
    public void EnterCommitsTheActiveOptionWithoutClosing()
    {
        var state = CreateState();
        state.SetOptions(Options);
        state.Open();

        var action = state.HandleKey("Enter");

        Assert.Equal(BzsOptionListAction.CommitActive, action);
        Assert.True(state.IsOpen);
        Assert.Equal("alpha", state.ActiveOption?.Id);
    }

    [Fact]
    public void SpaceCommitsOnlyWhenTheOwnerOptsIn()
    {
        var committing = CreateState();
        committing.SetOptions(Options);
        committing.Open();
        Assert.Equal(BzsOptionListAction.CommitActive, committing.HandleKey(" ", commitOnSpaceWhenOpen: true));

        var plain = CreateState();
        plain.SetOptions(Options);
        plain.Open();
        Assert.Equal(BzsOptionListAction.None, plain.HandleKey(" "));
    }

    [Fact]
    public void EscapeAsksTheOwnerToCloseWithoutClosingTheStateItself()
    {
        var state = CreateState();
        state.SetOptions(Options);
        state.Open();

        var action = state.HandleKey("Escape");

        Assert.Equal(BzsOptionListAction.CloseRequested, action);
        Assert.True(state.IsOpen);
    }

    [Fact]
    public void ClosingClearsTheActiveOption()
    {
        var state = CreateState();
        state.SetOptions(Options);
        state.Open();

        state.Close();

        Assert.False(state.IsOpen);
        Assert.Null(state.ActiveOption);
        Assert.Equal(-1, state.ActiveIndex);
    }

    [Fact]
    public void UnknownKeysLeaveTheStateUntouched()
    {
        var state = CreateState();
        state.SetOptions(Options);
        state.Open();

        Assert.Equal(BzsOptionListAction.None, state.HandleKey("a"));
        Assert.Equal(BzsOptionListAction.None, state.HandleKey(null));
        Assert.Equal("alpha", state.ActiveOption?.Id);
    }

    [Fact]
    public void PointerActivationIgnoresOutOfRangeAndDisabledOptions()
    {
        var state = CreateState();
        state.SetOptions(Options);
        state.Open();

        state.Activate(1);
        Assert.Equal("alpha", state.ActiveOption?.Id);

        state.Activate(99);
        Assert.Equal("alpha", state.ActiveOption?.Id);

        state.Activate(2);
        Assert.Equal("gamma", state.ActiveOption?.Id);
    }

    [Fact]
    public void AllDisabledAndEmptySnapshotsLeaveNothingActive()
    {
        var state = CreateState();
        state.SetOptions([new TestOption("alpha", Disabled: true), new TestOption("beta", Disabled: true)]);
        state.Open();
        Assert.Null(state.ActiveOption);
        Assert.Equal(BzsOptionListAction.ActiveChanged, state.HandleKey("ArrowDown"));
        Assert.Null(state.ActiveOption);

        state.SetOptions([]);
        Assert.Equal(-1, state.ActiveIndex);
        Assert.Null(state.ActiveOption);
        Assert.Empty(state.Options);
    }

    [Fact]
    public void ReplacingAnOpenSnapshotPreservesTheActiveOptionByIdentity()
    {
        var state = CreateState();
        state.SetOptions([new TestOption("alpha"), new TestOption("gamma")]);
        state.Open();
        state.Activate(1);

        state.SetOptions([new TestOption("gamma"), new TestOption("alpha")]);

        Assert.Equal("gamma", state.ActiveOption?.Id);
        Assert.Equal(0, state.ActiveIndex);
    }

    [Fact]
    public void RemovingOrDisablingTheActiveOptionFallsBackToTheFirstEnabledOption()
    {
        var state = CreateState();
        state.SetOptions([new TestOption("alpha"), new TestOption("gamma"), new TestOption("omega")]);
        state.Open();
        state.Activate(1);

        state.SetOptions([new TestOption("alpha"), new TestOption("gamma", Disabled: true), new TestOption("omega")]);
        Assert.Equal("alpha", state.ActiveOption?.Id);

        state.Activate(2);
        state.SetOptions([new TestOption("alpha")]);
        Assert.Equal("alpha", state.ActiveOption?.Id);
    }

    [Fact]
    public void ReplacingAClosedSnapshotClearsTheActiveOption()
    {
        var state = CreateState();
        state.SetOptions(Options);
        state.Open();
        state.Close();

        state.SetOptions([new TestOption("replacement")]);

        Assert.Equal(-1, state.ActiveIndex);
        Assert.Null(state.ActiveOption);
    }

    private static BzsOptionListState<TestOption> CreateState() => new(
        static option => option.Disabled,
        static (left, right) => string.Equals(left.Id, right.Id, StringComparison.Ordinal));

    private sealed record TestOption(string Id, bool Disabled = false);
}
