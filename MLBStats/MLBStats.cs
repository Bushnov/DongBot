using MLBAPI;
using MLBAPI.Models;

namespace MLBStatsBot
{
    public class MLBStats
    {
        private MLBData mlbData = new MLBData();

        public static readonly Dictionary<string, int> TeamIds = new Dictionary<string, int>()
        {
            { "Angels", 108 },
            { "DBacks", 109 },
            { "Orioles", 110 },
            { "RedSox", 111 },
            { "Cubs", 112 },
            { "Reds", 113 },
            { "Indians", 114 },
            { "Rockies", 115 },
            { "Tigers", 116 },
            { "Astros", 117 },
            { "Royals", 118 },
            { "Dodgers", 119 },
            { "Nationals", 120 },
            { "Mets", 121 },
            { "Athletics", 133 },
            { "Pirates", 134 },
            { "Padres", 135 },
            { "Mariners", 136 },
            { "Giants", 137 },
            { "Cardinals", 138 },
            { "Rays", 139 },
            { "Rangers", 140 },
            { "BlueJays", 141 },
            { "Twins", 142 },
            { "Phillies", 143 },
            { "Braves", 144 },
            { "WhiteSox", 145 },
            { "Marlins", 146 },
            { "Yankees", 147 },
            { "Brewers", 158 }
        };

        public static readonly Dictionary<string, int> RosterTypes = new Dictionary<string, int>()
        {
            { "RosterFull", 0 },
            { "Roster25", 1 },
            { "Roster40", 2 }
        };

        public async Task<string> GetScheduleAsync(DateTime date)
        {
            List<Schedule> schedule = new List<Schedule>(await mlbData.GetScheduleAsync(date));
            string formattedSchedule = "";
            foreach (Schedule s in schedule)
            {
                LiveGame lg = await mlbData.GetLiveGameAsync((int)s.gameID);
                DateTime localGameTime = DateTime.Parse(lg.gameTime);
                TimeZoneInfo tz = TimeZoneInfo.FindSystemTimeZoneById(lg.gameTimeZone);
                DateTime UTCGameTime = TimeZoneInfo.ConvertTimeToUtc(localGameTime);
                DateTime ESTGameTime = TimeZoneInfo.ConvertTimeFromUtc(UTCGameTime, TimeZoneInfo.Local);
                string game = s.HomeTeam + " at " + s.AwayTeam + ", " + ESTGameTime.ToString("hh:mm tt") + System.Environment.NewLine;
                formattedSchedule += game;
            }
            return formattedSchedule;
        }
    }
}