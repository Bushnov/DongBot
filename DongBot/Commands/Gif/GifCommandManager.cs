using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DongBot
{
    /// <summary>
    /// User-facing GIF command manager. Resolves configured GIF commands and returns channel-aware help.
    /// </summary>
    internal class GifCommandManager : ICommandManager
    {
        private readonly GifCommandService _gifService;
        private readonly string _adminChannelName;
        private readonly StatisticsTracker _statisticsTracker;
        private readonly bool _ownsTracker;

        public GifCommandManager(
            GifCommandService gifService,
            string adminChannelName = "dongbot-admin",
            StatisticsTracker? statisticsTracker = null)
        {
            _gifService = gifService;
            _adminChannelName = adminChannelName;
            _ownsTracker = statisticsTracker == null;
            _statisticsTracker = statisticsTracker ?? new StatisticsTracker();
        }

        public bool CanHandle(string command)
        {
            string upper = command.ToUpperInvariant();

            if (upper.Equals("HELP")
                || upper.Equals("GIF-HELP")
                || upper.StartsWith("MLB-")
                || upper.StartsWith("BRAVES-")
                || upper.StartsWith("AUDIT")
                || upper.Equals("STATS")
                || upper.StartsWith("STATS-")
                || upper.StartsWith("GIF-ADD")
                || upper.StartsWith("GIF-REMOVE")
                || upper.Equals("GIF-REFRESH")
                || upper.StartsWith("GIF-LIST")
                || upper.StartsWith("GIF-ALIAS")
                || upper.StartsWith("GIF-CHANNEL")
                || upper.StartsWith("GIF-VALIDATE"))
            {
                return false;
            }

            return true;
        }

        public Task<string> ProcessCommandAsync(string command, CommandContext context)
        {
            string result = _gifService.ProcessCommand(command, context.ChannelName, context.ChannelId);
            if (!string.IsNullOrEmpty(result))
            {
                _statisticsTracker.TrackCommand(command.ToUpperInvariant(), "GIF", context.UserId, context.Username, context.ChannelName, true);
            }

            return Task.FromResult(result);
        }

        public string GetHelp(CommandContext context)
        {
            List<GifCommandDisplayInfo> availableCommands = _gifService.GetAvailableCommands(context.ChannelName, context.ChannelId);

            StringBuilder sb = new StringBuilder();
            sb.AppendLine($"**DongBot Help - {context.ChannelName}**");
            sb.AppendLine();

            if (availableCommands.Count > 0)
            {
                sb.AppendLine($"**Available GIF Commands ({availableCommands.Count}):**");
                sb.AppendLine("```");

                foreach (GifCommandDisplayInfo command in availableCommands)
                {
                    string pattern = command.IsRegex
                        ? $"(regex: {command.Pattern})"
                        : $"!{command.Pattern.ToLowerInvariant()}";

                    sb.Append($"  {pattern}");
                    if (command.Aliases.Count > 0)
                    {
                        string aliasText = string.Join(", ", command.Aliases.Select(a => "!" + a.ToLowerInvariant()));
                        sb.Append($" [Aliases: {aliasText}]");
                    }

                    sb.AppendLine($" - {command.GifCount} GIF(s)");
                }

                sb.AppendLine("```");
            }
            else
            {
                sb.AppendLine("*No GIF commands are available in this channel.*");
                sb.AppendLine();
            }

            if (!context.IsAdminChannel)
            {
                sb.AppendLine();
                sb.AppendLine($"*For administrative commands, use !help in #{_adminChannelName}*");
            }

            sb.AppendLine();
            sb.AppendLine("**General Usage:**");
            sb.AppendLine("Type `!COMMANDNAME` to get a random GIF from that command.");
            sb.AppendLine("Multiple GIFs per command are selected randomly each time.");
            return sb.ToString();
        }

        public void Dispose()
        {
            // Only dispose the tracker if this instance created it internally.
            // Injected (shared) trackers are owned by the caller.
            if (_ownsTracker)
                _statisticsTracker.Dispose();
        }
    }
}
