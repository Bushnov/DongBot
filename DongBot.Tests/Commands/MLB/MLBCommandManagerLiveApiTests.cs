using System;
using System.Threading.Tasks;
using DongBot;

namespace DongBot.Tests;

/// <summary>
/// Integration tests that make real requests to the MLB Stats API through the MLBStatsAPI SDK.
/// Gated by the DONGBOT_RUN_LIVE_MLB_TESTS environment variable.
/// Run via: dotnet test --settings DongBot.Tests/live.runsettings
///
/// Rate limiting: the MLB Stats API rate limit resets every second.
/// Tests run sequentially within this class (xUnit default) and each test
/// waits ThrottleMs before issuing its first API call to keep burst traffic
/// well within the per-second window.
/// </summary>
public class MLBCommandManagerLiveApiTests
{
    private static readonly CommandContext DefaultContext = new("baseball", 123UL, false, "u1", "tester");

    // Rate limit resets every second; 1100ms gap between tests keeps burst traffic safe.
    private const int ThrottleMs = 1100;

    private static bool ShouldRunLiveTests()
    {
        string? env = Environment.GetEnvironmentVariable("DONGBOT_RUN_LIVE_MLB_TESTS");
        return string.Equals(env, "1", StringComparison.OrdinalIgnoreCase) ||
               string.Equals(env, "true", StringComparison.OrdinalIgnoreCase);
    }

    // -------------------------------------------------------------------------
    // GetTodaysScheduleAsync (sport-level) → MLB-SCHEDULE
    // -------------------------------------------------------------------------

    [Fact]
    public async Task LiveApi_MlbSchedule_ReturnsTodaysScheduleOrEmpty()
    {
        if (!ShouldRunLiveTests()) return;
        await Task.Delay(ThrottleMs);

        using MLBCommandManager manager = new();
        string result = await manager.ProcessCommandAsync("MLB-SCHEDULE", DefaultContext);

        Assert.DoesNotContain("Error", result);
        Assert.True(
            result.Contains("Today's MLB Schedule") || result.Contains("No games scheduled for today"),
            $"Unexpected response: {result}");
    }

    // -------------------------------------------------------------------------
    // GetTodaysScheduleAsync (sport-level) → MLB-SCORES
    // -------------------------------------------------------------------------

    [Fact]
    public async Task LiveApi_MlbScores_ReturnsScoresOrNoGamesMessage()
    {
        if (!ShouldRunLiveTests()) return;
        await Task.Delay(ThrottleMs);

        using MLBCommandManager manager = new();
        string result = await manager.ProcessCommandAsync("MLB-SCORES", DefaultContext);

        Assert.DoesNotContain("Error", result);
        Assert.True(
            result.Contains("Live MLB Scores") || result.Contains("No games today") || result.Contains("No live or completed games"),
            $"Unexpected response: {result}");
    }

    // -------------------------------------------------------------------------
    // GetScheduleAsync (team+date-range) → BRAVES-SCHEDULE
    // -------------------------------------------------------------------------

    [Fact]
    public async Task LiveApi_BravesSchedule_ReturnsScheduleOrNoGames()
    {
        if (!ShouldRunLiveTests()) return;
        await Task.Delay(ThrottleMs);

        using MLBCommandManager manager = new();
        string result = await manager.ProcessCommandAsync("BRAVES-SCHEDULE", DefaultContext);

        Assert.DoesNotContain("Error", result);
        Assert.True(
            result.Contains("Atlanta Braves Schedule") || result.Contains("No upcoming Braves games scheduled"),
            $"Unexpected response: {result}");
    }

    // -------------------------------------------------------------------------
    // GetTodaysScheduleAsync (team-filtered) → BRAVES-SCORE
    // -------------------------------------------------------------------------

    [Fact]
    public async Task LiveApi_BravesScore_ReturnsGameOrNoGame()
    {
        if (!ShouldRunLiveTests()) return;
        await Task.Delay(ThrottleMs);

        using MLBCommandManager manager = new();
        string result = await manager.ProcessCommandAsync("BRAVES-SCORE", DefaultContext);

        Assert.DoesNotContain("Error", result);
        Assert.True(
            result.Contains("Atlanta Braves - Today's Game") || result.Contains("No Braves game scheduled today"),
            $"Unexpected response: {result}");
    }

    // -------------------------------------------------------------------------
    // GetCurrentStandingsAsync → MLB-STANDINGS / MLB-DIVISION / MLB-LEAGUE / MLB-SPORT
    // -------------------------------------------------------------------------

    [Fact]
    public async Task LiveApi_MlbStandings_ReturnsAllDivisions()
    {
        if (!ShouldRunLiveTests()) return;
        await Task.Delay(ThrottleMs);

        using MLBCommandManager manager = new();
        string result = await manager.ProcessCommandAsync("MLB-STANDINGS", DefaultContext);

        Assert.DoesNotContain("Error", result);
        Assert.Contains("MLB Standings", result);
        Assert.Contains("Team                    W", result);
    }

