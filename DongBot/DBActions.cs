using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Discord.WebSocket;

namespace DongBot
{
        class DBActions
        {
            

        /**
         * TODOS::
         * - Add a wrapper function for all of the actions here so that all of them are private and used by a single public call
         * */

        private Random rand = new Random();
        private GifCommandManager gifCommandManager = new GifCommandManager();
        private AuditLogger auditLogger = new AuditLogger();
        private StatisticsTracker statisticsTracker = new StatisticsTracker();


        // GIF Command Handler
        /// <summary>
        /// Process a GIF command and return a random GIF URL if it matches.
        /// </summary>
        /// <param name="command">The command text (after the ! prefix)</param>
        /// <param name="channelName">The Discord channel name</param>
        /// <param name="channelId">The Discord channel ID</param>
        /// <param name="userId">The user ID executing the command</param>
        /// <param name="username">The username executing the command</param>
        /// <returns>A GIF URL if the command matches, empty string otherwise</returns>
        public string DongGifs(string command, string channelName, ulong channelId, string userId, string username)
        {
            string result = gifCommandManager.ProcessCommand(command, channelName, channelId);
            
            // Track if this was a successful GIF command
            if (!string.IsNullOrEmpty(result))
            {
                statisticsTracker.TrackCommand(command.ToUpper(), "GIF", userId, username, channelName, true);
            }
            
            return result;
        }

        // GIF Command Management Methods
        /// <summary>
        /// Add or update a GIF command.
        /// Format: !gif-add COMMANDNAME http://url.com/gif.gif [channel] [pattern] [isRegex] [aliases]
        /// </summary>
        public string GifAdd(string command, string userId, string username, string channelName)
        {
            try
            {
                // Expected format: GIF-ADD COMMANDNAME URL [CHANNEL] [PATTERN] [ISREGEX] [ALIASES]
                string[] parts = command.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                
                if (parts.Length < 3)
                {
                    return "Usage: !gif-add COMMANDNAME URL [CHANNEL] [PATTERN] [ISREGEX] [ALIASES]\nExample: !gif-add DONG http://example.com/gif.gif baseball \"\" false DINGER,HR";
                }

                string commandKey = parts[1].ToUpper();
                string gifUrl = parts[2];
                string channel = parts.Length > 3 ? parts[3] : "";
                string pattern = parts.Length > 4 ? parts[4] : null;
                bool isRegex = false;
                if (parts.Length > 5)
                {
                    bool parsedIsRegex;
                    if (bool.TryParse(parts[5], out parsedIsRegex))
                    {
                        isRegex = parsedIsRegex;
                    }
                }
                string aliases = parts.Length > 6 ? parts[6] : null;

                string result = gifCommandManager.AddOrUpdateCommand(commandKey, gifUrl, channel, pattern, isRegex, aliases);
                
                bool success = result.Contains("Error") == false;
                
                // Log the action
                auditLogger.Log(userId, username, "ADD", "GIF_COMMAND", commandKey, 
                    $"Added GIF: {gifUrl} (Channel: {(string.IsNullOrEmpty(channel) ? "All" : channel)})", 
                    channelName, success);
                
                // Track statistics
                statisticsTracker.TrackCommand("GIF-ADD", "ADMIN", userId, username, channelName, success);
                
                return result;
            }
            catch (Exception ex)
            {
                auditLogger.Log(userId, username, "ADD", "GIF_COMMAND", "UNKNOWN", 
                    $"Failed: {ex.Message}", channelName, false);
                return $"Error processing gif-add command: {ex.Message}";
            }
        }

