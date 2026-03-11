using DongBot;

namespace DongBot.Tests;

public class MainDongMessageHandlingTests
{
    [Fact]
    public async Task HandleMessageAsync_IgnoresNonCommandsAndBotMessages()
    {
        MainDong sut = new();
        sut.SetCommandManagersForTesting(Array.Empty<ICommandManager>());
        List<string> sent = new();

        await sut.HandleMessageAsync("hello", false, "u1", "tester", "baseball", 1UL, m => { sent.Add(m); return Task.CompletedTask; });
        await sut.HandleMessageAsync("!help", true, "u1", "tester", "baseball", 1UL, m => { sent.Add(m); return Task.CompletedTask; });

        Assert.Empty(sent);
    }

    [Fact]
    public async Task HandleMessageAsync_BlocksSchedulerCommandsOutsideAdminChannel()
    {
        MainDong sut = new();
        sut.SetCommandManagersForTesting(Array.Empty<ICommandManager>());
        List<string> sent = new();

        string nonAdmin = sut.AdminChannelNameForTesting + "-other";
        await sut.HandleMessageAsync("!braves-scheduler-status", false, "u1", "tester", nonAdmin, 1UL, m => { sent.Add(m); return Task.CompletedTask; });

        Assert.Single(sent);
        Assert.Contains("only be used in", sent[0]);
    }

    [Fact]
    public async Task HandleMessageAsync_DispatchesToFirstManagerWithResponse()
    {
        MainDong sut = new();
        StubCommandManager first = new(canHandle: true, response: "");
        StubCommandManager second = new(canHandle: true, response: "ok-response");
        sut.SetCommandManagersForTesting(new ICommandManager[] { first, second });
        List<string> sent = new();

        await sut.HandleMessageAsync("!dong", false, "u1", "tester", sut.AdminChannelNameForTesting, 1UL, m => { sent.Add(m); return Task.CompletedTask; });

        Assert.Single(sent);
        Assert.Equal("ok-response", sent[0]);
        Assert.Equal(1, first.ProcessCallCount);
        Assert.Equal(1, second.ProcessCallCount);
    }

    [Fact]
    public async Task HandleMessageAsync_HelpAggregatesVisibleSections()
    {
        MainDong sut = new();
        StubCommandManager hidden = new(canHandle: false, response: "", help: "");
        StubCommandManager visible = new(canHandle: false, response: "", help: "help-section");
        sut.SetCommandManagersForTesting(new ICommandManager[] { hidden, visible });
        List<string> sent = new();

        await sut.HandleMessageAsync("!help", false, "u1", "tester", sut.AdminChannelNameForTesting, 1UL, m => { sent.Add(m); return Task.CompletedTask; });

        Assert.Single(sent);
        Assert.Contains("help-section", sent[0]);
    }

    [Fact]
    public async Task SendChunkedAsync_SplitsLongMessagesAtNewlines()
    {
        List<string> sent = new();
        string payload = string.Join("\n", Enumerable.Repeat(new string('x', 200), 15));

        await MainDong.SendChunkedAsync(
            message =>
            {
                sent.Add(message);
                return Task.CompletedTask;
            },
            payload,
            maxLength: 500);

        Assert.True(sent.Count > 1);
        Assert.All(sent, chunk => Assert.True(chunk.Length <= 500));
    }

    [Fact]
    public async Task SendChunkedAsync_SplitsSingleOverlongLine()
    {
        List<string> sent = new();
        string payload = new string('y', 1200);

        await MainDong.SendChunkedAsync(
            message =>
            {
                sent.Add(message);
                return Task.CompletedTask;
            },
            payload,
            maxLength: 500);

        Assert.Equal(3, sent.Count);
        Assert.Equal(500, sent[0].Length);
        Assert.Equal(500, sent[1].Length);
        Assert.Equal(200, sent[2].Length);
    }

    [Fact]
    public async Task HandleMessageAsync_BadBot_LogsReportWithPreviousCommandAndComment()
    {
        MainDong sut = new();
        StubCommandManager responder = new(canHandle: true, response: "ok-response");
        sut.SetCommandManagersForTesting(new ICommandManager[] { responder });
        List<string> sent = new();

        await sut.HandleMessageAsync("!dong", false, "u1", "tester", "baseball", 1UL, m => { sent.Add(m); return Task.CompletedTask; });
        await sut.HandleMessageAsync("!badbot the gif link is broken", false, "u1", "tester", "baseball", 1UL, m => { sent.Add(m); return Task.CompletedTask; });

        Assert.Equal(2, sent.Count);
        Assert.Contains("Report logged", sent[1]);
        Assert.Contains("!dong", sent[1]);
        Assert.Contains("the gif link is broken", sent[1]);
    }

    [Fact]
    public async Task HandleMessageAsync_BadBot_WithNoPreviousCommand_ReportsNoneFound()
    {
        MainDong sut = new();
        sut.SetCommandManagersForTesting(Array.Empty<ICommandManager>());
        List<string> sent = new();

        await sut.HandleMessageAsync("!badbot", false, "u1", "tester", "baseball", 1UL, m => { sent.Add(m); return Task.CompletedTask; });

        Assert.Single(sent);
        Assert.Contains("(none found)", sent[0]);
    }

    [Fact]
    public async Task HandleMessageAsync_ReleaseNotes_RejectsNonAdminChannel()
    {
        MainDong sut = new();
        sut.SetCommandManagersForTesting(Array.Empty<ICommandManager>());
        List<string> sent = new();

        await sut.HandleMessageAsync("!release-notes", false, "u1", "tester", "baseball", 1UL, m => { sent.Add(m); return Task.CompletedTask; });

        Assert.Single(sent);
        Assert.Contains("only be used in", sent[0]);
    }

