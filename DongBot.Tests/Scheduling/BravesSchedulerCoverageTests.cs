using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;
using DongBot;
using MLBStatsAPI;
using MLBStatsAPI.IDs;
using MLBStatsAPI.Models;
using Discord.WebSocket;

namespace DongBot.Tests
{
    /// <summary>
    /// Stub MLB data client for testing BravesScheduler
    /// </summary>
    public class StubMlbClientForBravesTests : IMLBDataClient
    {
        public Task<ScheduleResponse?> GetScheduleAsync(string startDate, string endDate, int sportId, int? teamId = null) => Task.FromResult<ScheduleResponse?>(null);
        public Task<ScheduleResponse?> GetTodaysScheduleAsync(int sportId, int? teamId = null) => Task.FromResult<ScheduleResponse?>(null);
        public Task<StandingsResponse?> GetDivisionStandingsAsync(int divisionId, int season) => Task.FromResult<StandingsResponse?>(null);
        public Task<StandingsResponse?> GetCurrentStandingsAsync() => Task.FromResult<StandingsResponse?>(null);
        public Task<string> GetRosterAsync(int teamId, int? season = null, string rosterType = "active") => Task.FromResult<string>("");
        public Task<TeamsResponse?> GetTeamsAsync(int sportId) => Task.FromResult<TeamsResponse?>(null);
        public Task<GameResponse?> GetGameAsync(int gamePk) => Task.FromResult<GameResponse?>(null);
        public Task<string> SearchPeopleAsync(string playerName, int sportId) => Task.FromResult<string>("");
        public Task<PeopleResponse?> GetPersonAsync(int playerId) => Task.FromResult<PeopleResponse?>(null);
        public Task<StatsResponse?> GetPlayerStatsAsync(int playerId, int season) => Task.FromResult<StatsResponse?>(null);
        public void Dispose() { }
    }

    public class BravesSchedulerDecisionLogicTests
    {
        private readonly BravesScheduler _scheduler;

        public BravesSchedulerDecisionLogicTests()
        {
            var stubClient = new StubMlbClientForBravesTests();
            try
            {
                _scheduler = new BravesScheduler(null!, "baseball", stubClient);
            }
            catch
            {
                // If Discord client fails, use reflection to bypass
                _scheduler = new BravesScheduler(null!, "baseball", stubClient);
            }
        }

        #region ShouldPostDailySchedule Tests

        [Fact]
        public void ShouldPostDailySchedule_Returns_True_At_10am_First_Time()
        {
            var estTime = new DateTime(2025, 3, 11, 10, 03, 0);
            var lastPost = DateTime.MinValue;
            Assert.True(_scheduler.ShouldPostDailySchedule(estTime, lastPost));
        }

        [Fact]
        public void ShouldPostDailySchedule_Returns_True_At_Minute_4()
        {
            var estTime = new DateTime(2025, 3, 11, 10, 04, 0);
            var lastPost = DateTime.MinValue;
            Assert.True(_scheduler.ShouldPostDailySchedule(estTime, lastPost));
        }

        [Fact]
        public void ShouldPostDailySchedule_Returns_False_After_Minute_5()
        {
            var estTime = new DateTime(2025, 3, 11, 10, 05, 0);
            var lastPost = DateTime.MinValue;
            Assert.False(_scheduler.ShouldPostDailySchedule(estTime, lastPost));
        }

        [Fact]
        public void ShouldPostDailySchedule_Returns_False_Wrong_Hour()
        {
            var estTime = new DateTime(2025, 3, 11, 11, 03, 0);
            var lastPost = DateTime.MinValue;
            Assert.False(_scheduler.ShouldPostDailySchedule(estTime, lastPost));
        }

        [Fact]
        public void ShouldPostDailySchedule_Returns_False_Already_Posted_Today()
        {
            var estTime = new DateTime(2025, 3, 11, 10, 03, 0);
            var lastPost = new DateTime(2025, 3, 11, 10, 00, 0);
            Assert.False(_scheduler.ShouldPostDailySchedule(estTime, lastPost));
        }

        [Fact]
        public void ShouldPostDailySchedule_Returns_True_Next_Day()
        {
            var estTime = new DateTime(2025, 3, 12, 10, 03, 0);
            var lastPost = new DateTime(2025, 3, 11, 10, 00, 0);
            Assert.True(_scheduler.ShouldPostDailySchedule(estTime, lastPost));
        }

        #endregion

        #region ShouldPostGamePreview Tests

