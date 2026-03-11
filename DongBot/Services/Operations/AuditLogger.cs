using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace DongBot
{
    /// <summary>
    /// Audit logging system for tracking all bot operations.
    /// Writes are batched in-memory and flushed to disk every 30 seconds.
    /// Call Dispose() on shutdown to flush any pending data.
    /// </summary>
    public class AuditLogger : IDisposable
    {
        private readonly string _auditLogPath;
        private readonly int _maxEntries;
        private readonly bool _verboseConsoleLogging;
        private readonly object _lock = new object();
        private AuditLog _auditLog = new AuditLog();
        private readonly BackupManager _backupManager;
        private bool _isDirty = false;
        private readonly CancellationTokenSource _flushCancellation = new CancellationTokenSource();
        private readonly Task _flushTask;

        public AuditLogger(string auditLogPath = "bot_audit.json", int maxEntries = 1000, bool verboseConsoleLogging = false)
        {
            _auditLogPath = auditLogPath;
            _maxEntries = maxEntries;
            _verboseConsoleLogging = verboseConsoleLogging;

            string backupDir = Path.Combine(Path.GetDirectoryName(_auditLogPath) ?? ".", "backups");
            _backupManager = new BackupManager(backupDir, maxBackupsToKeep: 10);

            LoadAuditLog();

            // Flush dirty writes to disk every 30 seconds rather than on every Log() call
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

                    _backupManager.BackupOnLoad(_auditLogPath);

                    string json = File.ReadAllText(_auditLogPath);
                    _auditLog = JsonSerializer.Deserialize<AuditLog>(json, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    }) ?? new AuditLog();

                    if (_auditLog == null || _auditLog.Entries == null)
                        _auditLog = new AuditLog { Entries = new List<AuditEntry>() };

                    Console.WriteLine($"Loaded {_auditLog.Entries.Count} audit log entries from {_auditLogPath}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error loading audit log: {ex.Message}");
                    _auditLog = new AuditLog { Entries = new List<AuditEntry>() };
                }
            }
        }

        private void SaveAuditLog()
        {
            lock (_lock)
            {
                try
                {
                    _backupManager.BackupBeforeSave(_auditLogPath);

                    string json = JsonSerializer.Serialize(_auditLog, new JsonSerializerOptions
                    {
                        WriteIndented = true,
                        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                    });
                    File.WriteAllText(_auditLogPath, json);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error saving audit log: {ex.Message}");
                }
            }
        }

        private void FlushIfDirty()
        {
            lock (_lock)
            {
                if (!_isDirty) return;
                SaveAuditLog();
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
        /// Log a user action. The entry is held in memory and written to disk within 30 seconds.
        /// </summary>
        public void Log(string userId, string username, string action, string category,
                       string target, string details, string? channelName = null, bool success = true)
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

                    // Mark dirty - flush timer persists to disk within 30 seconds
                    _isDirty = true;

                    if (_verboseConsoleLogging)
                    {
                        Console.WriteLine($"[AUDIT] {username} ({userId}) - {action} - {category}/{target}: {details}");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error logging audit entry: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// Log a system event (no user involved).
        /// </summary>
        public void LogSystem(string action, string category, string target, string details, bool success = true)
        {
            Log("SYSTEM", "System", action, category, target, details, null, success);
        }

        /// <summary>
        /// Get recent audit entries, optionally filtered by category and/or user.
        /// </summary>
        public List<AuditEntry> GetRecentEntries(int count = 50, string? category = null, string? userId = null)
        {
            lock (_lock)
            {
                IEnumerable<AuditEntry> entries = _auditLog.Entries.OrderByDescending(e => e.Timestamp);

                if (!string.IsNullOrWhiteSpace(category))
                    entries = entries.Where(e => e.Category.Equals(category, StringComparison.OrdinalIgnoreCase));

                if (!string.IsNullOrWhiteSpace(userId))
                    entries = entries.Where(e => e.UserId == userId);

                return entries.Take(count).ToList();
            }
        }

        /// <summary>
        /// Get entries within a date range, optionally filtered by category.
        /// </summary>
        public List<AuditEntry> GetEntriesByDateRange(DateTime startDate, DateTime endDate, string? category = null)
        {
            lock (_lock)
            {
                IEnumerable<AuditEntry> entries = _auditLog.Entries
                    .Where(e => e.Timestamp >= startDate && e.Timestamp <= endDate)
                    .OrderByDescending(e => e.Timestamp);

                if (!string.IsNullOrWhiteSpace(category))
                    entries = entries.Where(e => e.Category.Equals(category, StringComparison.OrdinalIgnoreCase));

                return entries.ToList();
            }
        }

        /// <summary>
        /// Get aggregate statistics about the audit log.
        /// </summary>
        public AuditStatistics GetStatistics()
        {
            lock (_lock)
            {
                return new AuditStatistics
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
            }
        }

        /// <summary>
        /// Clear all audit log entries and immediately persist the empty log.
        /// </summary>
        public void ClearLog()
        {
            lock (_lock)
            {
                _auditLog.Entries.Clear();
                SaveAuditLog();
                _isDirty = false;
            }
        }
    }

    public class AuditEntry
    {
        public DateTime Timestamp { get; set; }
        public string UserId { get; set; } = string.Empty;
        public string Username { get; set; } = string.Empty;
        public string Action { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public string Target { get; set; } = string.Empty;
        public string Details { get; set; } = string.Empty;
        public string? ChannelName { get; set; }
        public bool Success { get; set; }
    }

    public class AuditLog
    {
        public List<AuditEntry> Entries { get; set; } = new List<AuditEntry>();
    }

    public class AuditStatistics
    {
        public int TotalEntries { get; set; }
        public int UniqueUsers { get; set; }
        public Dictionary<string, int> ActionCounts { get; set; } = new Dictionary<string, int>();
        public Dictionary<string, int> CategoryCounts { get; set; } = new Dictionary<string, int>();
        public DateTime? OldestEntry { get; set; }
        public DateTime? NewestEntry { get; set; }
    }
}
