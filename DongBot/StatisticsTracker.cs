using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;

namespace DongBot
{
    /// <summary>
    /// Generalized statistics tracking system for all bot commands and operations.
    /// Tracks usage counts, timing, users, channels, and trends.
    /// </summary>
    public class StatisticsTracker
    {
        private readonly string _statsFilePath;
        private readonly object _lock = new object();
        private CommandStatistics _statistics;
        private readonly BackupManager _backupManager;

        public StatisticsTracker(string statsFilePath = "bot_statistics.json")
        {
            _statsFilePath = statsFilePath;
            
            // Initialize backup manager with backups subdirectory
            string backupDir = Path.Combine(Path.GetDirectoryName(_statsFilePath) ?? ".", "backups");
            _backupManager = new BackupManager(backupDir, maxBackupsToKeep: 10, enableTimedBackups: false);
            
            LoadStatistics();
        }

        /// <summary>
        /// Load statistics from file
        /// </summary>
        private void LoadStatistics()
        {
            lock (_lock)
            {
                try
                {
                    if (!File.Exists(_statsFilePath))
                    {
                        _statistics = new CommandStatistics
                        {
                            Commands = new Dictionary<string, CommandStats>(),
                            Users = new Dictionary<string, UserStats>(),
                            Channels = new Dictionary<string, ChannelStats>(),
                            DailyStats = new Dictionary<string, DailyStats>()
                        };
                        SaveStatistics();
                        return;
                    }

                    // Create backup before loading
                    _backupManager.BackupOnLoad(_statsFilePath);

                    string json = File.ReadAllText(_statsFilePath);
                    _statistics = JsonSerializer.Deserialize<CommandStatistics>(json, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });

                    if (_statistics == null)
                    {
                        _statistics = new CommandStatistics
                        {
                            Commands = new Dictionary<string, CommandStats>(),
                            Users = new Dictionary<string, UserStats>(),
                            Channels = new Dictionary<string, ChannelStats>(),
                            DailyStats = new Dictionary<string, DailyStats>()
                        };
                    }

                    Console.WriteLine($"Loaded statistics: {_statistics.Commands.Count} commands tracked");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error loading statistics: {ex.Message}");
                    _statistics = new CommandStatistics
                    {
                        Commands = new Dictionary<string, CommandStats>(),
                        Users = new Dictionary<string, UserStats>(),
                        Channels = new Dictionary<string, ChannelStats>(),
                        DailyStats = new Dictionary<string, DailyStats>()
                    };
                }
            }
        }

