using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using DongBot;
using MLBStatsAPI.IDs;
using MLBStatsAPI.Models;

namespace DongBot.Tests;

public class MLBCommandManagerTests
{
    private static readonly CommandContext DefaultContext = new("baseball", 123UL, false, "u1", "tester");

    [Fact]
    public void CanHandle_MatchesMlbAndBravesPrefixes()
    {
        using MLBCommandManager manager = new();

        Assert.True(manager.CanHandle("MLB-SCHEDULE"));
        Assert.True(manager.CanHandle("BRAVES-SCORE"));
        Assert.False(manager.CanHandle("PING"));
    }

    [Fact]
    public async Task ProcessCommandAsync_BravesSchedulerEnable_InvokesSchedulerControl()
    {
        FakeBravesSchedulerControl scheduler = new();
        using MLBCommandManager manager = new(scheduler, new FakeMlbDataClient());
        CommandContext guildContext = new("baseball", 123UL, false, "u1", "tester", 777UL, "Guild 777");

        string result = await manager.ProcessCommandAsync("BRAVES-SCHEDULER-ENABLE", guildContext);

        Assert.True(scheduler.EnableCalled);
        Assert.Equal(777UL, scheduler.LastEnableGuildId);
        Assert.Equal("Braves scheduler enabled for this server.", result);
    }

    [Fact]
    public async Task ProcessCommandAsync_BravesSchedulerTestDaily_ReturnsSchedulerMessage()
    {
        FakeBravesSchedulerControl scheduler = new FakeBravesSchedulerControl { DailyResponse = "daily-ok" };
        using MLBCommandManager manager = new(scheduler, new FakeMlbDataClient());
        CommandContext guildContext = new("baseball", 123UL, false, "u1", "tester", 888UL, "Guild 888");

        string result = await manager.ProcessCommandAsync("BRAVES-SCHEDULER-TEST-DAILY", guildContext);

        Assert.Equal("daily-ok", result);
        Assert.Equal(888UL, scheduler.LastDailyGuildId);
    }

    [Fact]
    public async Task ProcessCommandAsync_BravesSchedulerTestWeekly_ForwardsGuildId()
    {
        FakeBravesSchedulerControl scheduler = new();
        using MLBCommandManager manager = new(scheduler, new FakeMlbDataClient());
        CommandContext guildContext = new("baseball", 123UL, false, "u1", "tester", 889UL, "Guild 889");

        string result = await manager.ProcessCommandAsync("BRAVES-SCHEDULER-TEST-WEEKLY", guildContext);

        Assert.Equal("weekly", result);
        Assert.Equal(889UL, scheduler.LastWeeklyGuildId);
    }

    [Fact]
    public async Task ProcessCommandAsync_BravesSchedulerStatus_WhenSchedulerMissing_ReturnsNotInitialized()
    {
        using MLBCommandManager manager = new(null, new FakeMlbDataClient());

        string result = await manager.ProcessCommandAsync("BRAVES-SCHEDULER-STATUS", DefaultContext);

        Assert.Equal("Scheduler not initialized.", result);
    }

    [Fact]
    public async Task ProcessCommandAsync_BravesSchedulerTestWeekly_WhenSchedulerMissing_ReturnsNotInitialized()
    {
        using MLBCommandManager manager = new(null, new FakeMlbDataClient());

        string result = await manager.ProcessCommandAsync("BRAVES-SCHEDULER-TEST-WEEKLY", DefaultContext);

        Assert.Equal("Scheduler not initialized.", result);
    }

    [Fact]
    public async Task ProcessCommandAsync_MlbSchedule_UsesInjectedClient()
    {
        FakeMlbDataClient mlbClient = new();
        using MLBCommandManager manager = new(null, mlbClient);

        string result = await manager.ProcessCommandAsync("MLB-SCHEDULE", DefaultContext);

        Assert.Contains("No games scheduled for today", result);
        Assert.True(mlbClient.GetTodaysScheduleCalled);
    }

    [Fact]
    public async Task ProcessCommandAsync_BravesRoster_ParsesRosterFromInjectedClient()
    {
        FakeMlbDataClient mlbClient = new FakeMlbDataClient
        {
            RosterJson = "{\"roster\":[{\"person\":{\"fullName\":\"Pitcher One\"},\"jerseyNumber\":\"10\",\"position\":{\"type\":\"Pitcher\",\"abbreviation\":\"P\"}},{\"person\":{\"fullName\":\"Batter One\"},\"jerseyNumber\":\"20\",\"position\":{\"type\":\"Infielder\",\"abbreviation\":\"1B\"}}]}"
        };

        using MLBCommandManager manager = new(null, mlbClient);

        string result = await manager.ProcessCommandAsync("BRAVES-ROSTER", DefaultContext);

        Assert.Contains("Atlanta Braves Active Roster", result);
        Assert.Contains("Pitcher One", result);
        Assert.Contains("Batter One", result);
    }

    [Fact]
    public async Task ProcessCommandAsync_BravesSchedule_UsesInjectedClientAndHandlesNoGames()
    {
        FakeMlbDataClient mlbClient = new();
        using MLBCommandManager manager = new(null, mlbClient);

        string result = await manager.ProcessCommandAsync("BRAVES-SCHEDULE", DefaultContext);

        Assert.Contains("No upcoming Braves games scheduled", result);
        Assert.True(mlbClient.GetScheduleCalled);
    }

    [Fact]
    public async Task ProcessCommandAsync_BravesStandings_UsesInjectedClientAndHandlesMissingData()
    {
        FakeMlbDataClient mlbClient = new();
        using MLBCommandManager manager = new(null, mlbClient);

        string result = await manager.ProcessCommandAsync("BRAVES-STANDINGS", DefaultContext);

        Assert.Contains("Could not retrieve NL East standings", result);
        Assert.True(mlbClient.GetDivisionStandingsCalled);
    }

    [Fact]
    public async Task ProcessCommandAsync_MlbTeam_RequiresTeamArgument()
    {
        FakeMlbDataClient mlbClient = new();
        using MLBCommandManager manager = new(null, mlbClient);

        string result = await manager.ProcessCommandAsync("MLB-TEAM", DefaultContext);

        Assert.Contains("Usage: !mlb-team", result);
        Assert.False(mlbClient.GetTeamsCalled);
    }

