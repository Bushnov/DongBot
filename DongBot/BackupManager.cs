#nullable enable

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace DongBot
{
    /// <summary>
    /// Manages backups of JSON data files with configurable retention and automatic backup strategies
    /// </summary>
    public class BackupManager : IDisposable
    {
        private readonly string _backupDirectory;
        private readonly int _maxBackupsToKeep;
        private readonly bool _enableTimedBackups;
        private Timer? _backupTimer;
        private readonly object _backupLock = new object();
        private bool _disposed;

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (_disposed)
            {
                return;
            }

            if (disposing)
            {
                // Dispose managed resources
                if (_backupTimer != null)
                {
                    _backupTimer.Dispose();
                    _backupTimer = null;
                }
            }

            _disposed = true;
        }
        /// <summary>
        /// Initializes a new BackupManager
        /// </summary>
        /// <param name="backupDirectory">Directory where backups will be stored</param>
        /// <param name="maxBackupsToKeep">Maximum number of backups to retain per file (default: 10)</param>
        /// <param name="enableTimedBackups">Enable automatic timed backups (default: false)</param>
        public BackupManager(string backupDirectory, int maxBackupsToKeep = 10, bool enableTimedBackups = false)
        {
            _backupDirectory = backupDirectory;
            _maxBackupsToKeep = maxBackupsToKeep;
            _enableTimedBackups = enableTimedBackups;

            // Create backup directory if it doesn't exist
            if (!Directory.Exists(_backupDirectory))
            {
                Directory.CreateDirectory(_backupDirectory);
            }

            // Initialize timed backup framework (disabled by default)
            if (_enableTimedBackups)
            {
                InitializeTimedBackups();
            }
        }

        /// <summary>
        /// Creates a backup of the specified file
        /// </summary>
        /// <param name="sourceFilePath">Path to the file to backup</param>
        /// <param name="reason">Reason for the backup (e.g., "load", "save", "auto")</param>
        /// <returns>Path to the created backup file, or null if backup failed</returns>
        public string? CreateBackup(string sourceFilePath, string reason = "manual")
        {
            lock (_backupLock)
            {
                try
                {
                    // Check if source file exists
                    if (!File.Exists(sourceFilePath))
                    {
                        return null;
                    }

                    // Generate backup filename with timestamp
                    string fileName = Path.GetFileNameWithoutExtension(sourceFilePath);
                    string extension = Path.GetExtension(sourceFilePath);
                    string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                    string backupFileName = $"{fileName}_{reason}_{timestamp}{extension}";
                    string backupFilePath = Path.Combine(_backupDirectory, backupFileName);

                    // Copy file to backup location
                    File.Copy(sourceFilePath, backupFilePath, overwrite: false);

                    // Clean up old backups
                    CleanupOldBackups(fileName, extension);

                    return backupFilePath;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[BackupManager] Failed to create backup of {sourceFilePath}: {ex.Message}");
                    return null;
                }
            }
        }

        /// <summary>
        /// Creates a backup before loading data (if file exists)
        /// </summary>
        /// <param name="filePath">Path to the file being loaded</param>
        /// <returns>Path to the backup file, or null if no backup was created</returns>
        public string? BackupOnLoad(string filePath)
        {
            return CreateBackup(filePath, "load");
        }

        /// <summary>
        /// Creates a backup before saving data
        /// </summary>
        /// <param name="filePath">Path to the file being saved</param>
        /// <returns>Path to the backup file, or null if no backup was created</returns>
        public string? BackupBeforeSave(string filePath)
        {
            return CreateBackup(filePath, "save");
        }

        /// <summary>
        /// Removes old backups, keeping only the most recent ones
        /// </summary>
        /// <param name="baseFileName">Base name of the file (without extension)</param>
        /// <param name="extension">File extension</param>
        private void CleanupOldBackups(string baseFileName, string extension)
        {
            try
            {
                // Get all backup files for this base name
                List<FileInfo> backupFiles = Directory.GetFiles(_backupDirectory, $"{baseFileName}_*{extension}")
                    .Select(f => new FileInfo(f))
                    .OrderByDescending(f => f.CreationTime)
                    .ToList();

                // Delete oldest backups if we exceed the max
                if (backupFiles.Count > _maxBackupsToKeep)
                {
                    IEnumerable<FileInfo> filesToDelete = backupFiles.Skip(_maxBackupsToKeep);
                    foreach (FileInfo file in filesToDelete)
                    {
                        try
                        {
                            file.Delete();
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"[BackupManager] Failed to delete old backup {file.Name}: {ex.Message}");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[BackupManager] Failed to cleanup old backups: {ex.Message}");
            }
        }

        /// <summary>
        /// Gets a list of all available backups for a specific file
        /// </summary>
        /// <param name="sourceFilePath">Path to the original file</param>
        /// <returns>List of backup file paths, ordered by creation time (newest first)</returns>
        public List<string> GetAvailableBackups(string sourceFilePath)
        {
            try
            {
                string fileName = Path.GetFileNameWithoutExtension(sourceFilePath);
                string extension = Path.GetExtension(sourceFilePath);

                return Directory.GetFiles(_backupDirectory, $"{fileName}_*{extension}")
                    .Select(f => new FileInfo(f))
                    .OrderByDescending(f => f.CreationTime)
                    .Select(f => f.FullName)
                    .ToList();
            }
            catch
            {
                return new List<string>();
            }
        }

        /// <summary>
        /// Restores a file from a backup
        /// </summary>
        /// <param name="backupFilePath">Path to the backup file</param>
        /// <param name="targetFilePath">Path where the backup should be restored</param>
        /// <param name="createSafetyBackup">If true, creates a backup of the current file before restoring</param>
        /// <returns>True if restore was successful</returns>
        public bool RestoreFromBackup(string backupFilePath, string targetFilePath, bool createSafetyBackup = true)
        {
            lock (_backupLock)
            {
                try
                {
                    if (!File.Exists(backupFilePath))
                    {
                        return false;
                    }

                    // Create a safety backup of the current file before restoring
                    if (createSafetyBackup && File.Exists(targetFilePath))
                    {
                        CreateBackup(targetFilePath, "pre-restore");
                    }

                    // Restore the backup
                    File.Copy(backupFilePath, targetFilePath, overwrite: true);
                    return true;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[BackupManager] Failed to restore from backup: {ex.Message}");
                    return false;
                }
            }
        }

        /// <summary>
        /// Initializes the timed backup system (framework for future use)
        /// </summary>
        private void InitializeTimedBackups()
        {
            // Framework for timed backups - can be enabled in the future
            // Default: backup every 6 hours
            TimeSpan backupInterval = TimeSpan.FromHours(6);
            
            _backupTimer = new Timer(
                callback: TimedBackupCallback,
                state: null,
                dueTime: backupInterval,
                period: backupInterval
            );
        }

        /// <summary>
        /// Callback for timed backups (framework for future use)
        /// </summary>
        private void TimedBackupCallback(object? state)
        {
            // This method would be called periodically when timed backups are enabled
            // Implementation would backup all tracked files
            // For now, this is just a framework placeholder
            Console.WriteLine("[BackupManager] Timed backup triggered (framework - not implemented)");
        }

        /// <summary>
        /// Registers a file for automatic timed backups (framework for future use)
        /// </summary>
        /// <param name="filePath">Path to the file to backup automatically</param>
        public void RegisterFileForTimedBackups(string filePath)
        {
            // Framework for future use - would track files to backup automatically
            // Not implemented as timed backups are disabled
        }

    }
}