        /// <summary>
        /// Remove a GIF from a command or remove the entire command.
        /// Format: !gif-remove COMMANDNAME [URL]
        /// </summary>
        public string GifRemove(string command, string userId, string username, string channelName)
        {
            try
            {
                // Expected format: GIF-REMOVE COMMANDNAME [URL]
                string[] parts = command.Split(new[] { ' ' }, 3, StringSplitOptions.RemoveEmptyEntries);
                
                if (parts.Length < 2)
                {
                    return "Usage: !gif-remove COMMANDNAME [URL]\nIf URL is omitted, the entire command is removed.";
                }

                string commandKey = parts[1].ToUpper();
                string gifUrl = parts.Length > 2 ? parts[2] : null;

                string result = gifCommandManager.RemoveCommand(commandKey, gifUrl);
                
                bool success = result.Contains("Error") == false;
                
                // Log the action
                string details = string.IsNullOrEmpty(gifUrl) 
                    ? "Removed entire command" 
                    : $"Removed GIF: {gifUrl}";
                auditLogger.Log(userId, username, "REMOVE", "GIF_COMMAND", commandKey, 
                    details, channelName, success);
                
                // Track statistics
                statisticsTracker.TrackCommand("GIF-REMOVE", "ADMIN", userId, username, channelName, success);
                
                return result;
            }
            catch (Exception ex)
            {
                auditLogger.Log(userId, username, "REMOVE", "GIF_COMMAND", "UNKNOWN", 
                    $"Failed: {ex.Message}", channelName, false);
                return $"Error processing gif-remove command: {ex.Message}";
            }
        }

        /// <summary>
        /// Refresh/reload the GIF commands from the file.
        /// Format: !gif-refresh
        /// </summary>
        public string GifRefresh(string command, string userId, string username, string channelName)
        {
            try
            {
                gifCommandManager.LoadCommands();
                int count = gifCommandManager.GetCommandCount();
                
                // Log the action
                auditLogger.Log(userId, username, "REFRESH", "GIF_COMMAND", "ALL", 
                    $"Reloaded {count} commands from file", channelName, true);
                
                // Track statistics
                statisticsTracker.TrackCommand("GIF-REFRESH", "ADMIN", userId, username, channelName, true);
                
                return $"GIF commands refreshed! Loaded {count} commands.";
            }
            catch (Exception ex)
            {
                auditLogger.Log(userId, username, "REFRESH", "GIF_COMMAND", "ALL", 
                    $"Failed: {ex.Message}", channelName, false);
                return $"Error refreshing GIF commands: {ex.Message}";
            }
        }

        /// <summary>
        /// List all GIF commands or get details about a specific command.
        /// Format: !gif-list [COMMANDNAME]
        /// </summary>
        public string GifList(string command, string userId, string username, string channelName)
        {
            try
            {
                // Expected format: GIF-LIST [COMMANDNAME]
                string[] parts = command.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                string commandKey = parts.Length > 1 ? parts[1] : null;

                string result = gifCommandManager.ListCommands(commandKey);
                
                // Track statistics
                statisticsTracker.TrackCommand("GIF-LIST", "ADMIN", userId, username, channelName, true);
                
                return result;
            }
            catch (Exception ex)
            {
                statisticsTracker.TrackCommand("GIF-LIST", "ADMIN", userId, username, channelName, false);
                return $"Error listing GIF commands: {ex.Message}";
            }
        }

