using DongBot;

namespace DongBot.Tests;

public class AdminAndGifAdminCoverageTests
{
    [Fact]
    public async Task AdminCommandManager_HandlesAuditStatsTopUserCommandAndUnknown()
    {
        using TestWorkspace workspace = new();
        using AuditLogger auditLogger = new(workspace.GetPath("audit.json"));
        using StatisticsTracker tracker = new(workspace.GetPath("stats.json"));
        tracker.TrackCommand("DONG", "GIF", "u1", "tester", "baseball", true);
        AdminReportingService reportingService = new(auditLogger, tracker);
        AdminCommandManager manager = new(reportingService, "dongbot-admin");
        CommandContext context = new("dongbot-admin", 1UL, true, "u1", "tester");

        string auditStats = await manager.ProcessCommandAsync("AUDIT-STATS", context);
        string top = await manager.ProcessCommandAsync("STATS-TOP 5", context);
        string user = await manager.ProcessCommandAsync("STATS-USER u1", context);
        string command = await manager.ProcessCommandAsync("STATS-COMMAND DONG", context);
        string badBotList = await manager.ProcessCommandAsync("BADBOT-LIST", context);
        string unknown = await manager.ProcessCommandAsync("STATS-UNKNOWN", context);

        Assert.Contains("Audit Log Statistics", auditStats);
        Assert.Contains("Top", top);
        Assert.Contains("User Statistics", user);
        Assert.Contains("Command Statistics", command);
        Assert.Contains("User error reporting is not configured", badBotList);
        Assert.Equal(string.Empty, unknown);
    }

    [Fact]
    public void AdminCommandManager_GetHelp_OnlyInAdminChannel()
    {
        using TestWorkspace workspace = new();
        using AuditLogger auditLogger = new(workspace.GetPath("audit.json"));
        using StatisticsTracker tracker = new(workspace.GetPath("stats.json"));
        AdminCommandManager manager = new(new AdminReportingService(auditLogger, tracker), "dongbot-admin");

        string adminHelp = manager.GetHelp(new CommandContext("dongbot-admin", 1UL, true, "u1", "tester"));
        string userHelp = manager.GetHelp(new CommandContext("baseball", 2UL, false, "u2", "viewer"));

        Assert.Contains("Audit & Statistics Commands", adminHelp);
        Assert.Contains("!badbot-list", adminHelp, System.StringComparison.OrdinalIgnoreCase);
        Assert.Equal(string.Empty, userHelp);
    }

    [Fact]
    public async Task GifAdminCommandManager_ExercisesAliasChannelListRefreshAndRemoveBranches()
    {
        using TestWorkspace workspace = new();
        GifCommandService service = new(workspace.GetPath("gifcommands.json"));
        using AuditLogger auditLogger = new(workspace.GetPath("audit.json"));
        using StatisticsTracker tracker = new(workspace.GetPath("stats.json"));
        GifAdminCommandManager manager = new(service, auditLogger, tracker, "dongbot-admin");
        CommandContext context = new("dongbot-admin", 1UL, true, "u1", "tester");

        await manager.ProcessCommandAsync("GIF-ADD DONG https://giphy.com/test.gif", context);

        string addAlias = await manager.ProcessCommandAsync("GIF-ALIAS DONG add DINGER", context);
        string listChannelsInitial = await manager.ProcessCommandAsync("GIF-CHANNEL DONG list", context);
        string addChannel = await manager.ProcessCommandAsync("GIF-CHANNEL DONG add 123456", context);
        string listChannelsAfterAdd = await manager.ProcessCommandAsync("GIF-CHANNEL DONG list", context);
        string removeChannel = await manager.ProcessCommandAsync("GIF-CHANNEL DONG remove 123456", context);
        string clearChannels = await manager.ProcessCommandAsync("GIF-CHANNEL DONG clear", context);
        string list = await manager.ProcessCommandAsync("GIF-LIST DONG", context);
        string refresh = await manager.ProcessCommandAsync("GIF-REFRESH", context);
        string removeAlias = await manager.ProcessCommandAsync("GIF-ALIAS DONG remove DINGER", context);
        string remove = await manager.ProcessCommandAsync("GIF-REMOVE DONG https://giphy.com/test.gif", context);

        Assert.Contains("Added alias", addAlias);
        Assert.Contains("no channel restrictions", listChannelsInitial, System.StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Added channel", addChannel);
        Assert.Contains("Allowed channels", listChannelsAfterAdd);
        Assert.Contains("Removed channel", removeChannel);
        Assert.True(
            clearChannels.Contains("Cleared") ||
            clearChannels.Contains("already has no channel restrictions", System.StringComparison.OrdinalIgnoreCase));
        Assert.Contains("Command 'DONG'", list);
        Assert.Contains("refreshed", refresh, System.StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Removed alias", removeAlias);
        Assert.Contains("Removed last GIF", remove);
    }

    [Fact]
    public void GifAdminCommandManager_GetHelp_OnlyInAdminChannel()
    {
        using TestWorkspace workspace = new();
        GifCommandService service = new(workspace.GetPath("gifcommands.json"));
        using AuditLogger auditLogger = new(workspace.GetPath("audit.json"));
        using StatisticsTracker tracker = new(workspace.GetPath("stats.json"));
        GifAdminCommandManager manager = new(service, auditLogger, tracker, "dongbot-admin");

        string adminHelp = manager.GetHelp(new CommandContext("dongbot-admin", 1UL, true, "u1", "tester"));
        string userHelp = manager.GetHelp(new CommandContext("baseball", 2UL, false, "u2", "viewer"));

        Assert.Contains("GIF Administrative Commands", adminHelp);
        Assert.Equal(string.Empty, userHelp);
    }
}
