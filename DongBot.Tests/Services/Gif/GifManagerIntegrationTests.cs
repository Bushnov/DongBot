using DongBot;

namespace DongBot.Tests;

public class GifManagerIntegrationTests
{
    [Fact]
    public async Task GifCommandManager_ProcessCommandAsync_ReturnsGifForConfiguredCommand()
    {
        using TestWorkspace workspace = new();
        GifCommandService service = new(workspace.GetPath("gifcommands.json"));
        StatisticsTracker tracker = new(workspace.GetPath("stats.json"));
        service.AddOrUpdateCommand("DONG", "https://giphy.com/test.gif", aliases: "DINGER");
        GifCommandManager manager = new(service, statisticsTracker: tracker);
        CommandContext context = new("baseball", 123UL, false, "u1", "tester");

        string result = await manager.ProcessCommandAsync("DINGER", context);

        Assert.Equal("https://giphy.com/test.gif", result);
    }

    [Fact]
    public void GifCommandManager_GetHelp_ShowsAvailableCommandsAndAdminHint()
    {
        using TestWorkspace workspace = new();
        GifCommandService service = new(workspace.GetPath("gifcommands.json"));
        service.AddOrUpdateCommand("DONG", "https://giphy.com/test.gif", aliases: "DINGER");
        GifCommandManager manager = new(service, adminChannelName: "dongbot-admin");
        CommandContext context = new("baseball", 123UL, false, "u1", "tester");

        string help = manager.GetHelp(context);

        Assert.Contains("Available GIF Commands", help);
        Assert.Contains("!dong", help.ToLowerInvariant());
        Assert.Contains("!dinger", help.ToLowerInvariant());
        Assert.Contains("dongbot-admin", help);
    }

    [Fact]
    public async Task GifAdminCommandManager_ProcessesGifAdd_AndPersistsCommand()
    {
        using TestWorkspace workspace = new();
        GifCommandService service = new(workspace.GetPath("gifcommands.json"));
        AuditLogger auditLogger = new(workspace.GetPath("audit.json"));
        StatisticsTracker tracker = new(workspace.GetPath("stats.json"));
        GifAdminCommandManager manager = new(service, auditLogger, tracker, "dongbot-admin");
        CommandContext context = new("dongbot-admin", 1UL, true, "u1", "tester");

        string response = await manager.ProcessCommandAsync("GIF-ADD DONG https://giphy.com/test.gif", context);
        string gif = service.ProcessCommand("DONG", "baseball");

        Assert.Contains("Created new command 'DONG'", response);
        Assert.Equal("https://giphy.com/test.gif", gif);
    }

    [Fact]
    public async Task GifAdminCommandManager_RejectsNonAdminUsage()
    {
        using TestWorkspace workspace = new();
        GifCommandService service = new(workspace.GetPath("gifcommands.json"));
        AuditLogger auditLogger = new(workspace.GetPath("audit.json"));
        StatisticsTracker tracker = new(workspace.GetPath("stats.json"));
        GifAdminCommandManager manager = new(service, auditLogger, tracker, "dongbot-admin");
        CommandContext context = new("baseball", 1UL, false, "u1", "tester");

        string response = await manager.ProcessCommandAsync("GIF-ADD DONG https://giphy.com/test.gif", context);

        Assert.Contains("only be used in #dongbot-admin", response);
    }
}