        /// <summary>
        /// Manage aliases for a command.
        /// Format: !gif-alias COMMANDNAME add|remove ALIAS
        /// </summary>
        public string GifAlias(string command, string userId, string username, string channelName)
        {
            try
            {
                // Expected format: GIF-ALIAS COMMANDNAME add|remove ALIAS
                string[] parts = command.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                
                if (parts.Length < 4)
                {
                    return "Usage: !gif-alias COMMANDNAME add|remove ALIAS\nExample: !gif-alias DONG add DINGER";
                }

                string commandKey = parts[1].ToUpper();
                string action = parts[2].ToLower();
                string alias = parts[3].ToUpper();

                string result;
                bool success;

                if (action == "add")
                {
                    result = gifCommandManager.AddAlias(commandKey, alias);
                    success = !result.Contains("Error");
                    
                    if (success)
                    {
                        auditLogger.Log(userId, username, "ADD_ALIAS", "GIF_COMMAND", commandKey, 
                            $"Added alias: {alias}", channelName, true);
                    }
                }
                else if (action == "remove")
                {
                    result = gifCommandManager.RemoveAlias(commandKey, alias);
                    success = !result.Contains("Error");
                    
                    if (success)
                    {
                        auditLogger.Log(userId, username, "REMOVE_ALIAS", "GIF_COMMAND", commandKey, 
                            $"Removed alias: {alias}", channelName, true);
                    }
                }
                else
                {
                    return "Invalid action. Use 'add' or 'remove'.";
                }

                // Track statistics
                statisticsTracker.TrackCommand("GIF-ALIAS", "ADMIN", userId, username, channelName, success);
                
                return result;
            }
            catch (Exception ex)
            {
                statisticsTracker.TrackCommand("GIF-ALIAS", "ADMIN", userId, username, channelName, false);
                return $"Error managing alias: {ex.Message}";
            }
        }

        /// <summary>
        /// Manage channel restrictions for GIF commands
        /// Format: !gif-channel COMMANDNAME add|remove|list|clear [CHANNELID]
        /// </summary>
        public string GifChannel(string command, string userId, string username, string channelName)
        {
            try
            {
                // Expected format: GIF-CHANNEL COMMANDNAME add|remove|list|clear [CHANNELID]
                string[] parts = command.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                
                if (parts.Length < 3)
                {
                    return "Usage: !gif-channel COMMANDNAME add|remove|list|clear [CHANNELID]\n" +
                           "Examples:\n" +
                           "  !gif-channel DONG add 123456789012345678\n" +
                           "  !gif-channel DONG remove 123456789012345678\n" +
                           "  !gif-channel DONG list\n" +
                           "  !gif-channel DONG clear";
                }

                string commandKey = parts[1].ToUpper();
                string action = parts[2].ToLower();

                string result;
                bool success;

                if (action == "add")
                {
                    if (parts.Length < 4)
                    {
                        return "Error: Channel ID required for 'add' action.\nUsage: !gif-channel COMMANDNAME add CHANNELID";
                    }

                    if (!ulong.TryParse(parts[3], out ulong channelId))
                    {
                        return "Error: Invalid channel ID. Must be a numeric value.";
                    }

                    result = gifCommandManager.AddAllowedChannel(commandKey, channelId);
                    success = !result.Contains("Error");
                    
                    if (success)
                    {
                        auditLogger.Log(userId, username, "ADD_CHANNEL_RESTRICTION", "GIF_COMMAND", commandKey, 
                            $"Added allowed channel: {channelId}", channelName, true);
                    }
                }
                else if (action == "remove")
                {
                    if (parts.Length < 4)
                    {
                        return "Error: Channel ID required for 'remove' action.\nUsage: !gif-channel COMMANDNAME remove CHANNELID";
                    }

                    if (!ulong.TryParse(parts[3], out ulong channelId))
                    {
                        return "Error: Invalid channel ID. Must be a numeric value.";
                    }

                    result = gifCommandManager.RemoveAllowedChannel(commandKey, channelId);
                    success = !result.Contains("Error");
                    
                    if (success)
                    {
                        auditLogger.Log(userId, username, "REMOVE_CHANNEL_RESTRICTION", "GIF_COMMAND", commandKey, 
                            $"Removed allowed channel: {channelId}", channelName, true);
                    }
                }
                else if (action == "list")
                {
                    result = gifCommandManager.ListAllowedChannels(commandKey);
                    success = !result.Contains("Error");
                    
                    if (success)
                    {
                        auditLogger.Log(userId, username, "LIST_CHANNEL_RESTRICTIONS", "GIF_COMMAND", commandKey, 
                            "Listed channel restrictions", channelName, true);
                    }
                }
                else if (action == "clear")
                {
                    result = gifCommandManager.ClearAllowedChannels(commandKey);
                    success = !result.Contains("Error");
                    
                    if (success)
                    {
                        auditLogger.Log(userId, username, "CLEAR_CHANNEL_RESTRICTIONS", "GIF_COMMAND", commandKey, 
                            "Cleared all channel restrictions", channelName, true);
                    }
                }
                else
                {
                    return "Invalid action. Use 'add', 'remove', 'list', or 'clear'.";
                }

                // Track statistics
                statisticsTracker.TrackCommand("GIF-CHANNEL", "ADMIN", userId, username, channelName, success);
                
                return result;
            }
            catch (Exception ex)
            {
                statisticsTracker.TrackCommand("GIF-CHANNEL", "ADMIN", userId, username, channelName, false);
                return $"Error managing channel restrictions: {ex.Message}";
            }
        }