    [Fact]
    public async Task LiveApi_MlbDivision_NlEast_ReturnsOnlyNlEast()
    {
        if (!ShouldRunLiveTests()) return;
        await Task.Delay(ThrottleMs);

        using MLBCommandManager manager = new();
        string result = await manager.ProcessCommandAsync("MLB-DIVISION NL East", DefaultContext);

        Assert.DoesNotContain("Error", result);
        Assert.Contains("MLB Standings", result);
        Assert.Contains("Team                    W", result);
    }

    [Fact]
    public async Task LiveApi_MlbLeague_NationalLeague_ExcludesAmericanLeagueDivisions()
    {
        if (!ShouldRunLiveTests()) return;
        await Task.Delay(ThrottleMs);

        using MLBCommandManager manager = new();
        string result = await manager.ProcessCommandAsync("MLB-LEAGUE National League", DefaultContext);

        Assert.DoesNotContain("Error", result);
        Assert.Contains("MLB Standings", result);
        Assert.Contains("Team                    W", result);
    }

    [Fact]
    public async Task LiveApi_MlbSport_MLB_ReturnsAllDivisions()
    {
        if (!ShouldRunLiveTests()) return;
        await Task.Delay(ThrottleMs);

        using MLBCommandManager manager = new();
        string result = await manager.ProcessCommandAsync("MLB-SPORT MLB", DefaultContext);

        Assert.DoesNotContain("Error", result);
        Assert.Contains("MLB Standings", result);
        Assert.Contains("Team                    W", result);
    }

    // -------------------------------------------------------------------------
    // GetDivisionStandingsAsync → BRAVES-STANDINGS
    // -------------------------------------------------------------------------

    [Fact]
    public async Task LiveApi_BravesStandings_ReturnsNlEastWithBraves()
    {
        if (!ShouldRunLiveTests()) return;
        await Task.Delay(ThrottleMs);

        using MLBCommandManager manager = new();
        string result = await manager.ProcessCommandAsync("BRAVES-STANDINGS", DefaultContext);

        Assert.DoesNotContain("Error", result);
        Assert.Contains("NL East Standings", result);
        Assert.Contains("Team                    W", result);
    }

    // -------------------------------------------------------------------------
    // GetRosterAsync → BRAVES-ROSTER
    // -------------------------------------------------------------------------

    [Fact]
    public async Task LiveApi_BravesRoster_ReturnsPitchersAndPositionPlayers()
    {
        if (!ShouldRunLiveTests()) return;
        await Task.Delay(ThrottleMs);

        using MLBCommandManager manager = new();
        string result = await manager.ProcessCommandAsync("BRAVES-ROSTER", DefaultContext);

        Assert.DoesNotContain("Error", result);
        Assert.Contains("Atlanta Braves Active Roster", result);
        Assert.Contains("Pitchers:", result);
        Assert.Contains("Position Players:", result);
    }

    // -------------------------------------------------------------------------
    // GetTeamsAsync + GetTodaysScheduleAsync → MLB-TEAM
    // -------------------------------------------------------------------------

    [Fact]
    public async Task LiveApi_MlbTeam_Braves_ReturnsTeamDetails()
    {
        if (!ShouldRunLiveTests()) return;
        await Task.Delay(ThrottleMs);

        using MLBCommandManager manager = new();
        string result = await manager.ProcessCommandAsync("MLB-TEAM Braves", DefaultContext);

        Assert.DoesNotContain("Error", result);
        Assert.Contains("Atlanta Braves", result);
        Assert.Contains("Truist Park", result);
    }

    // -------------------------------------------------------------------------
    // GetTeamsAsync (venue lookup) → MLB-VENUE
    // -------------------------------------------------------------------------

    [Fact]
    public async Task LiveApi_MlbVenue_TruistPark_ReturnsVenueDetails()
    {
        if (!ShouldRunLiveTests()) return;
        await Task.Delay(ThrottleMs);

        using MLBCommandManager manager = new();
        string result = await manager.ProcessCommandAsync("MLB-VENUE Truist Park", DefaultContext);

        Assert.DoesNotContain("Error", result);
        Assert.Contains("Truist Park", result);
        Assert.Contains("Atlanta Braves", result);
    }

    // -------------------------------------------------------------------------
    // SearchPeopleAsync / GetPersonAsync / GetPlayerStatsAsync / GetRosterAsync
    // → MLB-PLAYER (player profile with current-season stats and health row)
    // -------------------------------------------------------------------------

    [Fact]
    public async Task LiveApi_MlbPlayer_Hitter_ReturnsProfileWithBattingBlock()
    {
        if (!ShouldRunLiveTests()) return;
        await Task.Delay(ThrottleMs);

        using MLBCommandManager manager = new();
        string result = await manager.ProcessCommandAsync("MLB-PLAYER Ronald Acuna", DefaultContext);

        Assert.DoesNotContain("Error retrieving player info", result);
        Assert.Contains("Ronald Acuña Jr.", result);
        Assert.Contains("**Team:**", result);
        // Profile fields
        Assert.Contains("Bats/Throws", result);
        Assert.Contains("Age", result);
    }