    [Fact]
    public async Task ProcessCommandAsync_MlbTeam_UsesInjectedClientWhenQueryProvided()
    {
        FakeMlbDataClient mlbClient = new();
        using MLBCommandManager manager = new(null, mlbClient);

        string result = await manager.ProcessCommandAsync("MLB-TEAM Braves", DefaultContext);

        Assert.Contains("Could not retrieve team information", result);
        Assert.True(mlbClient.GetTeamsCalled);
    }

    [Fact]
    public async Task ProcessCommandAsync_MlbTeam_WhenNoMatchReturnsNotFound()
    {
        FakeMlbDataClient mlbClient = new FakeMlbDataClient
        {
            TeamsResponseResult = new TeamsResponse
            {
                Teams = new List<Team>
                {
                    new Team { Id = 1, Name = "Seattle Mariners", Abbreviation = "SEA" }
                }
            }
        };

        using MLBCommandManager manager = new(null, mlbClient);

        string result = await manager.ProcessCommandAsync("MLB-TEAM Braves", DefaultContext);

        Assert.Contains("Team not found: Braves", result);
    }

    [Fact]
    public async Task ProcessCommandAsync_BravesSchedule_WithGameFormatsOpponentAndScore()
    {
        FakeMlbDataClient mlbClient = new FakeMlbDataClient
        {
            ScheduleResponseResult = new ScheduleResponse
            {
                Dates = new List<ScheduleDate>
                {
                    new ScheduleDate
                    {
                        Games = new List<Game>
                        {
                            new Game
                            {
                                GameDate = DateTime.UtcNow,
                                Status = new GameStatus { DetailedState = "Final", AbstractGameState = "Final" },
                                Teams = new GameTeams
                                {
                                    Away = new GameTeam { Team = new Team { Id = 121, Name = "New York Mets" }, Score = 2 },
                                    Home = new GameTeam { Team = new Team { Id = 144, Name = "Atlanta Braves" }, Score = 5 }
                                }
                            }
                        }
                    }
                }
            }
        };

        using MLBCommandManager manager = new(null, mlbClient);

        string result = await manager.ProcessCommandAsync("BRAVES-SCHEDULE", DefaultContext);

        Assert.Contains("Atlanta Braves Schedule", result);
        Assert.Contains("Braves vs New York Mets", result);
        Assert.Contains("Score: New York Mets 2 - Atlanta Braves 5", result);
    }

    [Fact]
    public async Task ProcessCommandAsync_BravesStandings_WithDataFormatsTable()
    {
        FakeMlbDataClient mlbClient = new FakeMlbDataClient
        {
            DivisionStandingsResponseResult = new StandingsResponse
            {
                Records = new List<StandingRecord>
                {
                    new StandingRecord
                    {
                        TeamRecords = new List<TeamRecord>
                        {
                            new TeamRecord
                            {
                                Team = new Team { Id = 144, Name = "Atlanta Braves" },
                                Wins = 10,
                                Losses = 5,
                                GamesBack = "-",
                                WildCardGamesBack = "-",
                                Streak = new Streak { StreakCode = "W3" }
                            },
                            new TeamRecord
                            {
                                Team = new Team { Id = 121, Name = "New York Mets" },
                                Wins = 8,
                                Losses = 7,
                                GamesBack = "2.0",
                                WildCardGamesBack = "0.5",
                                Streak = new Streak { StreakCode = "L1" }
                            }
                        }
                    }
                }
            }
        };

        using MLBCommandManager manager = new(null, mlbClient);

        string result = await manager.ProcessCommandAsync("BRAVES-STANDINGS", DefaultContext);

        Assert.Contains("NL East Standings", result);
        Assert.Contains("Atlanta Braves", result);
        Assert.Contains("New York Mets", result);
        Assert.Contains("W3", result);
    }

    [Fact]
    public async Task ProcessCommandAsync_MlbTeam_WithMatchIncludesTeamDetailsAndTodaysGame()
    {
        FakeMlbDataClient mlbClient = new FakeMlbDataClient
        {
            TeamsResponseResult = new TeamsResponse
            {
                Teams = new List<Team>
                {
                    new Team
                    {
                        Id = 144,
                        Name = "Atlanta Braves",
                        Abbreviation = "ATL",
                        LocationName = "Atlanta",
                        TeamName = "Braves",
                        FirstYearOfPlay = "1876",
                        League = new League { Name = "National League" },
                        Division = new Division { Name = "NL East" },
                        Venue = new Venue { Name = "Truist Park" }
                    }
                }
            },
            TodaysScheduleResponseResult = new ScheduleResponse
            {
                Dates = new List<ScheduleDate>
                {
                    new ScheduleDate
                    {
                        Games = new List<Game>
                        {
                            new Game
                            {
                                GameDate = DateTime.UtcNow,
                                Status = new GameStatus { DetailedState = "Scheduled", AbstractGameState = "Preview" },
                                Teams = new GameTeams
                                {
                                    Away = new GameTeam { Team = new Team { Name = "New York Mets" } },
                                    Home = new GameTeam { Team = new Team { Name = "Atlanta Braves" } }
                                }
                            }
                        }
                    }
                }
            }
        };

        using MLBCommandManager manager = new(null, mlbClient);

        string result = await manager.ProcessCommandAsync("MLB-TEAM Braves", DefaultContext);

        Assert.Contains("Atlanta Braves", result);
        Assert.Contains("National League", result);
        Assert.Contains("Truist Park", result);
        Assert.Contains("Today's Game", result);
        Assert.Contains("New York Mets @ Atlanta Braves", result);
    }

    [Fact]
    public async Task ProcessCommandAsync_MlbVenue_RequiresArgument()
    {
        using MLBCommandManager manager = new(null, new FakeMlbDataClient());

        string result = await manager.ProcessCommandAsync("MLB-VENUE", DefaultContext);

        Assert.Contains("Usage: !mlb-venue", result);
    }

