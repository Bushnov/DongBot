using Discord;
using Discord.WebSocket;
using System;
using System.IO;
using System.Reflection.Metadata.Ecma335;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using MLBStatsBot;

namespace DongBot
{
    class MainDong
    {
        public static void Main(string[] args)
        => new MainDong().MainAsync().GetAwaiter().GetResult();

        private DiscordSocketClient _client;
        private DiscordSocketConfig _config;
        private Random rand = new Random();
        private DBActions DBActions = new DBActions();
        private DBConst DBConst = new DBConst();

        public async Task MainAsync()
        {
            this._config = new DiscordSocketConfig()
            {
                GatewayIntents = GatewayIntents.All
            };
            this._client = new DiscordSocketClient(_config);
            this._client.MessageReceived += this.CommandHandler;
            this._client.Log += this.Log;

            //  You can assign your bot token to a string, and pass that in to connect.
            //  This is, however, insecure, particularly if you plan to have your code hosted in a public repository.
            string token = File.ReadAllText("token.txt");

            //  Some alternate options would be to keep your token in an Environment Variable or a standalone file.
            //  var token = Environment.GetEnvironmentVariable("NameOfYourEnvironmentVariable");
            //  var token = File.ReadAllText("token.txt");
            //  var token = JsonConvert.DeserializedObject<AConfigurationClass>(File.ReadAllText("config.json")).Token;

            await this._client.LoginAsync(TokenType.Bot, token);
            await this._client.StartAsync();

            //  Block this task until the program is closed.
            await Task.Delay(-1);
        }

        private Task Log(LogMessage msg)
        {
            Console.WriteLine(msg.ToString());
            return Task.CompletedTask;
        }

        private async Task<Task> CommandHandler(SocketMessage message)
        {
            MLBStats stats = new MLBStats();

            // Filter out if the message is a command or not
            string command = DBActions.FilterMessage(message, '!');
            if (command == "")
            {
                return Task.CompletedTask;
            }

            // Process Gif Commands
            string gifCommand = DBActions.DongGifs(command, message.Channel.Name);
            if (!gifCommand.Equals(""))
            {
                await message.Channel.SendMessageAsync(gifCommand);
            }

            /**
            //Commands begin here
            //DONGS
            if (command.ToUpper().Equals("DONG") &&
                message.Channel.Name.Equals("baseball"))
            {
                int randomDong = this.rand.Next(DBConst.dongArray.Length);
                string dongGif = DBConst.dongArray[randomDong];
                await message.Channel.SendMessageAsync(dongGif);
            }

            //DINGS
            if (command.ToUpper().Equals("DING") &&
                message.Channel.Name.Equals("baseball"))
            {
                int randomDing = this.rand.Next(DBConst.dingArray.Length);
                string dingGif = DBConst.dingArray[randomDing];
                await message.Channel.SendMessageAsync(dingGif);
            }

            //GAMEDAY
            if (command.ToUpper().Equals("GAMEDAY") &&
                message.Channel.Name.Equals("college-sports"))
            {
                int randomGameday = this.rand.Next(DBConst.gamedayArray.Length);
                string gamedayGif = DBConst.gamedayArray[randomGameday];
                await message.Channel.SendMessageAsync(gamedayGif);
            }

            //Dumpster Fire
            if (command.ToUpper().Equals("DUMPSTERFIRE"))
            {
                int randomDumpster = this.rand.Next(DBConst.dumpsterArray.Length);
                string dumpsterGif = DBConst.dumpsterArray[randomDumpster];
                await message.Channel.SendMessageAsync(dumpsterGif);
            }

            //Guz
            if (Regex.IsMatch(command.ToUpper(), @"GU+Z$") &&
                message.Channel.Name.Equals("mls"))
            {
                int randomGuz = this.rand.Next(DBConst.guzArray.Length);
                string guzGif = DBConst.guzArray[randomGuz];
                await message.Channel.SendMessageAsync(guzGif);
            }

            //Boops
            if (command.ToUpper().Equals("BOOP") &&
                message.Channel.Name.Equals("baseball"))
            {
                int randomBoop = this.rand.Next(DBConst.boopArray.Length);
                string boopGif = DBConst.boopArray[randomBoop];
                await message.Channel.SendMessageAsync(boopGif);
            }

            //Noice
            if (command.ToUpper().Equals("NOICE"))
            {
                await message.Channel.SendMessageAsync("https://tenor.com/view/nice-nooice-bling-key-and-peele-gif-4294979");
            }

            //My Man
            if (command.ToUpper().Equals("MYMAN"))
            {
                int randomMyman = this.rand.Next(DBConst.mymanArray.Length);
                string mymanGif = DBConst.mymanArray[randomMyman];
                await message.Channel.SendMessageAsync(mymanGif);
            }

            //MVFREE
            //if (command.ToUpper().Equals("MVFREE") &&
            //    message.Channel.Name.Equals("baseball"))
            //{
            //    int randomFree = this.rand.Next(this.freeArray.Length);
            //    string freeGif = this.freeArray[randomFree];
            //    await message.Channel.SendMessageAsync(freeGif);
            //}

            //Duball
            if ( Regex.IsMatch(command.ToUpper(), @"DU+V+A+L+$") &&
                message.Channel.Name.Equals("baseball") )
            {
                int randomDuvall = this.rand.Next(DBConst.guzArray.Length);
                string duvallGif = DBConst.duvallArray[ 0 ];
                await message.Channel.SendMessageAsync(duvallGif);
            }

            //JIGGY
            if ( command.ToUpper().Equals("JIGGY") &&
                message.Channel.Name.Equals("baseball") )
            {
                string jiggyGif = DBConst.jiggyArray[ 0 ];
                await message.Channel.SendMessageAsync(jiggyGif);
            }

            */


            //SCHEDULE
            if ( command.ToUpper().StartsWith("MLB-SCHEDULE") &&
                message.Channel.Name.Equals("baseball"))
            {
                string date = command.Split('-')[ 2 ];
                string schedule = await stats.GetScheduleAsync(DateTime.Parse(date));
                await message.Channel.SendMessageAsync(schedule);
            }


            return Task.CompletedTask;
        }

    }
}
