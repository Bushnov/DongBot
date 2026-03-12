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
}