    [Fact]
    public async Task ProcessCommandAsync_MlbVenue_WithMatchIncludesVenueDetails()
    {
        FakeMlbDataClient mlbClient = new FakeMlbDataClient
        {
            TeamsResponseResult = new TeamsResponse
            {
                Teams = new List<Team>
                {
                    new Team
                    {
                        Id = 144,
                        Name = "Atlanta Braves",
                        Venue = new Venue
                        {
                            Id = 4705,
                            Name = "Truist Park",
                            Location = new Location { City = "Atlanta", StateAbbrev = "GA" },
                            FieldInfo = new FieldInfo { RoofType = "Open", TurfType = "Grass", Capacity = 41084 }
                        }
                    }
                }
            }
        };

        using MLBCommandManager manager = new(null, mlbClient);

        string result = await manager.ProcessCommandAsync("MLB-VENUE Truist Park", DefaultContext);

        Assert.Contains("Truist Park", result);
        Assert.Contains("**Venue ID:** 4705", result);
        Assert.Contains("Atlanta", result);
        Assert.Contains("**Home Team(s):** Atlanta Braves", result);
    }

    [Fact]
    public async Task ProcessCommandAsync_MlbVenue_WhenDataUnavailableReturnsError()
    {
        using MLBCommandManager manager = new(null, new FakeMlbDataClient());

        string result = await manager.ProcessCommandAsync("MLB-VENUE Truist Park", DefaultContext);

        Assert.Contains("Could not retrieve venue information", result);
    }

    [Fact]
    public async Task ProcessCommandAsync_MlbVenue_WhenNoMatchReturnsNotFound()
    {
        FakeMlbDataClient mlbClient = new FakeMlbDataClient
        {
            TeamsResponseResult = new TeamsResponse
            {
                Teams = new List<Team>
                {
                    new Team
                    {
                        Id = 121,
                        Name = "New York Mets",
                        Venue = new Venue
                        {
                            Id = 3289,
                            Name = "Citi Field"
                        }
                    }
                }
            }
        };

        using MLBCommandManager manager = new(null, mlbClient);

        string result = await manager.ProcessCommandAsync("MLB-VENUE Truist Park", DefaultContext);

        Assert.Contains("Venue not found: Truist Park", result);
    }

    [Fact]
    public async Task ProcessCommandAsync_MlbPlayer_WithMissingNameReturnsUsage()
    {
        using MLBCommandManager manager = new(null, new FakeMlbDataClient());

        string result = await manager.ProcessCommandAsync("MLB-PLAYER", DefaultContext);

        Assert.Contains("Usage: !mlb-player", result);
    }

    [Fact]
    public async Task ProcessCommandAsync_MlbPlayer_WhenSearchHasNoPeopleReturnsNotFound()
    {
        FakeMlbDataClient mlbClient = new FakeMlbDataClient
        {
            SearchPeopleResult = "{\"people\":[]}"
        };
        using MLBCommandManager manager = new(null, mlbClient);

        string result = await manager.ProcessCommandAsync("MLB-PLAYER Ghost Player", DefaultContext);

        Assert.Contains("Player not found: Ghost Player", result);
    }

    [Fact]
    public async Task ProcessCommandAsync_MlbPlayer_WhenSearchUnavailableReturnsDataError()
    {
        FakeMlbDataClient mlbClient = new FakeMlbDataClient
        {
            SearchPeopleResult = string.Empty
        };
        using MLBCommandManager manager = new(null, mlbClient);

        string result = await manager.ProcessCommandAsync("MLB-PLAYER Ghost Player", DefaultContext);

        Assert.Contains("Could not retrieve player information", result);
    }

    [Fact]
    public async Task ProcessCommandAsync_MlbPlayer_WithBattingStatsFormatsProfileAndBattingBlock()
    {
        FakeMlbDataClient mlbClient = new FakeMlbDataClient
        {
            SearchPeopleResult = "{\"people\":[{\"id\":660670,\"fullName\":\"Ronald Acuna Jr.\"}]}",
            PersonResponseResult = new PeopleResponse
            {
                People = new List<Person>
                {
                    new Person
                    {
                        Id = 660670,
                        FullName = "Ronald Acuna Jr.",
                        PrimaryNumber = "13",
                        BirthDate = "1997-12-18",
                        Height = "6' 0\"",
                        Weight = 205,
                        PrimaryPosition = new Position { Name = "Right Fielder" },
                        CurrentTeam = new Team { Name = "Atlanta Braves" },
                        BatSide = new BatSide { Code = "R" },
                        PitchHand = new PitchHand { Code = "R" }
                    }
                }
            },
            PlayerStatsResponseResult = new StatsResponse
            {
                Stats = new List<StatGroup>
                {
                    new StatGroup
                    {
                        Splits = new List<StatSplit>
                        {
                            new StatSplit
                            {
                                Stat = new PlayerStats
                                {
                                    AtBats = 100,
                                    Avg = ".300",
                                    Obp = ".380",
                                    Slg = ".550",
                                    HomeRuns = 10,
                                    Rbi = 25,
                                    Runs = 30,
                                    Hits = 30,
                                    Doubles = 5,
                                    Triples = 1,
                                    BaseOnBalls = 12,
                                    StrikeOuts = 20
                                }
                            }
                        }
                    }
                }
            }
        };

        using MLBCommandManager manager = new(null, mlbClient);

        string result = await manager.ProcessCommandAsync("MLB-PLAYER Ronald Acuna", DefaultContext);

        Assert.Contains("Ronald Acuna Jr.", result);
        Assert.Contains("Atlanta Braves", result);
        Assert.Contains("AVG: .300 | OBP: .380 | SLG: .550", result);
        Assert.Contains("HR: 10 | RBI: 25 | R: 30", result);
    }

