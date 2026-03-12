#nullable enable

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace DongBot
{
    /// <summary>
    /// Manages backups of JSON data files with configurable retention and automatic backup strategies
    /// </summary>
    public class BackupManager : IDisposable
    {
        private readonly string _backupDirectory;
        private readonly int _maxBackupsToKeep;
        private readonly TimeSpan _minSaveBackupInterval;
        private readonly object _backupLock = new object();
        private readonly Dictionary<string, DateTime> _lastSaveBackupByFile = new Dictionary<string, DateTime>(StringComparer.OrdinalIgnoreCase);
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
            }

            _disposed = true;
        }
        /// <summary>
        /// Initializes a new BackupManager
        /// </summary>
        /// <param name="backupDirectory">Directory where backups will be stored</param>
        /// <param name="maxBackupsToKeep">Maximum number of backups to retain per file (default: 10)</param>
        /// <param name="minSaveBackupInterval">Minimum interval between save-triggered backups for the same file (default: 30 minutes)</param>
        public BackupManager(
            string backupDirectory,
            int maxBackupsToKeep = 10,
            TimeSpan? minSaveBackupInterval = null)
        {
            _backupDirectory = backupDirectory;
            _maxBackupsToKeep = maxBackupsToKeep;
            _minSaveBackupInterval = minSaveBackupInterval ?? TimeSpan.FromMinutes(30);

            // Create backup directory if it doesn't exist
            if (!Directory.Exists(_backupDirectory))
            {
                Directory.CreateDirectory(_backupDirectory);
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

                    // Rate-limit high-frequency save backups per file
                    if (reason.Equals("save", StringComparison.OrdinalIgnoreCase))
                    {
                        string normalizedPath = Path.GetFullPath(sourceFilePath);
                        DateTime now = DateTime.UtcNow;
                        if (_lastSaveBackupByFile.TryGetValue(normalizedPath, out DateTime lastBackupTime))
                        {
                            TimeSpan elapsed = now - lastBackupTime;
                            if (elapsed < _minSaveBackupInterval)
                            {
                                return null;
                            }
                        }
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

                    if (reason.Equals("save", StringComparison.OrdinalIgnoreCase))
                    {
                        string normalizedPath = Path.GetFullPath(sourceFilePath);
                        _lastSaveBackupByFile[normalizedPath] = DateTime.UtcNow;
                    }

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

    }
}
