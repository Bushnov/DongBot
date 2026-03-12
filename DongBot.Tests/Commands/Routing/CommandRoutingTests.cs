using DongBot;

namespace DongBot.Tests;

public class CommandRoutingTests
{
    [Theory]
    [InlineData("AUDIT", true)]
    [InlineData("AUDIT-STATS", true)]
    [InlineData("BADBOT-LIST", true)]
    [InlineData("STATS", true)]
    [InlineData("STATS-TOP", true)]
    [InlineData("HELLO", false)]
    public void AdminCommandManager_CanHandle_MatchesExpectedCommands(string command, bool expected)
    {
        using TestWorkspace workspace = new();
        using AuditLogger logger = new(workspace.GetPath("audit.json"));
        using StatisticsTracker tracker = new(workspace.GetPath("stats.json"));
        AdminCommandManager manager = new(new AdminReportingService(logger, tracker), "dongbot-admin");

        Assert.Equal(expected, manager.CanHandle(command));
    }

    [Theory]
    [InlineData("GIF-ADD", true)]
    [InlineData("GIF-CHANNEL", true)]
    [InlineData("GIF-VALIDATE", true)]
    [InlineData("DONG", false)]
    public void GifAdminCommandManager_CanHandle_MatchesExpectedCommands(string command, bool expected)
    {
        using TestWorkspace workspace = new();
        GifCommandService service = new(workspace.GetPath("gifcommands.json"));
        using AuditLogger logger = new(workspace.GetPath("audit.json"));
        using StatisticsTracker tracker = new(workspace.GetPath("stats.json"));
        GifAdminCommandManager manager = new(service, logger, tracker, "dongbot-admin");

        Assert.Equal(expected, manager.CanHandle(command));
    }

    [Theory]
    [InlineData("DONG", true)]
    [InlineData("GIF-ADD", false)]
    [InlineData("MLB-SCORES", false)]
    [InlineData("HELP", false)]
    public void GifCommandManager_CanHandle_ExcludesReservedPrefixes(string command, bool expected)
    {
        using TestWorkspace workspace = new();
        GifCommandService service = new(workspace.GetPath("gifcommands.json"));
        using StatisticsTracker tracker = new(workspace.GetPath("stats.json"));
        GifCommandManager manager = new(service, statisticsTracker: tracker);

        Assert.Equal(expected, manager.CanHandle(command));
    }
}
