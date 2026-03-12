#nullable enable

using Discord;
using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using MLBStatsAPI;
using MLBStatsAPI.IDs;
using MLBStatsAPI.Models;

namespace DongBot
{
    public interface IBravesSchedulerControl
    {
        /// <summary>Enable scheduler for a specific guild.</summary>
        void Enable(ulong guildId);
        
        /// <summary>Disable scheduler for a specific guild.</summary>
        void Disable(ulong guildId);
        
        /// <summary>Get scheduler status for a specific guild.</summary>
        string GetStatus(ulong guildId);
        
        /// <summary>Manually trigger daily post for a specific guild (for testing).</summary>
        Task<string> TriggerDailyPost(ulong guildId);
        
        /// <summary>Manually trigger weekly post for a specific guild (for testing).</summary>
        Task<string> TriggerWeeklyPost(ulong guildId);
    }

    /// <summary>
    /// Handles scheduled automated messages for Atlanta Braves updates.
    /// Supports multiple Discord guilds (servers) with independent enable/disable state per guild.
    /// </summary>
    public class BravesScheduler : IBravesSchedulerControl
    {
        private readonly DiscordSocketClient _client;
        private readonly IMLBDataClient _mlbClient;
        private CancellationTokenSource? _schedulerCancellation;
        private Task? _dailyTask;
        private Task? _gamePreviewTask;
        private Task? _weeklyTask;
        private readonly string _bravesChannelName;
        
        private const int BRAVES_TEAM_ID = TeamIds.AtlantaBraves;
        private const int NL_EAST_DIVISION_ID = DivisionIds.NLEast;
        
        // Track state per guild
        private readonly Dictionary<ulong, DateTime> _lastDailyPost = new();
        private readonly Dictionary<ulong, DateTime> _lastWeeklyPost = new();
        private readonly Dictionary<ulong, int> _lastGamePreviewId = new();
        private readonly Dictionary<ulong, bool> _guildEnabledState = new();
        private const bool DEFAULT_ENABLED = true;

        public BravesScheduler(DiscordSocketClient client, string bravesChannelName = "baseball", IMLBDataClient? mlbClient = null)
        {
            _client = client;
            _mlbClient = mlbClient ?? new MLBDataClient();
            _bravesChannelName = bravesChannelName;
        }

        /// <summary>
        /// Start all scheduled tasks
        /// </summary>
        public void Start()
        {
            if (_schedulerCancellation != null)
            {
                return;
            }

            Console.WriteLine("Starting Braves Scheduler...");

            _schedulerCancellation = new CancellationTokenSource();
            CancellationToken token = _schedulerCancellation.Token;

            // Daily check every minute to catch 10am EST
            _dailyTask = RunPeriodicAsync(TimeSpan.Zero, TimeSpan.FromMinutes(1), CheckDailySchedule, token);

            // Game preview check every 5 minutes
            _gamePreviewTask = RunPeriodicAsync(TimeSpan.FromMinutes(1), TimeSpan.FromMinutes(5), CheckGamePreview, token);

            // Weekly standings check every hour
            _weeklyTask = RunPeriodicAsync(TimeSpan.FromMinutes(2), TimeSpan.FromHours(1), CheckWeeklyStandings, token);
            
            Console.WriteLine("Braves Scheduler started successfully.");
        }

        private static async Task RunPeriodicAsync(
            TimeSpan initialDelay,
            TimeSpan interval,
            Func<Task> action,
            CancellationToken cancellationToken)
        {
            try
            {
                if (initialDelay > TimeSpan.Zero)
                {
                    await Task.Delay(initialDelay, cancellationToken);
                }

                using PeriodicTimer timer = new PeriodicTimer(interval);
                do
                {
                    await action();
                }
                while (await timer.WaitForNextTickAsync(cancellationToken));
            }
            catch (OperationCanceledException)
            {
                // Expected during shutdown.
            }
        }

