using DongBot;
using MLBStatsAPI.IDs;
using MLBStatsAPI.Models;

namespace DongBot.Tests;

public class StandingsFilterServiceTests
{
    private static readonly IStandingsFilterService Service = new StandingsFilterService(NameIdResolver.Default);

    [Fact]
    public void ParseFilter_Auto_WithScopedToken_UsesScopedQuery()
    {
        string[] parts = ["MLB-STANDINGS", "LEAGUE", "National", "League"];

        StandingsFilter filter = Service.ParseFilter(parts, StandingsFilterScope.Auto);

        Assert.Equal(StandingsFilterScope.League, filter.Scope);
        Assert.Equal("National League", filter.Query);
    }

    [Fact]
    public void ParseFilter_Auto_WithoutScopedToken_UsesTailAsQuery()
    {
        string[] parts = ["MLB-STANDINGS", "AL", "East"];

        StandingsFilter filter = Service.ParseFilter(parts, StandingsFilterScope.Auto);

        Assert.Equal(StandingsFilterScope.Auto, filter.Scope);
        Assert.Equal("AL East", filter.Query);
    }

    [Fact]
    public void ParseFilter_ExplicitScope_UsesTailAsQuery()
    {
        string[] parts = ["MLB-LEAGUE", "American", "League"];

        StandingsFilter filter = Service.ParseFilter(parts, StandingsFilterScope.League);

        Assert.Equal(StandingsFilterScope.League, filter.Scope);
        Assert.Equal("American League", filter.Query);
    }

    [Fact]
    public void RecordMatches_EmptyQuery_Matches()
    {
        StandingRecord record = BuildRecord(DivisionIds.NLEast, "NL East", LeagueIds.NationalLeague, "National League", SportIds.MLB, "Major League Baseball");

        bool matches = Service.RecordMatches(record, new StandingsFilter(StandingsFilterScope.Auto, string.Empty));

        Assert.True(matches);
    }

    [Fact]
    public void RecordMatches_DivisionScope_UsesDivisionIdsMap()
    {
        StandingRecord record = BuildRecord(DivisionIds.NLEast, "NL East", LeagueIds.NationalLeague, "National League", SportIds.MLB, "Major League Baseball");

        bool matches = Service.RecordMatches(record, new StandingsFilter(StandingsFilterScope.Division, "NL East"));

        Assert.True(matches);
    }

    [Fact]
    public void RecordMatches_LeagueScope_UsesLeagueName()
    {
        StandingRecord record = BuildRecord(DivisionIds.NLEast, "NL East", LeagueIds.NationalLeague, "National League", SportIds.MLB, "Major League Baseball");

        bool matches = Service.RecordMatches(record, new StandingsFilter(StandingsFilterScope.League, "National"));

        Assert.True(matches);
    }

    [Fact]
    public void RecordMatches_SportScope_UsesSportIdsMap()
    {
        StandingRecord record = BuildRecord(DivisionIds.NLEast, "NL East", LeagueIds.NationalLeague, "National League", SportIds.MLB, "Major League Baseball");

        bool matches = Service.RecordMatches(record, new StandingsFilter(StandingsFilterScope.Sport, "MLB"));

        Assert.True(matches);
    }

    [Fact]
    public void RecordMatches_AutoScope_MatchesAcrossEntities()
    {
        StandingRecord record = BuildRecord(DivisionIds.NLEast, "NL East", LeagueIds.NationalLeague, "National League", SportIds.MLB, "Major League Baseball");

        bool matches = Service.RecordMatches(record, new StandingsFilter(StandingsFilterScope.Auto, "National League"));

        Assert.True(matches);
    }

    [Fact]
    public void RecordMatches_ExplicitScope_DoesNotCrossMatch()
    {
        StandingRecord record = BuildRecord(DivisionIds.NLEast, "NL East", LeagueIds.NationalLeague, "National League", SportIds.MLB, "Major League Baseball");

        bool matches = Service.RecordMatches(record, new StandingsFilter(StandingsFilterScope.Division, "National League"));

        Assert.False(matches);
    }

    private static StandingRecord BuildRecord(int divisionId, string divisionName, int leagueId, string leagueName, int sportId, string sportName)
    {
        return new StandingRecord
        {
            Division = new Division { Id = divisionId, Name = divisionName },
            League = new League { Id = leagueId, Name = leagueName },
            Sport = new Sport { Id = sportId, Name = sportName }
        };
    }
}
