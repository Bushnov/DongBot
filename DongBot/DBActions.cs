using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Discord.WebSocket;
using MLBStatsBot;

namespace DongBot
{
    class DBActions
    {

        /**
         * TODOS::
         * - Add commands for MLB API
         * - Add actions to add a new gif/gif command via a discord command
         * - Add a wrapper function for all of the actions here so that all of them are private and used by a single public call
         * */

        private Random rand = new Random();
        private DBConst DBConst = new DBConst();

        public string FilterMessage(SocketMessage message, char prefix)
        {
            int lengthOfCommand;

            //Filtering messages begin here
            if (!message.Content.StartsWith(prefix)) //This is your prefix
            {
                return "";
            }

            if (message.Author.IsBot) //This ignores all bots
            {
                return "";
            }

            lengthOfCommand = message.Content.Contains(' ') ? message.Content.IndexOf(' ') : message.Content.Length;

            return message.Content.Substring(1, lengthOfCommand - 1);
        }

        public string DongGifs(string command, string channel)
        {
            foreach (string key in DBConst.dongDict.Keys)
            {
                if (Regex.IsMatch(command.ToUpper(), @"^" + key + @"$") )
                {
                    string dictChannel = DBConst.dongDict[key].channel;
                    if (dictChannel.Equals(channel) || dictChannel.Equals(""))
                    {
                        string[] dongDictArray = DBConst.dongDict[key].gifArray;
                        return dongDictArray[rand.Next(dongDictArray.Length)];
                    }
                    return "";
                }
            }
            return "";
        }



    }
}
