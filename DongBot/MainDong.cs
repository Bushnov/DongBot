using Discord;
using Discord.WebSocket;
using System;
using System.IO;
using System.Reflection.Metadata.Ecma335;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace DongBot
{
    class MainDong
    {
        public static void Main(string[] args)
        => new MainDong().MainAsync().GetAwaiter().GetResult();

        private DiscordSocketClient _client;
        private Random rand = new Random();
        private string[] dongArray = 
            {
                "https://media.giphy.com/media/3MbeQGgcuHHdC/giphy.gif",
                "https://media.giphy.com/media/du9PSnZUzjOxsiCl1f/giphy.gif",
                "https://media.giphy.com/media/LrR8lk4mjCRhZKLAfk/giphy.gif",
                "https://media.giphy.com/media/HAd2zDwqOa5MY/giphy.gif",
                "https://tenor.com/r7Hq.gif",
                "https://gfycat.com/frankwhoppinggiantschnauzer",
                "https://media1.tenor.com/images/cde146841a3bb1992d494c6f3897318e/tenor.gif?itemid=7307130"
            };

        private string[] dingArray =
            {
                "https://tenor.com/view/drifter-destiny2-ding-tag-flick-gif-16330328",
                "https://tenor.com/view/ding-gif-9621380",
                "https://tenor.com/view/lost-in-translation-lost-in-translation-gifs-bill-murray-smooth-drink-gif-8607826",
                "https://tenor.com/view/carl-weathers-ding-ding-apollo-creed-boxer-gif-9214834",
                "https://tenor.com/view/right-cats-kitties-yes-ring-gif-12003347",
                "https://tenor.com/view/ring-bell-dog-cute-seeking-attention-ding-gif-13497885",
                "https://tenor.com/view/ringthatbell-vs-gbell-ding-ding-the-bell-gif-14783578",
            };

        private string[] gamedayArray =
            {
                "https://media.giphy.com/media/U4k5p9obbDgmRug8aH/giphy.gif",
                "https://media.giphy.com/media/KCehE3BnlHaaV75PVg/giphy.gif",
                "https://media.giphy.com/media/J26b5vwXFaVfWrQ5JD/giphy.gif",
                "https://media.giphy.com/media/fS3lHlhqJZ5FWa0EiO/giphy.gif",
                "https://media.giphy.com/media/l2ZE8NaNqzz8W05JC/giphy.gif",
                "https://media.giphy.com/media/Xa4eNNMfkNfihYG0ab/giphy.gif"
            };

        private string[] dumpsterArray =
            {
                "https://media.tenor.com/images/3ed4d9f441ec47208de930335085614b/tenor.gif",
                "https://media.tenor.com/images/fed9f36ef5b78727e0e272bcf9ef68d9/tenor.gif",
                "https://media.tenor.com/images/2f402d78d55c3822f0bad1b6a975cf9f/tenor.gif",
                "https://media4.giphy.com/media/Jrl4FlTaymFFbNiwU5/giphy.gif",
                "https://reactions.gifs.ninja/r/d942fb461b07fde1dc08.gif",
                "https://media1.giphy.com/media/yfEjNtvqFBfTa/200w.gif"
            };

        private string[] guzArray =
            {
                "https://imgur.com/gallery/gGKKfEb?nc=1",
                "https://images.photowall.com/products/58209/shades-of-red-brick-wall.jpg?h=699&q=85",
                "https://giphy.com/gifs/mashable-26xBFb8wtN5R0iyAM",
                "https://d3hne3c382ip58.cloudfront.net/files/uploads/bookmundi/resized/cmsfeatured/visiting-the-great-wall-of-china-featured-image-1560845062-785X440.jpg",
                "https://upload.wikimedia.org/wikipedia/en/1/13/PinkFloydWallCoverOriginalNoText.jpg",
                "https://i0.wp.com/24.media.tumblr.com/5c78051b1c751172d691c1c1fa3389db/tumblr_mi5w2kcVjk1rjrvmvo7_250.gif?resize=245%2C245"
            };

        private string[] boopArray =
            {
                "https://tenor.com/view/boop-deadpool-negasonic-teenage-warhead-brianna-hildebrand-gif-14216702",
                "https://tenor.com/view/archer-lana-boop-poke-poking-gif-5754722",
                "https://tenor.com/view/boop-snoot-boop-the-snoot-nose-boop-nose-gif-17934128",
                "https://tenor.com/view/boop-elephant-giraffe-playing-animals-gif-16798070",
                "https://tenor.com/view/archer-fx-boop-adam-reed-nose-gif-4228661",
                "https://tenor.com/view/boop-et-extra-terrestrial-gif-12066576",
                "https://tenor.com/view/boop-boopboopboop-bestfriends-superbad-gif-5041988"
            };

        private string[] mymanArray =
            {
                "https://tenor.com/view/my-man-denzel-washington-chuckles-hehe-gif-5032462",
                "https://tenor.com/view/steve-harvey-my-man-gif-11918627",
                "https://tenor.com/view/swanson-my-man-fist-pump-bump-gif-10597658",
                "https://tenor.com/view/the-interview-ames-franco-seth-rogen-my-fucking-man-high-five-gif-4094164",
                "https://tenor.com/view/myman-rickandmorty-gif-5032364",
                "https://tenor.com/view/myman-fistbump-gif-5032427",
                "https://tenor.com/view/my-man-my-main-man-nathaniel-taylor-rollo-larson-sanford-and-son-thats-my-boy-gif-17869407"
            };

        private string[] freeArray =
            {
                "https://i.gifer.com/UfTe.gif",
                "https://thumbs.gfycat.com/RightIllfatedFruitfly-size_restricted.gif",
                "https://lh3.googleusercontent.com/proxy/hwtoN45oQiRTuslIfQWiaxnurt3n9unOZBpGDEVNQh8Bs1th-lcNcDZF5N4BIWf1AgQQ5SYLG7nD-FhLdgYqCdd1TCA4aP-UH0T_K3jChLtESnbxh_LZbKxgRHwYZ2WrRQ",
                "https://fansided.com/files/2014/05/PiutOvU.gif",
                "https://tenor.com/view/atlanta-braves-freddie-freeman-braves-chopon-dance-gif-14645116",
                "https://tenor.com/view/freddie-freeman-dinger-fredward-baseball-sport-gif-17857667",
                "https://tenor.com/view/freddie-freeman-braves-baseball-sunflower-drink-gif-12553532",
                "https://tenor.com/view/chop-on-atlanta-braves-chop-freddie-freeman-walk-off-gif-14162373",
                "https://tenor.com/view/freddie-freeman-braves-atlanta-gif-13390407"
            };

        private string[] jiggyArray =
            {
                "https://www.youtube.com/watch?v=3JcmQONgXJM"
            };

        public async Task MainAsync()
        {
            this._client = new DiscordSocketClient();
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

        private Task CommandHandler(SocketMessage message)
        {
            string command = "";
            int lengthOfCommand = -1;

            //Filtering messages begin here
            if ( !message.Content.StartsWith('!') ) //This is your prefix
            {
                return Task.CompletedTask;
            }

            if ( message.Author.IsBot ) //This ignores all bots
            {
                return Task.CompletedTask;
            }

            if ( message.Content.Contains(' ') )
            {
                lengthOfCommand = message.Content.IndexOf(' ');
            }
            else
            {
                lengthOfCommand = message.Content.Length;
            }

            command = message.Content.Substring(1, lengthOfCommand - 1);


            //Commands begin here
            //DONGS
            if (command.ToUpper().Equals("DONG") &&
                message.Channel.Name.Equals("baseball"))
            {
                int randomDong = this.rand.Next(this.dongArray.Length);
                string dongGif = this.dongArray[randomDong];
                message.Channel.SendMessageAsync(dongGif);
            }

            //DINGS
            if (command.ToUpper().Equals("DING") &&
                message.Channel.Name.Equals("baseball"))
            {
                int randomDing = this.rand.Next(this.dingArray.Length);
                string dingGif = this.dingArray[randomDing];
                message.Channel.SendMessageAsync(dingGif);
            }

            //GAMEDAY
            if (command.ToUpper().Equals("GAMEDAY") &&
                message.Channel.Name.Equals("college-sports"))
            {
                int randomGameday = this.rand.Next(this.gamedayArray.Length);
                string gamedayGif = this.gamedayArray[randomGameday];
                message.Channel.SendMessageAsync(gamedayGif);
            }

            //Dumpster Fire
            if (command.ToUpper().Equals("DUMPSTERFIRE"))
            {
                int randomDumpster = this.rand.Next(this.dumpsterArray.Length);
                string dumpsterGif = this.dumpsterArray[randomDumpster];
                message.Channel.SendMessageAsync(dumpsterGif);
            }

            //Guz
            if (Regex.IsMatch(command.ToUpper(), @"GU+Z$") &&
                message.Channel.Name.Equals("mls"))
            {
                int randomGuz = this.rand.Next(this.guzArray.Length);
                string guzGif = this.guzArray[randomGuz];
                message.Channel.SendMessageAsync(guzGif);
            }

            //Boops
            if (command.ToUpper().Equals("BOOP") &&
                message.Channel.Name.Equals("baseball"))
            {
                int randomBoop = this.rand.Next(this.boopArray.Length);
                string boopGif = this.boopArray[randomBoop];
                message.Channel.SendMessageAsync(boopGif);
            }

            //Noice
            if (command.ToUpper().Equals("NOICE"))
            {
                message.Channel.SendMessageAsync("https://tenor.com/view/nice-nooice-bling-key-and-peele-gif-4294979");
            }

            //My Man
            if (command.ToUpper().Equals("MYMAN"))
            {
                int randomMyman = this.rand.Next(this.mymanArray.Length);
                string mymanGif = this.mymanArray[randomMyman];
                message.Channel.SendMessageAsync(mymanGif);
            }

            //MVFREE
            if (command.ToUpper().Equals("MVFREE") &&
                message.Channel.Name.Equals("baseball"))
            {
                int randomFree = this.rand.Next(this.freeArray.Length);
                string freeGif = this.freeArray[randomFree];
                message.Channel.SendMessageAsync(freeGif);
            }

            //JIGGY
            if ( command.ToUpper().Equals("JIGGY") &&
                message.Channel.Name.Equals("baseball") )
            {
                string jiggyGif = this.jiggyArray[ 0 ];
                message.Channel.SendMessageAsync(jiggyGif);
            }


            return Task.CompletedTask;
        }

    }
}
