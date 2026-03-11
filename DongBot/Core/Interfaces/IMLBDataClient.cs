using System;
using System.Threading.Tasks;
using MLBStatsAPI.Models;

namespace DongBot
{
    public interface IMLBDataClient : IDisposable
    {
        Task<ScheduleResponse?> GetScheduleAsync(string startDate, string endDate, int sportId, int? teamId = null);
        Task<ScheduleResponse?> GetTodaysScheduleAsync(int sportId, int? teamId = null);
        Task<StandingsResponse?> GetDivisionStandingsAsync(int divisionId, int season);
        Task<StandingsResponse?> GetCurrentStandingsAsync();
        Task<string> GetRosterAsync(int teamId, int? season = null, string rosterType = "active");
        Task<TeamsResponse?> GetTeamsAsync(int sportId);
        Task<GameResponse?> GetGameAsync(int gamePk);
        Task<string> SearchPeopleAsync(string playerName, int sportId);
        Task<PeopleResponse?> GetPersonAsync(int playerId);
        Task<StatsResponse?> GetPlayerStatsAsync(int playerId, int season);
    }
}