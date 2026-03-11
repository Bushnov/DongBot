using DongBot;

namespace DongBot.Tests;

public class StatisticsTrackerTests
{
    [Fact]
    public void TrackCommand_PopulatesSummaryAndLookups()
    {
        using TestWorkspace workspace = new();
        using StatisticsTracker tracker = new(workspace.GetPath("stats.json"));

        tracker.TrackCommand("DONG", "GIF", "u1", "tester", "baseball", true);
        tracker.TrackCommand("DONG", "GIF", "u1", "tester", "baseball", false);
        tracker.TrackCommand("STATS", "ADMIN", "u2", "admin", "dongbot-admin", true);

        StatisticsSummary summary = tracker.GetSummary();
        CommandStats? commandStats = tracker.GetCommandStats("DONG");
        UserStats? userStats = tracker.GetUserStats("u1");

        Assert.Equal(3, summary.TotalCommands);
        Assert.Equal("DONG", summary.MostUsedCommand);
        Assert.NotNull(commandStats);
        Assert.Equal(2, commandStats!.TotalExecutions);
        Assert.Equal(1, commandStats.FailedExecutions);
        Assert.NotNull(userStats);
        Assert.Equal(2, userStats!.TotalCommands);
    }

    [Fact]
    public void GetTopCommands_FiltersByCategory()
    {
        using TestWorkspace workspace = new();
        using StatisticsTracker tracker = new(workspace.GetPath("stats.json"));

        tracker.TrackCommand("DONG", "GIF", "u1", "tester", "baseball", true);
        tracker.TrackCommand("HR", "GIF", "u1", "tester", "baseball", true);
        tracker.TrackCommand("STATS", "ADMIN", "u2", "admin", "dongbot-admin", true);

        List<CommandStats> results = tracker.GetTopCommands(10, "GIF");

        Assert.Equal(2, results.Count);
        Assert.All(results, stat => Assert.Equal("GIF", stat.Category));
    }

    [Fact]
    public void GetDailyStats_ReturnsTrackedDay()
    {
        using TestWorkspace workspace = new();
        using StatisticsTracker tracker = new(workspace.GetPath("stats.json"));

        tracker.TrackCommand("DONG", "GIF", "u1", "tester", "baseball", true);

        List<DailyStats> stats = tracker.GetDailyStats(1);

        Assert.Single(stats);
        Assert.Equal(1, stats[0].TotalCommands);
    }
}
