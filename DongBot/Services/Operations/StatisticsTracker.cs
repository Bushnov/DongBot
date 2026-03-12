using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace DongBot
{
    /// <summary>
    /// Statistics tracking system for all bot commands and operations.
    /// Writes are batched in-memory and flushed to disk every 30 seconds.
    /// Call Dispose() on shutdown to flush any pending data.
    /// </summary>
    public class StatisticsTracker : IDisposable
    {
        private readonly string _statsFilePath;
        private readonly object _lock = new object();
        private CommandStatistics _statistics = new CommandStatistics();
        private readonly BackupManager _backupManager;
        private bool _isDirty = false;
        private readonly CancellationTokenSource _flushCancellation = new CancellationTokenSource();
        private readonly Task _flushTask;

        public StatisticsTracker(string statsFilePath = "bot_statistics.json")
        {
            _statsFilePath = statsFilePath;

            string backupDir = Path.Combine(Path.GetDirectoryName(_statsFilePath) ?? ".", "backups");
            _backupManager = new BackupManager(backupDir, maxBackupsToKeep: 10);

            LoadStatistics();

            // Flush dirty writes to disk every 30 seconds rather than on every TrackCommand() call
            _flushTask = RunFlushLoopAsync(_flushCancellation.Token);
        }

        private async Task RunFlushLoopAsync(CancellationToken cancellationToken)
        {
            try
            {
                using PeriodicTimer timer = new PeriodicTimer(TimeSpan.FromSeconds(30));
                while (await timer.WaitForNextTickAsync(cancellationToken))
                {
                    FlushIfDirty();
                }
            }
            catch (OperationCanceledException)
            {
                // Expected during shutdown.
            }
        }

        // Internal persistence

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

                    _backupManager.BackupOnLoad(_statsFilePath);

                    string json = File.ReadAllText(_statsFilePath);
                    _statistics = JsonSerializer.Deserialize<CommandStatistics>(json, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    }) ?? new CommandStatistics();

                    if (_statistics.Commands == null || _statistics.Users == null || _statistics.Channels == null || _statistics.DailyStats == null)
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

        private void SaveStatistics()
        {
            lock (_lock)
            {
                try
                {
                    _backupManager.BackupBeforeSave(_statsFilePath);

                    string json = JsonSerializer.Serialize(_statistics, new JsonSerializerOptions
                    {
                        WriteIndented = true,
                        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                    });
                    File.WriteAllText(_statsFilePath, json);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error saving statistics: {ex.Message}");
                }
            }
        }

        private void FlushIfDirty()
        {
            lock (_lock)
            {
                if (!_isDirty) return;
                SaveStatistics();
                _isDirty = false;
            }
        }

        /// <summary>
        /// Force-flush any pending writes and stop the flush timer.
        /// Call this on bot shutdown to avoid losing the last flush window of data.
        /// </summary>
        public void Dispose()
        {
            _flushCancellation.Cancel();
            try
            {
                _flushTask.GetAwaiter().GetResult();
            }
            catch (OperationCanceledException)
            {
                // Expected during shutdown.
            }
            _flushCancellation.Dispose();
            FlushIfDirty();
        }

        // Public API

        /// <summary>
        /// Track a command execution on a specific guild. The update is held in memory and written to disk within 30 seconds.
        /// </summary>
        public void TrackCommand(string commandName, string category, string userId,
                                string username, string channelName, bool success = true, ulong guildId = 0, string? guildName = null)
        {
            lock (_lock)
            {
                try
                {
                    DateTime now = DateTime.Now;
                    string dateKey = now.ToString("yyyy-MM-dd");

                    // Command-level statistics
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
                        cmdStats.SuccessfulExecutions++;
                    else
                        cmdStats.FailedExecutions++;
                    cmdStats.LastUsed = now;

                    if (!cmdStats.UserExecutions.ContainsKey(userId))
                        cmdStats.UserExecutions[userId] = 0;
                    cmdStats.UserExecutions[userId]++;

                    if (!string.IsNullOrEmpty(channelName))
                    {
                        if (!cmdStats.ChannelExecutions.ContainsKey(channelName))
                            cmdStats.ChannelExecutions[channelName] = 0;
                        cmdStats.ChannelExecutions[channelName]++;
                    }

                    // Track guild executions
                    if (guildId != 0)
                    {
                        if (!cmdStats.GuildExecutions.ContainsKey(guildId))
                            cmdStats.GuildExecutions[guildId] = 0;
                        cmdStats.GuildExecutions[guildId]++;
                    }

                    int hour = now.Hour;
                    if (!cmdStats.HourlyDistribution.ContainsKey(hour))
                        cmdStats.HourlyDistribution[hour] = 0;
                    cmdStats.HourlyDistribution[hour]++;

                    // User-level statistics
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
                    userStats.Username = username;
                    userStats.TotalCommands++;
                    userStats.LastSeen = now;

                    if (!userStats.CommandCounts.ContainsKey(commandName))
                        userStats.CommandCounts[commandName] = 0;
                    userStats.CommandCounts[commandName]++;

                    // Track guild command counts for partitioned display
                    if (guildId != 0)
                    {
                        if (!userStats.GuildCommandCounts.ContainsKey(guildId))
                            userStats.GuildCommandCounts[guildId] = 0;
                        userStats.GuildCommandCounts[guildId]++;
                    }

                    // Channel-level statistics
                    if (!string.IsNullOrEmpty(channelName))
                    {
                        if (!_statistics.Channels.ContainsKey(channelName))
                        {
                            _statistics.Channels[channelName] = new ChannelStats
                            {
                                ChannelName = channelName,
                                ChannelId = 0,  // Will be populated if available
                                GuildId = guildId,
                                GuildName = guildName,
                                TotalCommands = 0,
                                CommandCounts = new Dictionary<string, int>(),
                                ActiveUsers = new HashSet<string>()
                            };
                        }

                        ChannelStats channelStats = _statistics.Channels[channelName];
                        channelStats.GuildId = guildId;  // Update guild info on each track
                        channelStats.GuildName = guildName;
                        channelStats.TotalCommands++;

                        if (!channelStats.CommandCounts.ContainsKey(commandName))
                            channelStats.CommandCounts[commandName] = 0;
                        channelStats.CommandCounts[commandName]++;
                        channelStats.ActiveUsers.Add(userId);
                    }

                    // Daily statistics
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
                        dailyStats.CommandCounts[commandName] = 0;
                    dailyStats.CommandCounts[commandName]++;

                    // Mark dirty - flush timer persists to disk within 30 seconds
                    _isDirty = true;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error tracking command statistics: {ex.Message}");
                }
            }
        }

        /// <summary>Get top commands by usage, optionally filtered by guild.</summary>
        public List<CommandStats> GetTopCommands(int count = 10, string? category = null, ulong? guildId = null)
        {
            lock (_lock)
            {
                IEnumerable<CommandStats> commands = _statistics.Commands.Values
                    .OrderByDescending(c => c.TotalExecutions);

                if (!string.IsNullOrEmpty(category))
                    commands = commands.Where(c => c.Category.Equals(category, StringComparison.OrdinalIgnoreCase));

                if (guildId.HasValue && guildId.Value != 0)
                    commands = commands.Where(c => c.GuildExecutions.ContainsKey(guildId.Value));

                return commands.Take(count).ToList();
            }
        }

        /// <summary>Get statistics for a specific command.</summary>
        public CommandStats? GetCommandStats(string commandName)
        {
            lock (_lock)
            {
                return _statistics.Commands.ContainsKey(commandName)
                    ? _statistics.Commands[commandName]
                    : null;
            }
        }

        /// <summary>Get top users by command usage.</summary>
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

        /// <summary>Get statistics for a specific user.</summary>
        public UserStats? GetUserStats(string userId)
        {
            lock (_lock)
            {
                return _statistics.Users.ContainsKey(userId)
                    ? _statistics.Users[userId]
                    : null;
            }
        }

        /// <summary>Get channel statistics.</summary>
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

        /// <summary>Get daily statistics for a date range.</summary>
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

        /// <summary>Get overall statistics summary, optionally filtered by guild.</summary>
        public StatisticsSummary GetSummary(ulong? guildId = null)
        {
            lock (_lock)
            {
                IEnumerable<CommandStats> cmdValues = _statistics.Commands.Values;
                IEnumerable<UserStats> userValues = _statistics.Users.Values;
                IEnumerable<ChannelStats> channelValues = _statistics.Channels.Values;

                if (guildId.HasValue && guildId.Value != 0)
                {
                    cmdValues = cmdValues.Where(c => c.GuildExecutions.ContainsKey(guildId.Value));
                    channelValues = channelValues.Where(ch => ch.GuildId == guildId.Value);
                }

                int totalCommands = cmdValues.Sum(c => c.TotalExecutions);

                CommandStats? mostUsedCommand = cmdValues
                    .OrderByDescending(c => c.TotalExecutions)
                    .FirstOrDefault();

                UserStats? mostActiveUser = userValues
                    .OrderByDescending(u => u.TotalCommands)
                    .FirstOrDefault();

                string today = DateTime.Now.ToString("yyyy-MM-dd");
                int todayCommands = _statistics.DailyStats.ContainsKey(today)
                    ? _statistics.DailyStats[today].TotalCommands
                    : 0;

                return new StatisticsSummary
                {
                    TotalCommands = totalCommands,
                    TotalUniqueUsers = userValues.Count(),
                    TotalChannels = channelValues.Count(),
                    TotalCommandTypes = cmdValues.Count(),
                    MostUsedCommand = mostUsedCommand?.CommandName,
                    MostUsedCommandCount = mostUsedCommand?.TotalExecutions ?? 0,
                    MostActiveUser = mostActiveUser?.Username,
                    MostActiveUserCount = mostActiveUser?.TotalCommands ?? 0,
                    TodayCommandCount = todayCommands
                };
            }
        }
    }

    public class CommandStatistics
    {
        public Dictionary<string, CommandStats> Commands { get; set; } = new Dictionary<string, CommandStats>();
        public Dictionary<string, UserStats> Users { get; set; } = new Dictionary<string, UserStats>();
        public Dictionary<string, ChannelStats> Channels { get; set; } = new Dictionary<string, ChannelStats>();
        public Dictionary<string, DailyStats> DailyStats { get; set; } = new Dictionary<string, DailyStats>();
    }

    public class CommandStats
    {
        public string CommandName { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public int TotalExecutions { get; set; }
        public int SuccessfulExecutions { get; set; }
        public int FailedExecutions { get; set; }
        public DateTime FirstUsed { get; set; }
        public DateTime LastUsed { get; set; }
        public Dictionary<string, int> UserExecutions { get; set; } = new Dictionary<string, int>();
        public Dictionary<string, int> ChannelExecutions { get; set; } = new Dictionary<string, int>();
        public Dictionary<ulong, int> GuildExecutions { get; set; } = new Dictionary<ulong, int>();
        public Dictionary<int, int> HourlyDistribution { get; set; } = new Dictionary<int, int>();
    }

    public class UserStats
    {
        public string UserId { get; set; } = string.Empty;
        public string Username { get; set; } = string.Empty;
        public int TotalCommands { get; set; }
        public Dictionary<string, int> CommandCounts { get; set; } = new Dictionary<string, int>();
        public Dictionary<ulong, int> GuildCommandCounts { get; set; } = new Dictionary<ulong, int>();
        public DateTime FirstSeen { get; set; }
        public DateTime LastSeen { get; set; }
    }

    public class ChannelStats
    {
        public string ChannelName { get; set; } = string.Empty;
        public ulong ChannelId { get; set; }
        public ulong GuildId { get; set; }
        public string? GuildName { get; set; }
        public int TotalCommands { get; set; }
        public Dictionary<string, int> CommandCounts { get; set; } = new Dictionary<string, int>();
        public HashSet<string> ActiveUsers { get; set; } = new HashSet<string>();
    }

    public class DailyStats
    {
        public string Date { get; set; } = string.Empty;
        public int TotalCommands { get; set; }
        public HashSet<string> UniqueUsers { get; set; } = new HashSet<string>();
        public Dictionary<string, int> CommandCounts { get; set; } = new Dictionary<string, int>();
    }

    public class StatisticsSummary
    {
        public int TotalCommands { get; set; }
        public int TotalUniqueUsers { get; set; }
        public int TotalChannels { get; set; }
        public int TotalCommandTypes { get; set; }
        public string? MostUsedCommand { get; set; }
        public int MostUsedCommandCount { get; set; }
        public string? MostActiveUser { get; set; }
        public int MostActiveUserCount { get; set; }
        public int TodayCommandCount { get; set; }
    }
}
