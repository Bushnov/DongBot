using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace DongBot
{
    public class UserErrorReportLogger : IDisposable
    {
        private readonly string _reportFilePath;
        private readonly int _maxEntries;
        private readonly object _lock = new object();
        private readonly BackupManager _backupManager;
        private UserErrorReportLog _log = new UserErrorReportLog();

        public UserErrorReportLogger(string reportFilePath = "user_error_reports.json", int maxEntries = 5000)
        {
            _reportFilePath = reportFilePath;
            _maxEntries = maxEntries;

            string backupDir = Path.Combine(Path.GetDirectoryName(_reportFilePath) ?? ".", "backups");
            _backupManager = new BackupManager(backupDir, maxBackupsToKeep: 10);

            Load();
        }

        public void LogReport(string userId, string username, string channelName, string? previousCommand, string? comment)
        {
            lock (_lock)
            {
                UserErrorReportEntry entry = new UserErrorReportEntry
                {
                    Timestamp = DateTime.Now,
                    UserId = userId,
                    Username = username,
                    ChannelName = channelName,
                    PreviousCommand = previousCommand,
                    Comment = comment
                };

                _log.Entries.Add(entry);

                if (_log.Entries.Count > _maxEntries)
                {
                    int toRemove = _log.Entries.Count - _maxEntries;
                    _log.Entries = _log.Entries.Skip(toRemove).ToList();
                }

                Save();
            }
        }

        public List<UserErrorReportEntry> GetRecentReports(int count = 100)
        {
            lock (_lock)
            {
                return _log.Entries
                    .OrderByDescending(e => e.Timestamp)
                    .Take(count)
                    .ToList();
            }
        }

        public void Dispose()
        {
            _backupManager?.Dispose();
        }

        private void Load()
        {
            lock (_lock)
            {
                try
                {
                    if (!File.Exists(_reportFilePath))
                    {
                        _log = new UserErrorReportLog();
                        Save();
                        return;
                    }

                    _backupManager.BackupOnLoad(_reportFilePath);

                    string json = File.ReadAllText(_reportFilePath);
                    _log = JsonSerializer.Deserialize<UserErrorReportLog>(json, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    }) ?? new UserErrorReportLog();

                    if (_log.Entries == null)
                    {
                        _log = new UserErrorReportLog();
                    }
                }
                catch
                {
                    _log = new UserErrorReportLog();
                }
            }
        }

        private void Save()
        {
            lock (_lock)
            {
                try
                {
                    _backupManager.BackupBeforeSave(_reportFilePath);

                    string json = JsonSerializer.Serialize(_log, new JsonSerializerOptions
                    {
                        WriteIndented = true,
                        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                    });
                    File.WriteAllText(_reportFilePath, json);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error saving user error reports: {ex.Message}");
                }
            }
        }
    }

    public class UserErrorReportLog
    {
        public List<UserErrorReportEntry> Entries { get; set; } = new List<UserErrorReportEntry>();
    }

    public class UserErrorReportEntry
    {
        public DateTime Timestamp { get; set; }
        public string UserId { get; set; } = string.Empty;
        public string Username { get; set; } = string.Empty;
        public string ChannelName { get; set; } = string.Empty;
        public string? PreviousCommand { get; set; }
        public string? Comment { get; set; }
    }
}
