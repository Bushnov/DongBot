using System;
using System.IO;
using System.Reflection.Metadata.Ecma335;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using MLBAPI;
using MLBAPI.Constants;
using MLBAPI.Models;
using MLBStatsBot;

namespace DongBot
{
    class LocalTestConsole
    {

        public static async Task Main()
        {
            //MLBData mlb = new MLBData();
            //IEnumerable<TeamRoster>? roster = await mlb.GetTeamRosterAsync(eTeamId.Braves, 2021, new DateTime(2021, 7, 4), eRosterType.roster25);
            //int x = 1;
            MLBStats stats = new MLBStats();
            string sched = await stats.GetScheduleAsync(DateTime.Parse("09APR2022"));
            Console.WriteLine(sched);
        }
    }
}