        /// <summary>
        /// Validate GIF URLs in commands
        /// Format: !gif-validate [COMMANDNAME] [--check-access]
        /// </summary>
        public async Task<string> GifValidate(string command, string userId, string username, string channelName)
        {
            try
            {
                // Expected format: GIF-VALIDATE [COMMANDNAME] [--check-access]
                string[] parts = command.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                
                bool checkAccessibility = parts.Any(p => p.Equals("--check-access", StringComparison.OrdinalIgnoreCase));
                string commandKey = null;

                // Find command name (ignore flags)
                if (parts.Length > 1)
                {
                    commandKey = parts.FirstOrDefault(p => !p.Equals("GIF-VALIDATE", StringComparison.OrdinalIgnoreCase) 
                                                          && !p.StartsWith("--", StringComparison.OrdinalIgnoreCase));
                }

                string result;
                if (!string.IsNullOrEmpty(commandKey))
                {
                    // Validate specific command
                    result = await gifCommandManager.ValidateCommandUrlsAsync(commandKey.ToUpper(), checkAccessibility);
                    auditLogger.Log(userId, username, "VALIDATE", "GIF_COMMAND", commandKey.ToUpper(), 
                        $"Validated URLs (Accessibility check: {checkAccessibility})", channelName, true);
                }
                else
                {
                    // Validate all commands
                    result = await gifCommandManager.ValidateAllUrlsAsync(checkAccessibility);
                    auditLogger.Log(userId, username, "VALIDATE", "GIF_COMMAND", "ALL", 
                        $"Validated all URLs (Accessibility check: {checkAccessibility})", channelName, true);
                }

                // Track statistics
                statisticsTracker.TrackCommand("GIF-VALIDATE", "ADMIN", userId, username, channelName, true);
                
                return result;
            }
            catch (Exception ex)
            {
                statisticsTracker.TrackCommand("GIF-VALIDATE", "ADMIN", userId, username, channelName, false);
                return $"Error validating URLs: {ex.Message}";
            }
        }

        public string FilterMessage(SocketMessage message, char prefix)
        {
            // Filtering messages begin here
            if (!message.Content.StartsWith(prefix)) // This is your prefix
            {
                return "";
            }

            if (message.Author.IsBot) // This ignores all bots
            {
                return "";
            }

            int lengthOfCommand = message.Content.Contains(' ') ? message.Content.IndexOf(' ') : message.Content.Length;
            return message.Content.Substring(1, lengthOfCommand - 1);
        }

