#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using MLBStatsAPI.IDs;
using MLBStatsAPI.Models;

namespace DongBot
{
    /// <summary>
    /// Manages MLB-related Discord commands using the MLBStatsAPI
    /// </summary>
    public class MLBCommandManager : ICommandManager
    {
        private readonly IMLBDataClient _mlbClient;
        private readonly IBravesSchedulerControl? _bravesScheduler;
        private readonly INameIdResolver _nameIdResolver;
        private readonly IStandingsFilterService _standingsFilterService;
        private readonly IEntityLookupService _entityLookupService;
        private const int BRAVES_TEAM_ID = TeamIds.AtlantaBraves;
        private const int NL_EAST_DIVISION_ID = DivisionIds.NLEast;

        public MLBCommandManager(
            IBravesSchedulerControl? bravesScheduler = null,
            IMLBDataClient? mlbClient = null,
            INameIdResolver? nameIdResolver = null,
            IStandingsFilterService? standingsFilterService = null,
            IEntityLookupService? entityLookupService = null)
        {
            _mlbClient = mlbClient ?? new MLBDataClient();
            _bravesScheduler = bravesScheduler;
            _nameIdResolver = nameIdResolver ?? NameIdResolver.Default;
            _standingsFilterService = standingsFilterService ?? new StandingsFilterService(_nameIdResolver);
            _entityLookupService = entityLookupService ?? new EntityLookupService(_mlbClient, _nameIdResolver);
        }

        /// <summary>
        /// Returns true for MLB- and BRAVES- prefixed commands
        /// </summary>
        public bool CanHandle(string command)
        {
            string upper = command.ToUpper();
            return upper.StartsWith("MLB-") || upper.StartsWith("BRAVES-");
        }

        /// <summary>
        /// Process MLB commands and return response
        /// </summary>
        public async Task<string> ProcessCommandAsync(string command, CommandContext context)
        {
            try
            {
                string upperCommand = command.ToUpper();

                // Braves scheduler commands
                if (upperCommand.Equals("BRAVES-SCHEDULER-STATUS"))
                {
                    return _bravesScheduler?.GetStatus() ?? "Scheduler not initialized.";
                }

                if (upperCommand.Equals("BRAVES-SCHEDULER-ENABLE"))
                {
                    _bravesScheduler?.Enable();
                    return "Braves scheduler enabled.";
                }

                if (upperCommand.Equals("BRAVES-SCHEDULER-DISABLE"))
                {
                    _bravesScheduler?.Disable();
                    return "Braves scheduler disabled.";
                }

                if (upperCommand.Equals("BRAVES-SCHEDULER-TEST-DAILY"))
                {
                    return _bravesScheduler != null
                        ? await _bravesScheduler.TriggerDailyPost()
                        : "Scheduler not initialized.";
                }

                if (upperCommand.Equals("BRAVES-SCHEDULER-TEST-WEEKLY"))
                {
                    return _bravesScheduler != null
                        ? await _bravesScheduler.TriggerWeeklyPost()
                        : "Scheduler not initialized.";
                }

                // Braves-specific commands
                if (upperCommand.Equals("BRAVES-SCHEDULE"))
                {
                    return await GetBravesScheduleAsync();
                }

                if (upperCommand.Equals("BRAVES-SCORE"))
                {
                    return await GetBravesScoreAsync();
                }

                if (upperCommand.Equals("BRAVES-STANDINGS"))
                {
                    return await GetBravesStandingsAsync();
                }

                if (upperCommand.Equals("BRAVES-ROSTER"))
                {
                    return await GetBravesRosterAsync();
                }

                // General MLB commands
                if (upperCommand.Equals("MLB-SCHEDULE"))
                {
                    return await GetTodaysScheduleAsync();
                }

                if (upperCommand.Equals("MLB-SCORES"))
                {
                    return await GetLiveScoresAsync();
                }

                if (upperCommand.StartsWith("MLB-STANDINGS"))
                {
                    return await GetStandingsAsync(command);
                }

                if (upperCommand.StartsWith("MLB-DIVISION"))
                {
                    return await GetStandingsAsync(command, StandingsFilterScope.Division);
                }

                if (upperCommand.StartsWith("MLB-LEAGUE"))
                {
                    return await GetStandingsAsync(command, StandingsFilterScope.League);
                }

                if (upperCommand.StartsWith("MLB-SPORT"))
                {
                    return await GetStandingsAsync(command, StandingsFilterScope.Sport);
                }

                if (upperCommand.StartsWith("MLB-TEAM"))
                {
                    return await GetTeamInfoAsync(command);
                }

                if (upperCommand.StartsWith("MLB-VENUE"))
                {
                    return await GetVenueInfoAsync(command);
                }

                // Player stats commands
                if (upperCommand.StartsWith("MLB-PLAYER-STATS"))
                {
                    return await GetPlayerStatsAsync(command);
                }

                if (upperCommand.StartsWith("MLB-PLAYER"))
                {
                    return await GetPlayerInfoAsync(command);
                }

                if (upperCommand.Equals("MLB-HELP") || upperCommand.Equals("BRAVES-HELP"))
                {
                    return GetHelp(context);
                }

                return string.Empty; // Not an MLB command
            }
            catch (Exception ex)
            {
                return $"Error processing MLB command: {ex.Message}";
            }
        }

