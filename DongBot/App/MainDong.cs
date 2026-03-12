using Discord;
using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace DongBot
{
    class MainDong
    {
        public static void Main(string[] args)
        => new MainDong().MainAsync().GetAwaiter().GetResult();

        private readonly BotConfig _botConfig = BotConfig.Load();
        private readonly string _adminChannelName;
        private DiscordSocketClient _client = null!;
        private DiscordSocketConfig _config = null!;
        private readonly AuditLogger _auditLogger;
        private readonly StatisticsTracker _statisticsTracker;
        private readonly UserErrorReportLogger _userErrorReportLogger;
        private readonly AdminReportingService _adminReportingService;
        private MLBCommandManager _mlbManager = null!;
        private GifCommandService _gifService = null!;
        private GifCommandManager _gifManager = null!;
        private GifAdminCommandManager _gifAdminManager = null!;
        private AdminCommandManager _adminManager = null!;
        private List<ICommandManager> _commandManagers = new List<ICommandManager>();
        private BravesScheduler? _bravesScheduler;
        private readonly object _lastCommandLock = new object();
        private readonly Dictionary<string, string> _lastCommandByUser = new Dictionary<string, string>();
        private int _initialized = 0;
        private int _shutdownStarted = 0;
        private const string ReleaseNotesChannelName = "dongdot";
        private static readonly string UserReleaseNotesRelativePath = Path.Combine("docs", "RELEASE_NOTES_USER.md");

        public MainDong()
        {
            _adminChannelName = _botConfig.AdminChannelName;
            _auditLogger = new AuditLogger(_botConfig.AuditLogFilePath, verboseConsoleLogging: _botConfig.AuditVerboseConsoleLogging);
            _statisticsTracker = new StatisticsTracker(_botConfig.StatisticsFilePath);
            _userErrorReportLogger = new UserErrorReportLogger(_botConfig.UserErrorReportsFilePath);
            _adminReportingService = new AdminReportingService(_auditLogger, _statisticsTracker);
        }

        public async Task MainAsync()
        {
            this._config = new DiscordSocketConfig()
            {
                GatewayIntents = GatewayIntents.All
            };
            this._client = new DiscordSocketClient(_config);
            this._client.MessageReceived += this.CommandHandler;
            this._client.Log += this.Log;
            this._client.Ready += this.OnReady;

            //  You can assign your bot token to a string, and pass that in to connect.
            //  This is, however, insecure, particularly if you plan to have your code hosted in a public repository.
            string token = File.ReadAllText(_botConfig.TokenFilePath);

            //  Some alternate options would be to keep your token in an Environment Variable or a standalone file.
            //  var token = Environment.GetEnvironmentVariable("NameOfYourEnvironmentVariable");
            //  var token = File.ReadAllText("token.txt");
            //  var token = JsonConvert.DeserializedObject<AConfigurationClass>(File.ReadAllText("config.json")).Token;

            await this._client.LoginAsync(TokenType.Bot, token);
            await this._client.StartAsync();

            // Flush audit/stats on graceful shutdown (CTRL+C or process exit)
            // Both handlers delegate to ShutdownOnce() which is idempotent,
            // so even if ProcessExit fires after CancelKeyPress calls Environment.Exit(0)
            // the second invocation is a no-op.
            AppDomain.CurrentDomain.ProcessExit += (_, __) => ShutdownOnce();
            Console.CancelKeyPress += (_, e) =>
            {
                e.Cancel = true; // prevent abrupt termination – let ShutdownOnce handle cleanup
                ShutdownOnce();
                Environment.Exit(0);
            };

            //  Block this task until the program is closed.
            await Task.Delay(-1);
        }

        private Task Log(LogMessage msg)
        {
            Console.WriteLine(msg.ToString());
            return Task.CompletedTask;
        }

        /// <summary>
        /// Called when the bot is connected and ready.
        /// Guard against duplicate initialization on reconnects: Discord's Ready event
        /// can fire more than once (e.g., after a disconnection/reconnection cycle).
        /// </summary>
        private Task OnReady()
        {
            if (Interlocked.CompareExchange(ref _initialized, 1, 0) != 0)
            {
                Console.WriteLine("Bot reconnected; reusing existing scheduler and managers.");
                return Task.CompletedTask;
            }

            Console.WriteLine($"Bot is connected as {_client.CurrentUser}");

            // Initialize and start the Braves scheduler
            _bravesScheduler = new BravesScheduler(_client, _botConfig.BravesChannelName);
            _bravesScheduler.Start();

            // Initialize command managers - order determines dispatch priority
            _mlbManager = new MLBCommandManager(_bravesScheduler);
            _gifService = new GifCommandService(_botConfig.GifCommandsFilePath);
            _gifManager = new GifCommandManager(
                gifService: _gifService,
                adminChannelName: _adminChannelName,
                statisticsTracker: _statisticsTracker);
            _gifAdminManager = new GifAdminCommandManager(
                _gifService,
                _auditLogger,
                _statisticsTracker,
                _adminChannelName);
            _adminManager = new AdminCommandManager(_adminReportingService, _adminChannelName, _userErrorReportLogger);
            _commandManagers = new List<ICommandManager>
            {
                _mlbManager,
                _adminManager,
                _gifAdminManager,
                _gifManager
            };
            return Task.CompletedTask;
        }

        /// <summary>
        /// Check if a command is being run in the admin channel.
        /// </summary>
        private bool IsAdminChannel(string channelName)
        {
            return channelName.Equals(_adminChannelName, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Sends a message to a channel, splitting at newline boundaries if it exceeds
        /// Discord's 2000-character limit. Formatting is preserved because lines are never
        /// broken mid-line.
        /// </summary>
        private static Task SendChunkedAsync(IMessageChannel channel, string text, int maxLength = 1950)
            => SendChunkedAsync(message => channel.SendMessageAsync(message), text, maxLength);

        internal static async Task SendChunkedAsync(Func<string, Task> sendAsync, string text, int maxLength = 1950)
        {
            if (string.IsNullOrEmpty(text))
                return;

            if (text.Length <= maxLength)
            {
                await sendAsync(text);
                return;
            }

            string[] lines = text.Split('\n');
            StringBuilder chunk = new StringBuilder();

            foreach (string line in lines)
            {
                // +1 for the newline we'll re-append
                if (chunk.Length + line.Length + 1 > maxLength)
                {
                    if (chunk.Length > 0)
                    {
                        await sendAsync(chunk.ToString().TrimEnd());
                        chunk.Clear();
                    }

                    // Edge case: a single line that exceeds maxLength on its own
                    if (line.Length > maxLength)
                    {
                        for (int i = 0; i < line.Length; i += maxLength)
                            await sendAsync(line.Substring(i, Math.Min(maxLength, line.Length - i)));
                        continue;
                    }
                }

                chunk.AppendLine(line);
            }

            if (chunk.Length > 0)
                await sendAsync(chunk.ToString().TrimEnd());
        }

        private async Task CommandHandler(SocketMessage message)
        {
            await HandleMessageAsync(
                content: message.Content,
                authorIsBot: message.Author.IsBot,
                userId: message.Author.Id.ToString(),
                username: message.Author.Username,
                channelName: message.Channel.Name,
                channelId: message.Channel.Id,
                sendAsync: text => message.Channel.SendMessageAsync(text));
        }

        internal async Task HandleMessageAsync(
            string content,
            bool authorIsBot,
            string userId,
            string username,
            string channelName,
            ulong channelId,
            Func<string, Task> sendAsync)
        {
            // Filter out if the message is a command or not
            string command = FilterMessage(content, authorIsBot, '!');
            if (command == "")
            {
                return;
            }

            if (command.Equals("BADBOT", StringComparison.OrdinalIgnoreCase)
                || command.StartsWith("BADBOT ", StringComparison.OrdinalIgnoreCase))
            {
                string comment = command.Length > 6 ? command.Substring(6).Trim() : string.Empty;
                string? previousCommand = GetLastCommand(userId);

                _userErrorReportLogger.LogReport(userId, username, channelName, previousCommand, comment);
                _auditLogger.Log(
                    userId,
                    username,
                    "USER_ERROR_REPORT",
                    "BADBOT",
                    previousCommand ?? "NO_PREVIOUS_COMMAND",
                    string.IsNullOrWhiteSpace(comment) ? "No user comment provided." : comment,
                    channelName,
                    true);
                _statisticsTracker.TrackCommand("BADBOT", "USER_REPORT", userId, username, channelName, true);

                string previousDisplay = previousCommand == null ? "(none found)" : $"!{previousCommand}";
                string commentDisplay = string.IsNullOrWhiteSpace(comment) ? "(no comment provided)" : comment;
                await sendAsync($"⚠️ Report logged for investigation.\nPrevious command: {previousDisplay}\nComment: {commentDisplay}");
                return;
            }

            if (command.Equals("RELEASE-NOTES", StringComparison.OrdinalIgnoreCase)
                || command.StartsWith("RELEASE-NOTES ", StringComparison.OrdinalIgnoreCase))
            {
                if (!IsAdminChannel(channelName))
                {
                    await sendAsync($"Error: This command can only be used in #{_adminChannelName}");
                    return;
                }

                string? versionArgument = ParseReleaseNotesArgument(command);
                string? markdown = ReadUserReleaseNotesMarkdown();
                if (string.IsNullOrWhiteSpace(markdown))
                {
                    await sendAsync("❌ Could not load user release notes file.");
                    return;
                }

                if (!TryExtractReleaseNotesSections(markdown, versionArgument, out string resolvedLabel, out string releaseSection))
                {
                    await sendAsync($"❌ Release notes for version '{versionArgument}' were not found.");
                    return;
                }

                IMessageChannel? targetChannel = FindTextChannelByName(ReleaseNotesChannelName);
                if (targetChannel == null)
                {
                    await sendAsync($"❌ Could not find #{ReleaseNotesChannelName} channel.");
                    return;
                }

                string formatted = FormatReleaseNotesForDiscord(releaseSection, resolvedLabel);
                await SendChunkedAsync(targetChannel, formatted);
                await sendAsync($"✅ Posted release notes v{resolvedLabel} to #{ReleaseNotesChannelName}.");
                RecordLastCommand(userId, command);
                return;
            }

            // Build shared context for all command managers
            CommandContext ctx = new CommandContext(
                channelName,
                channelId,
                IsAdminChannel(channelName),
                userId,
                username
            );

            // Braves scheduler commands remain admin-only
            // Do NOT record as last command – rejections are not successful interactions.
            if (command.ToUpper().StartsWith("BRAVES-SCHEDULER-") && !ctx.IsAdminChannel)
            {
                await sendAsync($"Error: This command can only be used in #{_adminChannelName}");
                return;
            }

            // Dispatch to registered managers - first non-empty response wins
            foreach (ICommandManager manager in _commandManagers)
            {
                if (!manager.CanHandle(command))
                    continue;
                string managerResponse = await manager.ProcessCommandAsync(command, ctx);
                if (!string.IsNullOrEmpty(managerResponse))
                {
                    await SendChunkedAsync(sendAsync, managerResponse);
                    // Only record as last command when the response is a real success,
                    // not a channel-gate rejection or other error.
                    if (!IsErrorResponse(managerResponse))
                        RecordLastCommand(userId, command);
                    return;
                }
            }

            // Help Command (Available in all channels, shows channel-specific info)
            if (command.ToUpper().Equals("HELP") || command.ToUpper().Equals("GIF-HELP"))
            {
                List<string> helpSections = new List<string>();
                foreach (ICommandManager manager in _commandManagers)
                {
                    string? helpText = manager.GetHelp(ctx)?.Trim();
                    if (!string.IsNullOrWhiteSpace(helpText))
                    {
                        helpSections.Add(helpText);
                    }
                }

                if (helpSections.Count > 0)
                {
                    await SendChunkedAsync(sendAsync, string.Join("\n\n", helpSections));
                    RecordLastCommand(userId, command);
                }
                return;
            }
        }

        private static string FilterMessage(string content, bool authorIsBot, char prefix)
        {
            if (!content.StartsWith(prefix))
                return string.Empty;

            if (authorIsBot)
                return string.Empty;

            if (content.Length <= 1)
                return string.Empty;

            return content.Substring(1).Trim();
        }

        internal static string? ParseReleaseNotesArgument(string command)
        {
            string[] parts = command.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length < 2)
            {
                return null;
            }

            string argument = parts[1].Trim();
            if (TryParseReleaseNotesRange(argument, out string? startVersion, out string? endVersion))
            {
                return $"{startVersion}-{endVersion}";
            }

            return NormalizeVersionToken(argument);
        }

        internal static bool TryParseReleaseNotesRange(string? argument, out string? startVersion, out string? endVersion)
        {
            startVersion = null;
            endVersion = null;

            if (string.IsNullOrWhiteSpace(argument))
            {
                return false;
            }

            string normalized = argument.Replace("..", "|");
            int dashIndex = normalized.IndexOf('-');
            int pipeIndex = normalized.IndexOf('|');
            int splitIndex = pipeIndex >= 0 ? pipeIndex : dashIndex;

            if (splitIndex <= 0 || splitIndex >= normalized.Length - 1)
            {
                return false;
            }

            startVersion = NormalizeVersionToken(normalized.Substring(0, splitIndex).Trim());
            endVersion = NormalizeVersionToken(normalized.Substring(splitIndex + 1).Trim());
            return !string.IsNullOrWhiteSpace(startVersion) && !string.IsNullOrWhiteSpace(endVersion);
        }

        internal static bool TryExtractReleaseNotesSections(string markdown, string? requestedArgument, out string resolvedVersion, out string section)
        {
            resolvedVersion = string.Empty;
            section = string.Empty;

            string[] lines = markdown.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);
            Regex versionHeaderRegex = new Regex(@"^##\s+v?(\d+\.\d+\.\d+)\b", RegexOptions.IgnoreCase);

            List<(int Index, string Version)> headers = new List<(int, string)>();
            for (int i = 0; i < lines.Length; i++)
            {
                Match match = versionHeaderRegex.Match(lines[i]);
                if (match.Success)
                {
                    headers.Add((i, match.Groups[1].Value));
                }
            }

            if (!headers.Any())
            {
                return false;
            }

            if (string.IsNullOrWhiteSpace(requestedArgument))
            {
                var selected = headers.First();
                section = ExtractSection(lines, headers, selected.Index).Trim();
                resolvedVersion = selected.Version;
                return !string.IsNullOrWhiteSpace(section);
            }

            if (TryParseReleaseNotesRange(requestedArgument, out string? startVersion, out string? endVersion))
            {
                if (!TryParseSemanticVersion(startVersion!, out Version? start) || !TryParseSemanticVersion(endVersion!, out Version? end))
                {
                    return false;
                }

                Version lower = start! <= end! ? start! : end!;
                Version upper = start! <= end! ? end! : start!;

                List<string> matchingSections = headers
                    .Where(h => TryParseSemanticVersion(h.Version, out Version? parsed)
                        && parsed >= lower
                        && parsed <= upper)
                    .Select(h => ExtractSection(lines, headers, h.Index).Trim())
                    .Where(s => !string.IsNullOrWhiteSpace(s))
                    .ToList();

                if (!matchingSections.Any())
                {
                    return false;
                }

                resolvedVersion = $"{startVersion}-{endVersion}";
                section = string.Join("\n\n", matchingSections);
                return true;
            }

            var single = headers.FirstOrDefault(h => h.Version.Equals(requestedArgument, StringComparison.OrdinalIgnoreCase));
            if (string.IsNullOrWhiteSpace(single.Version))
            {
                return false;
            }

            section = ExtractSection(lines, headers, single.Index).Trim();
            resolvedVersion = single.Version;
            return !string.IsNullOrWhiteSpace(section);
        }

        private static string ExtractSection(string[] lines, List<(int Index, string Version)> headers, int startIndex)
        {
            int headerPosition = headers.FindIndex(h => h.Index == startIndex);
            int endIndex = (headerPosition >= 0 && headerPosition < headers.Count - 1)
                ? headers[headerPosition + 1].Index
                : lines.Length;

            return string.Join("\n", lines.Skip(startIndex).Take(endIndex - startIndex));
        }

        private static string NormalizeVersionToken(string token)
            => token.StartsWith("v", StringComparison.OrdinalIgnoreCase)
                ? token.Substring(1)
                : token;

        private static bool TryParseSemanticVersion(string value, out Version? version)
        {
            version = null;
            string[] parts = value.Split('.', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length != 3)
            {
                return false;
            }

            if (!int.TryParse(parts[0], out int major)
                || !int.TryParse(parts[1], out int minor)
                || !int.TryParse(parts[2], out int patch))
            {
                return false;
            }

            version = new Version(major, minor, patch);
            return true;
        }

        internal static string FormatReleaseNotesForDiscord(string section, string version)
        {
            string[] lines = section.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);
            StringBuilder sb = new StringBuilder();
            sb.AppendLine($"📣 **DongBot Release Notes v{version}**");
            sb.AppendLine();

            bool inCodeBlock = false;
            foreach (string rawLine in lines)
            {
                string line = rawLine;
                string trimmed = line.TrimStart();

                if (trimmed.StartsWith("```"))
                {
                    inCodeBlock = !inCodeBlock;
                    sb.AppendLine(line);
                    continue;
                }

                if (inCodeBlock)
                {
                    sb.AppendLine(line);
                    continue;
                }

                if (trimmed.StartsWith("## "))
                {
                    sb.AppendLine($"**{trimmed.Substring(3).Trim()}**");
                    continue;
                }

                if (trimmed.StartsWith("### "))
                {
                    sb.AppendLine($"**{trimmed.Substring(4).Trim()}**");
                    continue;
                }

                if (trimmed.StartsWith("# "))
                {
                    sb.AppendLine($"**{trimmed.Substring(2).Trim()}**");
                    continue;
                }

                sb.AppendLine(line);
            }

            return sb.ToString().TrimEnd();
        }

        private string? ReadUserReleaseNotesMarkdown()
        {
            IEnumerable<string> startDirectories = new[] { Directory.GetCurrentDirectory(), AppContext.BaseDirectory }
                .Where(d => !string.IsNullOrWhiteSpace(d));

            foreach (string startDirectory in startDirectories)
            {
                string? current = startDirectory;
                for (int i = 0; i < 8 && !string.IsNullOrWhiteSpace(current); i++)
                {
                    string candidate = Path.Combine(current, UserReleaseNotesRelativePath);
                    if (File.Exists(candidate))
                    {
                        return File.ReadAllText(candidate);
                    }

                    current = Directory.GetParent(current)?.FullName;
                }
            }

            return null;
        }

        private IMessageChannel? FindTextChannelByName(string channelName)
        {
            if (_client == null)
            {
                return null;
            }

            foreach (SocketGuild guild in _client.Guilds)
            {
                SocketTextChannel? channel = guild.TextChannels
                    .FirstOrDefault(c => c.Name.Equals(channelName, StringComparison.OrdinalIgnoreCase));

                if (channel != null)
                {
                    return channel;
                }
            }

            return null;
        }

        private void RecordLastCommand(string userId, string command)
        {
            lock (_lastCommandLock)
            {
                _lastCommandByUser[userId] = command;
            }
        }

        private string? GetLastCommand(string userId)
        {
            lock (_lastCommandLock)
            {
                return _lastCommandByUser.TryGetValue(userId, out string? command) ? command : null;
            }
        }

        /// <summary>
        /// Idempotent shutdown: stops the scheduler and disposes all loggers.
        /// Safe to call from both ProcessExit and CancelKeyPress; second call is a no-op.
        /// </summary>
        private void ShutdownOnce()
        {
            if (Interlocked.CompareExchange(ref _shutdownStarted, 1, 0) != 0)
                return;

            _bravesScheduler?.Stop();
            _auditLogger.Dispose();
            _statisticsTracker.Dispose();
            _userErrorReportLogger.Dispose();
        }

        /// <summary>
        /// Returns true if the manager response represents a known error or channel-gate
        /// rejection, which should not overwrite the user's last successful command context.
        /// </summary>
        private static bool IsErrorResponse(string response)
            => response.StartsWith("Error:", StringComparison.OrdinalIgnoreCase)
            || response.StartsWith("❌")
            || response.StartsWith("Usage:", StringComparison.OrdinalIgnoreCase)
            || response.Contains("Invalid", StringComparison.OrdinalIgnoreCase); // ❌

        internal void SetCommandManagersForTesting(IEnumerable<ICommandManager> managers)
        {
            _commandManagers = new List<ICommandManager>(managers);
        }

        internal string AdminChannelNameForTesting => _adminChannelName;

    }
}
