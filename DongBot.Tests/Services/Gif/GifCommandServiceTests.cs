using DongBot;

namespace DongBot.Tests;

public class GifCommandServiceTests
{
    [Fact]
    public void AddOrUpdateCommand_CreatesCommand_AndProcessCommandReturnsGif()
    {
        using TestWorkspace workspace = new();
        GifCommandService service = new(workspace.GetPath("gifcommands.json"));

        string result = service.AddOrUpdateCommand("DONG", "https://giphy.com/test.gif");
        string gif = service.ProcessCommand("DONG", "baseball");

        Assert.Contains("Created new command 'DONG'", result);
        Assert.Equal("https://giphy.com/test.gif", gif);
    }

    [Fact]
    public void AddAlias_AllowsAliasToResolvePrimaryCommand()
    {
        using TestWorkspace workspace = new();
        GifCommandService service = new(workspace.GetPath("gifcommands.json"));
        service.AddOrUpdateCommand("DONG", "https://giphy.com/test.gif");

        string aliasResult = service.AddAlias("DONG", "DINGER");
        string gif = service.ProcessCommand("DINGER", "baseball");

        Assert.Contains("Added alias 'DINGER'", aliasResult);
        Assert.Equal("https://giphy.com/test.gif", gif);
    }

    [Fact]
    public void ChannelRestrictions_BlockCommandsOutsideAllowedChannels()
    {
        using TestWorkspace workspace = new();
        GifCommandService service = new(workspace.GetPath("gifcommands.json"));
        service.AddOrUpdateCommand("DONG", "https://giphy.com/test.gif");
        service.AddAllowedChannel("DONG", 123UL);

        string allowed = service.ProcessCommand("DONG", "baseball", 123UL);
        string blocked = service.ProcessCommand("DONG", "baseball", 456UL);

        Assert.Equal("https://giphy.com/test.gif", allowed);
        Assert.Equal(string.Empty, blocked);
    }

    [Fact]
    public void RemoveCommand_RemovesCommandFromLookup()
    {
        using TestWorkspace workspace = new();
        GifCommandService service = new(workspace.GetPath("gifcommands.json"));
        service.AddOrUpdateCommand("DONG", "https://giphy.com/test.gif");

        string removeResult = service.RemoveCommand("DONG");
        string gif = service.ProcessCommand("DONG", "baseball");

        Assert.Contains("Removed command 'DONG' entirely.", removeResult);
        Assert.Equal(string.Empty, gif);
    }
}