        #region Braves-Specific Commands

        /// <summary>
        /// Get Atlanta Braves schedule (next 7 days)
        /// </summary>
        private async Task<string> GetBravesScheduleAsync()
        {
            DateTime today = DateTime.UtcNow;
            DateTime nextWeek = today.AddDays(7);

            ScheduleResponse? schedule = await _mlbClient.GetScheduleAsync(
                startDate: today.ToString("yyyy-MM-dd"),
                endDate: nextWeek.ToString("yyyy-MM-dd"),
                sportId: SportIds.MLB,
                teamId: BRAVES_TEAM_ID
            );

            if (schedule?.Dates == null || !schedule.Dates.Any())
            {
                return "🅰️ No upcoming Braves games scheduled.";
            }

            StringBuilder sb = new StringBuilder();
            sb.AppendLine("🅰️ **Atlanta Braves Schedule**\n");

            foreach (ScheduleDate date in schedule.Dates)
            {
                foreach (Game game in date.Games)
                {
                    string awayTeam = game.Teams?.Away?.Team?.Name ?? "TBD";
                    string homeTeam = game.Teams?.Home?.Team?.Name ?? "TBD";
                    string gameTime = game.GameDate.ToLocalTime().ToString("ddd, MMM dd - h:mm tt");
                    string status = game.Status?.DetailedState ?? "Scheduled";
                    
                    string location = game.Teams?.Home?.Team?.Id == BRAVES_TEAM_ID ? "vs" : "@";
                    string opponent = game.Teams?.Home?.Team?.Id == BRAVES_TEAM_ID ? awayTeam : homeTeam;
                    
                    sb.AppendLine($"⚾ **Braves {location} {opponent}**");
                    sb.AppendLine($"   {gameTime}");
                    sb.AppendLine($"   Status: {status}");
                    
                    if (game.Status?.AbstractGameState == "Live" || game.Status?.AbstractGameState == "Final")
                    {
                        int awayScore = game.Teams?.Away?.Score ?? 0;
                        int homeScore = game.Teams?.Home?.Score ?? 0;
                        sb.AppendLine($"   Score: {awayTeam} {awayScore} - {homeTeam} {homeScore}");
                    }
                    
                    sb.AppendLine();
                }
            }

            return sb.ToString().TrimEnd();
        }

        /// <summary>
        /// Get today's Braves game score
        /// </summary>
        private async Task<string> GetBravesScoreAsync()
        {
            ScheduleResponse? schedule = await _mlbClient.GetTodaysScheduleAsync(
                sportId: SportIds.MLB,
                teamId: BRAVES_TEAM_ID
            );

            if (schedule?.Dates == null || !schedule.Dates.Any())
            {
                return "🅰️ No Braves game scheduled today.";
            }

            Game? game = schedule.Dates.First().Games.FirstOrDefault();
            if (game == null)
            {
                return "🅰️ No Braves game scheduled today.";
            }

            string awayTeam = game.Teams?.Away?.Team?.Name ?? "TBD";
            string homeTeam = game.Teams?.Home?.Team?.Name ?? "TBD";
            string status = game.Status?.DetailedState ?? "Scheduled";

            StringBuilder sb = new StringBuilder();
            sb.AppendLine("🅰️ **Atlanta Braves - Today's Game**\n");
            sb.AppendLine($"⚾ **{awayTeam}** @ **{homeTeam}**");
            sb.AppendLine($"Status: {status}");

            if (game.Status?.AbstractGameState == "Live" || game.Status?.AbstractGameState == "Final")
            {
                int awayScore = game.Teams?.Away?.Score ?? 0;
                int homeScore = game.Teams?.Home?.Score ?? 0;
                sb.AppendLine($"**Score: {awayScore} - {homeScore}**");
            }
            else
            {
                string gameTime = game.GameDate.ToLocalTime().ToString("h:mm tt");
                sb.AppendLine($"Game Time: {gameTime}");
            }

            return sb.ToString();
        }

