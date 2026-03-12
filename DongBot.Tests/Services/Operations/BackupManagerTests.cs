using DongBot;
using System.IO;

namespace DongBot.Tests;

public class BackupManagerTests
{
    [Fact]
    public void CreateBackup_CreatesBackupFile_WhenSourceExists()
    {
        using TestWorkspace workspace = new();
        string sourceFile = workspace.GetPath("data.json");
        File.WriteAllText(sourceFile, "{}");

        using BackupManager manager = new(workspace.GetPath("backups"));

        string? backupPath = manager.CreateBackup(sourceFile, "manual");

        Assert.NotNull(backupPath);
        Assert.True(File.Exists(backupPath));
    }

    [Fact]
    public void BackupBeforeSave_RateLimitsRepeatedSaveBackups()
    {
        using TestWorkspace workspace = new();
        string sourceFile = workspace.GetPath("data.json");
        File.WriteAllText(sourceFile, "{}");

        using BackupManager manager = new(workspace.GetPath("backups"), minSaveBackupInterval: TimeSpan.FromHours(1));

        string? first = manager.BackupBeforeSave(sourceFile);
        string? second = manager.BackupBeforeSave(sourceFile);

        Assert.NotNull(first);
        Assert.Null(second);
    }

    [Fact]
    public void RestoreFromBackup_ReplacesTargetContents()
    {
        using TestWorkspace workspace = new();
        string sourceFile = workspace.GetPath("data.json");
        string targetFile = workspace.GetPath("target.json");
        File.WriteAllText(sourceFile, "{\"value\":1}");
        File.WriteAllText(targetFile, "{\"value\":0}");

        using BackupManager manager = new(workspace.GetPath("backups"));
        string? backupPath = manager.CreateBackup(sourceFile, "manual");

        bool restored = manager.RestoreFromBackup(backupPath!, targetFile, createSafetyBackup: false);

        Assert.True(restored);
        Assert.Equal("{\"value\":1}", File.ReadAllText(targetFile));
    }
}