    [Fact]
    public async Task ProcessCommandAsync_MlbPlayer_UsesSdkPlayerIdMapBeforeApiSearch()
    {
        FakeMlbDataClient mlbClient = new FakeMlbDataClient
        {
            PersonResponseResultById = new Dictionary<int, PeopleResponse>
            {
                [660670] = new PeopleResponse
                {
                    People = new List<Person>
                    {
                        new Person
                        {
                            Id = 660670,
                            FullName = "Ronald Acuna Jr.",
                            PrimaryNumber = "13",
                            BirthDate = "1997-12-18",
                            Height = "6' 0\"",
                            Weight = 205,
                            PrimaryPosition = new Position { Name = "Right Fielder" },
                            CurrentTeam = new Team { Name = "Atlanta Braves" },
                            BatSide = new BatSide { Code = "R" },
                            PitchHand = new PitchHand { Code = "R" }
                        }
                    }
                }
            },
            PlayerStatsResponseResult = new StatsResponse
            {
                Stats = new List<StatGroup>
                {
                    new StatGroup
                    {
                        Splits = new List<StatSplit>
                        {
                            new StatSplit
                            {
                                Stat = new PlayerStats { AtBats = 50, Avg = ".280", Obp = ".350", Slg = ".500", HomeRuns = 7, Rbi = 20, Runs = 18 }
                            }
                        }
                    }
                }
            }
        };

        using MLBCommandManager manager = new(null, mlbClient);

        string result = await manager.ProcessCommandAsync("MLB-PLAYER Ronald Acuna", DefaultContext);

        Assert.Contains("Ronald Acuna Jr.", result);
        Assert.False(mlbClient.SearchPeopleCalled);
        Assert.True(mlbClient.GetPersonCalled);
    }

    [Fact]
    public async Task ProcessCommandAsync_MlbPlayerStats_WithInvalidSeasonReturnsError()
    {
        using MLBCommandManager manager = new(null, new FakeMlbDataClient());

        string result = await manager.ProcessCommandAsync("MLB-PLAYER-STATS Ronald Acuna nope", DefaultContext);

        Assert.Contains("Invalid season year", result);
    }

    [Fact]
    public async Task ProcessCommandAsync_MlbPlayerStats_WithMissingSeasonReturnsUsage()
    {
        using MLBCommandManager manager = new(null, new FakeMlbDataClient());

        string result = await manager.ProcessCommandAsync("MLB-PLAYER-STATS Ronald Acuna", DefaultContext);

        Assert.Contains("Usage: !mlb-player-stats", result);
    }

    [Fact]
    public async Task ProcessCommandAsync_MlbPlayerStats_WhenPlayerNotFoundReturnsNotFound()
    {
        FakeMlbDataClient mlbClient = new FakeMlbDataClient
        {
            SearchPeopleResult = "{\"people\":[]}"
        };

        using MLBCommandManager manager = new(null, mlbClient);

        string result = await manager.ProcessCommandAsync("MLB-PLAYER-STATS Ghost Player 2024", DefaultContext);

        Assert.Contains("Player not found: Ghost Player", result);
    }

    [Fact]
    public async Task ProcessCommandAsync_MlbPlayerStats_WithPitchingStatsFormatsPitchingBlock()
    {
        FakeMlbDataClient mlbClient = new FakeMlbDataClient
        {
            SearchPeopleResult = "{\"people\":[{\"id\":605483,\"fullName\":\"Max Fried\"}]}",
            PlayerStatsResponseResult = new StatsResponse
            {
                Stats = new List<StatGroup>
                {
                    new StatGroup
                    {
                        Splits = new List<StatSplit>
                        {
                            new StatSplit
                            {
                                Team = new Team { Name = "Atlanta Braves" },
                                Stat = new PlayerStats
                                {
                                    GamesPlayed = 20,
                                    Wins = 12,
                                    Losses = 4,
                                    Era = "2.95",
                                    Whip = "1.10",
                                    InningsPitched = "130.0",
                                    Hits = 100,
                                    Runs = 45,
                                    EarnedRuns = 43,
                                    StrikeOuts = 140,
                                    BaseOnBalls = 35,
                                    HomeRuns = 10,
                                    Avg = ".220",
                                    Saves = 0
                                }
                            }
                        }
                    }
                }
            }
        };

        using MLBCommandManager manager = new(null, mlbClient);

        string result = await manager.ProcessCommandAsync("MLB-PLAYER-STATS Max Fried 2023", DefaultContext);

        Assert.Contains("Max Fried - 2023 Season Stats", result);
        Assert.Contains("Pitching Stats", result);
        Assert.Contains("W-L: 12-4  ERA: 2.95  WHIP: 1.10", result);
        Assert.Contains("SO: 140  BB: 35  HR: 10", result);
    }

    [Fact]
    public async Task ProcessCommandAsync_MlbPlayerStats_PitcherRole_ShowsPitchingAndFieldingOnly()
    {
        FakeMlbDataClient mlbClient = new FakeMlbDataClient
        {
            SearchPeopleResult = "{\"people\":[{\"id\":675911,\"fullName\":\"Spencer Strider\"}]}",
            PersonResponseResult = new PeopleResponse
            {
                People = new List<Person>
                {
                    new Person
                    {
                        Id = 675911,
                        FullName = "Spencer Strider",
                        PrimaryPosition = new Position { Name = "Pitcher", Type = "Pitcher", Abbreviation = "P", Code = "1" }
                    }
                }
            },
            PlayerStatsResponseResult = new StatsResponse
            {
                Stats = new List<StatGroup>
                {
                    new StatGroup
                    {
                        Splits = new List<StatSplit>
                        {
                            new StatSplit
                            {
                                Team = new Team { Name = "Atlanta Braves" },
                                Stat = new PlayerStats
                                {
                                    AtBats = 12,
                                    Avg = ".208",
                                    StolenBases = 2,
                                    Wins = 20,
                                    Losses = 5,
                                    Era = "3.00",
                                    InningsPitched = "186.2",
                                    StrikeOuts = 281,
                                    Assists = 5,
                                    PutOuts = 15,
                                    Errors = 0,
                                    FieldingPercentage = "1.000",
                                    Chances = 20,
                                    DoublePlays = 1
                                }
                            }
                        }
                    }
                }
            }
        };

        using MLBCommandManager manager = new(null, mlbClient);

        string result = await manager.ProcessCommandAsync("MLB-PLAYER-STATS Spencer Strider 2023", DefaultContext);

        Assert.Contains("Pitching Stats", result);
        Assert.Contains("Fielding Stats", result);
        Assert.DoesNotContain("Batting Stats", result);
        Assert.DoesNotContain("Baserunning Stats", result);
    }