        /// <summary>
        /// Get NL East standings (Braves' division)
        /// </summary>
        private async Task<string> GetBravesStandingsAsync()
        {
            int currentYear = DateTime.UtcNow.Year;
            StandingsResponse? standings = await _mlbClient.GetDivisionStandingsAsync(
                divisionId: NL_EAST_DIVISION_ID,
                season: currentYear
            );

            if (standings?.Records == null || !standings.Records.Any())
            {
                return "❌ Could not retrieve NL East standings.";
            }

            StringBuilder sb = new StringBuilder();
            sb.AppendLine("🅰️ **NL East Standings**\n");
            sb.AppendLine("```");
            sb.AppendLine("Team                    W    L   GB   WCGB  Streak");
            sb.AppendLine("─────────────────────────────────────────────────────");

            StandingRecord record = standings.Records.First();
            if (record.TeamRecords != null)
            {
                foreach (TeamRecord team in record.TeamRecords)
                {
                    string teamName = team.Team?.Name ?? "Unknown";
                    
                    // Highlight the Braves
                    string prefix = team.Team?.Id == BRAVES_TEAM_ID ? ">" : " ";
                    
                    teamName = teamName.Length > 21 ? teamName.Substring(0, 21) : teamName.PadRight(21);
                    
                    string wins = team.Wins.ToString();
                    string losses = team.Losses.ToString();
                    string gb = team.GamesBack ?? "-";
                    string wcgb = team.WildCardGamesBack ?? "-";
                    string streak = team.Streak?.StreakCode ?? "-";

                    sb.AppendLine($"{prefix}{teamName} {wins,4} {losses,4} {gb,5} {wcgb,6}  {streak}");
                }
            }

            sb.AppendLine("```");
            return sb.ToString();
        }

        /// <summary>
        /// Get Braves roster
        /// </summary>
        private async Task<string> GetBravesRosterAsync()
        {
            try
            {
                int currentYear = DateTime.UtcNow.Year;
                string rosterJson = await _mlbClient.GetRosterAsync(
                    teamId: BRAVES_TEAM_ID,
                    season: currentYear,
                    rosterType: "active"
                );

                // Parse the JSON to extract roster info
                using JsonDocument doc = JsonDocument.Parse(rosterJson);
                JsonElement root = doc.RootElement;

                if (!root.TryGetProperty("roster", out JsonElement rosterArray))
                {
                    return "❌ Could not retrieve Braves roster.";
                }

                StringBuilder sb = new StringBuilder();
                sb.AppendLine("🅰️ **Atlanta Braves Active Roster**\n");

                // Group by position
                sb.AppendLine("**Pitchers:**");
                foreach (JsonElement player in rosterArray.EnumerateArray())
                {
                    if (player.TryGetProperty("position", out JsonElement pos) &&
                        pos.TryGetProperty("type", out JsonElement posType) &&
                        posType.GetString() == "Pitcher")
                    {
                        string name = player.GetProperty("person").GetProperty("fullName").GetString() ?? "Unknown";
                        string number = player.TryGetProperty("jerseyNumber", out JsonElement num) ? num.GetString() ?? "-" : "-";
                        sb.AppendLine($"  #{number} {name}");
                    }
                }

                sb.AppendLine("\n**Position Players:**");
                foreach (JsonElement player in rosterArray.EnumerateArray())
                {
                    if (player.TryGetProperty("position", out JsonElement pos) &&
                        pos.TryGetProperty("type", out JsonElement posType) &&
                        posType.GetString() != "Pitcher")
                    {
                        string name = player.GetProperty("person").GetProperty("fullName").GetString() ?? "Unknown";
                        string number = player.TryGetProperty("jerseyNumber", out JsonElement num) ? num.GetString() ?? "-" : "-";
                        string position = pos.GetProperty("abbreviation").GetString() ?? "-";
                        sb.AppendLine($"  #{number} {name} ({position})");
                    }
                }

                return sb.ToString();
            }
            catch (Exception ex)
            {
                return $"❌ Error retrieving roster: {ex.Message}";
            }
        }

