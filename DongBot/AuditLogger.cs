using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace DongBot
{
    /// <summary>
    /// Generalized audit logging system for tracking all bot operations.
    /// Logs user actions, command executions, and system events to a JSON file.
    /// </summary>
    public class AuditLogger
    {
        private readonly string _auditLogPath;
        private readonly int _maxEntries;
        private readonly object _lock = new object();
        private AuditLog _auditLog;
        private readonly BackupManager _backupManager;

        public AuditLogger(string auditLogPath = "bot_audit.json", int maxEntries = 1000)
        {
            _auditLogPath = auditLogPath;
            _maxEntries = maxEntries;
            
            // Initialize backup manager with backups subdirectory
            string backupDir = Path.Combine(Path.GetDirectoryName(_auditLogPath) ?? ".", "backups");
            _backupManager = new BackupManager(backupDir, maxBackupsToKeep: 10, enableTimedBackups: false);
            
            LoadAuditLog();
        }

        /// <summary>
        /// Load audit log from file
        /// </summary>
        private void LoadAuditLog()
        {
            lock (_lock)
            {
                try
                {
                    if (!File.Exists(_auditLogPath))
                    {
                        _auditLog = new AuditLog { Entries = new List<AuditEntry>() };
                        SaveAuditLog();
                        return;
                    }

                    // Create backup before loading
                    _backupManager.BackupOnLoad(_auditLogPath);

                    string json = File.ReadAllText(_auditLogPath);
                    _auditLog = JsonSerializer.Deserialize<AuditLog>(json, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });

                    if (_auditLog == null || _auditLog.Entries == null)
                    {
                        _auditLog = new AuditLog { Entries = new List<AuditEntry>() };
                    }

                    Console.WriteLine($"Loaded {_auditLog.Entries.Count} audit log entries from {_auditLogPath}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error loading audit log: {ex.Message}");
                    _auditLog = new AuditLog { Entries = new List<AuditEntry>() };
                }
            }
        }

        /// <summary>
        /// Save audit log to file
        /// </summary>
        private void SaveAuditLog()
        {
            lock (_lock)
            {
                try
                {
                    // Create backup before saving
                    _backupManager.BackupBeforeSave(_auditLogPath);

                    JsonSerializerOptions options = new JsonSerializerOptions
                    {
                        WriteIndented = true,
                        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                    };

                    string json = JsonSerializer.Serialize(_auditLog, options);
                    File.WriteAllText(_auditLogPath, json);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error saving audit log: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// Log an action to the audit log
        /// </summary>
        /// <param name="userId">Discord user ID</param>
        /// <param name="username">Discord username</param>
        /// <param name="action">Action type (e.g., ADD, REMOVE, EXECUTE, etc.)</param>
        /// <param name="category">Category of action (e.g., GIF_COMMAND, SYSTEM, BOT_COMMAND)</param>
        /// <param name="target">Target of the action (e.g., command name, file name)</param>
        /// <param name="details">Additional details about the action</param>
        /// <param name="channelName">Optional: Channel where action occurred</param>
        /// <param name="success">Whether the action was successful</param>
        public void Log(string userId, string username, string action, string category, 
                       string target, string details, string channelName = null, bool success = true)
        {
            lock (_lock)
            {
                try
                {
                    AuditEntry entry = new AuditEntry
                    {
                        Timestamp = DateTime.Now,
                        UserId = userId ?? "SYSTEM",
                        Username = username ?? "System",
                        Action = action,
                        Category = category,
                        Target = target,
                        Details = details,
                        ChannelName = channelName,
                        Success = success
                    };

                    _auditLog.Entries.Add(entry);

                    // Trim to max entries if exceeded
                    if (_auditLog.Entries.Count > _maxEntries)
                    {
                        int toRemove = _auditLog.Entries.Count - _maxEntries;
                        _auditLog.Entries = _auditLog.Entries.Skip(toRemove).ToList();
                    }

                    SaveAuditLog();

                    // Log to console for debugging
                    Console.WriteLine($"[AUDIT] {username} ({userId}) - {action} - {category}/{target}: {details}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error logging audit entry: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// Log a system event (no user involved)
        /// </summary>
        public void LogSystem(string action, string category, string target, string details, bool success = true)
        {
            Log("SYSTEM", "System", action, category, target, details, null, success);
        }

        /// <summary>
        /// Get recent audit entries
        /// </summary>
        /// <param name="count">Number of entries to retrieve</param>
        /// <param name="category">Optional: filter by category</param>
        /// <param name="userId">Optional: filter by user ID</param>
        /// <returns>List of audit entries</returns>
        public List<AuditEntry> GetRecentEntries(int count = 50, string category = null, string userId = null)
        {
            lock (_lock)
            {
                IEnumerable<AuditEntry> entries = _auditLog.Entries.OrderByDescending(e => e.Timestamp);

                if (!string.IsNullOrWhiteSpace(category))
                {
                    entries = entries.Where(e => e.Category.Equals(category, StringComparison.OrdinalIgnoreCase));
                }

                if (!string.IsNullOrWhiteSpace(userId))
                {
                    entries = entries.Where(e => e.UserId == userId);
                }

                return entries.Take(count).ToList();
            }
        }

        /// <summary>
        /// Get entries by date range
        /// </summary>
        public List<AuditEntry> GetEntriesByDateRange(DateTime startDate, DateTime endDate, string category = null)
        {
            lock (_lock)
            {
                IEnumerable<AuditEntry> entries = _auditLog.Entries
                    .Where(e => e.Timestamp >= startDate && e.Timestamp <= endDate)
                    .OrderByDescending(e => e.Timestamp);

                if (!string.IsNullOrWhiteSpace(category))
                {
                    entries = entries.Where(e => e.Category.Equals(category, StringComparison.OrdinalIgnoreCase));
                }

                return entries.ToList();
            }
        }

        /// <summary>
        /// Get statistics about audit log
        /// </summary>
        public AuditStatistics GetStatistics()
        {
            lock (_lock)
            {
                AuditStatistics stats = new AuditStatistics
                {
                    TotalEntries = _auditLog.Entries.Count,
                    UniqueUsers = _auditLog.Entries.Select(e => e.UserId).Distinct().Count(),
                    ActionCounts = _auditLog.Entries
                        .GroupBy(e => e.Action)
                        .ToDictionary(g => g.Key, g => g.Count()),
                    CategoryCounts = _auditLog.Entries
                        .GroupBy(e => e.Category)
                        .ToDictionary(g => g.Key, g => g.Count()),
                    OldestEntry = _auditLog.Entries.OrderBy(e => e.Timestamp).FirstOrDefault()?.Timestamp,
                    NewestEntry = _auditLog.Entries.OrderByDescending(e => e.Timestamp).FirstOrDefault()?.Timestamp
                };

                return stats;
            }
        }

        /// <summary>
        /// Clear audit log (use with caution)
        /// </summary>
        public void ClearLog()
        {
            lock (_lock)
            {
                _auditLog.Entries.Clear();
                SaveAuditLog();
            }
        }
    }

    /// <summary>
    /// Represents a single audit log entry
    /// </summary>
    public class AuditEntry
    {
        public DateTime Timestamp { get; set; }
        public string UserId { get; set; }
        public string Username { get; set; }
        public string Action { get; set; }
        public string Category { get; set; }
        public string Target { get; set; }
        public string Details { get; set; }
        public string ChannelName { get; set; }
        public bool Success { get; set; }
    }

    /// <summary>
    /// Container for audit log entries
    /// </summary>
    public class AuditLog
    {
        public List<AuditEntry> Entries { get; set; }
    }

    /// <summary>
    /// Statistics about the audit log
    /// </summary>
    public class AuditStatistics
    {
        public int TotalEntries { get; set; }
        public int UniqueUsers { get; set; }
        public Dictionary<string, int> ActionCounts { get; set; }
        public Dictionary<string, int> CategoryCounts { get; set; }
        public DateTime? OldestEntry { get; set; }
        public DateTime? NewestEntry { get; set; }
    }
}