    [Fact]
    public async Task ProcessCommandAsync_MlbPlayerStats_HitterRole_ShowsBattingBaserunningAndFieldingOnly()
    {
        FakeMlbDataClient mlbClient = new FakeMlbDataClient
        {
            SearchPeopleResult = "{\"people\":[{\"id\":660670,\"fullName\":\"Ronald Acuna Jr.\"}]}",
            PersonResponseResult = new PeopleResponse
            {
                People = new List<Person>
                {
                    new Person
                    {
                        Id = 660670,
                        FullName = "Ronald Acuna Jr.",
                        PrimaryPosition = new Position { Name = "Right Field", Type = "Outfielder", Abbreviation = "RF", Code = "9" }
                    }
                }
            },
            PlayerStatsResponseResult = new StatsResponse
            {
                Stats = new List<StatGroup>
                {
                    new StatGroup
                    {
                        Splits = new List<StatSplit>
                        {
                            new StatSplit
                            {
                                Team = new Team { Name = "Atlanta Braves" },
                                Stat = new PlayerStats
                                {
                                    AtBats = 643,
                                    Avg = ".337",
                                    Obp = ".416",
                                    Slg = ".596",
                                    Ops = "1.012",
                                    StolenBases = 73,
                                    CaughtStealing = 14,
                                    Assists = 8,
                                    PutOuts = 280,
                                    Errors = 5,
                                    FieldingPercentage = ".983",
                                    Chances = 293,
                                    DoublePlays = 2,
                                    Era = "4.50",
                                    InningsPitched = "2.0"
                                }
                            }
                        }
                    }
                }
            }
        };

        using MLBCommandManager manager = new(null, mlbClient);

        string result = await manager.ProcessCommandAsync("MLB-PLAYER-STATS Ronald Acuna 2023", DefaultContext);

        Assert.Contains("Batting Stats", result);
        Assert.Contains("Baserunning Stats", result);
        Assert.Contains("Fielding Stats", result);
        Assert.DoesNotContain("Pitching Stats", result);
    }

    [Fact]
    public async Task ProcessCommandAsync_MlbPlayerStats_ShoheiRole_ShowsAllSections()
    {
        FakeMlbDataClient mlbClient = new FakeMlbDataClient
        {
            SearchPeopleResult = "{\"people\":[{\"id\":660271,\"fullName\":\"Shohei Ohtani\"}]}",
            PersonResponseResult = new PeopleResponse
            {
                People = new List<Person>
                {
                    new Person
                    {
                        Id = 660271,
                        FullName = "Shohei Ohtani",
                        PrimaryPosition = new Position { Name = "Pitcher", Type = "Pitcher", Abbreviation = "P", Code = "1" }
                    }
                }
            },
            PlayerStatsResponseResult = new StatsResponse
            {
                Stats = new List<StatGroup>
                {
                    new StatGroup
                    {
                        Splits = new List<StatSplit>
                        {
                            new StatSplit
                            {
                                Team = new Team { Name = "Los Angeles Dodgers" },
                                Stat = new PlayerStats
                                {
                                    AtBats = 497,
                                    Avg = ".304",
                                    Obp = ".412",
                                    Slg = ".654",
                                    Ops = "1.066",
                                    StolenBases = 20,
                                    CaughtStealing = 3,
                                    Wins = 10,
                                    Losses = 5,
                                    Era = "3.14",
                                    InningsPitched = "132.0",
                                    StrikeOuts = 167,
                                    Assists = 3,
                                    PutOuts = 30,
                                    Errors = 1,
                                    FieldingPercentage = ".971",
                                    Chances = 34,
                                    DoublePlays = 1
                                }
                            }
                        }
                    }
                }
            }
        };

        using MLBCommandManager manager = new(null, mlbClient);

        string result = await manager.ProcessCommandAsync("MLB-PLAYER-STATS Shohei Ohtani 2023", DefaultContext);

        Assert.Contains("Batting Stats", result);
        Assert.Contains("Baserunning Stats", result);
        Assert.Contains("Pitching Stats", result);
        Assert.Contains("Fielding Stats", result);
    }

    [Fact]
    public async Task ProcessCommandAsync_MlbPlayer_WhenRosterStatusAvailable_IncludesHealthRow()
    {
        FakeMlbDataClient mlbClient = new FakeMlbDataClient
        {
            SearchPeopleResult = "{\"people\":[{\"id\":660670,\"fullName\":\"Ronald Acuna Jr.\"}]}",
            PersonResponseResult = new PeopleResponse
            {
                People = new List<Person>
                {
                    new Person
                    {
                        Id = 660670,
                        FullName = "Ronald Acuna Jr.",
                        PrimaryNumber = "13",
                        BirthDate = "1997-12-18",
                        Height = "6' 0\"",
                        Weight = 205,
                        PrimaryPosition = new Position { Name = "Right Fielder" },
                        CurrentTeam = new Team { Id = TeamIds.AtlantaBraves, Name = "Atlanta Braves" },
                        BatSide = new BatSide { Code = "R" },
                        PitchHand = new PitchHand { Code = "R" }
                    }
                }
            },
            PlayerStatsResponseResult = new StatsResponse { Stats = new List<StatGroup>() },
            RosterJsonByType = new Dictionary<string, string>
            {
                ["active"] = "{\"roster\":[{\"person\":{\"id\":660670},\"status\":{\"description\":\"Day-To-Day\"}}]}"
            }
        };

        using MLBCommandManager manager = new(null, mlbClient);

        string result = await manager.ProcessCommandAsync("MLB-PLAYER Ronald Acuna", DefaultContext);

        Assert.Contains("**Health:** Day-To-Day", result);
    }