        #endregion

        #region Player Stats Commands

        /// <summary>
        /// Get player information and current season stats
        /// </summary>
        private async Task<string> GetPlayerInfoAsync(string command)
        {
            string[] parts = command.Split(' ', StringSplitOptions.RemoveEmptyEntries);

            if (parts.Length < 2)
            {
                return "❌ Usage: !mlb-player [player name]\nExample: !mlb-player Ronald Acuna";
            }

            string playerName = string.Join(" ", parts.Skip(1));

            try
            {
                PlayerLookupResult match = await _entityLookupService.ResolvePlayerAsync(playerName);
                if (match.Status == EntityLookupStatus.DataUnavailable)
                {
                    return "❌ Could not retrieve player information.";
                }

                if (match.Status != EntityLookupStatus.Found || !match.PlayerId.HasValue)
                {
                    return $"❌ Player not found: {playerName}";
                }

                // Get player details
                PeopleResponse? playerInfo = await _mlbClient.GetPersonAsync(match.PlayerId.Value);
                if (playerInfo?.People == null || !playerInfo.People.Any())
                {
                    return $"❌ Could not retrieve player details.";
                }

                Person player = playerInfo.People.First();
                
                // Get current season stats
                int currentYear = DateTime.UtcNow.Year;
                StatsResponse? stats = await _mlbClient.GetPlayerStatsAsync(match.PlayerId.Value, season: currentYear);

                StringBuilder sb = new StringBuilder();
                sb.AppendLine($"⚾ **{player.FullName}**");
                sb.AppendLine($"#{player.PrimaryNumber ?? "N/A"} | {player.PrimaryPosition?.Name ?? "N/A"}");
                sb.AppendLine($"**Team:** {player.CurrentTeam?.Name ?? "N/A"}");
                sb.AppendLine($"**Bats/Throws:** {player.BatSide?.Code ?? "?"}/{player.PitchHand?.Code ?? "?"}");
                sb.AppendLine($"**Age:** {CalculateAge(player.BirthDate)} | **Height:** {player.Height ?? "N/A"} | **Weight:** {(player.Weight.HasValue ? player.Weight.Value + " lbs" : "N/A")}");
                
                // Display stats if available
                if (stats?.Stats != null && stats.Stats.Any())
                {
                    sb.AppendLine($"\n**{currentYear} Season Stats:**");
                    foreach (StatGroup statGroup in stats.Stats)
                    {
                        if (statGroup.Splits != null && statGroup.Splits.Any())
                        {
                            StatSplit split = statGroup.Splits.First();
                            PlayerStats? playerStats = split.Stat;

                            if (playerStats != null)
                            {
                                // Check if batter or pitcher
                                if (playerStats.AtBats.HasValue && playerStats.AtBats > 0)
                                {
                                    // Batting stats
                                    sb.AppendLine("```");
                                    sb.AppendLine($"AVG: {playerStats.Avg ?? ".000"} | OBP: {playerStats.Obp ?? ".000"} | SLG: {playerStats.Slg ?? ".000"}");
                                    sb.AppendLine($"HR: {playerStats.HomeRuns ?? 0} | RBI: {playerStats.Rbi ?? 0} | R: {playerStats.Runs ?? 0}");
                                    sb.AppendLine($"H: {playerStats.Hits ?? 0} | 2B: {playerStats.Doubles ?? 0} | 3B: {playerStats.Triples ?? 0}");
                                    sb.AppendLine($"BB: {playerStats.BaseOnBalls ?? 0} | SO: {playerStats.StrikeOuts ?? 0}");
                                    sb.AppendLine("```");
                                }
                                else if (playerStats.Wins.HasValue || playerStats.InningsPitched != null)
                                {
                                    // Pitching stats
                                    sb.AppendLine("```");
                                    sb.AppendLine($"W-L: {playerStats.Wins ?? 0}-{playerStats.Losses ?? 0} | ERA: {playerStats.Era ?? "-.--"}");
                                    sb.AppendLine($"IP: {playerStats.InningsPitched ?? "0.0"} | SO: {playerStats.StrikeOuts ?? 0} | BB: {playerStats.BaseOnBalls ?? 0}");
                                    sb.AppendLine($"WHIP: {playerStats.Whip ?? "-.--"} | BAA: {playerStats.Avg ?? ".---"}");
                                    sb.AppendLine("```");
                                }
                            }
                        }
                    }
                }
                else
                {
                    sb.AppendLine($"\n*No stats available for {currentYear} season*");
                }

                return sb.ToString();
            }
            catch (Exception ex)
            {
                return $"❌ Error retrieving player info: {ex.Message}";
            }
        }