    [Fact]
    public async Task HandleMessageAsync_ReleaseNotes_InvalidVersionReturnsNotFoundMessage()
    {
        MainDong sut = new();
        sut.SetCommandManagersForTesting(Array.Empty<ICommandManager>());
        List<string> sent = new();

        await sut.HandleMessageAsync("!release-notes 9.9.9", false, "u1", "tester", sut.AdminChannelNameForTesting, 1UL, m => { sent.Add(m); return Task.CompletedTask; });

        Assert.Single(sent);
        Assert.Contains("were not found", sent[0]);
    }

    [Fact]
    public void ParseReleaseNotesArgument_HandlesEmptyAndPrefixedInputs()
    {
        Assert.Null(MainDong.ParseReleaseNotesArgument("RELEASE-NOTES"));
        Assert.Equal("2.0.0", MainDong.ParseReleaseNotesArgument("RELEASE-NOTES 2.0.0"));
        Assert.Equal("2.0.0", MainDong.ParseReleaseNotesArgument("RELEASE-NOTES v2.0.0"));
        Assert.Equal("2.0.0-2.0.2", MainDong.ParseReleaseNotesArgument("RELEASE-NOTES v2.0.0-v2.0.2"));
    }

    [Fact]
    public void TryParseReleaseNotesRange_ParsesDashAndDoubleDotSyntax()
    {
        Assert.True(MainDong.TryParseReleaseNotesRange("2.0.0-2.0.2", out string? dashStart, out string? dashEnd));
        Assert.Equal("2.0.0", dashStart);
        Assert.Equal("2.0.2", dashEnd);

        Assert.True(MainDong.TryParseReleaseNotesRange("v2.0.0..v2.0.2", out string? dotStart, out string? dotEnd));
        Assert.Equal("2.0.0", dotStart);
        Assert.Equal("2.0.2", dotEnd);

        Assert.False(MainDong.TryParseReleaseNotesRange("2.0.0", out _, out _));
    }

    [Fact]
    public void TryExtractReleaseNotesSections_SelectsLatestSpecificOrRange()
    {
        string markdown = "# Notes\n\n## v2.0.2 (2026-03-13)\nC\n\n## v2.0.1 (2026-03-12)\nB\n\n## v2.0.0 (2026-03-11)\nA";

        bool latestOk = MainDong.TryExtractReleaseNotesSections(markdown, null, out string latestVersion, out string latestSection);
        bool specificOk = MainDong.TryExtractReleaseNotesSections(markdown, "2.0.1", out string specificVersion, out string specificSection);
        bool rangeOk = MainDong.TryExtractReleaseNotesSections(markdown, "2.0.0..2.0.1", out string rangeVersion, out string rangeSection);
        bool missingOk = MainDong.TryExtractReleaseNotesSections(markdown, "9.9.9", out _, out _);

        Assert.True(latestOk);
        Assert.Equal("2.0.2", latestVersion);
        Assert.Contains("C", latestSection);

        Assert.True(specificOk);
        Assert.Equal("2.0.1", specificVersion);
        Assert.Contains("B", specificSection);

        Assert.True(rangeOk);
        Assert.Equal("2.0.0-2.0.1", rangeVersion);
        Assert.Contains("A", rangeSection);
        Assert.Contains("B", rangeSection);
        Assert.DoesNotContain("C", rangeSection);

        Assert.False(missingOk);
    }

    [Fact]
    public void TryExtractReleaseNotesSections_RangeAllowsMissingIntermediateVersions()
    {
        string markdown = "# Notes\n\n## v2.0.5 (2026-03-16)\nE\n\n## v2.0.2 (2026-03-13)\nC\n\n## v2.0.1 (2026-03-12)\nB\n\n## v2.0.0 (2026-03-11)\nA";

        bool rangeOk = MainDong.TryExtractReleaseNotesSections(markdown, "2.0.0-2.0.4", out string rangeVersion, out string rangeSection);

        Assert.True(rangeOk);
        Assert.Equal("2.0.0-2.0.4", rangeVersion);
        Assert.Contains("A", rangeSection);
        Assert.Contains("B", rangeSection);
        Assert.Contains("C", rangeSection);
        Assert.DoesNotContain("E", rangeSection);
    }

    [Fact]
    public void FormatReleaseNotesForDiscord_NormalizesHeaders()
    {
        string section = "## v2.0.0 (2026-03-11)\n### Highlights\n- Item";

        string formatted = MainDong.FormatReleaseNotesForDiscord(section, "2.0.0");

        Assert.Contains("DongBot Release Notes v2.0.0", formatted);
        Assert.Contains("**v2.0.0 (2026-03-11)**", formatted);
        Assert.Contains("**Highlights**", formatted);
        Assert.Contains("- Item", formatted);
    }

    private sealed class StubCommandManager : ICommandManager
    {
        private readonly bool _canHandle;
        private readonly string _response;
        private readonly string _help;

        public int ProcessCallCount { get; private set; }

        public StubCommandManager(bool canHandle, string response, string help = "")
        {
            _canHandle = canHandle;
            _response = response;
            _help = help;
        }

        public bool CanHandle(string command) => _canHandle;

        public Task<string> ProcessCommandAsync(string command, CommandContext context)
        {
            ProcessCallCount++;
            return Task.FromResult(_response);
        }

        public string GetHelp(CommandContext context) => _help;

        public void Dispose()
        {
        }
    }
}