        // Audit Log Commands
        /// <summary>
        /// View recent audit log entries
        /// Format: !audit [count] [category]
        /// </summary>
        public string GetAuditLog(string command, string userId, string username)
        {
            try
            {
                string[] parts = command.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                int count = 10; // Default
                string category = null;

                if (parts.Length > 1 && int.TryParse(parts[1], out int parsedCount))
                {
                    count = Math.Min(parsedCount, 50); // Max 50
                }

                if (parts.Length > 2)
                {
                    category = parts[2].ToUpper();
                }

                List<AuditEntry> entries = auditLogger.GetRecentEntries(count, category, null);
                
                if (entries.Count == 0)
                {
                    return "No audit log entries found.";
                }

                StringBuilder sb = new StringBuilder();
                sb.AppendLine($"**Audit Log** (Last {entries.Count} entries)");
                sb.AppendLine($"```");
                
                foreach (AuditEntry entry in entries)
                {
                    string status = entry.Success ? "✓" : "✗";
                    sb.AppendLine($"{status} {entry.Timestamp:MM/dd HH:mm} | {entry.Username}");
                    sb.AppendLine($"   {entry.Action} - {entry.Category}/{entry.Target}");
                    sb.AppendLine($"   {entry.Details}");
                    sb.AppendLine();
                }
                
                sb.AppendLine($"```");
                
                // Log this audit view request
                auditLogger.Log(userId, username, "VIEW", "AUDIT", "LOG", 
                    $"Viewed {entries.Count} entries" + (category != null ? $" (Category: {category})" : ""));
                
                // Track statistics
                statisticsTracker.TrackCommand("AUDIT", "AUDIT", userId, username, "", true);
                
                return sb.ToString();
            }
            catch (Exception ex)
            {
                return $"Error retrieving audit log: {ex.Message}";
            }
        }

        /// <summary>
        /// Get audit log statistics
        /// Format: !audit-stats
        /// </summary>
        public string GetAuditStats(string userId, string username)
        {
            try
            {
                AuditStatistics stats = auditLogger.GetStatistics();
                
                StringBuilder sb = new StringBuilder();
                sb.AppendLine($"**Audit Log Statistics**");
                sb.AppendLine($"```");
                sb.AppendLine($"Total Entries: {stats.TotalEntries}");
                sb.AppendLine($"Unique Users: {stats.UniqueUsers}");
                
                if (stats.OldestEntry.HasValue && stats.NewestEntry.HasValue)
                {
                    sb.AppendLine($"Date Range: {stats.OldestEntry:MM/dd/yyyy} - {stats.NewestEntry:MM/dd/yyyy}");
                }
                
                sb.AppendLine();
                sb.AppendLine("Top Actions:");
                foreach (KeyValuePair<string, int> kvp in stats.ActionCounts.OrderByDescending(x => x.Value).Take(5))
                {
                    sb.AppendLine($"  {kvp.Key}: {kvp.Value}");
                }
                
                sb.AppendLine();
                sb.AppendLine("Categories:");
                foreach (KeyValuePair<string, int> kvp in stats.CategoryCounts.OrderByDescending(x => x.Value))
                {
                    sb.AppendLine($"  {kvp.Key}: {kvp.Value}");
                }
                
                sb.AppendLine($"```");
                
                // Log this stats view request
                auditLogger.Log(userId, username, "VIEW", "AUDIT", "STATS", "Viewed audit statistics");
                
                // Track statistics
                statisticsTracker.TrackCommand("AUDIT-STATS", "AUDIT", userId, username, "", true);
                
                return sb.ToString();
            }
            catch (Exception ex)
            {
                return $"Error retrieving audit statistics: {ex.Message}";
            }
        }