        /// <summary>
        /// Get player stats for a specific season or date range
        /// </summary>
        private async Task<string> GetPlayerStatsAsync(string command)
        {
            string[] parts = command.Split(' ', StringSplitOptions.RemoveEmptyEntries);

            if (parts.Length < 3)
            {
                return "❌ Usage: !mlb-player-stats [player name] [season]\nExample: !mlb-player-stats Ronald Acuna 2023";
            }

            // Last part should be the season year
            if (!int.TryParse(parts[parts.Length - 1], out int season))
            {
                return "❌ Invalid season year. Usage: !mlb-player-stats [player name] [season]";
            }

            string playerName = string.Join(" ", parts.Skip(1).Take(parts.Length - 2));

            try
            {
                PlayerLookupResult match = await _entityLookupService.ResolvePlayerAsync(playerName);
                if (match.Status == EntityLookupStatus.DataUnavailable)
                {
                    return "❌ Could not retrieve player information.";
                }

                if (match.Status != EntityLookupStatus.Found || !match.PlayerId.HasValue)
                {
                    return $"❌ Player not found: {playerName}";
                }

                // Get stats for the specified season
                StatsResponse? stats = await _mlbClient.GetPlayerStatsAsync(match.PlayerId.Value, season: season);

                if (stats?.Stats == null || !stats.Stats.Any())
                {
                    return $"❌ No stats found for {match.ResolvedName ?? playerName} in {season} season.";
                }

                StringBuilder sb = new StringBuilder();
                sb.AppendLine($"⚾ **{match.ResolvedName ?? playerName} - {season} Season Stats**\n");

                foreach (StatGroup statGroup in stats.Stats)
                {
                    if (statGroup.Splits != null && statGroup.Splits.Any())
                    {
                        StatSplit split = statGroup.Splits.First();
                        PlayerStats? playerStats = split.Stat;
                        string teamName = split.Team?.Name ?? "Unknown";

                        if (playerStats != null)
                        {
                            sb.AppendLine($"**Team:** {teamName}");
                            sb.AppendLine($"**Games:** {playerStats.GamesPlayed ?? 0}");

                            // Check if batter or pitcher
                            if (playerStats.AtBats.HasValue && playerStats.AtBats > 0)
                            {
                                // Batting stats
                                sb.AppendLine("\n**Batting Stats:**");
                                sb.AppendLine("```");
                                sb.AppendLine($"AVG: {playerStats.Avg ?? ".000"}  OBP: {playerStats.Obp ?? ".000"}  SLG: {playerStats.Slg ?? ".000"}  OPS: {playerStats.Ops ?? ".000"}");
                                sb.AppendLine($"AB: {playerStats.AtBats}  H: {playerStats.Hits ?? 0}  2B: {playerStats.Doubles ?? 0}  3B: {playerStats.Triples ?? 0}");
                                sb.AppendLine($"HR: {playerStats.HomeRuns ?? 0}  RBI: {playerStats.Rbi ?? 0}  R: {playerStats.Runs ?? 0}");
                                sb.AppendLine($"BB: {playerStats.BaseOnBalls ?? 0}  SO: {playerStats.StrikeOuts ?? 0}");
                                sb.AppendLine($"SB: {playerStats.StolenBases ?? 0}  CS: {playerStats.CaughtStealing ?? 0}");
                                sb.AppendLine("```");
                            }
                            else if (playerStats.Wins.HasValue || playerStats.InningsPitched != null)
                            {
                                // Pitching stats
                                sb.AppendLine("\n**Pitching Stats:**");
                                sb.AppendLine("```");
                                sb.AppendLine($"W-L: {playerStats.Wins ?? 0}-{playerStats.Losses ?? 0}  ERA: {playerStats.Era ?? "-.--"}  WHIP: {playerStats.Whip ?? "-.--"}");
                                sb.AppendLine($"IP: {playerStats.InningsPitched ?? "0.0"}  H: {playerStats.Hits ?? 0}  R: {playerStats.Runs ?? 0}  ER: {playerStats.EarnedRuns ?? 0}");
                                sb.AppendLine($"SO: {playerStats.StrikeOuts ?? 0}  BB: {playerStats.BaseOnBalls ?? 0}  HR: {playerStats.HomeRuns ?? 0}");
                                sb.AppendLine($"BAA: {playerStats.Avg ?? ".---"}  SV: {playerStats.Saves ?? 0}");
                                sb.AppendLine("```");
                            }
                        }
                    }
                }

                return sb.ToString();
            }
            catch (Exception ex)
            {
                return $"❌ Error retrieving player stats: {ex.Message}";
            }
        }

