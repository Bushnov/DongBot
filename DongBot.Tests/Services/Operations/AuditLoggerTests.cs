using DongBot;

namespace DongBot.Tests;

public class AuditLoggerTests
{
    [Fact]
    public void Log_AndGetRecentEntries_ReturnsNewestEntriesFirst()
    {
        using TestWorkspace workspace = new();
        using AuditLogger logger = new(workspace.GetPath("audit.json"));

        logger.Log("u1", "tester", "ADD", "GIF_COMMAND", "DONG", "Created", "admin", true);
        logger.Log("u2", "tester2", "REMOVE", "GIF_COMMAND", "DONG", "Removed", "admin", false);

        List<AuditEntry> entries = logger.GetRecentEntries(10);

        Assert.Equal(2, entries.Count);
        Assert.Equal("REMOVE", entries[0].Action);
        Assert.Equal("ADD", entries[1].Action);
    }

    [Fact]
    public void GetStatistics_ReturnsAggregatedCounts()
    {
        using TestWorkspace workspace = new();
        using AuditLogger logger = new(workspace.GetPath("audit.json"));

        logger.Log("u1", "tester", "ADD", "GIF_COMMAND", "DONG", "Created", "admin", true);
        logger.Log("u2", "tester2", "ADD", "GIF_COMMAND", "HR", "Created", "admin", true);
        logger.Log("u1", "tester", "REMOVE", "GIF_COMMAND", "DONG", "Removed", "admin", false);

        AuditStatistics stats = logger.GetStatistics();

        Assert.Equal(3, stats.TotalEntries);
        Assert.Equal(2, stats.UniqueUsers);
        Assert.Equal(2, stats.ActionCounts["ADD"]);
        Assert.Equal(3, stats.CategoryCounts["GIF_COMMAND"]);
    }

    [Fact]
    public void ClearLog_RemovesAllEntries()
    {
        using TestWorkspace workspace = new();
        using AuditLogger logger = new(workspace.GetPath("audit.json"));
        logger.Log("u1", "tester", "ADD", "GIF_COMMAND", "DONG", "Created", "admin", true);

        logger.ClearLog();

        Assert.Empty(logger.GetRecentEntries(10));
    }
}
