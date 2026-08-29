namespace Bzs.Blazor.Tests;

public sealed class DataGridProviderTests
{
    [Fact]
    public void RequestSnapshotsFiltersAndRejectsDuplicateColumns()
    {
        var filters = new List<BzsDataGridFilter>
        {
            new BzsDataGridTextFilter("name", "Ada"),
        };
        var request = new BzsDataGridRequest(2, 25, filters: filters);

        filters.Clear();

        var filter = Assert.IsType<BzsDataGridTextFilter>(Assert.Single(request.Filters));
        Assert.Equal("name", filter.ColumnKey);
        Assert.Equal("Ada", filter.Value);
        Assert.Throws<ArgumentException>(() => new BzsDataGridRequest(
            1,
            10,
            filters:
            [
                new BzsDataGridTextFilter("name", "Ada"),
                new BzsDataGridBooleanFilter("name", true),
            ]));
    }

    [Fact]
    public void FilterContractsValidateAndNormalizeTheirValues()
    {
        var text = new BzsDataGridTextFilter(
            " name ",
            " Ada ",
            BzsDataGridTextOperator.StartsWith,
            caseSensitive: true);
        var number = new BzsDataGridNumberFilter("score", 42.5m, BzsDataGridComparisonOperator.GreaterThan);
        var date = new BzsDataGridDateFilter("created", new DateOnly(2026, 8, 9));
        var boolean = new BzsDataGridBooleanFilter("active", true);

        Assert.Equal("name", text.ColumnKey);
        Assert.Equal("Ada", text.Value);
        Assert.True(text.CaseSensitive);
        Assert.Equal(42.5m, number.Value);
        Assert.Equal(new DateOnly(2026, 8, 9), date.Value);
        Assert.True(boolean.Value);
        Assert.Throws<ArgumentException>(() => new BzsDataGridTextFilter("name", " "));
        Assert.Throws<ArgumentOutOfRangeException>(() => new BzsDataGridNumberFilter(
            "score",
            1,
            (BzsDataGridComparisonOperator)99));
    }

    [Fact]
    public void ResultConstructorsMakeKnownAndUnknownTotalsMutuallyExclusive()
    {
        var known = new BzsDataGridResult<int>([1, 2], totalCount: 5);
        var unknown = new BzsDataGridResult<int>([3], hasNextPage: true);

        Assert.Equal(5, known.TotalCount);
        Assert.Null(known.HasNextPage);
        Assert.Null(unknown.TotalCount);
        Assert.True(unknown.HasNextPage);
        Assert.Throws<ArgumentOutOfRangeException>(() => new BzsDataGridResult<int>([], -1));
    }

    [Fact]
    public void ResultSnapshotsProviderOwnedItems()
    {
        var items = new List<int> { 1, 2 };
        var result = new BzsDataGridResult<int>(items, totalCount: 2);

        items[0] = 99;
        items.Add(3);

        Assert.Equal([1, 2], result.Items);
    }

    [Fact]
    public async Task ProviderAdapterRejectsANullResultWithTheFeatureError()
    {
        var provider = new NullResultProvider();
        using var adapter = new BzsDataGridProviderAdapter<int>(provider);

        var outcome = await adapter.LoadAsync(new BzsDataGridRequest(1, 10));

        var failed = Assert.IsType<BzsCurrentProviderCallOutcome<BzsDataGridResult<int>>.Failed>(outcome);
        Assert.Equal("The DataGrid provider returned a null result.", failed.Error.Message);
    }

    private sealed class NullResultProvider : IBzsDataGridProvider<int>
    {
        public ValueTask<BzsDataGridResult<int>> GetItemsAsync(
            BzsDataGridRequest request,
            CancellationToken cancellationToken) =>
            ValueTask.FromResult<BzsDataGridResult<int>>(null!);
    }
}