        /// <summary>
        /// Calculate age from birth date
        /// </summary>
        private int CalculateAge(string? birthDate)
        {
            if (string.IsNullOrEmpty(birthDate) || !DateTime.TryParse(birthDate, out DateTime birth))
            {
                return 0;
            }

            DateTime today = DateTime.Today;
            int age = today.Year - birth.Year;
            if (birth.Date > today.AddYears(-age)) age--;
            return age;
        }

        #endregion

        /// <summary>
        /// Get today's schedule
        /// </summary>
        private async Task<string> GetTodaysScheduleAsync()
        {
            ScheduleResponse? schedule = await _mlbClient.GetTodaysScheduleAsync(sportId: SportIds.MLB);

            if (schedule?.Dates == null || !schedule.Dates.Any())
            {
                return "📅 No games scheduled for today.";
            }

            StringBuilder sb = new StringBuilder();
            sb.AppendLine("📅 **Today's MLB Schedule**\n");

            foreach (ScheduleDate date in schedule.Dates)
            {
                foreach (Game game in date.Games)
                {
                    string awayTeam = game.Teams?.Away?.Team?.Name ?? "TBD";
                    string homeTeam = game.Teams?.Home?.Team?.Name ?? "TBD";
                    string gameTime = game.GameDate.ToLocalTime().ToString("h:mm tt");
                    string status = game.Status?.DetailedState ?? "Unknown";
                    
                    sb.AppendLine($"⚾ **{awayTeam}** @ **{homeTeam}**");
                    sb.AppendLine($"   Time: {gameTime} | Status: {status}");
                    
                    if (game.Status?.AbstractGameState == "Live" || game.Status?.AbstractGameState == "Final")
                    {
                        int awayScore = game.Teams?.Away?.Score ?? 0;
                        int homeScore = game.Teams?.Home?.Score ?? 0;
                        sb.AppendLine($"   Score: {awayTeam} {awayScore} - {homeTeam} {homeScore}");
                    }
                    
                    sb.AppendLine();
                }
            }

            return sb.ToString().TrimEnd();
        }

        /// <summary>
        /// Get live scores
        /// </summary>
        private async Task<string> GetLiveScoresAsync()
        {
            ScheduleResponse? schedule = await _mlbClient.GetTodaysScheduleAsync(sportId: SportIds.MLB);

            if (schedule?.Dates == null || !schedule.Dates.Any())
            {
                return "📊 No games today.";
            }

            StringBuilder sb = new StringBuilder();
            sb.AppendLine("📊 **Live MLB Scores**\n");
            bool hasLiveGames = false;

            foreach (ScheduleDate date in schedule.Dates)
            {
                foreach (Game game in date.Games)
                {
                    string? status = game.Status?.AbstractGameState;
                    
                    if (status == "Live" || status == "Final")
                    {
                        hasLiveGames = true;
                        string awayTeam = game.Teams?.Away?.Team?.Name ?? "TBD";
                        string homeTeam = game.Teams?.Home?.Team?.Name ?? "TBD";
                        int awayScore = game.Teams?.Away?.Score ?? 0;
                        int homeScore = game.Teams?.Home?.Score ?? 0;

                        sb.AppendLine($"⚾ **{awayTeam}** {awayScore} - {homeScore} **{homeTeam}**");
                        sb.AppendLine($"   {game.Status?.DetailedState}");
                        sb.AppendLine();
                    }
                }
            }

            if (!hasLiveGames)
            {
                return "📊 No live or completed games at this time.";
            }

            return sb.ToString().TrimEnd();
        }

        /// <summary>
        /// Get standings
        /// </summary>
        private async Task<string> GetStandingsAsync(string command)
            => await GetStandingsAsync(command, StandingsFilterScope.Auto);