        /// <summary>
        /// Stop all scheduled tasks
        /// </summary>
        public void Stop()
        {
            _schedulerCancellation?.Cancel();

            Task[] tasks = new[] { _dailyTask, _gamePreviewTask, _weeklyTask }
                .Where(t => t != null)
                .Cast<Task>()
                .ToArray();

            if (tasks.Length > 0)
            {
                try
                {
                    Task.WaitAll(tasks, TimeSpan.FromSeconds(5));
                }
                catch (AggregateException ex) when (ex.InnerExceptions.All(e => e is OperationCanceledException))
                {
                    // Expected during shutdown.
                }
            }

            _schedulerCancellation?.Dispose();
            _schedulerCancellation = null;
            _dailyTask = null;
            _gamePreviewTask = null;
            _weeklyTask = null;
            _mlbClient?.Dispose();
        }

        /// <summary>
        /// Enable scheduler for a specific guild
        /// </summary>
        public void Enable(ulong guildId)
        {
            _guildEnabledState[guildId] = true;
            Console.WriteLine($"Braves Scheduler enabled for guild {guildId}.");
        }

        /// <summary>
        /// Disable scheduler for a specific guild
        /// </summary>
        public void Disable(ulong guildId)
        {
            _guildEnabledState[guildId] = false;
            Console.WriteLine($"Braves Scheduler disabled for guild {guildId}.");
        }

        /// <summary>
        /// Get scheduler status for a specific guild
        /// </summary>
        public string GetStatus(ulong guildId)
        {
            bool isEnabled = IsEnabledForGuild(guildId);
            DateTime estNow = GetEasternTime();
            DateTime lastDaily = GetLastDailyPost(guildId);
            DateTime lastWeekly = GetLastWeeklyPost(guildId);

            return $"**Braves Scheduler Status (Guild {guildId}):**\n" +
                   $"Enabled: {(isEnabled ? "✅ Yes" : "❌ No")}\n" +
                   $"Current EST Time: {estNow:yyyy-MM-dd HH:mm:ss}\n" +
                   $"Last Daily Post: {(lastDaily == DateTime.MinValue ? "Never" : lastDaily.ToString("yyyy-MM-dd"))}\n" +
                   $"Last Weekly Post: {(lastWeekly == DateTime.MinValue ? "Never" : lastWeekly.ToString("yyyy-MM-dd"))}\n" +
                   $"Target Channel: #{_bravesChannelName}";
        }

        private bool IsEnabledForGuild(ulong guildId)
        {
            if (!_guildEnabledState.ContainsKey(guildId))
                _guildEnabledState[guildId] = DEFAULT_ENABLED;
            return _guildEnabledState[guildId];
        }

        private DateTime GetLastDailyPost(ulong guildId)
        {
            return _lastDailyPost.ContainsKey(guildId) ? _lastDailyPost[guildId] : DateTime.MinValue;
        }

        private DateTime GetLastWeeklyPost(ulong guildId)
        {
            return _lastWeeklyPost.ContainsKey(guildId) ? _lastWeeklyPost[guildId] : DateTime.MinValue;
        }

        private int GetLastGamePreviewId(ulong guildId)
        {
            return _lastGamePreviewId.ContainsKey(guildId) ? _lastGamePreviewId[guildId] : 0;
        }

        /// <summary>
        /// Manually trigger daily schedule post for a specific guild (for testing)
        /// </summary>
        public async Task<string> TriggerDailyPost(ulong guildId)
        {
            try
            {
                await PostDailySchedule(guildId);
                return "✅ Daily schedule post triggered successfully.";
            }
            catch (Exception ex)
            {
                return $"❌ Error triggering daily post: {ex.Message}";
            }
        }

        /// <summary>
        /// Manually trigger weekly standings post for a specific guild (for testing)
        /// </summary>
        public async Task<string> TriggerWeeklyPost(ulong guildId)
        {
            try
            {
                await PostWeeklyStandings(guildId);
                return "✅ Weekly standings post triggered successfully.";
            }
            catch (Exception ex)
            {
                return $"❌ Error triggering weekly post: {ex.Message}";
            }
        }