        // Statistics Commands
        /// <summary>
        /// Get overall statistics summary
        /// Format: !stats
        /// </summary>
        public string GetStats(string userId, string username)
        {
            try
            {
                StatisticsSummary summary = statisticsTracker.GetSummary();

                StringBuilder sb = new StringBuilder();
                sb.AppendLine($"**Bot Statistics Summary**");
                sb.AppendLine($"```");
                sb.AppendLine($"Total Commands Executed: {summary.TotalCommands:N0}");
                sb.AppendLine($"Unique Users: {summary.TotalUniqueUsers}");
                sb.AppendLine($"Active Channels: {summary.TotalChannels}");
                sb.AppendLine($"Command Types: {summary.TotalCommandTypes}");
                sb.AppendLine($"Commands Today: {summary.TodayCommandCount}");
                sb.AppendLine();
                
                if (!string.IsNullOrEmpty(summary.MostUsedCommand))
                {
                    sb.AppendLine($"Most Used Command:");
                    sb.AppendLine($"  {summary.MostUsedCommand} ({summary.MostUsedCommandCount:N0} times)");
                    sb.AppendLine();
                }
                
                if (!string.IsNullOrEmpty(summary.MostActiveUser))
                {
                    sb.AppendLine($"Most Active User:");
                    sb.AppendLine($"  {summary.MostActiveUser} ({summary.MostActiveUserCount:N0} commands)");
                }
                
                sb.AppendLine($"```");
                
                // Track this stats view
                statisticsTracker.TrackCommand("STATS", "STATS", userId, username, "", true);
                
                return sb.ToString();
            }
            catch (Exception ex)
            {
                return $"Error retrieving statistics: {ex.Message}";
            }
        }

        /// <summary>
        /// Get top commands by usage
        /// Format: !stats-top [count] [category]
        /// </summary>
        public string GetTopCommands(string command, string userId, string username)
        {
            try
            {
                string[] parts = command.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                int count = 10; // Default
                string category = null;

                if (parts.Length > 1 && int.TryParse(parts[1], out int parsedCount))
                {
                    count = Math.Min(parsedCount, 25); // Max 25
                }

                if (parts.Length > 2)
                {
                    category = parts[2].ToUpper();
                }

                List<CommandStats> topCommands = statisticsTracker.GetTopCommands(count, category);
                
                if (topCommands.Count == 0)
                {
                    return "No command statistics found.";
                }

                StringBuilder sb = new StringBuilder();
                sb.AppendLine($"**Top {topCommands.Count} Commands**" + (category != null ? $" (Category: {category})" : ""));
                sb.AppendLine($"```");
                
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
                
                sb.AppendLine($"```");
                
                // Track this stats view
                statisticsTracker.TrackCommand("STATS-TOP", "STATS", userId, username, "", true);
                
                return sb.ToString();
            }
            catch (Exception ex)
            {
                return $"Error retrieving top commands: {ex.Message}";
            }
        }

        /// <summary>
        /// Get statistics for a specific user
        /// Format: !stats-user [userId]
        /// </summary>
        public string GetUserStatistics(string command, string userId, string username)
        {
            try
            {
                string[] parts = command.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                string targetUserId = parts.Length > 1 ? parts[1] : userId; // Default to requesting user

                UserStats userStats = statisticsTracker.GetUserStats(targetUserId);
                
                if (userStats == null)
                {
                    return "No statistics found for this user.";
                }

                StringBuilder sb = new StringBuilder();
                sb.AppendLine($"**User Statistics: {userStats.Username}**");
                sb.AppendLine($"```");
                sb.AppendLine($"Total Commands: {userStats.TotalCommands:N0}");
                sb.AppendLine($"First Seen: {userStats.FirstSeen:MM/dd/yyyy HH:mm}");
                sb.AppendLine($"Last Seen: {userStats.LastSeen:MM/dd/yyyy HH:mm}");
                sb.AppendLine();
                sb.AppendLine("Top Commands:");
                
                int rank = 1;
                foreach ( KeyValuePair<string, int> kvp in userStats.CommandCounts.OrderByDescending(x => x.Value).Take(10))
                {
                    sb.AppendLine($"  {rank}. {kvp.Key}: {kvp.Value:N0}");
                    rank++;
                }
                
                sb.AppendLine($"```");
                
                // Track this stats view
                statisticsTracker.TrackCommand("STATS-USER", "STATS", userId, username, "", true);
                
                return sb.ToString();
            }
            catch (Exception ex)
            {
                return $"Error retrieving user statistics: {ex.Message}";
            }
        }

