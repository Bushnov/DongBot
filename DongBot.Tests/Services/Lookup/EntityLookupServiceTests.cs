using DongBot;
using MLBStatsAPI.IDs;
using MLBStatsAPI.Models;

namespace DongBot.Tests;

public class EntityLookupServiceTests
{
    [Fact]
    public async Task ResolvePlayerAsync_UsesStaticMapBeforeSearch()
    {
        FakeMlbClient client = new()
        {
            PersonById = new Dictionary<int, PeopleResponse>
            {
                [660670] = new PeopleResponse
                {
                    People = new List<Person>
                    {
                        new Person { Id = 660670, FullName = "Ronald Acuna Jr." }
                    }
                }
            }
        };

        EntityLookupService service = new(client, NameIdResolver.Default);

        PlayerLookupResult result = await service.ResolvePlayerAsync("Ronald Acuna");

        Assert.Equal(EntityLookupStatus.Found, result.Status);
        Assert.Equal(660670, result.PlayerId);
        Assert.Equal("Ronald Acuna Jr.", result.ResolvedName);
        Assert.False(client.SearchPeopleCalled);
    }

    [Fact]
    public async Task ResolvePlayerAsync_FallsBackToApiBestMatch()
    {
        FakeMlbClient client = new()
        {
            SearchPeopleResult = "{\"people\":[{\"id\":1,\"fullName\":\"Ronald Bolanos\"},{\"id\":660670,\"fullName\":\"Ronald Acuna Jr.\"}]}"
        };

        EntityLookupService service = new(client, NameIdResolver.Default);

        PlayerLookupResult result = await service.ResolvePlayerAsync("Ronald Acuna");

        Assert.Equal(EntityLookupStatus.Found, result.Status);
        Assert.Equal(660670, result.PlayerId);
        Assert.True(client.SearchPeopleCalled);
    }

    [Fact]
    public async Task ResolvePlayerAsync_EmptySearchResult_ReturnsDataUnavailable()
    {
        FakeMlbClient client = new()
        {
            SearchPeopleResult = string.Empty
        };

        EntityLookupService service = new(client, NameIdResolver.Default);

        PlayerLookupResult result = await service.ResolvePlayerAsync("Any Player");

        Assert.Equal(EntityLookupStatus.DataUnavailable, result.Status);
        Assert.Null(result.PlayerId);
    }

    [Fact]
    public async Task ResolveTeamAsync_ResolvesTeamFromCandidates()
    {
        FakeMlbClient client = new()
        {
            TeamsResponseResult = new TeamsResponse
            {
                Teams = new List<Team>
                {
                    new Team { Id = TeamIds.AtlantaBraves, Name = "Atlanta Braves", Abbreviation = "ATL" },
                    new Team { Id = TeamIds.NewYorkMets, Name = "New York Mets", Abbreviation = "NYM" }
                }
            }
        };

        EntityLookupService service = new(client, NameIdResolver.Default);

        TeamLookupResult result = await service.ResolveTeamAsync("ATL");

        Assert.Equal(EntityLookupStatus.Found, result.Status);
        Assert.NotNull(result.Team);
        Assert.Equal(TeamIds.AtlantaBraves, result.Team!.Id);
    }

    [Fact]
    public async Task ResolveTeamAsync_WhenTeamsUnavailable_ReturnsDataUnavailable()
    {
        FakeMlbClient client = new();
        EntityLookupService service = new(client, NameIdResolver.Default);

        TeamLookupResult result = await service.ResolveTeamAsync("Braves");

        Assert.Equal(EntityLookupStatus.DataUnavailable, result.Status);
        Assert.Null(result.Team);
    }

    [Fact]
    public async Task ResolveVenueAsync_ReturnsVenueAndHomeTeams()
    {
        FakeMlbClient client = new()
        {
            TeamsResponseResult = new TeamsResponse
            {
                Teams = new List<Team>
                {
                    new Team
                    {
                        Id = TeamIds.AtlantaBraves,
                        Name = "Atlanta Braves",
                        Venue = new Venue
                        {
                            Id = VenueIds.TruistPark,
                            Name = "Truist Park"
                        }
                    }
                }
            }
        };

        EntityLookupService service = new(client, NameIdResolver.Default);

        VenueLookupResult result = await service.ResolveVenueAsync("Truist Park");

        Assert.Equal(EntityLookupStatus.Found, result.Status);
        Assert.NotNull(result.Venue);
        Assert.Equal(VenueIds.TruistPark, result.Venue!.Id);
        Assert.Single(result.HomeTeams);
        Assert.Equal("Atlanta Braves", result.HomeTeams[0].Name);
    }

    [Fact]
    public async Task ResolveVenueAsync_WhenTeamsUnavailable_ReturnsDataUnavailable()
    {
        FakeMlbClient client = new();
        EntityLookupService service = new(client, NameIdResolver.Default);

        VenueLookupResult result = await service.ResolveVenueAsync("Truist Park");

        Assert.Equal(EntityLookupStatus.DataUnavailable, result.Status);
        Assert.Null(result.Venue);
    }

    private sealed class FakeMlbClient : IMLBDataClient
    {
        public string SearchPeopleResult { get; set; } = "{\"people\":[]}";
        public bool SearchPeopleCalled { get; private set; }
        public Dictionary<int, PeopleResponse>? PersonById { get; set; }
        public TeamsResponse? TeamsResponseResult { get; set; }

        public Task<ScheduleResponse?> GetScheduleAsync(string startDate, string endDate, int sportId, int? teamId = null)
            => Task.FromResult<ScheduleResponse?>(null);

        public Task<ScheduleResponse?> GetTodaysScheduleAsync(int sportId, int? teamId = null)
            => Task.FromResult<ScheduleResponse?>(null);

        public Task<StandingsResponse?> GetStandingsAsync(int? leagueId, int season)
            => Task.FromResult<StandingsResponse?>(null);

        public Task<StandingsResponse?> GetDivisionStandingsAsync(int divisionId, int season)
            => Task.FromResult<StandingsResponse?>(null);

        public Task<StandingsResponse?> GetCurrentStandingsAsync()
            => Task.FromResult<StandingsResponse?>(null);

        public Task<string> GetRosterAsync(int teamId, int? season = null, string rosterType = "active")
            => Task.FromResult(string.Empty);

        public Task<TeamsResponse?> GetTeamsAsync(int sportId, int? season = null)
            => Task.FromResult(TeamsResponseResult);

        public Task<GameResponse?> GetGameAsync(int gamePk)
            => Task.FromResult<GameResponse?>(null);

        public Task<string> SearchPeopleAsync(string playerName, int sportId)
        {
            SearchPeopleCalled = true;
            return Task.FromResult(SearchPeopleResult);
        }

        public Task<PeopleResponse?> GetPersonAsync(int playerId, int? season = null)
        {
            if (PersonById != null && PersonById.TryGetValue(playerId, out PeopleResponse? person))
            {
                return Task.FromResult<PeopleResponse?>(person);
            }

            return Task.FromResult<PeopleResponse?>(null);
        }

        public Task<StatsResponse?> GetPlayerStatsAsync(int playerId, int season)
            => Task.FromResult<StatsResponse?>(null);

        public void Dispose() { }
    }
}