        #region Scheduled Tasks

        /// <summary>
        /// Check if it's time for the daily 10am EST post and post to all enabled guilds
        /// </summary>
        private async Task CheckDailySchedule()
        {
            try
            {
                DateTime estNow = GetEasternTime();

                foreach (SocketGuild guild in _client.Guilds)
                {
                    if (!IsEnabledForGuild(guild.Id)) continue;

                    DateTime lastDaily = GetLastDailyPost(guild.Id);
                    if (ShouldPostDailySchedule(estNow, lastDaily))
                    {
                        Console.WriteLine($"Triggering daily schedule post at {estNow} for guild {guild.Name}");
                        _lastDailyPost[guild.Id] = estNow.Date;
                        await PostDailySchedule(guild.Id);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in CheckDailySchedule: {ex.Message}");
            }
        }

        /// <summary>
        /// Check if there's a game starting in ~30 minutes and post to all enabled guilds
        /// </summary>
        private async Task CheckGamePreview()
        {
            try
            {
                ScheduleResponse? schedule = await _mlbClient.GetTodaysScheduleAsync(
                    sportId: SportIds.MLB,
                    teamId: BRAVES_TEAM_ID);

                if (schedule?.Dates != null && schedule.Dates.Any())
                {
                    foreach (ScheduleDate date in schedule.Dates)
                    {
                        foreach (MLBStatsAPI.Models.Game game in date.Games)
                        {
                            foreach (SocketGuild guild in _client.Guilds)
                            {
                                if (!IsEnabledForGuild(guild.Id)) continue;

                                int lastPreviewId = GetLastGamePreviewId(guild.Id);
                                if (ShouldPostGamePreview(game, lastPreviewId, DateTime.UtcNow))
                                {
                                    Console.WriteLine($"Triggering game preview for game {game.GamePk} in guild {guild.Name}");
                                    _lastGamePreviewId[guild.Id] = game.GamePk;
                                    await PostGamePreview(game, guild.Id);
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in CheckGamePreview: {ex.Message}");
            }
        }

        /// <summary>
        /// Check if it's Monday morning for weekly standings post and post to all enabled guilds
        /// </summary>
        private async Task CheckWeeklyStandings()
        {
            try
            {
                DateTime estNow = GetEasternTime();

                foreach (SocketGuild guild in _client.Guilds)
                {
                    if (!IsEnabledForGuild(guild.Id)) continue;

                    DateTime lastWeekly = GetLastWeeklyPost(guild.Id);
                    if (ShouldPostWeeklyStandings(estNow, lastWeekly))
                    {
                        Console.WriteLine($"Triggering weekly standings post at {estNow} for guild {guild.Name}");
                        _lastWeeklyPost[guild.Id] = estNow.Date;
                        await PostWeeklyStandings(guild.Id);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in CheckWeeklyStandings: {ex.Message}");
            }
        }

        #endregion

        #region Testable Logic (Pure Methods)

        /// <summary>
        /// Determines if it's time to post the daily schedule (10am EST, once per day)
        /// </summary>
        internal bool ShouldPostDailySchedule(DateTime estNow, DateTime lastDailyPost)
        {
            DateTime today = estNow.Date;
            return estNow.Hour == 10 && estNow.Minute < 5 && lastDailyPost.Date != today;
        }

        /// <summary>
        /// Determines if a game preview should be posted (30-35 min before game start)
        /// </summary>
        internal bool ShouldPostGamePreview(MLBStatsAPI.Models.Game game, int lastGamePreviewId, DateTime utcNow)
        {
            TimeSpan timeUntilGame = game.GameDate - utcNow;
            return timeUntilGame.TotalMinutes >= 30 && timeUntilGame.TotalMinutes <= 35 && 
                   game.GamePk != lastGamePreviewId;
        }

        /// <summary>
        /// Determines if it's time to post weekly standings (Monday 8am-11am EST, once per week)
        /// </summary>
        internal bool ShouldPostWeeklyStandings(DateTime estNow, DateTime lastWeeklyPost)
        {
            DateTime today = estNow.Date;
            return estNow.DayOfWeek == DayOfWeek.Monday && 
                   estNow.Hour >= 8 && estNow.Hour < 11 &&
                   lastWeeklyPost.Date != today;
        }

        /// <summary>
        /// Builds the daily schedule message (testable, no Discord dependency)
        /// </summary>
        internal async Task<string> BuildDailyScheduleMessageAsync(ScheduleResponse? schedule, ScheduleResponse? upcomingSchedule)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("🅰️ **Good Morning, Braves Country!**\n");

            if (schedule?.Dates == null || !schedule.Dates.Any())
            {
                // No game today - find next game
                sb.AppendLine("**No game scheduled for today.** 🏖️");
                
                if (upcomingSchedule?.Dates != null && upcomingSchedule.Dates.Any())
                {
                    ScheduleDate nextDate = upcomingSchedule.Dates.First();
                    MLBStatsAPI.Models.Game nextGame = nextDate.Games.First();
                    
                    string opponent = nextGame.Teams?.Home?.Team?.Id == BRAVES_TEAM_ID 
                        ? nextGame.Teams?.Away?.Team?.Name ?? "TBD"
                        : nextGame.Teams?.Home?.Team?.Name ?? "TBD";
                    string location = nextGame.Teams?.Home?.Team?.Id == BRAVES_TEAM_ID ? "vs" : "@";
                    string gameTime = nextGame.GameDate.ToLocalTime().ToString("dddd, MMMM dd @ h:mm tt");
                    
                    sb.AppendLine($"\n**Next Game:**");
                    sb.AppendLine($"⚾ Braves {location} **{opponent}**");
                    sb.AppendLine($"📅 {gameTime}");
                }
            }
            else
            {
                MLBStatsAPI.Models.Game game = schedule.Dates.First().Games.First();
                
                string awayTeam = game.Teams?.Away?.Team?.Name ?? "TBD";
                string homeTeam = game.Teams?.Home?.Team?.Name ?? "TBD";
                int? awayTeamId = game.Teams?.Away?.Team?.Id;
                int? homeTeamId = game.Teams?.Home?.Team?.Id;
                
                string opponent = homeTeamId == BRAVES_TEAM_ID ? awayTeam : homeTeam;
                string location = homeTeamId == BRAVES_TEAM_ID ? "vs" : "@";
                string gameTime = game.GameDate.ToLocalTime().ToString("h:mm tt");
                
                sb.AppendLine($"**Today's Game:**");
                sb.AppendLine($"⚾ Braves {location} **{opponent}**");
                sb.AppendLine($"🕐 Game Time: {gameTime} EST");
                sb.AppendLine($"📺 Venue: {game.Venue?.Name ?? "TBD"}");

                // Fetch records from standings
                try
                {
                    int currentYear = DateTime.UtcNow.Year;
                    StandingsResponse? recordsData = await _mlbClient.GetDivisionStandingsAsync(
                        divisionId: NL_EAST_DIVISION_ID,
                        season: currentYear);

                    // Also fetch all standings to cover the opponent (any division)
                    StandingsResponse? allStandings = await _mlbClient.GetCurrentStandingsAsync();

                    int opponentId = homeTeamId == BRAVES_TEAM_ID ? (awayTeamId ?? 0) : (homeTeamId ?? 0);

                    TeamRecord? bravesRecord = recordsData?.Records
                        ?.SelectMany(r => r.TeamRecords ?? Enumerable.Empty<TeamRecord>())
                        .FirstOrDefault(t => t.Team?.Id == BRAVES_TEAM_ID);

                    TeamRecord? opponentRecord = allStandings?.Records
                        ?.SelectMany(r => r.TeamRecords ?? Enumerable.Empty<TeamRecord>())
                        .FirstOrDefault(t => t.Team?.Id == opponentId);

                    sb.AppendLine();
                    if (bravesRecord != null)
                        sb.AppendLine($"**Braves Record:** {bravesRecord.Wins}-{bravesRecord.Losses} ({bravesRecord.WinningPercentage})");
                    if (opponentRecord != null)
                        sb.AppendLine($"**{opponent} Record:** {opponentRecord.Wins}-{opponentRecord.Losses} ({opponentRecord.WinningPercentage})");
                }
                catch
                {
                    // Skip records if not available
                }

                // Try to get probable pitchers info from game data
                try
                {
                    GameResponse? gameData = await _mlbClient.GetGameAsync(game.GamePk);
                    if (gameData?.GameData != null)
                    {
                        string gameDataJson = JsonSerializer.Serialize(gameData.GameData);
                        using JsonDocument doc = JsonDocument.Parse(gameDataJson);
                        
                        if (doc.RootElement.TryGetProperty("probablePitchers", out JsonElement pitchers))
                        {
                            sb.AppendLine("\n**Probable Pitchers:**");
                            
                            if (pitchers.TryGetProperty("away", out JsonElement awayPitcher) && 
                                awayPitcher.TryGetProperty("fullName", out JsonElement awayName))
                            {
                                sb.AppendLine($"  {awayTeam}: {awayName.GetString()}");
                            }
                            
                            if (pitchers.TryGetProperty("home", out JsonElement homePitcher) && 
                                homePitcher.TryGetProperty("fullName", out JsonElement homeName))
                            {
                                sb.AppendLine($"  {homeTeam}: {homeName.GetString()}");
                            }
                        }
                    }
                }
                catch
                {
                    // Skip pitcher info if not available
                }
                
                sb.AppendLine("\n*Preview post coming 30 minutes before first pitch!* 🔥");
            }

            return sb.ToString();
        }

        /// <summary>
        /// Builds the game preview message (testable, no Discord dependency)
        /// </summary>
        internal async Task<string> BuildGamePreviewMessageAsync(MLBStatsAPI.Models.Game game)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("🔥 **GAME DAY - 30 MINUTES UNTIL FIRST PITCH!** 🔥\n");

            string awayTeam = game.Teams?.Away?.Team?.Name ?? "TBD";
            string homeTeam = game.Teams?.Home?.Team?.Name ?? "TBD";
            string opponent = game.Teams?.Home?.Team?.Id == BRAVES_TEAM_ID ? awayTeam : homeTeam;
            string location = game.Teams?.Home?.Team?.Id == BRAVES_TEAM_ID ? "vs" : "@";
            
            sb.AppendLine($"⚾ **Braves {location} {opponent}**");
            sb.AppendLine($"🕐 First Pitch: {game.GameDate.ToLocalTime():h:mm tt} EST");
            sb.AppendLine($"📺 {game.Venue?.Name ?? "TBD"}");
            
            // Get detailed game info
            try
            {
                GameResponse? gameData = await _mlbClient.GetGameAsync(game.GamePk);
                
                if (gameData?.GameData != null)
                {
                    string gameDataJson = JsonSerializer.Serialize(gameData.GameData);
                    using JsonDocument doc = JsonDocument.Parse(gameDataJson);
                    
                    // Probable Pitchers
                    if (doc.RootElement.TryGetProperty("probablePitchers", out JsonElement pitchers))
                    {
                        sb.AppendLine("\n**🎯 Starting Pitchers:**");
                        
                        if (pitchers.TryGetProperty("away", out JsonElement awayP))
                        {
                            string name = awayP.TryGetProperty("fullName", out JsonElement n) ? n.GetString() ?? "TBD" : "TBD";
                            sb.AppendLine($"  **{awayTeam}:** {name}");
                        }
                        
                        if (pitchers.TryGetProperty("home", out JsonElement homeP))
                        {
                            string name = homeP.TryGetProperty("fullName", out JsonElement n) ? n.GetString() ?? "TBD" : "TBD";
                            sb.AppendLine($"  **{homeTeam}:** {name}");
                        }
                    }
                    
                    // Weather
                    if (doc.RootElement.TryGetProperty("weather", out JsonElement weather))
                    {
                        string temp = weather.TryGetProperty("temp", out JsonElement t) ? t.GetString() ?? "?" : "?";
                        string condition = weather.TryGetProperty("condition", out JsonElement c) ? c.GetString() ?? "Unknown" : "Unknown";
                        string wind = weather.TryGetProperty("wind", out JsonElement w) ? w.GetString() ?? "?" : "?";
                        
                        sb.AppendLine($"\n**🌤️ Weather:** {temp}°F, {condition}");
                        sb.AppendLine($"**💨 Wind:** {wind}");
                    }
                }
                
                // Try to get lineups if available
                if (gameData?.LiveData != null)
                {
                    string liveDataJson = JsonSerializer.Serialize(gameData.LiveData);
                    using JsonDocument liveDoc = JsonDocument.Parse(liveDataJson);
                    
                    if (liveDoc.RootElement.TryGetProperty("boxscore", out JsonElement boxscore) &&
                        boxscore.TryGetProperty("teams", out JsonElement teams))
                    {
                        sb.AppendLine("\n**📋 Lineups:**");
                        sb.AppendLine("*Lineups will be posted when officially released*");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting detailed game info: {ex.Message}");
            }
            
            sb.AppendLine("\n**Let's Go Braves!** 🪓🪓🪓");

            return sb.ToString();
        }

        /// <summary>
        /// Builds the weekly standings message (testable, no Discord dependency)
        /// </summary>
        internal string BuildWeeklyStandingsMessage(StandingsResponse? standings)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("🅰️ **MONDAY MORNING STANDINGS UPDATE** 🅰️\n");
            sb.AppendLine("**⚾ NL East Standings:**\n");

            if (standings?.Records == null || !standings.Records.Any())
            {
                sb.AppendLine("*Unable to fetch standings data*");
                return sb.ToString();
            }

            sb.AppendLine("```");
            sb.AppendLine("Team                    W    L   GB   WCGB   RS   RA  DIFF  Streak");
            sb.AppendLine("──────────────────────────────────────────────────────────────────");

            StandingRecord record = standings.Records.First();
            if (record.TeamRecords != null)
            {
                foreach (TeamRecord team in record.TeamRecords)
                {
                    string teamName = team.Team?.Name ?? "Unknown";
                    
                    // Highlight the Braves
                    string prefix = team.Team?.Id == BRAVES_TEAM_ID ? ">" : " ";
                    
                    teamName = teamName.Length > 21 ? teamName.Substring(0, 21) : teamName.PadRight(21);
                    
                    string wins   = team.Wins.ToString();
                    string losses = team.Losses.ToString();
                    string gb     = team.GamesBack ?? "-";
                    string wcgb   = team.WildCardGamesBack ?? "-";
                    string rs     = team.RunsScored.HasValue ? team.RunsScored.Value.ToString() : "-";
                    string ra     = team.RunsAllowed.HasValue ? team.RunsAllowed.Value.ToString() : "-";
                    string diff   = team.RunDifferential.HasValue
                        ? (team.RunDifferential.Value >= 0 ? $"+{team.RunDifferential.Value}" : team.RunDifferential.Value.ToString())
                        : "-";
                    string streak = team.Streak?.StreakCode ?? "-";

                    sb.AppendLine($"{prefix}{teamName} {wins,4} {losses,4} {gb,5} {wcgb,6} {rs,4} {ra,4} {diff,5}  {streak}");
                }
            }

            sb.AppendLine("```");
            sb.AppendLine("\n*Have a great week, Braves fans!* ⚾");

            return sb.ToString();
        }

        #endregion

        #region Message Generators

        /// <summary>
        /// Post daily schedule to Braves channel in a specific guild
        /// </summary>
        private async Task PostDailySchedule(ulong guildId)
        {
            try
            {
                IMessageChannel? channel = await GetBravesChannelForGuild(guildId);
                if (channel == null) return;

                ScheduleResponse? schedule = await _mlbClient.GetTodaysScheduleAsync(
                    sportId: SportIds.MLB,
                    teamId: BRAVES_TEAM_ID);

                ScheduleResponse? upcomingSchedule = null;
                if (schedule?.Dates == null || !schedule.Dates.Any())
                {
                    // No game today - find next game
                    DateTime today = DateTime.UtcNow;
                    DateTime nextWeek = today.AddDays(7);
                    
                    upcomingSchedule = await _mlbClient.GetScheduleAsync(
                        startDate: today.ToString("yyyy-MM-dd"),
                        endDate: nextWeek.ToString("yyyy-MM-dd"),
                        sportId: SportIds.MLB,
                        teamId: BRAVES_TEAM_ID
                    );
                }

                string message = await BuildDailyScheduleMessageAsync(schedule, upcomingSchedule);
                await channel.SendMessageAsync(message);
                Console.WriteLine("Daily schedule posted successfully.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error posting daily schedule: {ex.Message}");
            }
        }

        /// <summary>
        /// Post detailed game preview to Braves channel in a specific guild
        /// </summary>
        private async Task PostGamePreview(MLBStatsAPI.Models.Game game, ulong guildId)
        {
            try
            {
                IMessageChannel? channel = await GetBravesChannelForGuild(guildId);
                if (channel == null) return;

                string message = await BuildGamePreviewMessageAsync(game);
                await channel.SendMessageAsync(message);
                Console.WriteLine($"Game preview posted for game {game.GamePk}.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error posting game preview: {ex.Message}");
            }
        }

        /// <summary>
        /// Post weekly NL East standings to Braves channel in a specific guild
        /// </summary>
        private async Task PostWeeklyStandings(ulong guildId)
        {
            try
            {
                IMessageChannel? channel = await GetBravesChannelForGuild(guildId);
                if (channel == null) return;

                int currentYear = DateTime.UtcNow.Year;
                StandingsResponse? standings = await _mlbClient.GetDivisionStandingsAsync(
                    divisionId: NL_EAST_DIVISION_ID,
                    season: currentYear);

                string message = BuildWeeklyStandingsMessage(standings);
                await channel.SendMessageAsync(message);
                Console.WriteLine("Weekly standings posted successfully.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error posting weekly standings: {ex.Message}");
            }
        }

        #endregion

        #region Helper Methods

        /// <summary>
        /// Get Eastern Time (EST/EDT)
        /// </summary>
        private DateTime GetEasternTime()
        {
            TimeZoneInfo easternZone = TimeZoneInfo.FindSystemTimeZoneById("Eastern Standard Time");
            return TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, easternZone);
        }

        /// <summary>
        /// Find and return the Braves channel for a specific guild
        /// </summary>
        private async Task<IMessageChannel?> GetBravesChannelForGuild(ulong guildId)
        {
            await Task.Delay(0); // Make async
            
            SocketGuild? guild = _client.Guilds.FirstOrDefault(g => g.Id == guildId);
            if (guild == null)
            {
                Console.WriteLine($"Warning: Guild {guildId} not found");
                return null;
            }

            IMessageChannel? channel = guild.TextChannels.FirstOrDefault(c => 
                c.Name.Equals(_bravesChannelName, StringComparison.OrdinalIgnoreCase));
            
            if (channel != null)
            {
                Console.WriteLine($"Found Braves channel: {channel.Name} in guild: {guild.Name}");
                return channel;
            }
            
            Console.WriteLine($"Warning: Could not find channel named '{_bravesChannelName}' in guild {guild.Name}");
            return null;
        }

        #endregion
    }
}