    [Fact]
    public async Task ProcessCommandAsync_MlbPlayerStats_SelectsBestApiNameMatchNotFirst()
    {
        FakeMlbDataClient mlbClient = new FakeMlbDataClient
        {
            SearchPeopleResult = "{\"people\":[{\"id\":1,\"fullName\":\"Ronald Bolanos\"},{\"id\":660670,\"fullName\":\"Ronald Acuna Jr.\"}]}",
            PlayerStatsResponseResult = new StatsResponse
            {
                Stats = new List<StatGroup>
                {
                    new StatGroup
                    {
                        Splits = new List<StatSplit>
                        {
                            new StatSplit { Team = new Team { Name = "Atlanta Braves" }, Stat = new PlayerStats { GamesPlayed = 1, AtBats = 1, Avg = ".000" } }
                        }
                    }
                }
            }
        };

        using MLBCommandManager manager = new(null, mlbClient);

        string result = await manager.ProcessCommandAsync("MLB-PLAYER-STATS Ronald Acuna 2024", DefaultContext);

        Assert.Contains("Ronald Acuna Jr. - 2024 Season Stats", result);
    }

    [Fact]
    public async Task ProcessCommandAsync_MlbPlayerStats_WhenNoStatsFoundReturnsError()
    {
        FakeMlbDataClient mlbClient = new FakeMlbDataClient
        {
            SearchPeopleResult = "{\"people\":[{\"id\":1,\"fullName\":\"Example Player\"}]}",
            PlayerStatsResponseResult = new StatsResponse { Stats = new List<StatGroup>() }
        };

        using MLBCommandManager manager = new(null, mlbClient);

        string result = await manager.ProcessCommandAsync("MLB-PLAYER-STATS Example Player 2024", DefaultContext);

        Assert.Contains("No stats found for Example Player in 2024 season", result);
    }

    [Fact]
    public async Task ProcessCommandAsync_MlbScores_WithNoGamesReturnsNoGamesMessage()
    {
        FakeMlbDataClient mlbClient = new FakeMlbDataClient();
        using MLBCommandManager manager = new(null, mlbClient);

        string result = await manager.ProcessCommandAsync("MLB-SCORES", DefaultContext);

        Assert.Contains("No games today", result);
    }

    [Fact]
    public async Task ProcessCommandAsync_MlbScores_WithFinalGameIncludesScoreLine()
    {
        FakeMlbDataClient mlbClient = new FakeMlbDataClient
        {
            TodaysScheduleResponseResult = new ScheduleResponse
            {
                Dates = new List<ScheduleDate>
                {
                    new ScheduleDate
                    {
                        Games = new List<Game>
                        {
                            new Game
                            {
                                Status = new GameStatus { AbstractGameState = "Final", DetailedState = "Final" },
                                Teams = new GameTeams
                                {
                                    Away = new GameTeam { Team = new Team { Name = "Atlanta Braves" }, Score = 4 },
                                    Home = new GameTeam { Team = new Team { Name = "New York Mets" }, Score = 2 }
                                }
                            }
                        }
                    }
                }
            }
        };

        using MLBCommandManager manager = new(null, mlbClient);

        string result = await manager.ProcessCommandAsync("MLB-SCORES", DefaultContext);

        Assert.Contains("Live MLB Scores", result);
        Assert.Contains("Atlanta Braves", result);
        Assert.Contains("4 - 2", result);
    }

    [Fact]
    public async Task ProcessCommandAsync_MlbScores_WithOnlyScheduledGamesReturnsNoLiveMessage()
    {
        FakeMlbDataClient mlbClient = new FakeMlbDataClient
        {
            TodaysScheduleResponseResult = new ScheduleResponse
            {
                Dates = new List<ScheduleDate>
                {
                    new ScheduleDate
                    {
                        Games = new List<Game>
                        {
                            new Game
                            {
                                Status = new GameStatus { AbstractGameState = "Preview", DetailedState = "Scheduled" },
                                Teams = new GameTeams
                                {
                                    Away = new GameTeam { Team = new Team { Name = "Atlanta Braves" }, Score = 0 },
                                    Home = new GameTeam { Team = new Team { Name = "New York Mets" }, Score = 0 }
                                }
                            }
                        }
                    }
                }
            }
        };

        using MLBCommandManager manager = new(null, mlbClient);

        string result = await manager.ProcessCommandAsync("MLB-SCORES", DefaultContext);

        Assert.Contains("No live or completed games", result);
    }

    [Fact]
    public async Task ProcessCommandAsync_MlbStandings_WithDivisionFilterReturnsOnlyMatchingDivision()
    {
        FakeMlbDataClient mlbClient = new FakeMlbDataClient
        {
            CurrentStandingsResponseResult = new StandingsResponse
            {
                Records = new List<StandingRecord>
                {
                    new StandingRecord
                    {
                        Division = new Division { Name = "AL East" },
                        TeamRecords = new List<TeamRecord>
                        {
                            new TeamRecord { Team = new Team { Name = "Yankees" }, Wins = 10, Losses = 5, GamesBack = "-", WildCardGamesBack = "-" }
                        }
                    },
                    new StandingRecord
                    {
                        Division = new Division { Name = "NL East" },
                        TeamRecords = new List<TeamRecord>
                        {
                            new TeamRecord { Team = new Team { Name = "Braves" }, Wins = 9, Losses = 6, GamesBack = "-", WildCardGamesBack = "-" }
                        }
                    }
                }
            }
        };

        using MLBCommandManager manager = new(null, mlbClient);

        string result = await manager.ProcessCommandAsync("MLB-STANDINGS AL East", DefaultContext);

        Assert.Contains("AL East", result);
        Assert.DoesNotContain("NL East", result);
        Assert.Contains("Yankees", result);
    }