        [Fact]
        public void ShouldPostGamePreview_Returns_True_Within_Window()
        {
            var now = DateTime.UtcNow;
            var game = new MLBStatsAPI.Models.Game { GamePk = 12345, GameDate = now.AddMinutes(32) };
            Assert.True(_scheduler.ShouldPostGamePreview(game, 0, now));
        }

        [Fact]
        public void ShouldPostGamePreview_Returns_True_At_30_Minutes()
        {
            var now = DateTime.UtcNow;
            var game = new MLBStatsAPI.Models.Game { GamePk = 12345, GameDate = now.AddMinutes(30) };
            Assert.True(_scheduler.ShouldPostGamePreview(game, 0, now));
        }

        [Fact]
        public void ShouldPostGamePreview_Returns_True_At_35_Minutes()
        {
            var now = DateTime.UtcNow;
            var game = new MLBStatsAPI.Models.Game { GamePk = 12345, GameDate = now.AddMinutes(35) };
            Assert.True(_scheduler.ShouldPostGamePreview(game, 0, now));
        }

        [Fact]
        public void ShouldPostGamePreview_Returns_False_Too_Soon()
        {
            var now = DateTime.UtcNow;
            var game = new MLBStatsAPI.Models.Game { GamePk = 12345, GameDate = now.AddMinutes(29) };
            Assert.False(_scheduler.ShouldPostGamePreview(game, 0, now));
        }

        [Fact]
        public void ShouldPostGamePreview_Returns_False_Too_Late()
        {
            var now = DateTime.UtcNow;
            var game = new MLBStatsAPI.Models.Game { GamePk = 12345, GameDate = now.AddMinutes(36) };
            Assert.False(_scheduler.ShouldPostGamePreview(game, 0, now));
        }

        [Fact]
        public void ShouldPostGamePreview_Returns_False_Already_Posted()
        {
            var now = DateTime.UtcNow;
            var game = new MLBStatsAPI.Models.Game { GamePk = 12345, GameDate = now.AddMinutes(32) };
            Assert.False(_scheduler.ShouldPostGamePreview(game, 12345, now));
        }

        #endregion

        #region ShouldPostWeeklyStandings Tests

        [Fact]
        public void ShouldPostWeeklyStandings_Returns_True_Monday_Morning_First_Time()
        {
            var estTime = new DateTime(2025, 3, 10, 9, 30, 0); // Monday
            var lastPost = DateTime.MinValue;
            Assert.True(_scheduler.ShouldPostWeeklyStandings(estTime, lastPost));
        }

        [Fact]
        public void ShouldPostWeeklyStandings_Returns_True_Monday_At_8am()
        {
            var estTime = new DateTime(2025, 3, 10, 8, 00, 0);
            var lastPost = DateTime.MinValue;
            Assert.True(_scheduler.ShouldPostWeeklyStandings(estTime, lastPost));
        }

        [Fact]
        public void ShouldPostWeeklyStandings_Returns_True_Monday_Before_11am()
        {
            var estTime = new DateTime(2025, 3, 10, 10, 59, 0);
            var lastPost = DateTime.MinValue;
            Assert.True(_scheduler.ShouldPostWeeklyStandings(estTime, lastPost));
        }

        [Fact]
        public void ShouldPostWeeklyStandings_Returns_False_Monday_At_11am()
        {
            var estTime = new DateTime(2025, 3, 10, 11, 00, 0);
            var lastPost = DateTime.MinValue;
            Assert.False(_scheduler.ShouldPostWeeklyStandings(estTime, lastPost));
        }

        [Fact]
        public void ShouldPostWeeklyStandings_Returns_False_Before_8am()
        {
            var estTime = new DateTime(2025, 3, 10, 7, 59, 0);
            var lastPost = DateTime.MinValue;
            Assert.False(_scheduler.ShouldPostWeeklyStandings(estTime, lastPost));
        }

        [Fact]
        public void ShouldPostWeeklyStandings_Returns_False_Wrong_Day()
        {
            var estTime = new DateTime(2025, 3, 11, 9, 30, 0); // Tuesday
            var lastPost = DateTime.MinValue;
            Assert.False(_scheduler.ShouldPostWeeklyStandings(estTime, lastPost));
        }

        [Fact]
        public void ShouldPostWeeklyStandings_Returns_False_Already_Posted()
        {
            var estTime = new DateTime(2025, 3, 10, 9, 30, 0);
            var lastPost = new DateTime(2025, 3, 10, 8, 30, 0);
            Assert.False(_scheduler.ShouldPostWeeklyStandings(estTime, lastPost));
        }

        #endregion

        #region Message Builder Tests

