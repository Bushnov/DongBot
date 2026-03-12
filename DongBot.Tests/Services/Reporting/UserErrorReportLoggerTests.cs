using DongBot;

namespace DongBot.Tests;

public class UserErrorReportLoggerTests
{
    [Fact]
    public void LogReport_PersistsEntryToDedicatedFile()
    {
        using TestWorkspace workspace = new();
        string reportPath = workspace.GetPath("user_error_reports.json");

        using UserErrorReportLogger logger = new(reportPath);
        logger.LogReport("u1", "tester", "baseball", "MLB-TEAM Braves", "the gif link is broken");

        string json = File.ReadAllText(reportPath);
        Assert.Contains("MLB-TEAM Braves", json);
        Assert.Contains("the gif link is broken", json);
        Assert.Contains("tester", json);
    }

    [Fact]
    public void GetRecentReports_ReturnsMostRecentFirst()
    {
        using TestWorkspace workspace = new();
        string reportPath = workspace.GetPath("user_error_reports.json");

        using UserErrorReportLogger logger = new(reportPath);
        logger.LogReport("u1", "tester", "baseball", "MLB-SCORES", "first");
        logger.LogReport("u1", "tester", "baseball", "MLB-SCHEDULE", "second");

        List<UserErrorReportEntry> reports = logger.GetRecentReports(2);

        Assert.Equal(2, reports.Count);
        Assert.Equal("second", reports[0].Comment);
        Assert.Equal("first", reports[1].Comment);
    }
}