    [Fact]
    public async Task ProcessCommandAsync_MlbDivision_WithNameFiltersDivisionStandings()
    {
        FakeMlbDataClient mlbClient = new FakeMlbDataClient
        {
            CurrentStandingsResponseResult = new StandingsResponse
            {
                Records = new List<StandingRecord>
                {
                    new StandingRecord
                    {
                        Division = new Division { Id = DivisionIds.ALEast, Name = "AL East" },
                        League = new League { Id = LeagueIds.AmericanLeague, Name = "American League" },
                        Sport = new Sport { Id = SportIds.MLB, Name = "Major League Baseball" },
                        TeamRecords = new List<TeamRecord>
                        {
                            new TeamRecord { Team = new Team { Name = "Yankees" }, Wins = 10, Losses = 5, GamesBack = "-", WildCardGamesBack = "-" }
                        }
                    },
                    new StandingRecord
                    {
                        Division = new Division { Id = DivisionIds.NLEast, Name = "NL East" },
                        League = new League { Id = LeagueIds.NationalLeague, Name = "National League" },
                        Sport = new Sport { Id = SportIds.MLB, Name = "Major League Baseball" },
                        TeamRecords = new List<TeamRecord>
                        {
                            new TeamRecord { Team = new Team { Name = "Braves" }, Wins = 9, Losses = 6, GamesBack = "-", WildCardGamesBack = "-" }
                        }
                    }
                }
            }
        };

        using MLBCommandManager manager = new(null, mlbClient);

        string result = await manager.ProcessCommandAsync("MLB-DIVISION NL East", DefaultContext);

        Assert.Contains("NL East", result);
        Assert.Contains("Braves", result);
        Assert.DoesNotContain("AL East", result);
    }

    [Fact]
    public async Task ProcessCommandAsync_MlbLeague_WithNameFiltersLeagueStandings()
    {
        FakeMlbDataClient mlbClient = new FakeMlbDataClient
        {
            CurrentStandingsResponseResult = new StandingsResponse
            {
                Records = new List<StandingRecord>
                {
                    new StandingRecord
                    {
                        Division = new Division { Id = DivisionIds.ALEast, Name = "AL East" },
                        League = new League { Id = LeagueIds.AmericanLeague, Name = "American League" },
                        Sport = new Sport { Id = SportIds.MLB, Name = "Major League Baseball" },
                        TeamRecords = new List<TeamRecord>
                        {
                            new TeamRecord { Team = new Team { Name = "Yankees" }, Wins = 10, Losses = 5, GamesBack = "-", WildCardGamesBack = "-" }
                        }
                    },
                    new StandingRecord
                    {
                        Division = new Division { Id = DivisionIds.NLEast, Name = "NL East" },
                        League = new League { Id = LeagueIds.NationalLeague, Name = "National League" },
                        Sport = new Sport { Id = SportIds.MLB, Name = "Major League Baseball" },
                        TeamRecords = new List<TeamRecord>
                        {
                            new TeamRecord { Team = new Team { Name = "Braves" }, Wins = 9, Losses = 6, GamesBack = "-", WildCardGamesBack = "-" }
                        }
                    }
                }
            }
        };

        using MLBCommandManager manager = new(null, mlbClient);

        string result = await manager.ProcessCommandAsync("MLB-LEAGUE National League", DefaultContext);

        Assert.Contains("NL East", result);
        Assert.Contains("Braves", result);
        Assert.DoesNotContain("AL East", result);
    }

    [Fact]
    public async Task ProcessCommandAsync_MlbSport_WithNameMatchesAllMlbStandings()
    {
        FakeMlbDataClient mlbClient = new FakeMlbDataClient
        {
            CurrentStandingsResponseResult = new StandingsResponse
            {
                Records = new List<StandingRecord>
                {
                    new StandingRecord
                    {
                        Division = new Division { Id = DivisionIds.ALEast, Name = "AL East" },
                        League = new League { Id = LeagueIds.AmericanLeague, Name = "American League" },
                        Sport = new Sport { Id = SportIds.MLB, Name = "Major League Baseball" },
                        TeamRecords = new List<TeamRecord>
                        {
                            new TeamRecord { Team = new Team { Name = "Yankees" }, Wins = 10, Losses = 5, GamesBack = "-", WildCardGamesBack = "-" }
                        }
                    },
                    new StandingRecord
                    {
                        Division = new Division { Id = DivisionIds.NLEast, Name = "NL East" },
                        League = new League { Id = LeagueIds.NationalLeague, Name = "National League" },
                        Sport = new Sport { Id = SportIds.MLB, Name = "Major League Baseball" },
                        TeamRecords = new List<TeamRecord>
                        {
                            new TeamRecord { Team = new Team { Name = "Braves" }, Wins = 9, Losses = 6, GamesBack = "-", WildCardGamesBack = "-" }
                        }
                    }
                }
            }
        };

        using MLBCommandManager manager = new(null, mlbClient);

        string result = await manager.ProcessCommandAsync("MLB-SPORT MLB", DefaultContext);

        Assert.Contains("AL East", result);
        Assert.Contains("NL East", result);
        Assert.Contains("Yankees", result);
        Assert.Contains("Braves", result);
    }

    [Fact]
    public async Task ProcessCommandAsync_BravesSchedulerStatusAndDisable_UseSchedulerControl()
    {
        FakeBravesSchedulerControl scheduler = new FakeBravesSchedulerControl();
        using MLBCommandManager manager = new(scheduler, new FakeMlbDataClient());
        CommandContext guildContext = new("baseball", 123UL, false, "u1", "tester", 999UL, "Guild 999");

        string status = await manager.ProcessCommandAsync("BRAVES-SCHEDULER-STATUS", guildContext);
        string disable = await manager.ProcessCommandAsync("BRAVES-SCHEDULER-DISABLE", guildContext);

        Assert.Equal("enabled", status);
        Assert.Equal("Braves scheduler disabled for this server.", disable);
        Assert.Equal(999UL, scheduler.LastStatusGuildId);
        Assert.Equal(999UL, scheduler.LastDisableGuildId);
        Assert.True(scheduler.DisableCalled);
    }

    [Fact]
    public async Task ProcessCommandAsync_MlbHelp_ReturnsHelpText()
    {
        using MLBCommandManager manager = new(null, new FakeMlbDataClient());

        string result = await manager.ProcessCommandAsync("MLB-HELP", DefaultContext);

        Assert.Contains("MLB Stats Commands", result);
        Assert.Contains("!mlb-schedule", result);
    }

