using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DongBot
{
    /// <summary>
    /// Builds audit and statistics responses for admin commands.
    /// </summary>
    internal sealed class AdminReportingService
    {
        private readonly AuditLogger _auditLogger;
        private readonly StatisticsTracker _statisticsTracker;

        public AdminReportingService(AuditLogger auditLogger, StatisticsTracker statisticsTracker)
        {
            _auditLogger = auditLogger;
            _statisticsTracker = statisticsTracker;
        }

        public string GetAuditLog(string command, string userId, string username, string channelName, ulong guildId = 0)
        {
            try
            {
                string[] parts = command.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                int count = 10;
                string? category = null;

                if (parts.Length > 1 && int.TryParse(parts[1], out int parsedCount))
                    count = Math.Clamp(parsedCount, 1, 50);

                if (parts.Length > 2)
                    category = parts[2].ToUpperInvariant();

                List<AuditEntry> entries = _auditLogger.GetRecentEntries(count, category, null, guildId);

                if (entries.Count == 0)
                    return "No audit log entries found for this server.";

                StringBuilder sb = new StringBuilder();
                sb.AppendLine($"**Audit Log** (Last {entries.Count} entries)");
                sb.AppendLine("```");

                foreach (AuditEntry entry in entries)
                {
                    string status = entry.Success ? "✓" : "✗";
                    sb.AppendLine($"{status} {entry.Timestamp:MM/dd HH:mm} | {entry.Username}");
                    sb.AppendLine($"   {entry.Action} - {entry.Category}/{entry.Target}");
                    sb.AppendLine($"   {entry.Details}");
                    sb.AppendLine();
                }

                sb.AppendLine("```");

                _auditLogger.Log(userId, username, "VIEW", "AUDIT", "LOG",
                    $"Viewed {entries.Count} entries" + (category != null ? $" (Category: {category})" : string.Empty), 
                    channelName, guildId);
                _statisticsTracker.TrackCommand("AUDIT", "AUDIT", userId, username, channelName, true, guildId);

                return sb.ToString();
            }
            catch (Exception ex)
            {
                return $"Error retrieving audit log: {ex.Message}";
            }
        }

        public string GetAuditStats(string userId, string username, string channelName, ulong guildId = 0)
        {
            try
            {
                AuditStatistics stats = _auditLogger.GetStatistics(guildId);

                StringBuilder sb = new StringBuilder();
                sb.AppendLine("**Audit Log Statistics** (This Server)");
                sb.AppendLine("```");
                sb.AppendLine($"Total Entries: {stats.TotalEntries}");
                sb.AppendLine($"Unique Users: {stats.UniqueUsers}");

                if (stats.OldestEntry.HasValue && stats.NewestEntry.HasValue)
                    sb.AppendLine($"Date Range: {stats.OldestEntry:MM/dd/yyyy} - {stats.NewestEntry:MM/dd/yyyy}");

                sb.AppendLine();
                sb.AppendLine("Top Actions:");
                foreach (KeyValuePair<string, int> kvp in stats.ActionCounts.OrderByDescending(x => x.Value).Take(5))
                    sb.AppendLine($"  {kvp.Key}: {kvp.Value}");

                sb.AppendLine();
                sb.AppendLine("Categories:");
                foreach (KeyValuePair<string, int> kvp in stats.CategoryCounts.OrderByDescending(x => x.Value))
                    sb.AppendLine($"  {kvp.Key}: {kvp.Value}");

                sb.AppendLine("```");

                _auditLogger.Log(userId, username, "VIEW", "AUDIT", "STATS", "Viewed audit statistics", 
                    channelName, guildId);
                _statisticsTracker.TrackCommand("AUDIT-STATS", "AUDIT", userId, username, channelName, true, guildId);

                return sb.ToString();
            }
            catch (Exception ex)
            {
                return $"Error retrieving audit statistics: {ex.Message}";
            }
        }

        public string GetStats(string userId, string username, string channelName, ulong guildId = 0)
        {
            try
            {
                StatisticsSummary summary = _statisticsTracker.GetSummary(guildId);

                StringBuilder sb = new StringBuilder();
                sb.AppendLine("**Bot Statistics Summary** (This Server)");
                sb.AppendLine("```");
                sb.AppendLine($"Total Commands Executed: {summary.TotalCommands:N0}");
                sb.AppendLine($"Unique Users: {summary.TotalUniqueUsers}");
                sb.AppendLine($"Active Channels: {summary.TotalChannels}");
                sb.AppendLine($"Command Types: {summary.TotalCommandTypes}");
                sb.AppendLine($"Commands Today: {summary.TodayCommandCount}");
                sb.AppendLine();

                if (!string.IsNullOrEmpty(summary.MostUsedCommand))
                {
                    sb.AppendLine("Most Used Command:");
                    sb.AppendLine($"  {summary.MostUsedCommand} ({summary.MostUsedCommandCount:N0} times)");
                    sb.AppendLine();
                }

                if (!string.IsNullOrEmpty(summary.MostActiveUser))
                {
                    sb.AppendLine("Most Active User:");
                    sb.AppendLine($"  {summary.MostActiveUser} ({summary.MostActiveUserCount:N0} commands)");
                }

                sb.AppendLine("```");
                _statisticsTracker.TrackCommand("STATS", "STATS", userId, username, channelName, true, guildId);
                return sb.ToString();
            }
            catch (Exception ex)
            {
                return $"Error retrieving statistics: {ex.Message}";
            }
        }

        public string GetTopCommands(string command, string userId, string username, string channelName, ulong guildId = 0)
        {
            try
            {
                string[] parts = command.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                int count = 10;
                string? category = null;

                if (parts.Length > 1 && int.TryParse(parts[1], out int parsedCount))
                    count = Math.Clamp(parsedCount, 1, 25);

                if (parts.Length > 2)
                    category = parts[2].ToUpperInvariant();

                List<CommandStats> topCommands = _statisticsTracker.GetTopCommands(count, category, guildId);

                if (topCommands.Count == 0)
                    return "No command statistics found for this server.";

                StringBuilder sb = new StringBuilder();
                sb.AppendLine($"**Top {topCommands.Count} Commands** (This Server)" + (category != null ? $" (Category: {category})" : string.Empty));
                sb.AppendLine("```");

                int rank = 1;
                foreach (CommandStats cmd in topCommands)
                {
                    double successRate = cmd.TotalExecutions > 0
                        ? (double)cmd.SuccessfulExecutions / cmd.TotalExecutions * 100
                        : 0;
                    sb.AppendLine($"{rank}. {cmd.CommandName} [{cmd.Category}]");
                    sb.AppendLine($"   Total: {cmd.TotalExecutions:N0} | Success: {successRate:F1}%");
                    sb.AppendLine($"   Last used: {cmd.LastUsed:MM/dd/yyyy HH:mm}");
                    sb.AppendLine();
                    rank++;
                }

                sb.AppendLine("```");
                _statisticsTracker.TrackCommand("STATS-TOP", "STATS", userId, username, channelName, true, guildId);
                return sb.ToString();
            }
            catch (Exception ex)
            {
                return $"Error retrieving top commands: {ex.Message}";
            }
        }

        public string GetUserStatistics(string command, string userId, string username, string channelName, ulong guildId = 0)
        {
            try
            {
                string[] parts = command.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                string targetUserId = parts.Length > 1 ? parts[1] : userId;

                UserStats? userStats = _statisticsTracker.GetUserStats(targetUserId);
                if (userStats == null)
                    return "No statistics found for this user.";

                StringBuilder sb = new StringBuilder();
                sb.AppendLine($"**User Statistics: {userStats.Username}** (Global - partitioned by server)");
                sb.AppendLine("```");
                sb.AppendLine($"Total Commands: {userStats.TotalCommands:N0}");
                sb.AppendLine($"First Seen: {userStats.FirstSeen:MM/dd/yyyy HH:mm}");
                sb.AppendLine($"Last Seen: {userStats.LastSeen:MM/dd/yyyy HH:mm}");
                sb.AppendLine();
                
                if (userStats.GuildCommandCounts.Any())
                {
                    sb.AppendLine("Commands by Server:");
                    foreach (var kvp in userStats.GuildCommandCounts.OrderByDescending(x => x.Value))
                    {
                        sb.AppendLine($"  Guild {kvp.Key}: {kvp.Value:N0} commands");
                    }
                    sb.AppendLine();
                }

                sb.AppendLine("Top Commands:");

                int rank = 1;
                foreach (KeyValuePair<string, int> kvp in userStats.CommandCounts.OrderByDescending(x => x.Value).Take(10))
                {
                    sb.AppendLine($"  {rank}. {kvp.Key}: {kvp.Value:N0}");
                    rank++;
                }

                sb.AppendLine("```");
                _statisticsTracker.TrackCommand("STATS-USER", "STATS", userId, username, channelName, true, guildId);
                return sb.ToString();
            }
            catch (Exception ex)
            {
                return $"Error retrieving user statistics: {ex.Message}";
            }
        }

        public string GetCommandStatistics(string command, string userId, string username, string channelName, ulong guildId = 0)
        {
            try
            {
                string[] parts = command.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

                if (parts.Length < 2)
                    return "Usage: !stats-command COMMANDNAME";

                string commandName = parts[1].ToUpperInvariant();
                CommandStats? cmdStats = _statisticsTracker.GetCommandStats(commandName);

                if (cmdStats == null)
                    return $"No statistics found for command: {commandName}";

                StringBuilder sb = new StringBuilder();
                sb.AppendLine($"**Command Statistics: {cmdStats.CommandName}** (Global - partitioned by server)");
                sb.AppendLine("```");
                sb.AppendLine($"Category: {cmdStats.Category}");
                sb.AppendLine($"Total Executions: {cmdStats.TotalExecutions:N0}");
                sb.AppendLine($"Successful: {cmdStats.SuccessfulExecutions:N0} ({(cmdStats.TotalExecutions > 0 ? (double)cmdStats.SuccessfulExecutions / cmdStats.TotalExecutions * 100 : 0):F1}%)");
                sb.AppendLine($"Failed: {cmdStats.FailedExecutions:N0}");
                sb.AppendLine($"First Used: {cmdStats.FirstUsed:MM/dd/yyyy HH:mm}");
                sb.AppendLine($"Last Used: {cmdStats.LastUsed:MM/dd/yyyy HH:mm}");
                sb.AppendLine();

                if (cmdStats.GuildExecutions.Any())
                {
                    sb.AppendLine("Executions by Server:");
                    foreach (var kvp in cmdStats.GuildExecutions.OrderByDescending(x => x.Value))
                    {
                        double pct = (double)kvp.Value / cmdStats.TotalExecutions * 100;
                        sb.AppendLine($"  Guild {kvp.Key}: {kvp.Value:N0} ({pct:F1}%)");
                    }
                    sb.AppendLine();
                }

                sb.AppendLine("Top Users:");
                int rank = 1;
                foreach (KeyValuePair<string, int> kvp in cmdStats.UserExecutions.OrderByDescending(x => x.Value).Take(5))
                {
                    sb.AppendLine($"  {rank}. User {kvp.Key}: {kvp.Value:N0}");
                    rank++;
                }

                if (cmdStats.ChannelExecutions.Count > 0)
                {
                    sb.AppendLine();
                    sb.AppendLine("Top Channels:");
                    rank = 1;
                    foreach (KeyValuePair<string, int> kvp in cmdStats.ChannelExecutions.OrderByDescending(x => x.Value).Take(5))
                    {
                        sb.AppendLine($"  {rank}. {kvp.Key}: {kvp.Value:N0}");
                        rank++;
                    }
                }

                if (cmdStats.HourlyDistribution.Count > 0)
                {
                    sb.AppendLine();
                    sb.AppendLine("Peak Hours:");
                    rank = 1;
                    foreach (KeyValuePair<int, int> kvp in cmdStats.HourlyDistribution.OrderByDescending(x => x.Value).Take(3))
                    {
                        string hourLabel = kvp.Key == 0 ? "12 AM"
                            : kvp.Key < 12 ? $"{kvp.Key} AM"
                            : kvp.Key == 12 ? "12 PM"
                            : $"{kvp.Key - 12} PM";
                        sb.AppendLine($"  {rank}. {hourLabel}: {kvp.Value:N0}");
                        rank++;
                    }
                }

                sb.AppendLine("```");
                _statisticsTracker.TrackCommand("STATS-COMMAND", "STATS", userId, username, channelName, true, guildId);
                return sb.ToString();
            }
            catch (Exception ex)
            {
                return $"Error retrieving command statistics: {ex.Message}";
            }
        }
    }
}
