using DongBot;

namespace DongBot.Tests;

public class AdminManagerIntegrationTests
{
    [Fact]
    public async Task AdminCommandManager_ReturnsStatsSummary_ForAdminChannel()
    {
        using TestWorkspace workspace = new();
        AuditLogger auditLogger = new(workspace.GetPath("audit.json"));
        StatisticsTracker tracker = new(workspace.GetPath("stats.json"));
        tracker.TrackCommand("DONG", "GIF", "u1", "tester", "baseball", true);
        AdminReportingService reportingService = new(auditLogger, tracker);
        AdminCommandManager manager = new(reportingService, "dongbot-admin");
        CommandContext context = new("dongbot-admin", 1UL, true, "u1", "tester");

        string response = await manager.ProcessCommandAsync("STATS", context);

        Assert.Contains("Bot Statistics Summary", response);
        Assert.Contains("Total Commands Executed", response);
    }

    [Fact]
    public async Task AdminCommandManager_ReturnsAuditEntries_ForAdminChannel()
    {
        using TestWorkspace workspace = new();
        AuditLogger auditLogger = new(workspace.GetPath("audit.json"));
        StatisticsTracker tracker = new(workspace.GetPath("stats.json"));
        auditLogger.Log("u1", "tester", "ADD", "GIF_COMMAND", "DONG", "Created command", "dongbot-admin", true);
        AdminReportingService reportingService = new(auditLogger, tracker);
        AdminCommandManager manager = new(reportingService, "dongbot-admin");
        CommandContext context = new("dongbot-admin", 1UL, true, "u1", "tester");

        string response = await manager.ProcessCommandAsync("AUDIT", context);

        Assert.Contains("Audit Log", response);
        Assert.Contains("GIF_COMMAND/DONG", response);
    }

    [Fact]
    public async Task AdminCommandManager_RejectsNonAdminUsage()
    {
        using TestWorkspace workspace = new();
        AuditLogger auditLogger = new(workspace.GetPath("audit.json"));
        StatisticsTracker tracker = new(workspace.GetPath("stats.json"));
        AdminReportingService reportingService = new(auditLogger, tracker);
        AdminCommandManager manager = new(reportingService, "dongbot-admin");
        CommandContext context = new("baseball", 1UL, false, "u1", "tester");

        string response = await manager.ProcessCommandAsync("STATS", context);

        Assert.Contains("only be used in #dongbot-admin", response);
    }

    [Fact]
    public async Task AdminCommandManager_BadBotList_ReturnsRecentUserReports()
    {
        using TestWorkspace workspace = new();
        using AuditLogger auditLogger = new(workspace.GetPath("audit.json"));
        using StatisticsTracker tracker = new(workspace.GetPath("stats.json"));
        using UserErrorReportLogger reportLogger = new(workspace.GetPath("user_errors.json"));
        reportLogger.LogReport("u1", "tester", "baseball", "DONG", "link broken");

        AdminReportingService reportingService = new(auditLogger, tracker);
        AdminCommandManager manager = new(reportingService, "dongbot-admin", reportLogger);
        CommandContext context = new("dongbot-admin", 1UL, true, "u1", "tester");

        string response = await manager.ProcessCommandAsync("BADBOT-LIST 5", context);

        Assert.Contains("Recent User Error Reports", response);
        Assert.Contains("tester", response);
        Assert.Contains("link broken", response);
    }
}