        [Fact]
        public async Task BuildDailyScheduleMessageAsync_Returns_Message_With_No_Game_Today()
        {
            // Test path when there's no game today
            string message = await _scheduler.BuildDailyScheduleMessageAsync(null, null);
            Assert.Contains("No game scheduled for today", message);
        }

        [Fact]
        public async Task BuildGamePreviewMessageAsync_Contains_Game_Details()
        {
            // Simplified test - just ensure method returns a non-empty string
            var game = new MLBStatsAPI.Models.Game
            {
                GamePk = 111,
                GameDate = DateTime.UtcNow.AddMinutes(30),
                Venue = new Venue { Name = "Busch Stadium" }
            };

            string message = await _scheduler.BuildGamePreviewMessageAsync(game);

            Assert.Contains("GAME DAY", message);
            Assert.Contains("Busch Stadium", message);
            Assert.Contains("Let's Go Braves", message);
        }

        [Fact]
        public void BuildWeeklyStandingsMessage_Returns_Message_With_Standings()
        {
            var standings = new StandingsResponse
            {
                Records = new List<StandingRecord>
                {
                    new StandingRecord
                    {
                        TeamRecords = new List<TeamRecord>
                        {
                            new TeamRecord
                            {
                                Team = new Team { Id = TeamIds.AtlantaBraves, Name = "Braves" },
                                Wins = 45, Losses = 35, GamesBack = "-", WildCardGamesBack = "-",
                                RunsScored = 320, RunsAllowed = 280, RunDifferential = 40,
                                Streak = new Streak { StreakCode = "W5" }
                            },
                            new TeamRecord
                            {
                                Team = new Team { Id = 104, Name = "Mets" },
                                Wins = 43, Losses = 37, GamesBack = "2", WildCardGamesBack = "1",
                                RunsScored = 310, RunsAllowed = 300, RunDifferential = 10,
                                Streak = new Streak { StreakCode = "L2" }
                            }
                        }
                    }
                }
            };

            string message = _scheduler.BuildWeeklyStandingsMessage(standings);

            Assert.Contains("MONDAY MORNING STANDINGS UPDATE", message);
            Assert.Contains("NL East Standings", message);
            Assert.Contains("Braves", message);
            Assert.Contains("45", message);
            Assert.Contains("35", message);
            Assert.Contains("Mets", message);
        }

        [Fact]
        public void BuildWeeklyStandingsMessage_Handles_Null_Standings()
        {
            string message = _scheduler.BuildWeeklyStandingsMessage(null);
            Assert.Contains("Unable to fetch standings data", message);
        }

        [Fact]
        public void BuildWeeklyStandingsMessage_Handles_Empty_Standings()
        {
            var standings = new StandingsResponse { Records = new List<StandingRecord>() };
            string message = _scheduler.BuildWeeklyStandingsMessage(standings);
            Assert.Contains("Unable to fetch standings data", message);
        }

        #endregion
    }

    public class BravesSchedulerEdgeCaseTests
    {
        private readonly BravesScheduler _scheduler;

        public BravesSchedulerEdgeCaseTests()
        {
            var stubClient = new StubMlbClientForBravesTests();
            _scheduler = new BravesScheduler(null!, "baseball", stubClient);
        }

        [Fact]
        public void ShouldPostDailySchedule_Minute_Boundary_0()
        {
            var estTime = new DateTime(2025, 3, 11, 10, 00, 0);
            Assert.True(_scheduler.ShouldPostDailySchedule(estTime, DateTime.MinValue));
        }

        [Fact]
        public void ShouldPostDailySchedule_Minute_Boundary_4()
        {
            var estTime = new DateTime(2025, 3, 11, 10, 04, 59);
            Assert.True(_scheduler.ShouldPostDailySchedule(estTime, DateTime.MinValue));
        }

        [Fact]
        public void ShouldPostGamePreview_Minute_Within_Range()
        {
            var now = DateTime.UtcNow;
            var game = new MLBStatsAPI.Models.Game { GamePk = 999, GameDate = now.AddMinutes(32.5) };
            Assert.True(_scheduler.ShouldPostGamePreview(game, 0, now));
        }

        [Fact]
        public void ShouldPostWeeklyStandings_Sunday_Before_Monday()
        {
            var estTime = new DateTime(2025, 3, 9, 10, 00, 0); // Sunday
            Assert.False(_scheduler.ShouldPostWeeklyStandings(estTime, DateTime.MinValue));
        }

        [Fact]
        public void ShouldPostWeeklyStandings_Tuesday_After_Monday()
        {
            var estTime = new DateTime(2025, 3, 11, 10, 00, 0); // Tuesday
            Assert.False(_scheduler.ShouldPostWeeklyStandings(estTime, DateTime.MinValue));
        }
    }
}
