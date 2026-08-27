namespace Bzs.Blazor.Tests;

public sealed class SelectNavigationLogicTests
{
    private static readonly IReadOnlyList<BzsSelectOption<string>> Options =
    [
        new("alpha", "Alpha"),
        new("beta", "Beta", disabled: true) { Description = "Second choice" },
        new("gamma", "Gamma") { Description = "Final approval" },
        new("delta", "Delta", disabled: true),
    ];

    [Theory]
    [InlineData("ALP", "alpha")]
    [InlineData("approval", "gamma")]
    [InlineData("SECOND", "beta")]
    public void FilterMatchesLabelsAndDescriptionsIgnoringCase(string searchText, string expectedValue)
    {
        var result = BzsSelectNavigation.Filter(Options, searchText);

        Assert.Equal(expectedValue, Assert.Single(result).Value);
    }

    [Fact]
    public void FilterReturnsAllOptionsForBlankSearch()
    {
        var result = BzsSelectNavigation.Filter(Options, "  ");

        Assert.Same(Options, result);
    }

    [Fact]
    public void FilterOfEmptyOptionsYieldsNothing()
    {
        BzsSelectOption<string>[] options = [];

        Assert.Empty(BzsSelectNavigation.Filter(options, "anything"));
    }

    [Fact]
    public void FindsFirstAndLastEnabledOptions()
    {
        Assert.Equal(0, BzsListboxNavigation.FindFirstEnabled(Options, static option => option.Disabled));
        Assert.Equal(2, BzsListboxNavigation.FindLastEnabled(Options, static option => option.Disabled));
    }

    [Theory]
    [InlineData(0, 1, 2)]
    [InlineData(2, 1, 0)]
    [InlineData(2, -1, 0)]
    [InlineData(0, -1, 2)]
    public void MovementSkipsDisabledOptionsAndWraps(int activeIndex, int delta, int expectedIndex)
    {
        var result = BzsListboxNavigation.Move(Options, static option => option.Disabled, activeIndex, delta);

        Assert.Equal(expectedIndex, result);
    }

    [Fact]
    public void EmptyOptionsHaveNoNavigationTarget()
    {
        BzsSelectOption<string>[] options = [];

        Assert.Equal(-1, BzsListboxNavigation.FindFirstEnabled(options, static option => option.Disabled));
        Assert.Equal(-1, BzsListboxNavigation.FindLastEnabled(options, static option => option.Disabled));
        Assert.Equal(-1, BzsListboxNavigation.Move(options, static option => option.Disabled, 0, 1));
    }

    [Fact]
    public void AllDisabledOptionsHaveNoEnabledTargetAndMovementPreservesTheActiveIndex()
    {
        BzsSelectOption<string>[] options =
        [
            new("alpha", "Alpha", disabled: true),
            new("beta", "Beta", disabled: true),
        ];

        Assert.Equal(-1, BzsListboxNavigation.FindFirstEnabled(options, static option => option.Disabled));
        Assert.Equal(-1, BzsListboxNavigation.FindLastEnabled(options, static option => option.Disabled));
        Assert.Equal(-1, BzsListboxNavigation.Move(options, static option => option.Disabled, -1, 1));
        Assert.Equal(-1, BzsListboxNavigation.Move(options, static option => option.Disabled, -1, -1));
    }
}