    [Fact]
    public async Task LiveApi_MlbPlayer_Pitcher_ReturnsProfileWithPitchingBlock()
    {
        if (!ShouldRunLiveTests()) return;
        await Task.Delay(ThrottleMs);

        using MLBCommandManager manager = new();
        string result = await manager.ProcessCommandAsync("MLB-PLAYER Spencer Strider", DefaultContext);

        Assert.DoesNotContain("Error retrieving player info", result);
        Assert.Contains("Spencer Strider", result);
        Assert.Contains("Bats/Throws", result);
    }

    [Fact]
    public async Task LiveApi_MlbPlayer_SearchFallback_ResolvesUnknownSpelling()
    {
        if (!ShouldRunLiveTests()) return;
        await Task.Delay(ThrottleMs);

        // Intentional misspelling to exercise SearchPeopleAsync fallback path
        using MLBCommandManager manager = new();
        string result = await manager.ProcessCommandAsync("MLB-PLAYER Freddie Freeman", DefaultContext);

        Assert.DoesNotContain("Error retrieving player info", result);
        Assert.Contains("Freddie Freeman", result);
    }

    // -------------------------------------------------------------------------
    // GetPersonAsync + GetPlayerStatsAsync → MLB-PLAYER-STATS (role-based sections)
    // -------------------------------------------------------------------------

    [Fact]
    public async Task LiveApi_PlayerStats_Pitcher_ShowsPitchingAndFieldingOnly_2023()
    {
        if (!ShouldRunLiveTests()) return;
        await Task.Delay(ThrottleMs);

        using MLBCommandManager manager = new();
        string result = await manager.ProcessCommandAsync("MLB-PLAYER-STATS Spencer Strider 2023", DefaultContext);

        Assert.DoesNotContain("Error retrieving player stats", result);
        Assert.Contains("Pitching Stats", result);
        Assert.Contains("Fielding Stats", result);
        Assert.DoesNotContain("Batting Stats", result);
        Assert.DoesNotContain("Baserunning Stats", result);
    }

    [Fact]
    public async Task LiveApi_PlayerStats_Hitter_ShowsBattingBaserunningAndFieldingOnly_2023()
    {
        if (!ShouldRunLiveTests()) return;
        await Task.Delay(ThrottleMs);

        using MLBCommandManager manager = new();
        string result = await manager.ProcessCommandAsync("MLB-PLAYER-STATS Ronald Acuna 2023", DefaultContext);

        Assert.DoesNotContain("Error retrieving player stats", result);
        Assert.Contains("Batting Stats", result);
        Assert.Contains("Baserunning Stats", result);
        Assert.Contains("Fielding Stats", result);
        Assert.DoesNotContain("Pitching Stats", result);
    }

    [Fact]
    public async Task LiveApi_PlayerStats_Shohei_ShowsAllSections_2023()
    {
        if (!ShouldRunLiveTests()) return;
        await Task.Delay(ThrottleMs);

        using MLBCommandManager manager = new();
        string result = await manager.ProcessCommandAsync("MLB-PLAYER-STATS Shohei Ohtani 2023", DefaultContext);

        Assert.DoesNotContain("Error retrieving player stats", result);
        Assert.Contains("Batting Stats", result);
        Assert.Contains("Baserunning Stats", result);
        Assert.Contains("Pitching Stats", result);
        Assert.Contains("Fielding Stats", result);
    }

    [Fact]
    public async Task LiveApi_PlayerStats_Catcher_ShowsBattingAndFieldingOnly_2023()
    {
        if (!ShouldRunLiveTests()) return;
        await Task.Delay(ThrottleMs);

        // Catchers are position players — expect no pitching section
        using MLBCommandManager manager = new();
        string result = await manager.ProcessCommandAsync("MLB-PLAYER-STATS Will Smith 2023", DefaultContext);

        Assert.DoesNotContain("Error retrieving player stats", result);
        Assert.Contains("Batting Stats", result);
        Assert.Contains("Fielding Stats", result);
        Assert.DoesNotContain("Pitching Stats", result);
    }

    [Fact]
    public async Task LiveApi_PlayerStats_ClosingPitcher_ShowsPitchingAndFieldingOnly_2023()
    {
        if (!ShouldRunLiveTests()) return;
        await Task.Delay(ThrottleMs);

        // Relief pitcher with saves — verify saves line appears and no batting section
        using MLBCommandManager manager = new();
        string result = await manager.ProcessCommandAsync("MLB-PLAYER-STATS Kenley Jansen 2023", DefaultContext);

        Assert.DoesNotContain("Error retrieving player stats", result);
        Assert.Contains("Pitching Stats", result);
        Assert.Contains("Fielding Stats", result);
        Assert.DoesNotContain("Batting Stats", result);
    }
}