        /// <summary>
        /// Save statistics to file
        /// </summary>
        private void SaveStatistics()
        {
            lock (_lock)
            {
                try
                {
                    // Create backup before saving
                    _backupManager.BackupBeforeSave(_statsFilePath);

                    JsonSerializerOptions options = new JsonSerializerOptions
                    {
                        WriteIndented = true,
                        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                    };

                    string json = JsonSerializer.Serialize(_statistics, options);
                    File.WriteAllText(_statsFilePath, json);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error saving statistics: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// Track a command execution
        /// </summary>
        /// <param name="commandName">Name of the command</param>
        /// <param name="category">Category (GIF, AUDIT, ADMIN, etc.)</param>
        /// <param name="userId">Discord user ID</param>
        /// <param name="username">Discord username</param>
        /// <param name="channelName">Channel name where executed</param>
        /// <param name="success">Whether the command succeeded</param>
        public void TrackCommand(string commandName, string category, string userId, 
                                string username, string channelName, bool success = true)
        {
            lock (_lock)
            {
                try
                {
                    DateTime now = DateTime.Now;
                    string dateKey = now.ToString("yyyy-MM-dd");

                    // Track command-level statistics
                    if (!_statistics.Commands.ContainsKey(commandName))
                    {
                        _statistics.Commands[commandName] = new CommandStats
                        {
                            CommandName = commandName,
                            Category = category,
                            TotalExecutions = 0,
                            SuccessfulExecutions = 0,
                            FailedExecutions = 0,
                            FirstUsed = now,
                            LastUsed = now,
                            UserExecutions = new Dictionary<string, int>(),
                            ChannelExecutions = new Dictionary<string, int>(),
                            HourlyDistribution = new Dictionary<int, int>()
                        };
                    }

                    CommandStats cmdStats = _statistics.Commands[commandName];
                    cmdStats.TotalExecutions++;
                    if (success)
                    {
                        cmdStats.SuccessfulExecutions++;
                    }
                    else
                    {
                        cmdStats.FailedExecutions++;
                    }

                    cmdStats.LastUsed = now;

                    // Track by user
#pragma warning disable IDE0011 // Add braces
                    if (!cmdStats.UserExecutions.ContainsKey(userId))
                        cmdStats.UserExecutions[userId] = 0;
#pragma warning restore IDE0011 // Add braces
                    cmdStats.UserExecutions[userId]++;

                    // Track by channel
                    if (!string.IsNullOrEmpty(channelName))
                    {
                        if (!cmdStats.ChannelExecutions.ContainsKey(channelName))
                        {
                            cmdStats.ChannelExecutions[channelName] = 0;
                        }

                        cmdStats.ChannelExecutions[channelName]++;
                    }

                    // Track by hour of day
                    int hour = now.Hour;
                    if (!cmdStats.HourlyDistribution.ContainsKey(hour))
                    {
                        cmdStats.HourlyDistribution[hour] = 0;
                    }

                    cmdStats.HourlyDistribution[hour]++;

                    // Track user-level statistics
                    if (!_statistics.Users.ContainsKey(userId))
                    {
                        _statistics.Users[userId] = new UserStats
                        {
                            UserId = userId,
                            Username = username,
                            TotalCommands = 0,
                            CommandCounts = new Dictionary<string, int>(),
                            FirstSeen = now,
                            LastSeen = now
                        };
                    }

                    UserStats userStats = _statistics.Users[userId];
                    userStats.Username = username; // Update in case username changed
                    userStats.TotalCommands++;
                    userStats.LastSeen = now;

                    if (!userStats.CommandCounts.ContainsKey(commandName))
                    {
                        userStats.CommandCounts[commandName] = 0;
                    }

                    userStats.CommandCounts[commandName]++;

                    // Track channel-level statistics
                    if (!string.IsNullOrEmpty(channelName))
                    {
                        if (!_statistics.Channels.ContainsKey(channelName))
                        {
                            _statistics.Channels[channelName] = new ChannelStats
                            {
                                ChannelName = channelName,
                                TotalCommands = 0,
                                CommandCounts = new Dictionary<string, int>(),
                                ActiveUsers = new HashSet<string>()
                            };
                        }

                        ChannelStats channelStats = _statistics.Channels[channelName];
                        channelStats.TotalCommands++;

                        if (!channelStats.CommandCounts.ContainsKey(commandName))
                        {
                            channelStats.CommandCounts[commandName] = 0;
                        }

                        channelStats.CommandCounts[commandName]++;

                        channelStats.ActiveUsers.Add(userId);
                    }

                    // Track daily statistics
                    if (!_statistics.DailyStats.ContainsKey(dateKey))
                    {
                        _statistics.DailyStats[dateKey] = new DailyStats
                        {
                            Date = dateKey,
                            TotalCommands = 0,
                            UniqueUsers = new HashSet<string>(),
                            CommandCounts = new Dictionary<string, int>()
                        };
                    }

                    DailyStats dailyStats = _statistics.DailyStats[dateKey];
                    dailyStats.TotalCommands++;
                    dailyStats.UniqueUsers.Add(userId);

                    if (!dailyStats.CommandCounts.ContainsKey(commandName))
                    {
                        dailyStats.CommandCounts[commandName] = 0;
                    }

                    dailyStats.CommandCounts[commandName]++;

                    SaveStatistics();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error tracking command statistics: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// Get top commands by usage
        /// </summary>
        public List<CommandStats> GetTopCommands(int count = 10, string category = null)
        {
            lock (_lock)
            {
                IEnumerable<CommandStats> commands = _statistics.Commands.Values
                    .OrderByDescending(c => c.TotalExecutions);

                if (!string.IsNullOrEmpty(category))
                {
                    commands = commands.Where(c => c.Category.Equals(category, StringComparison.OrdinalIgnoreCase));
                }

                return commands.Take(count).ToList();
            }
        }

        /// <summary>
        /// Get statistics for a specific command
        /// </summary>
        public CommandStats GetCommandStats(string commandName)
        {
            lock (_lock)
            {
                return _statistics.Commands.ContainsKey(commandName) 
                    ? _statistics.Commands[commandName] 
                    : null;
            }
        }

        /// <summary>
        /// Get top users by command usage
        /// </summary>
        public List<UserStats> GetTopUsers(int count = 10)
        {
            lock (_lock)
            {
                return _statistics.Users.Values
                    .OrderByDescending(u => u.TotalCommands)
                    .Take(count)
                    .ToList();
            }
        }

        /// <summary>
        /// Get statistics for a specific user
        /// </summary>
        public UserStats GetUserStats(string userId)
        {
            lock (_lock)
            {
                return _statistics.Users.ContainsKey(userId) 
                    ? _statistics.Users[userId] 
                    : null;
            }
        }

        /// <summary>
        /// Get channel statistics
        /// </summary>
        public List<ChannelStats> GetChannelStats(int count = 10)
        {
            lock (_lock)
            {
                return _statistics.Channels.Values
                    .OrderByDescending(c => c.TotalCommands)
                    .Take(count)
                    .ToList();
            }
        }

        /// <summary>
        /// Get daily statistics for date range
        /// </summary>
        public List<DailyStats> GetDailyStats(int days = 7)
        {
            lock (_lock)
            {
                DateTime startDate = DateTime.Now.AddDays(-days);
                return _statistics.DailyStats.Values
                    .Where(d => DateTime.Parse(d.Date) >= startDate)
                    .OrderByDescending(d => d.Date)
                    .ToList();
            }
        }

        /// <summary>
        /// Get overall statistics summary
        /// </summary>
        public StatisticsSummary GetSummary()
        {
            lock (_lock)
            {
                int totalCommands = _statistics.Commands.Values.Sum(c => c.TotalExecutions);
                int totalUsers = _statistics.Users.Count;
                int totalChannels = _statistics.Channels.Count;

                CommandStats mostUsedCommand = _statistics.Commands.Values
                    .OrderByDescending(c => c.TotalExecutions)
                    .FirstOrDefault();

                UserStats mostActiveUser = _statistics.Users.Values
                    .OrderByDescending(u => u.TotalCommands)
                    .FirstOrDefault();

                // Get today's stats
                string today = DateTime.Now.ToString("yyyy-MM-dd");
                int todayCommands = _statistics.DailyStats.ContainsKey(today) 
                    ? _statistics.DailyStats[today].TotalCommands 
                    : 0;

                return new StatisticsSummary
                {
                    TotalCommands = totalCommands,
                    TotalUniqueUsers = totalUsers,
                    TotalChannels = totalChannels,
                    TotalCommandTypes = _statistics.Commands.Count,
                    MostUsedCommand = mostUsedCommand?.CommandName,
                    MostUsedCommandCount = mostUsedCommand?.TotalExecutions ?? 0,
                    MostActiveUser = mostActiveUser?.Username,
                    MostActiveUserCount = mostActiveUser?.TotalCommands ?? 0,
                    TodayCommandCount = todayCommands
                };
            }
        }
    }

    // Data classes
    public class CommandStatistics
    {
        public Dictionary<string, CommandStats> Commands { get; set; }
        public Dictionary<string, UserStats> Users { get; set; }
        public Dictionary<string, ChannelStats> Channels { get; set; }
        public Dictionary<string, DailyStats> DailyStats { get; set; }
    }

    public class CommandStats
    {
        public string CommandName { get; set; }
        public string Category { get; set; }
        public int TotalExecutions { get; set; }
        public int SuccessfulExecutions { get; set; }
        public int FailedExecutions { get; set; }
        public DateTime FirstUsed { get; set; }
        public DateTime LastUsed { get; set; }
        public Dictionary<string, int> UserExecutions { get; set; }
        public Dictionary<string, int> ChannelExecutions { get; set; }
        public Dictionary<int, int> HourlyDistribution { get; set; }
    }

    public class UserStats
    {
        public string UserId { get; set; }
        public string Username { get; set; }
        public int TotalCommands { get; set; }
        public Dictionary<string, int> CommandCounts { get; set; }
        public DateTime FirstSeen { get; set; }
        public DateTime LastSeen { get; set; }
    }

    public class ChannelStats
    {
        public string ChannelName { get; set; }
        public int TotalCommands { get; set; }
        public Dictionary<string, int> CommandCounts { get; set; }
        public HashSet<string> ActiveUsers { get; set; }
    }

    public class DailyStats
    {
        public string Date { get; set; }
        public int TotalCommands { get; set; }
        public HashSet<string> UniqueUsers { get; set; }
        public Dictionary<string, int> CommandCounts { get; set; }
    }

    public class StatisticsSummary
    {
        public int TotalCommands { get; set; }
        public int TotalUniqueUsers { get; set; }
        public int TotalChannels { get; set; }
        public int TotalCommandTypes { get; set; }
        public string MostUsedCommand { get; set; }
        public int MostUsedCommandCount { get; set; }
        public string MostActiveUser { get; set; }
        public int MostActiveUserCount { get; set; }
        public int TodayCommandCount { get; set; }
    }
}
