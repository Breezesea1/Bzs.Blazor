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
    public void FindsFirstAndLastEnabledOptions()
    {
        Assert.Equal(0, BzsSelectNavigation.FindFirstEnabledIndex(Options));
        Assert.Equal(2, BzsSelectNavigation.FindLastEnabledIndex(Options));
    }

    [Theory]
    [InlineData(0, 1, 2)]
    [InlineData(2, 1, 0)]
    [InlineData(2, -1, 0)]
    [InlineData(0, -1, 2)]
    public void MovementSkipsDisabledOptionsAndWraps(int activeIndex, int delta, int expectedIndex)
    {
        var result = BzsSelectNavigation.MoveActiveIndex(Options, activeIndex, delta);

        Assert.Equal(expectedIndex, result);
    }

    [Fact]
    public void InitialIndexUsesTheEnabledSelectedOption()
    {
        Assert.Equal(2, BzsSelectNavigation.FindInitialActiveIndex(Options, "gamma"));
    }

    [Theory]
    [InlineData("beta")]
    [InlineData("missing")]
    public void InitialIndexFallsBackToFirstEnabledOption(string selectedValue)
    {
        Assert.Equal(0, BzsSelectNavigation.FindInitialActiveIndex(Options, selectedValue));
    }

    [Fact]
    public void EmptyOptionsHaveNoNavigationTarget()
    {
        BzsSelectOption<string>[] options = [];

        Assert.Empty(BzsSelectNavigation.Filter(options, "anything"));
        Assert.Equal(-1, BzsSelectNavigation.FindFirstEnabledIndex(options));
        Assert.Equal(-1, BzsSelectNavigation.FindLastEnabledIndex(options));
        Assert.Equal(-1, BzsSelectNavigation.FindInitialActiveIndex(options, "missing"));
        Assert.Equal(-1, BzsSelectNavigation.MoveActiveIndex(options, 0, 1));
    }

    [Fact]
    public void AllDisabledOptionsHaveNoEnabledTargetAndMovementPreservesTheActiveIndex()
    {
        BzsSelectOption<string>[] options =
        [
            new("alpha", "Alpha", disabled: true),
            new("beta", "Beta", disabled: true),
        ];

        Assert.Equal(-1, BzsSelectNavigation.FindFirstEnabledIndex(options));
        Assert.Equal(-1, BzsSelectNavigation.FindLastEnabledIndex(options));
        Assert.Equal(-1, BzsSelectNavigation.FindInitialActiveIndex(options, "alpha"));
        Assert.Equal(-1, BzsSelectNavigation.MoveActiveIndex(options, -1, 1));
        Assert.Equal(-1, BzsSelectNavigation.MoveActiveIndex(options, -1, -1));
    }
}
