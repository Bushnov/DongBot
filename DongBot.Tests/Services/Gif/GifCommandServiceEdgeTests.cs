using DongBot;

namespace DongBot.Tests;

public class GifCommandServiceEdgeTests
{
    [Fact]
    public void AddOrUpdateCommand_RejectsEmptyInputs()
    {
        using TestWorkspace workspace = new();
        GifCommandService service = new(workspace.GetPath("gifcommands.json"));

        string emptyKey = service.AddOrUpdateCommand("", "https://giphy.com/test.gif");
        string emptyUrl = service.AddOrUpdateCommand("DONG", "");

        Assert.Contains("Command key cannot be empty", emptyKey);
        Assert.Contains("GIF URL cannot be empty", emptyUrl);
    }

    [Fact]
    public void RemoveCommand_HandlesMissingCommandAndMissingGif()
    {
        using TestWorkspace workspace = new();
        GifCommandService service = new(workspace.GetPath("gifcommands.json"));
        service.AddOrUpdateCommand("DONG", "https://giphy.com/test.gif");

        string missingCommand = service.RemoveCommand("NOPE");
        string missingGif = service.RemoveCommand("DONG", "https://giphy.com/other.gif");

        Assert.Contains("not found", missingCommand, System.StringComparison.OrdinalIgnoreCase);
        Assert.Contains("GIF not found", missingGif);
    }

    [Fact]
    public void AliasAndChannelMethods_ValidateMissingCommandsAndDuplicateCases()
    {
        using TestWorkspace workspace = new();
        GifCommandService service = new(workspace.GetPath("gifcommands.json"));
        service.AddOrUpdateCommand("DONG", "https://giphy.com/test.gif");

        string duplicateAlias = service.AddAlias("DONG", "DONG");
        string missingAliasRemove = service.RemoveAlias("DONG", "NOPE");
        string duplicateChannel = service.AddAllowedChannel("DONG", 123UL);
        duplicateChannel = service.AddAllowedChannel("DONG", 123UL);
        string missingChannelRemove = service.RemoveAllowedChannel("DONG", 999UL);

        Assert.Contains("already a primary command", duplicateAlias);
        Assert.Contains("not an alias", missingAliasRemove);
        Assert.Contains("already allowed", duplicateChannel);
        Assert.Contains("not in the allowed channels", missingChannelRemove);
    }

    [Fact]
    public void ProcessCommand_GracefullySkipsInvalidRegexPattern()
    {
        using TestWorkspace workspace = new();
        GifCommandService service = new(workspace.GetPath("gifcommands.json"));
        service.AddOrUpdateCommand("BADREGEX", "https://giphy.com/test.gif", pattern: "[", isRegex: true);

        string result = service.ProcessCommand("ANYTHING", "baseball");

        Assert.Equal(string.Empty, result);
    }

    [Fact]
    public void GetAvailableCommands_RespectsAllowedChannelRestrictions()
    {
        using TestWorkspace workspace = new();
        GifCommandService service = new(workspace.GetPath("gifcommands.json"));
        service.AddOrUpdateCommand("DONG", "https://giphy.com/test.gif");
        service.AddOrUpdateCommand("HR", "https://giphy.com/hr.gif");
        service.AddAllowedChannel("HR", 999UL);

        var regular = service.GetAvailableCommands("baseball", 1UL);
        var restricted = service.GetAvailableCommands("baseball", 999UL);

        Assert.Contains(regular, c => c.CommandKey == "DONG");
        Assert.DoesNotContain(regular, c => c.CommandKey == "HR");
        Assert.Contains(restricted, c => c.CommandKey == "HR");
    }
}
