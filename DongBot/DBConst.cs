using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DongBot
{
    class DBConst
    {
        /**
         * TODOS::
         * - Change all of this to be in an XML file or a SQL table
         * - Add actions to add a new gif/gif command via a discord command
         * */
        public Dictionary<string, DongDictEntry> dongDict = new Dictionary<string, DongDictEntry>();

        public string[] dongArray =
            {
                "https://media.giphy.com/media/3MbeQGgcuHHdC/giphy.gif",
                "https://media.giphy.com/media/du9PSnZUzjOxsiCl1f/giphy.gif",
                "https://media.giphy.com/media/LrR8lk4mjCRhZKLAfk/giphy.gif",
                "https://media.giphy.com/media/HAd2zDwqOa5MY/giphy.gif",
                "https://tenor.com/r7Hq.gif",
                "https://gfycat.com/frankwhoppinggiantschnauzer",
                "https://media1.tenor.com/images/cde146841a3bb1992d494c6f3897318e/tenor.gif?itemid=7307130",
                "https://gfycat.com/composedfarflungbernesemountaindog"
            };

        public string[] dingArray =
            {
                "https://tenor.com/view/drifter-destiny2-ding-tag-flick-gif-16330328",
                "https://tenor.com/view/ding-gif-9621380",
                "https://tenor.com/view/lost-in-translation-lost-in-translation-gifs-bill-murray-smooth-drink-gif-8607826",
                "https://tenor.com/view/carl-weathers-ding-ding-apollo-creed-boxer-gif-9214834",
                "https://tenor.com/view/right-cats-kitties-yes-ring-gif-12003347",
                "https://tenor.com/view/ring-bell-dog-cute-seeking-attention-ding-gif-13497885",
                "https://tenor.com/view/ringthatbell-vs-gbell-ding-ding-the-bell-gif-14783578",
                "https://giphy.com/gifs/KiaundraJackson-noise-ding-kiaundra-jackson-hdP8JpVJXMTGvazIlx"
            };

        public string[] gamedayArray =
            {
                "https://media.giphy.com/media/U4k5p9obbDgmRug8aH/giphy.gif",
                "https://media.giphy.com/media/KCehE3BnlHaaV75PVg/giphy.gif",
                "https://media.giphy.com/media/J26b5vwXFaVfWrQ5JD/giphy.gif",
                "https://media.giphy.com/media/fS3lHlhqJZ5FWa0EiO/giphy.gif",
                "https://media.giphy.com/media/l2ZE8NaNqzz8W05JC/giphy.gif",
                "https://media.giphy.com/media/Xa4eNNMfkNfihYG0ab/giphy.gif"
            };

        public string[] dumpsterArray =
            {
                "https://media.tenor.com/images/3ed4d9f441ec47208de930335085614b/tenor.gif",
                "https://media.tenor.com/images/fed9f36ef5b78727e0e272bcf9ef68d9/tenor.gif",
                "https://media.tenor.com/images/2f402d78d55c3822f0bad1b6a975cf9f/tenor.gif",
                "https://media4.giphy.com/media/Jrl4FlTaymFFbNiwU5/giphy.gif",
                "https://reactions.gifs.ninja/r/d942fb461b07fde1dc08.gif",
                "https://media1.giphy.com/media/yfEjNtvqFBfTa/200w.gif"
            };

        public string[] guzArray =
            {
                "https://imgur.com/gallery/gGKKfEb?nc=1",
                "https://images.photowall.com/products/58209/shades-of-red-brick-wall.jpg?h=699&q=85",
                "https://giphy.com/gifs/mashable-26xBFb8wtN5R0iyAM",
                "https://d3hne3c382ip58.cloudfront.net/files/uploads/bookmundi/resized/cmsfeatured/visiting-the-great-wall-of-china-featured-image-1560845062-785X440.jpg",
                "https://upload.wikimedia.org/wikipedia/en/1/13/PinkFloydWallCoverOriginalNoText.jpg",
                "https://i0.wp.com/24.media.tumblr.com/5c78051b1c751172d691c1c1fa3389db/tumblr_mi5w2kcVjk1rjrvmvo7_250.gif?resize=245%2C245"
            };

        public string[] boopArray =
            {
                "https://tenor.com/view/boop-deadpool-negasonic-teenage-warhead-brianna-hildebrand-gif-14216702",
                "https://tenor.com/view/archer-lana-boop-poke-poking-gif-5754722",
                "https://tenor.com/view/boop-snoot-boop-the-snoot-nose-boop-nose-gif-17934128",
                "https://tenor.com/view/boop-elephant-giraffe-playing-animals-gif-16798070",
                "https://tenor.com/view/archer-fx-boop-adam-reed-nose-gif-4228661",
                "https://tenor.com/view/boop-et-extra-terrestrial-gif-12066576",
                "https://tenor.com/view/boop-boopboopboop-bestfriends-superbad-gif-5041988"
            };

        public string[] mymanArray =
            {
                "https://tenor.com/view/my-man-denzel-washington-chuckles-hehe-gif-5032462",
                "https://tenor.com/view/steve-harvey-my-man-gif-11918627",
                "https://tenor.com/view/swanson-my-man-fist-pump-bump-gif-10597658",
                "https://tenor.com/view/the-interview-ames-franco-seth-rogen-my-fucking-man-high-five-gif-4094164",
                "https://tenor.com/view/myman-rickandmorty-gif-5032364",
                "https://tenor.com/view/myman-fistbump-gif-5032427",
                "https://tenor.com/view/my-man-my-main-man-nathaniel-taylor-rollo-larson-sanford-and-son-thats-my-boy-gif-17869407"
            };

        //private string[] freeArray =
        //    {
        //        "https://i.gifer.com/UfTe.gif",
        //        "https://thumbs.gfycat.com/RightIllfatedFruitfly-size_restricted.gif",
        //        "https://lh3.googleusercontent.com/proxy/hwtoN45oQiRTuslIfQWiaxnurt3n9unOZBpGDEVNQh8Bs1th-lcNcDZF5N4BIWf1AgQQ5SYLG7nD-FhLdgYqCdd1TCA4aP-UH0T_K3jChLtESnbxh_LZbKxgRHwYZ2WrRQ",
        //        "https://fansided.com/files/2014/05/PiutOvU.gif",
        //        "https://tenor.com/view/atlanta-braves-freddie-freeman-braves-chopon-dance-gif-14645116",
        //        "https://tenor.com/view/freddie-freeman-dinger-fredward-baseball-sport-gif-17857667",
        //        "https://tenor.com/view/freddie-freeman-braves-baseball-sunflower-drink-gif-12553532",
        //        "https://tenor.com/view/chop-on-atlanta-braves-chop-freddie-freeman-walk-off-gif-14162373",
        //        "https://tenor.com/view/freddie-freeman-braves-atlanta-gif-13390407"
        //    };

        public string[] jiggyArray =
            {
                "https://www.youtube.com/watch?v=3JcmQONgXJM"
            };

        public string[] duvallArray =
            {
                "https://cdn.discordapp.com/attachments/493515081506750485/965421908512350238/ezgif-3-eb8cd00053.gif"
            };

        public string[] noiceArray =
            {
                "https://tenor.com/view/nice-nooice-bling-key-and-peele-gif-4294979",
                "https://tenor.com/view/noice-brooklyn-ninenine-b99-smile-gif-11928987"
            };

        public string[] dongerArray =
            {
                "https://giphy.com/gifs/workaholics-season-5-episode-1-l0ErOPdvIk9Zq1YfS",
                "https://tenor.com/view/donkey-kong-intensify-shaking-gif-14723368",
                "https://tenor.com/view/its-huge-sup-joseph-donovan-hudson-and-rex-large-big-gif-23978872",
                "https://tenor.com/view/donger-thats-a-donger-slade-bulge-gif-13294282"
            };

        public string[] dongestArray =
            {
                "https://tenor.com/view/dong-dongs-gif-18220383",
                "https://tenor.com/view/fake-penis-morph-gif-13172581"
            };

        public string[] salamiArray =
            {
                "https://tenor.com/view/wcth-elizabeth-hearties-what-a-salami-gif-13499385",
                "https://tenor.com/view/embarrassed-panda-oh-god-holy-salami-hide-face-gif-17443872",
                "https://giphy.com/gifs/splat-nicksplat-rockos-modern-life-l4EpkZThLWJrqkfVm",
                "https://giphy.com/gifs/Marcher-at-salami-wurst-loidl-QxApjrdi9RjgpbQVeB"
            };

        public string[] sweepArray =
            {
                "https://tenor.com/view/mrs-doubtfire-cleaning-dancing-happy-gif-15445847",
                "https://tenor.com/view/sweeping-cleaning-mopping-mr-clean-gif-17663331",
                "https://tenor.com/view/broom-sweep-patrick-fail-stupid-gif-5417128",
                "https://tenor.com/view/sweep-padres-gif-22851806"
            };

        public string[] washArray =
            {
                "https://tenor.com/view/ron-washington-gif-23550421",
                "https://tenor.com/view/ron-washington-gif-25561091",
                "https://tenor.com/view/ron-washington-atlanta-braves-shake-gif-27636305",
                "https://tenor.com/view/ron-washington-william-contreras-salute-gif-25697516",
                "https://giphy.com/gifs/mlb-sports-braves-ron-washington-hHMd9LPgG5fDhBIFhy",
                "https://giphy.com/gifs/mlb-sports-ron-washington-send-em-kIJsORuundDCPkzv9o",
                "https://giphy.com/gifs/mlb-sports-baseball-joc-pederson-x0qVkQfe69H4g06lyT",
                "https://giphy.com/gifs/mlb-laughing-washington-ron-h2UDgOSXdLqRN7E7h9"
            };

        public string[] thicccArray =
            {
                "https://tenor.com/view/extra-thicc-extra-thick-thicc-thick-aku-gif-21813785",
                "https://tenor.com/view/thiccest-donut-media-james-pumphrey-round-gif-17411561",
                "https://tenor.com/view/thicc-king-andre-braugher-raymond-holt-brooklyn-nine-nine-thick-gif-16913349",
                "https://tenor.com/view/brandon-herrera-ak50-thicc-damn-rear-trunnion-gif-19257419",
                "https://tenor.com/view/thick-thicker-bowl-oatmeal-point-gif-5457950",
                "https://tenor.com/view/gettin-thicker-and-thicker-thick-yas-queen-slay-shake-it-gif-14049394",
                "https://giphy.com/gifs/hyperrpg-twitch-rat-queens-ratqueens-kcOVt7d7hcUVkADVS1",
                "https://giphy.com/gifs/vh1-26BRvneVlfR2MMyuQ",
                "https://giphy.com/gifs/daveonfxx-thicc-thiccness-advanced-3tK0Yo2rs1nkSqUq9m"
            };

        public string[] tootArray =
            {
                "https://tenor.com/view/toot-tin-man-wizard-of-oz-gif-15462159",
                "https://tenor.com/view/toot-toot-bitch-know-it-all-show-off-arrogant-hater-gif-15549253",
                "https://tenor.com/view/jake-peralta-brooklyn-nine-nine-nbc-toot-b99-gif-16001901",
                "https://tenor.com/view/jake-peralta-brooklyn-nine-nine-nbc-toot-b99-gif-16001901",
                "https://giphy.com/gifs/drunkhistory-comedy-central-drunk-history-IeG3YafCD9Drk2y3ZB"
            };

        public string[] dingdingdingArray =
            {
                "https://tenor.com/view/tyler-hynes-hynies-ding-ding-ding-we-have-a-winner-its-christmas-eve-gif-19820176",
                "https://tenor.com/view/jim-carrey-liar-ding-ding-ding-what-do-we-have-for-her-gif-15762298",
                "https://tenor.com/view/better-call-saul-don-eladio-gif-26390716",
                "https://giphy.com/gifs/survivorau-survivor-kgrCxsTQFj0HAh2TUG",
                "https://giphy.com/gifs/clawstnt-bryce-uncle-daddy-quiet-ann-rqMxQEZlney9bsVGtH"
            };

        public string[] molsonArray =
            {
                "https://tenor.com/view/beer-alcohol-thirsty-molson-canadian-canadian-beer-gif-19540823",
                "https://tenor.com/view/beer-labatts-beer-canadian-beer-schooner-beer-boddingtons-beer-gif-20482782",
                "https://giphy.com/gifs/workaholics-comedy-central-season-2-episode-9-l0ErOxnmLuj9pMIvu",
                "https://giphy.com/gifs/MacGruber-macgruber-show-series-v0SoH4cES4qJ77PaTi"
            };

        public DBConst()
        {
            dongDict = new Dictionary<string, DongDictEntry>
                {
                    { "DONG", new DongDictEntry("baseball" , dongArray) },
                    { "DING", new DongDictEntry("baseball" , dingArray) },
                    { "GAMEDAY", new DongDictEntry("college-sports" , gamedayArray) },
                    { "DUMPSTERFIRE", new DongDictEntry("" , dumpsterArray) },
                    { @"GU+Z$", new DongDictEntry("mls" , guzArray) },
                    { "BOOP", new DongDictEntry("baseball" , boopArray) },
                    { "NOICE", new DongDictEntry("" , noiceArray) },
                    { "MYMAN", new DongDictEntry("" , mymanArray) },
                    { @"DU+V+A+L+$", new DongDictEntry("baseball" , duvallArray) },
                    { "JIGGY", new DongDictEntry("baseball" , jiggyArray) },
                    { "DONGER", new DongDictEntry("baseball" , dongerArray) },
                    { "DONGEST", new DongDictEntry("baseball" , dongestArray) },
                    { "SALAMI", new DongDictEntry("baseball" , salamiArray) },
                    { "SWEEP", new DongDictEntry("baseball" , sweepArray) },
                    { "WASH", new DongDictEntry("baseball" , washArray) },
                    { "WINDMILL", new DongDictEntry("baseball" , washArray) },
                    { @"THICC+$", new DongDictEntry("baseball" , thicccArray) },
                    { "TOOT", new DongDictEntry("baseball" , tootArray) },
                    { "DINGDINGDING", new DongDictEntry("baseball" , dingdingdingArray) },
                    { "MOLSON", new DongDictEntry("baseball" , molsonArray) }
                };
        }

        public class DongDictEntry
        {
            public string channel;
            public string[] gifArray;

            public DongDictEntry(string channel, string[] gifArray)
            {
                this.channel = channel;
                this.gifArray = gifArray;
            }
        }

    }
}