        private async Task<string> GetStandingsAsync(string command, StandingsFilterScope filterScope)
        {
            string[] parts = command.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            
            // Default to current standings
            StandingsResponse? standings = await _mlbClient.GetCurrentStandingsAsync();

            if (standings?.Records == null || !standings.Records.Any())
            {
                return "❌ Could not retrieve standings.";
            }

            StringBuilder sb = new StringBuilder();
            sb.AppendLine("🏆 **MLB Standings**\n");

            StandingsFilter filter = _standingsFilterService.ParseFilter(parts, filterScope);

            foreach (StandingRecord record in standings.Records)
            {
                string divisionName = record.Division?.Name ?? "Unknown";
                
                if (!_standingsFilterService.RecordMatches(record, filter))
                {
                    continue;
                }

                sb.AppendLine($"**{divisionName}**");
                sb.AppendLine("```");
                sb.AppendLine("Team                    W    L   GB   WCGB  Streak");
                sb.AppendLine("─────────────────────────────────────────────────────");

                if (record.TeamRecords != null)
                {
                    foreach (var team in record.TeamRecords)
                    {
                        var teamName = team.Team?.Name ?? "Unknown";
                        teamName = teamName.Length > 22 ? teamName.Substring(0, 22) : teamName.PadRight(22);
                        
                        var wins = team.Wins.ToString();
                        var losses = team.Losses.ToString();
                        var gb = team.GamesBack ?? "-";
                        var wcgb = team.WildCardGamesBack ?? "-";
                        var streak = team.Streak?.StreakCode ?? "-";

                        sb.AppendLine($"{teamName} {wins,4} {losses,4} {gb,5} {wcgb,6}  {streak}");
                    }
                }

                sb.AppendLine("```\n");
            }

            return sb.ToString().TrimEnd();
        }

        /// <summary>
        /// Get team information
        /// </summary>
        private async Task<string> GetTeamInfoAsync(string command)
        {
            string[] parts = command.Split(' ', StringSplitOptions.RemoveEmptyEntries);

            if (parts.Length < 2)
            {
                return "❌ Usage: !mlb-team [team name or abbreviation]";
            }

            string teamQuery = string.Join(" ", parts.Skip(1));
            TeamLookupResult teamLookup = await _entityLookupService.ResolveTeamAsync(teamQuery);

            if (teamLookup.Status == EntityLookupStatus.DataUnavailable)
            {
                return "❌ Could not retrieve team information.";
            }

            Team? team = teamLookup.Team;

            if (team == null)
            {
                return $"❌ Team not found: {string.Join(" ", parts.Skip(1))}";
            }

            StringBuilder sb = new StringBuilder();
            sb.AppendLine($"⚾ **{team.Name}**\n");
            sb.AppendLine($"**Abbreviation:** {team.Abbreviation}");
            sb.AppendLine($"**League:** {team.League?.Name ?? "N/A"}");
            sb.AppendLine($"**Division:** {team.Division?.Name ?? "N/A"}");
            sb.AppendLine($"**Venue:** {team.Venue?.Name ?? "N/A"}");
            sb.AppendLine($"**Location:** {team.LocationName ?? "N/A"}");
            sb.AppendLine($"**First Year:** {team.FirstYearOfPlay ?? "N/A"}");

            // Get today's game for this team
            ScheduleResponse? schedule = await _mlbClient.GetTodaysScheduleAsync(sportId: SportIds.MLB, teamId: team.Id);
            
            if (schedule?.Dates != null && schedule.Dates.Any())
            {
                sb.AppendLine("\n**Today's Game:**");
                Game? game = schedule.Dates.First().Games.FirstOrDefault();
                if (game != null)
                {
                    string awayTeam = game.Teams?.Away?.Team?.Name ?? "TBD";
                    string homeTeam = game.Teams?.Home?.Team?.Name ?? "TBD";
                    string gameTime = game.GameDate.ToLocalTime().ToString("h:mm tt");
                    string status = game.Status?.DetailedState ?? "Unknown";
                    
                    sb.AppendLine($"{awayTeam} @ {homeTeam}");
                    sb.AppendLine($"Time: {gameTime} | Status: {status}");

                    if (game.Status?.AbstractGameState == "Live" || game.Status?.AbstractGameState == "Final")
                    {
                        int awayScore = game.Teams?.Away?.Score ?? 0;
                        int homeScore = game.Teams?.Home?.Score ?? 0;
                        sb.AppendLine($"Score: {awayScore} - {homeScore}");
                    }
                }
            }

            return sb.ToString().TrimEnd();
        }