    [Fact]
    public async Task ProcessCommandAsync_BravesHelp_ReturnsHelpText()
    {
        using MLBCommandManager manager = new(null, new FakeMlbDataClient());

        string result = await manager.ProcessCommandAsync("BRAVES-HELP", DefaultContext);

        Assert.Contains("MLB Stats Commands", result);
        Assert.Contains("!braves-score", result);
    }

    [Fact]
    public async Task ProcessCommandAsync_UnknownMlbCommand_ReturnsEmptyString()
    {
        using MLBCommandManager manager = new(null, new FakeMlbDataClient());

        string result = await manager.ProcessCommandAsync("MLB-DOES-NOT-EXIST", DefaultContext);

        Assert.Equal(string.Empty, result);
    }

    [Fact]
    public async Task ProcessCommandAsync_WhenClientThrows_ReturnsTopLevelErrorMessage()
    {
        FakeMlbDataClient mlbClient = new FakeMlbDataClient
        {
            ThrowOnGetTodaysSchedule = true
        };

        using MLBCommandManager manager = new(null, mlbClient);

        string result = await manager.ProcessCommandAsync("MLB-SCHEDULE", DefaultContext);

        Assert.Contains("Error processing MLB command", result);
        Assert.Contains("boom", result);
    }

    private sealed class FakeBravesSchedulerControl : IBravesSchedulerControl
    {
        public bool EnableCalled { get; private set; }
        public bool DisableCalled { get; private set; }
        public string DailyResponse { get; set; } = "ok";
        public ulong LastEnableGuildId { get; private set; }
        public ulong LastDisableGuildId { get; private set; }
        public ulong LastStatusGuildId { get; private set; }
        public ulong LastDailyGuildId { get; private set; }
        public ulong LastWeeklyGuildId { get; private set; }

        public void Enable(ulong guildId)
        {
            EnableCalled = true;
            LastEnableGuildId = guildId;
        }

        public void Disable(ulong guildId)
        {
            DisableCalled = true;
            LastDisableGuildId = guildId;
        }

        public string GetStatus(ulong guildId)
        {
            LastStatusGuildId = guildId;
            return "enabled";
        }

        public Task<string> TriggerDailyPost(ulong guildId)
        {
            LastDailyGuildId = guildId;
            return Task.FromResult(DailyResponse);
        }

        public Task<string> TriggerWeeklyPost(ulong guildId)
        {
            LastWeeklyGuildId = guildId;
            return Task.FromResult("weekly");
        }
    }

    private sealed class FakeMlbDataClient : IMLBDataClient
    {
        public bool GetTodaysScheduleCalled { get; private set; }
        public bool GetScheduleCalled { get; private set; }
        public bool GetDivisionStandingsCalled { get; private set; }
        public bool GetTeamsCalled { get; private set; }
        public bool SearchPeopleCalled { get; private set; }
        public bool GetPersonCalled { get; private set; }
        public bool ThrowOnGetTodaysSchedule { get; set; }
        public string RosterJson { get; set; } = "{\"roster\":[]}";
        public Dictionary<string, string>? RosterJsonByType { get; set; }
        public ScheduleResponse? ScheduleResponseResult { get; set; }
        public ScheduleResponse? TodaysScheduleResponseResult { get; set; }
        public StandingsResponse? GetStandingsResult { get; set; }
        public StandingsResponse? DivisionStandingsResponseResult { get; set; }
        public StandingsResponse? CurrentStandingsResponseResult { get; set; }
        public TeamsResponse? TeamsResponseResult { get; set; }
        public string SearchPeopleResult { get; set; } = "{\"people\":[]}";
        public PeopleResponse? PersonResponseResult { get; set; }
        public Dictionary<int, PeopleResponse>? PersonResponseResultById { get; set; }
        public StatsResponse? PlayerStatsResponseResult { get; set; }

        public Task<ScheduleResponse?> GetScheduleAsync(string startDate, string endDate, int sportId, int? teamId = null)
        {
            GetScheduleCalled = true;
            return Task.FromResult(ScheduleResponseResult);
        }

        public Task<ScheduleResponse?> GetTodaysScheduleAsync(int sportId, int? teamId = null)
        {
            GetTodaysScheduleCalled = true;

            if (ThrowOnGetTodaysSchedule)
            {
                throw new InvalidOperationException("boom");
            }

            return Task.FromResult(TodaysScheduleResponseResult);
        }

        public Task<StandingsResponse?> GetStandingsAsync(int? leagueId, int season)
            => Task.FromResult(GetStandingsResult);

        public Task<StandingsResponse?> GetDivisionStandingsAsync(int divisionId, int season)
        {
            GetDivisionStandingsCalled = true;
            return Task.FromResult(DivisionStandingsResponseResult);
        }

        public Task<StandingsResponse?> GetCurrentStandingsAsync()
            => Task.FromResult(CurrentStandingsResponseResult);

        public Task<string> GetRosterAsync(int teamId, int? season = null, string rosterType = "active")
        {
            if (RosterJsonByType != null && RosterJsonByType.TryGetValue(rosterType, out string? rosterJson))
            {
                return Task.FromResult(rosterJson);
            }

            return Task.FromResult(RosterJson);
        }

        public Task<TeamsResponse?> GetTeamsAsync(int sportId, int? season = null)
        {
            GetTeamsCalled = true;
            return Task.FromResult(TeamsResponseResult);
        }

        public Task<GameResponse?> GetGameAsync(int gamePk)
            => Task.FromResult<GameResponse?>(null);

        public Task<string> SearchPeopleAsync(string playerName, int sportId)
        {
            SearchPeopleCalled = true;
            return Task.FromResult(SearchPeopleResult);
        }

        public Task<PeopleResponse?> GetPersonAsync(int playerId, int? season = null)
        {
            GetPersonCalled = true;

            if (PersonResponseResultById != null && PersonResponseResultById.TryGetValue(playerId, out PeopleResponse? mapped))
            {
                return Task.FromResult<PeopleResponse?>(mapped);
            }

            return Task.FromResult(PersonResponseResult);
        }

        public Task<StatsResponse?> GetPlayerStatsAsync(int playerId, int season)
            => Task.FromResult(PlayerStatsResponseResult);

        public void Dispose() { }
    }
}
