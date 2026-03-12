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

        string result = await manager.ProcessCommandAsync("BRAVES-SCHEDULER-ENABLE", DefaultContext);

        Assert.True(scheduler.EnableCalled);
        Assert.Equal("Braves scheduler enabled.", result);
    }

    [Fact]
    public async Task ProcessCommandAsync_BravesSchedulerTestDaily_ReturnsSchedulerMessage()
    {
        FakeBravesSchedulerControl scheduler = new FakeBravesSchedulerControl { DailyResponse = "daily-ok" };
        using MLBCommandManager manager = new(scheduler, new FakeMlbDataClient());

        string result = await manager.ProcessCommandAsync("BRAVES-SCHEDULER-TEST-DAILY", DefaultContext);

        Assert.Equal("daily-ok", result);
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

        string status = await manager.ProcessCommandAsync("BRAVES-SCHEDULER-STATUS", DefaultContext);
        string disable = await manager.ProcessCommandAsync("BRAVES-SCHEDULER-DISABLE", DefaultContext);

        Assert.Equal("enabled", status);
        Assert.Equal("Braves scheduler disabled.", disable);
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

        public void Enable() => EnableCalled = true;
        public void Disable() => DisableCalled = true;
        public string GetStatus() => "enabled";
        public Task<string> TriggerDailyPost() => Task.FromResult(DailyResponse);
        public Task<string> TriggerWeeklyPost() => Task.FromResult("weekly");
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
        public ScheduleResponse? ScheduleResponseResult { get; set; }
        public ScheduleResponse? TodaysScheduleResponseResult { get; set; }
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

        public Task<StandingsResponse?> GetDivisionStandingsAsync(int divisionId, int season)
        {
            GetDivisionStandingsCalled = true;
            return Task.FromResult(DivisionStandingsResponseResult);
        }

        public Task<StandingsResponse?> GetCurrentStandingsAsync()
            => Task.FromResult(CurrentStandingsResponseResult);

        public Task<string> GetRosterAsync(int teamId, int? season = null, string rosterType = "active")
            => Task.FromResult(RosterJson);

        public Task<TeamsResponse?> GetTeamsAsync(int sportId)
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

        public Task<PeopleResponse?> GetPersonAsync(int playerId)
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
