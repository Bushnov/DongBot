using DongBot;

namespace DongBot.Tests;

public class AdminReportingServiceTests
{
    [Fact]
    public void GetTopCommands_ReturnsNoDataMessage_WhenNoCommandsTracked()
    {
        using TestWorkspace workspace = new();
        using AuditLogger auditLogger = new(workspace.GetPath("audit.json"));
        using StatisticsTracker tracker = new(workspace.GetPath("stats.json"));
        AdminReportingService service = new(auditLogger, tracker);

        string result = service.GetTopCommands("STATS-TOP", "u1", "tester", "dongbot-admin");

        Assert.Contains("No command statistics found", result);
    }

    [Fact]
    public void GetUserStatistics_ReturnsNoDataMessage_WhenUserNotFound()
    {
        using TestWorkspace workspace = new();
        using AuditLogger auditLogger = new(workspace.GetPath("audit.json"));
        using StatisticsTracker tracker = new(workspace.GetPath("stats.json"));
        AdminReportingService service = new(auditLogger, tracker);

        string result = service.GetUserStatistics("STATS-USER missing", "u1", "tester", "dongbot-admin");

        Assert.Contains("No statistics found for this user", result);
    }

    [Fact]
    public void GetCommandStatistics_ReturnsUsage_WhenCommandMissing()
    {
        using TestWorkspace workspace = new();
        using AuditLogger auditLogger = new(workspace.GetPath("audit.json"));
        using StatisticsTracker tracker = new(workspace.GetPath("stats.json"));
        AdminReportingService service = new(auditLogger, tracker);

        string result = service.GetCommandStatistics("STATS-COMMAND", "u1", "tester", "dongbot-admin");

        Assert.Contains("Usage: !stats-command COMMANDNAME", result);
    }

    [Fact]
    public void GetCommandStatistics_ReturnsNoData_WhenCommandNeverTracked()
    {
        using TestWorkspace workspace = new();
        using AuditLogger auditLogger = new(workspace.GetPath("audit.json"));
        using StatisticsTracker tracker = new(workspace.GetPath("stats.json"));
        AdminReportingService service = new(auditLogger, tracker);

        string result = service.GetCommandStatistics("STATS-COMMAND NOPE", "u1", "tester", "dongbot-admin");

        Assert.Contains("No statistics found for command: NOPE", result);
    }

    [Fact]
    public void GetAuditLog_FiltersEntriesByGuild()
    {
        using TestWorkspace workspace = new();
        using AuditLogger auditLogger = new(workspace.GetPath("audit.json"));
        using StatisticsTracker tracker = new(workspace.GetPath("stats.json"));
        AdminReportingService service = new(auditLogger, tracker);

        auditLogger.Log("u1", "tester", "ADD", "GIF_COMMAND", "DONG", "Created in guild 100", "dongbot-admin", 100UL, "Guild 100", true);
        auditLogger.Log("u2", "tester2", "ADD", "GIF_COMMAND", "HR", "Created in guild 200", "dongbot-admin", 200UL, "Guild 200", true);

        string result = service.GetAuditLog("AUDIT 10", "admin", "admin", "dongbot-admin", 100UL);

        Assert.Contains("GIF_COMMAND/DONG", result);
        Assert.DoesNotContain("GIF_COMMAND/HR", result);
    }

    [Fact]
    public void GetTopCommands_FiltersCommandsByGuild()
    {
        using TestWorkspace workspace = new();
        using AuditLogger auditLogger = new(workspace.GetPath("audit.json"));
        using StatisticsTracker tracker = new(workspace.GetPath("stats.json"));
        AdminReportingService service = new(auditLogger, tracker);

        tracker.TrackCommand("DONG", "GIF", "u1", "tester", "baseball", true, 100UL, "Guild 100");
        tracker.TrackCommand("DONG", "GIF", "u1", "tester", "baseball", true, 100UL, "Guild 100");
        tracker.TrackCommand("HR", "GIF", "u2", "tester2", "baseball", true, 200UL, "Guild 200");
        tracker.TrackCommand("HR", "GIF", "u2", "tester2", "baseball", true, 200UL, "Guild 200");

        string result = service.GetTopCommands("STATS-TOP 5", "admin", "admin", "dongbot-admin", 100UL);

        Assert.Contains("DONG [GIF]", result);
        Assert.DoesNotContain("HR [GIF]", result);
    }

    [Fact]
    public void GetUserStatistics_ShowsServerPartitionBreakdown()
    {
        using TestWorkspace workspace = new();
        using AuditLogger auditLogger = new(workspace.GetPath("audit.json"));
        using StatisticsTracker tracker = new(workspace.GetPath("stats.json"));
        AdminReportingService service = new(auditLogger, tracker);

        tracker.TrackCommand("DONG", "GIF", "u1", "tester", "baseball", true, 100UL, "Guild 100");
        tracker.TrackCommand("HR", "GIF", "u1", "tester", "baseball", true, 100UL, "Guild 100");
        tracker.TrackCommand("BRAVES-SCORE", "MLB", "u1", "tester", "baseball", true, 200UL, "Guild 200");

        string result = service.GetUserStatistics("STATS-USER u1", "admin", "admin", "dongbot-admin", 100UL);

        Assert.Contains("Commands by Server", result);
        Assert.Contains("Guild 100: 2 commands", result);
        Assert.Contains("Guild 200: 1 commands", result);
    }

    [Fact]
    public void GetCommandStatistics_ShowsServerPartitionBreakdown()
    {
        using TestWorkspace workspace = new();
        using AuditLogger auditLogger = new(workspace.GetPath("audit.json"));
        using StatisticsTracker tracker = new(workspace.GetPath("stats.json"));
        AdminReportingService service = new(auditLogger, tracker);

        tracker.TrackCommand("DONG", "GIF", "u1", "tester", "baseball", true, 100UL, "Guild 100");
        tracker.TrackCommand("DONG", "GIF", "u1", "tester", "baseball", true, 100UL, "Guild 100");
        tracker.TrackCommand("DONG", "GIF", "u2", "tester2", "baseball", true, 200UL, "Guild 200");

        string result = service.GetCommandStatistics("STATS-COMMAND DONG", "admin", "admin", "dongbot-admin", 100UL);

        Assert.Contains("Executions by Server", result);
        Assert.Contains("Guild 100: 2", result);
        Assert.Contains("Guild 200: 1", result);
    }
}
