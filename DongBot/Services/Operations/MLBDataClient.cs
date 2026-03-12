using System.Threading.Tasks;
using MLBStatsAPI;
using MLBStatsAPI.Models;

namespace DongBot
{
    internal sealed class MLBDataClient : IMLBDataClient
    {
        private readonly MLBApiClient _mlbClient = new MLBApiClient();

        public Task<ScheduleResponse?> GetScheduleAsync(string startDate, string endDate, int sportId, int? teamId = null)
            => _mlbClient.Schedule.GetScheduleAsync(startDate, endDate, sportId, teamId: teamId);

        public Task<ScheduleResponse?> GetTodaysScheduleAsync(int sportId, int? teamId = null)
            => _mlbClient.Schedule.GetTodaysScheduleAsync(sportId: sportId, teamId: teamId);

        public Task<StandingsResponse?> GetStandingsAsync(int? leagueId, int season)
            => _mlbClient.Standings.GetStandingsAsync(leagueId: leagueId, season: season);

        public Task<StandingsResponse?> GetDivisionStandingsAsync(int divisionId, int season)
            => _mlbClient.Standings.GetDivisionStandingsAsync(divisionId, season);

        public Task<StandingsResponse?> GetCurrentStandingsAsync()
            => _mlbClient.Standings.GetCurrentStandingsAsync();

        public Task<string> GetRosterAsync(int teamId, int? season = null, string rosterType = "active")
            => _mlbClient.Teams.GetRosterAsync(teamId: teamId, season: season, rosterType: rosterType);

        public Task<TeamsResponse?> GetTeamsAsync(int sportId, int? season = null)
            => _mlbClient.Teams.GetTeamsAsync(sportId: sportId, season: season);

        public Task<GameResponse?> GetGameAsync(int gamePk)
            => _mlbClient.Games.GetGameAsync(gamePk);

        public Task<string> SearchPeopleAsync(string playerName, int sportId)
            => _mlbClient.People.SearchPeopleAsync(playerName, sportId);

        public Task<PeopleResponse?> GetPersonAsync(int playerId, int? season = null)
            => _mlbClient.People.GetPersonAsync(playerId, season);

        public Task<StatsResponse?> GetPlayerStatsAsync(int playerId, int season)
            => _mlbClient.People.GetPlayerStatsAsync(playerId, season: season);

        public void Dispose()
        {
            _mlbClient.Dispose();
        }
    }
}