        /// <summary>
        /// Get statistics for a specific command
        /// Format: !stats-command COMMANDNAME
        /// </summary>
        public string GetCommandStatistics(string command, string userId, string username)
        {
            try
            {
                string[] parts = command.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                
                if (parts.Length < 2)
                {
                    return "Usage: !stats-command COMMANDNAME";
                }

                string commandName = parts[1].ToUpper();
                CommandStats cmdStats = statisticsTracker.GetCommandStats(commandName);
                
                if (cmdStats == null)
                {
                    return $"No statistics found for command: {commandName}";
                }

                StringBuilder sb = new StringBuilder();
                sb.AppendLine($"**Command Statistics: {cmdStats.CommandName}**");
                sb.AppendLine($"```");
                sb.AppendLine($"Category: {cmdStats.Category}");
                sb.AppendLine($"Total Executions: {cmdStats.TotalExecutions:N0}");
                sb.AppendLine($"Successful: {cmdStats.SuccessfulExecutions:N0} ({(cmdStats.TotalExecutions > 0 ? (double)cmdStats.SuccessfulExecutions / cmdStats.TotalExecutions * 100 : 0):F1}%)");
                sb.AppendLine($"Failed: {cmdStats.FailedExecutions:N0}");
                sb.AppendLine($"First Used: {cmdStats.FirstUsed:MM/dd/yyyy HH:mm}");
                sb.AppendLine($"Last Used: {cmdStats.LastUsed:MM/dd/yyyy HH:mm}");
                sb.AppendLine();
                
                sb.AppendLine("Top Users:");
                int rank = 1;
                foreach ( KeyValuePair<string, int> kvp in cmdStats.UserExecutions.OrderByDescending(x => x.Value).Take(5))
                {
                    sb.AppendLine($"  {rank}. User {kvp.Key}: {kvp.Value:N0}");
                    rank++;
                }
                
                if (cmdStats.ChannelExecutions.Count > 0)
                {
                    sb.AppendLine();
                    sb.AppendLine("Top Channels:");
                    rank = 1;
                    foreach ( KeyValuePair<string, int> kvp in cmdStats.ChannelExecutions.OrderByDescending(x => x.Value).Take(5))
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
                    foreach ( KeyValuePair<int, int> kvp in cmdStats.HourlyDistribution.OrderByDescending(x => x.Value).Take(3))
                    {
                        string hourLabel = kvp.Key == 0 ? "12 AM" : (kvp.Key < 12 ? $"{kvp.Key} AM" : (kvp.Key == 12 ? "12 PM" : $"{kvp.Key - 12} PM"));
                        sb.AppendLine($"  {rank}. {hourLabel}: {kvp.Value:N0}");
                        rank++;
                    }
                }
                
                sb.AppendLine($"```");
                
                // Track this stats view
                statisticsTracker.TrackCommand("STATS-COMMAND", "STATS", userId, username, "", true);
                
                return sb.ToString();
            }
            catch (Exception ex)
            {
                return $"Error retrieving command statistics: {ex.Message}";
            }
        }

        /// <summary>
        /// Get help information for the current channel
        /// Format: !help
        /// </summary>
        public string GetHelp(string channelName, ulong channelId, bool isAdminChannel, string userId, string username)
        {
            try
            {
                string result = gifCommandManager.GetChannelHelp(channelName, channelId, isAdminChannel);
                
                // Log help access
                auditLogger.Log(userId, username, "HELP", "INFO", "HELP", 
                    $"Accessed help in channel: {channelName}", channelName, true);
                
                // Track statistics
                statisticsTracker.TrackCommand("HELP", "INFO", userId, username, channelName, true);
                
                return result;
            }
            catch (Exception ex)
            {
                statisticsTracker.TrackCommand("HELP", "INFO", userId, username, channelName, false);
                return $"Error retrieving help: {ex.Message}";
            }
        }
    }
}
