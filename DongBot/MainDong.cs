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
        // Admin channel name - only administrative commands can be run here
        private const string ADMIN_CHANNEL_NAME = "dongbot-admin";

        public static void Main(string[] args)
        => new MainDong().MainAsync().GetAwaiter().GetResult();

        private DiscordSocketClient _client;
        private DiscordSocketConfig _config;
        private Random rand = new Random();
        private DBActions DBActions = new DBActions();

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

        /// <summary>
        /// Check if a command is being run in the admin channel
        /// </summary>
        private bool IsAdminChannel(string channelName)
        {
            return channelName.Equals(ADMIN_CHANNEL_NAME, StringComparison.OrdinalIgnoreCase);
        }

        private async Task<Task> CommandHandler(SocketMessage message)
        {
            // Filter out if the message is a command or not
            string command = DBActions.FilterMessage(message, '!');
            if (command == "")
            {
                return Task.CompletedTask;
            }

            // Get user info once for all commands
            string userId = message.Author.Id.ToString();
            string username = message.Author.Username;

            // Process Gif Commands (pass channel ID for channel restrictions)
            string gifCommand = DBActions.DongGifs(command, message.Channel.Name, message.Channel.Id, userId, username);
            if (!gifCommand.Equals(""))
            {
                await message.Channel.SendMessageAsync(gifCommand);
                return Task.CompletedTask;
            }

            // GIF Management Commands (Admin Only)
            if (command.ToUpper().StartsWith("GIF-ADD"))
            {
                if (!IsAdminChannel(message.Channel.Name))
                {
                    await message.Channel.SendMessageAsync($"Error: This command can only be used in #{ADMIN_CHANNEL_NAME}");
                    return Task.CompletedTask;
                }
                string result = DBActions.GifAdd(command, userId, username, message.Channel.Name);
                await message.Channel.SendMessageAsync(result);
                return Task.CompletedTask;
            }

            if (command.ToUpper().StartsWith("GIF-REMOVE"))
            {
                if (!IsAdminChannel(message.Channel.Name))
                {
                    await message.Channel.SendMessageAsync($"Error: This command can only be used in #{ADMIN_CHANNEL_NAME}");
                    return Task.CompletedTask;
                }
                string result = DBActions.GifRemove(command, userId, username, message.Channel.Name);
                await message.Channel.SendMessageAsync(result);
                return Task.CompletedTask;
            }

            if (command.ToUpper().Equals("GIF-REFRESH"))
            {
                if (!IsAdminChannel(message.Channel.Name))
                {
                    await message.Channel.SendMessageAsync($"Error: This command can only be used in #{ADMIN_CHANNEL_NAME}");
                    return Task.CompletedTask;
                }
                string result = DBActions.GifRefresh(command, userId, username, message.Channel.Name);
                await message.Channel.SendMessageAsync(result);
                return Task.CompletedTask;
            }

            if (command.ToUpper().StartsWith("GIF-LIST"))
            {
                if (!IsAdminChannel(message.Channel.Name))
                {
                    await message.Channel.SendMessageAsync($"Error: This command can only be used in #{ADMIN_CHANNEL_NAME}");
                    return Task.CompletedTask;
                }
                string result = DBActions.GifList(command, userId, username, message.Channel.Name);
                await message.Channel.SendMessageAsync(result);
                return Task.CompletedTask;
            }

            if (command.ToUpper().StartsWith("GIF-ALIAS"))
            {
                if (!IsAdminChannel(message.Channel.Name))
                {
                    await message.Channel.SendMessageAsync($"Error: This command can only be used in #{ADMIN_CHANNEL_NAME}");
                    return Task.CompletedTask;
                }
                string result = DBActions.GifAlias(command, userId, username, message.Channel.Name);
                await message.Channel.SendMessageAsync(result);
                return Task.CompletedTask;
            }

            if (command.ToUpper().StartsWith("GIF-CHANNEL"))
            {
                if (!IsAdminChannel(message.Channel.Name))
                {
                    await message.Channel.SendMessageAsync($"Error: This command can only be used in #{ADMIN_CHANNEL_NAME}");
                    return Task.CompletedTask;
                }
                string result = DBActions.GifChannel(command, userId, username, message.Channel.Name);
                await message.Channel.SendMessageAsync(result);
                return Task.CompletedTask;
            }

            if (command.ToUpper().StartsWith("GIF-VALIDATE"))
            {
                if (!IsAdminChannel(message.Channel.Name))
                {
                    await message.Channel.SendMessageAsync($"Error: This command can only be used in #{ADMIN_CHANNEL_NAME}");
                    return Task.CompletedTask;
                }
                string result = await DBActions.GifValidate(command, userId, username, message.Channel.Name);
                await message.Channel.SendMessageAsync(result);
                return Task.CompletedTask;
            }

            // Audit Log Commands (Admin Only)
            if (command.ToUpper().StartsWith("AUDIT") && !command.ToUpper().StartsWith("AUDIT-STATS"))
            {
                if (!IsAdminChannel(message.Channel.Name))
                {
                    await message.Channel.SendMessageAsync($"Error: This command can only be used in #{ADMIN_CHANNEL_NAME}");
                    return Task.CompletedTask;
                }
                string result = DBActions.GetAuditLog(command, userId, username);
                await message.Channel.SendMessageAsync(result);
                return Task.CompletedTask;
            }

            if (command.ToUpper().Equals("AUDIT-STATS"))
            {
                if (!IsAdminChannel(message.Channel.Name))
                {
                    await message.Channel.SendMessageAsync($"Error: This command can only be used in #{ADMIN_CHANNEL_NAME}");
                    return Task.CompletedTask;
                }
                string result = DBActions.GetAuditStats(userId, username);
                await message.Channel.SendMessageAsync(result);
                return Task.CompletedTask;
            }

            // Statistics Commands (Admin Only)
            if (command.ToUpper().Equals("STATS"))
            {
                if (!IsAdminChannel(message.Channel.Name))
                {
                    await message.Channel.SendMessageAsync($"Error: This command can only be used in #{ADMIN_CHANNEL_NAME}");
                    return Task.CompletedTask;
                }
                string result = DBActions.GetStats(userId, username);
                await message.Channel.SendMessageAsync(result);
                return Task.CompletedTask;
            }

            if (command.ToUpper().StartsWith("STATS-TOP"))
            {
                if (!IsAdminChannel(message.Channel.Name))
                {
                    await message.Channel.SendMessageAsync($"Error: This command can only be used in #{ADMIN_CHANNEL_NAME}");
                    return Task.CompletedTask;
                }
                string result = DBActions.GetTopCommands(command, userId, username);
                await message.Channel.SendMessageAsync(result);
                return Task.CompletedTask;
            }

            if (command.ToUpper().StartsWith("STATS-USER"))
            {
                if (!IsAdminChannel(message.Channel.Name))
                {
                    await message.Channel.SendMessageAsync($"Error: This command can only be used in #{ADMIN_CHANNEL_NAME}");
                    return Task.CompletedTask;
                }
                string result = DBActions.GetUserStatistics(command, userId, username);
                await message.Channel.SendMessageAsync(result);
                return Task.CompletedTask;
            }

            if (command.ToUpper().StartsWith("STATS-COMMAND"))
            {
                if (!IsAdminChannel(message.Channel.Name))
                {
                    await message.Channel.SendMessageAsync($"Error: This command can only be used in #{ADMIN_CHANNEL_NAME}");
                    return Task.CompletedTask;
                }
                string result = DBActions.GetCommandStatistics(command, userId, username);
                await message.Channel.SendMessageAsync(result);
                return Task.CompletedTask;
            }

            // Help Command (Available in all channels, shows channel-specific info)
            if (command.ToUpper().Equals("HELP") || command.ToUpper().Equals("GIF-HELP"))
            {
                string result = DBActions.GetHelp(message.Channel.Name, message.Channel.Id, IsAdminChannel(message.Channel.Name), userId, username);
                await message.Channel.SendMessageAsync(result);
                return Task.CompletedTask;
            }

            return Task.CompletedTask;
        }

    }
}
