using VehicleExplorer.Core.Vehicles;
using VehicleExplorer.Tests.Fakes;

namespace VehicleExplorer.Tests.Core;

public sealed class MakeSearchIndexTests
{
    [Fact]
    public async Task Search_ExactMatch_RanksBeforeStartsWithMatch()
    {
        var makeSearchIndex = await CreateIndexAsync("FORDSON", "FORD");

        var names = SearchNames(makeSearchIndex, "ford");

        Assert.Equal(["FORD", "FORDSON"], names);
    }

    [Fact]
    public async Task Search_PrefixMatch_RanksBeforeWordStartAndContainsMatch()
    {
        var makeSearchIndex = await CreateIndexAsync("AFORDABLE", "BIG FORD TRUCKS", "FORDSON");

        var names = SearchNames(makeSearchIndex, "ford");

        Assert.Equal(["FORDSON", "BIG FORD TRUCKS", "AFORDABLE"], names);
    }

    [Fact]
    public async Task Search_SameMatchRank_RanksPopularMakeFirst()
    {
        var makeSearchIndex = await CreateIndexAsync("HOLDEN", "HONDA");

        var names = SearchNames(makeSearchIndex, "ho");

        Assert.Equal(["HONDA", "HOLDEN"], names);
    }

    [Fact]
    public async Task Search_SameMatchRankAndPopularity_SortsByName()
    {
        var makeSearchIndex = await CreateIndexAsync("HOLMES", "HOLDEN");

        var names = SearchNames(makeSearchIndex, "hol");

        Assert.Equal(["HOLDEN", "HOLMES"], names);
    }

    [Fact]
    public async Task Search_DifferentCaseAndPunctuation_StillMatches()
    {
        var makeSearchIndex = await CreateIndexAsync("Mercedes-Benz");

        var names = SearchNames(makeSearchIndex, "mercedes benz");

        Assert.Equal(["Mercedes-Benz"], names);
    }

    [Fact]
    public async Task Search_MoreMatchesThanLimit_ReturnsLimit()
    {
        var makeSearchIndex = await CreateIndexAsync("HONDA", "HOLDEN", "HOLMES");

        var names = SearchNames(makeSearchIndex, "ho", limit: 2);

        Assert.Equal(2, names.Count);
    }

    [Fact]
    public async Task Search_OnlyPunctuation_ReturnsNothing()
    {
        var makeSearchIndex = await CreateIndexAsync("HONDA");

        var names = SearchNames(makeSearchIndex, " - ");

        Assert.Empty(names);
    }

    [Fact]
    public async Task FindMake_UnknownId_ReturnsNull()
    {
        var makeSearchIndex = await CreateIndexAsync("HONDA");

        Assert.Null(makeSearchIndex.FindMake(999));
    }

    private static List<string> SearchNames(MakeSearchIndex makeSearchIndex, string search, int limit = 10) =>
        [.. makeSearchIndex.Search(search, limit).Select(make => make.Name)];

    private static async Task<MakeSearchIndex> CreateIndexAsync(params string[] makeNames)
    {
        var nhtsaClient = new FakeNhtsaClient();
        nhtsaClient.Makes.AddRange(makeNames.Select((name, index) => new VehicleMake(index + 1, name)));

        var makeSearchIndex = new MakeSearchIndex();
        await makeSearchIndex.LoadAsync(nhtsaClient, TestContext.Current.CancellationToken);
        return makeSearchIndex;
    }
}