        private async Task<string> GetVenueInfoAsync(string command)
        {
            string[] parts = command.Split(' ', StringSplitOptions.RemoveEmptyEntries);

            if (parts.Length < 2)
            {
                return "❌ Usage: !mlb-venue [venue name]";
            }

            string venueQuery = string.Join(" ", parts.Skip(1));
            VenueLookupResult resolvedVenue = await _entityLookupService.ResolveVenueAsync(venueQuery);

            if (resolvedVenue.Status == EntityLookupStatus.DataUnavailable)
            {
                return "❌ Could not retrieve venue information.";
            }

            if (resolvedVenue.Status != EntityLookupStatus.Found || resolvedVenue.Venue == null)
            {
                return $"❌ Venue not found: {venueQuery}";
            }

            Venue venue = resolvedVenue.Venue;
            IReadOnlyList<Team> homeTeams = resolvedVenue.HomeTeams;

            StringBuilder sb = new StringBuilder();
            sb.AppendLine($"🏟️ **{venue.Name}**\n");
            sb.AppendLine($"**Venue ID:** {venue.Id}");
            sb.AppendLine($"**City:** {venue.Location?.City ?? "N/A"}");
            sb.AppendLine($"**State:** {venue.Location?.StateAbbrev ?? venue.Location?.State ?? "N/A"}");
            sb.AppendLine($"**Roof:** {venue.FieldInfo?.RoofType ?? "N/A"}");
            sb.AppendLine($"**Surface:** {venue.FieldInfo?.TurfType ?? "N/A"}");
            sb.AppendLine($"**Capacity:** {(venue.FieldInfo?.Capacity?.ToString() ?? "N/A")}");

            if (homeTeams.Any())
            {
                string teams = string.Join(", ", homeTeams.Select(t => t.Name).Where(n => !string.IsNullOrWhiteSpace(n)));
                sb.AppendLine($"**Home Team(s):** {teams}");
            }

            return sb.ToString().TrimEnd();
        }

        /// <summary>
        /// Get help text for MLB commands
        /// </summary>
        public string GetHelp(CommandContext context)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("⚾ **MLB Stats Commands**\n");
            
            sb.AppendLine("**🅰️ Atlanta Braves Commands:**");
            sb.AppendLine("**!braves-schedule** - Show Braves upcoming games (next 7 days)");
            sb.AppendLine("**!braves-score** - Show today's Braves game score");
            sb.AppendLine("**!braves-standings** - Show NL East standings");
            sb.AppendLine("**!braves-roster** - Show Braves active roster");
            
            sb.AppendLine("\n**⚾ General MLB Commands:**");
            sb.AppendLine("**!mlb-schedule** - Show today's MLB schedule");
            sb.AppendLine("**!mlb-scores** - Show live scores and final scores");
            sb.AppendLine("**!mlb-standings** [division] - Show current standings (optional: filter by division)");
            sb.AppendLine("**!mlb-division** [name] - Show standings for a division");
            sb.AppendLine("**!mlb-league** [name] - Show standings for a league");
            sb.AppendLine("**!mlb-sport** [name] - Show standings for a sport");
            sb.AppendLine("**!mlb-team** [name] - Show team information and today's game");
            sb.AppendLine("**!mlb-venue** [name] - Show venue information and home team");
            
            sb.AppendLine("\n**👤 Player Stats Commands:**");
            sb.AppendLine("**!mlb-player** [name] - Show player info and current season stats");
            sb.AppendLine("**!mlb-player-stats** [name] [season] - Show player stats for a specific season");
            
            sb.AppendLine("\n**!mlb-help** - Show this help message");
            
            sb.AppendLine("\n*Examples:*");
            sb.AppendLine("`!braves-score` - Today's Braves game");
            sb.AppendLine("`!braves-standings` - NL East standings");
            sb.AppendLine("`!mlb-player Ronald Acuna` - Current season stats");
            sb.AppendLine("`!mlb-player-stats Max Fried 2023` - 2023 season stats");
            sb.AppendLine("`!mlb-standings AL East` - AL East standings");

            return sb.ToString();
        }

        public void Dispose()
        {
            _mlbClient?.Dispose();
        }
    }
}
