using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DongBot
{
    /// <summary>
    /// Handles admin-only GIF management commands.
    /// </summary>
    internal class GifAdminCommandManager : ICommandManager
    {
        private readonly GifCommandService _gifService;
        private readonly AuditLogger _auditLogger;
        private readonly StatisticsTracker _statisticsTracker;
        private readonly string _adminChannelName;

        public GifAdminCommandManager(
            GifCommandService gifService,
            AuditLogger auditLogger,
            StatisticsTracker statisticsTracker,
            string adminChannelName)
        {
            _gifService = gifService;
            _auditLogger = auditLogger;
            _statisticsTracker = statisticsTracker;
            _adminChannelName = adminChannelName;
        }

        public bool CanHandle(string command)
        {
            string upper = command.ToUpperInvariant();
            return upper.StartsWith("GIF-ADD")
                || upper.StartsWith("GIF-REMOVE")
                || upper.Equals("GIF-REFRESH")
                || upper.StartsWith("GIF-LIST")
                || upper.StartsWith("GIF-ALIAS")
                || upper.StartsWith("GIF-CHANNEL")
                || upper.StartsWith("GIF-VALIDATE");
        }

        public async Task<string> ProcessCommandAsync(string command, CommandContext context)
        {
            if (!context.IsAdminChannel)
            {
                return $"Error: This command can only be used in #{_adminChannelName}";
            }

            string upper = command.ToUpperInvariant();

            if (upper.StartsWith("GIF-ADD"))
                return GifAdd(command, context.UserId, context.Username, context.ChannelName, context.GuildId, context.GuildName);

            if (upper.StartsWith("GIF-REMOVE"))
                return GifRemove(command, context.UserId, context.Username, context.ChannelName, context.GuildId, context.GuildName);

            if (upper.Equals("GIF-REFRESH"))
                return GifRefresh(context.UserId, context.Username, context.ChannelName, context.GuildId, context.GuildName);

            if (upper.StartsWith("GIF-LIST"))
                return GifList(command, context.UserId, context.Username, context.ChannelName, context.GuildId, context.GuildName);

            if (upper.StartsWith("GIF-ALIAS"))
                return GifAlias(command, context.UserId, context.Username, context.ChannelName, context.GuildId, context.GuildName);

            if (upper.StartsWith("GIF-CHANNEL"))
                return GifChannel(command, context.UserId, context.Username, context.ChannelName, context.GuildId, context.GuildName);

            if (upper.StartsWith("GIF-VALIDATE"))
                return await GifValidate(command, context.UserId, context.Username, context.ChannelName, context.GuildId, context.GuildName);

            return string.Empty;
        }

        private string GifAdd(string command, string userId, string username, string channelName, ulong guildId, string guildName)
        {
            try
            {
                string[] parts = command.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length < 3)
                    return "Usage: !gif-add COMMANDNAME URL [CHANNEL] [PATTERN] [ISREGEX] [ALIASES]\nExample: !gif-add DONG http://example.com/gif.gif baseball \"\" false DINGER,HR";

                string commandKey = parts[1].ToUpperInvariant();
                string gifUrl = parts[2];
                string channel = parts.Length > 3 ? parts[3] : string.Empty;
                string? pattern = parts.Length > 4 ? parts[4] : null;
                bool isRegex = parts.Length > 5 && bool.TryParse(parts[5], out bool parsedIsRegex) && parsedIsRegex;
                string? aliases = parts.Length > 6 ? parts[6] : null;

                string result = _gifService.AddOrUpdateCommand(commandKey, gifUrl, channel, pattern, isRegex, aliases);
                bool success = !result.Contains("Error", StringComparison.OrdinalIgnoreCase);
                _auditLogger.Log(userId, username, "ADD", "GIF_COMMAND", commandKey,
                    $"Added GIF: {gifUrl} (Channel: {(string.IsNullOrEmpty(channel) ? "All" : channel)})", channelName, guildId, guildName, success);
                _statisticsTracker.TrackCommand("GIF-ADD", "ADMIN", userId, username, channelName, success, guildId, guildName);
                return result;
            }
            catch (Exception ex)
            {
                _auditLogger.Log(userId, username, "ADD", "GIF_COMMAND", "UNKNOWN", $"Failed: {ex.Message}", channelName, guildId, guildName, false);
                return $"Error processing gif-add command: {ex.Message}";
            }
        }

        private string GifRemove(string command, string userId, string username, string channelName, ulong guildId, string guildName)
        {
            try
            {
                string[] parts = command.Split(new[] { ' ' }, 3, StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length < 2)
                    return "Usage: !gif-remove COMMANDNAME [URL]\nIf URL is omitted, the entire command is removed.";

                string commandKey = parts[1].ToUpperInvariant();
                string? gifUrl = parts.Length > 2 ? parts[2] : null;

                string result = _gifService.RemoveCommand(commandKey, gifUrl);
                bool success = !result.Contains("Error", StringComparison.OrdinalIgnoreCase);
                string details = string.IsNullOrEmpty(gifUrl) ? "Removed entire command" : $"Removed GIF: {gifUrl}";
                _auditLogger.Log(userId, username, "REMOVE", "GIF_COMMAND", commandKey, details, channelName, guildId, guildName, success);
                _statisticsTracker.TrackCommand("GIF-REMOVE", "ADMIN", userId, username, channelName, success, guildId, guildName);
                return result;
            }
            catch (Exception ex)
            {
                _auditLogger.Log(userId, username, "REMOVE", "GIF_COMMAND", "UNKNOWN", $"Failed: {ex.Message}", channelName, guildId, guildName, false);
                return $"Error processing gif-remove command: {ex.Message}";
            }
        }

        private string GifRefresh(string userId, string username, string channelName, ulong guildId, string guildName)
        {
            try
            {
                _gifService.LoadCommands();
                int count = _gifService.GetCommandCount();
                _auditLogger.Log(userId, username, "REFRESH", "GIF_COMMAND", "ALL", $"Reloaded {count} commands from file", channelName, guildId, guildName, true);
                _statisticsTracker.TrackCommand("GIF-REFRESH", "ADMIN", userId, username, channelName, true, guildId, guildName);
                return $"GIF commands refreshed! Loaded {count} commands.";
            }
            catch (Exception ex)
            {
                _auditLogger.Log(userId, username, "REFRESH", "GIF_COMMAND", "ALL", $"Failed: {ex.Message}", channelName, guildId, guildName, false);
                return $"Error refreshing GIF commands: {ex.Message}";
            }
        }

        private string GifList(string command, string userId, string username, string channelName, ulong guildId, string guildName)
        {
            try
            {
                string[] parts = command.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                string? commandKey = parts.Length > 1 ? parts[1] : null;
                string result = _gifService.ListCommands(commandKey);
                _statisticsTracker.TrackCommand("GIF-LIST", "ADMIN", userId, username, channelName, true, guildId, guildName);
                return result;
            }
            catch (Exception ex)
            {
                _statisticsTracker.TrackCommand("GIF-LIST", "ADMIN", userId, username, channelName, false, guildId, guildName);
                return $"Error listing GIF commands: {ex.Message}";
            }
        }

        private string GifAlias(string command, string userId, string username, string channelName, ulong guildId, string guildName)
        {
            try
            {
                string[] parts = command.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length < 4)
                    return "Usage: !gif-alias COMMANDNAME add|remove ALIAS\nExample: !gif-alias DONG add DINGER";

                string commandKey = parts[1].ToUpperInvariant();
                string action = parts[2].ToLowerInvariant();
                string alias = parts[3].ToUpperInvariant();
                string result;
                bool success;

                if (action == "add")
                {
                    result = _gifService.AddAlias(commandKey, alias);
                    success = !result.Contains("Error", StringComparison.OrdinalIgnoreCase);
                    if (success)
                        _auditLogger.Log(userId, username, "ADD_ALIAS", "GIF_COMMAND", commandKey, $"Added alias: {alias}", channelName, guildId, guildName, true);
                }
                else if (action == "remove")
                {
                    result = _gifService.RemoveAlias(commandKey, alias);
                    success = !result.Contains("Error", StringComparison.OrdinalIgnoreCase);
                    if (success)
                        _auditLogger.Log(userId, username, "REMOVE_ALIAS", "GIF_COMMAND", commandKey, $"Removed alias: {alias}", channelName, guildId, guildName, true);
                }
                else
                {
                    return "Invalid action. Use 'add' or 'remove'.";
                }

                _statisticsTracker.TrackCommand("GIF-ALIAS", "ADMIN", userId, username, channelName, success, guildId, guildName);
                return result;
            }
            catch (Exception ex)
            {
                _statisticsTracker.TrackCommand("GIF-ALIAS", "ADMIN", userId, username, channelName, false, guildId, guildName);
                return $"Error managing alias: {ex.Message}";
            }
        }

        private string GifChannel(string command, string userId, string username, string channelName, ulong guildId, string guildName)
        {
            try
            {
                string[] parts = command.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length < 3)
                    return "Usage: !gif-channel COMMANDNAME add|remove|list|clear [CHANNELID]\nExamples:\n  !gif-channel DONG add 123456789\n  !gif-channel DONG clear";

                string commandKey = parts[1].ToUpperInvariant();
                string action = parts[2].ToLowerInvariant();
                string result;
                bool success;

                if (action == "add")
                {
                    if (parts.Length < 4) return "Error: Channel ID required for 'add' action.";
                    if (!ulong.TryParse(parts[3], out ulong addId)) return "Error: Invalid channel ID. Must be numeric.";
                    result = _gifService.AddAllowedChannel(commandKey, addId);
                    success = !result.Contains("Error", StringComparison.OrdinalIgnoreCase);
                    if (success) _auditLogger.Log(userId, username, "ADD_CHANNEL_RESTRICTION", "GIF_COMMAND", commandKey, $"Added allowed channel: {addId}", channelName, guildId, guildName, true);
                }
                else if (action == "remove")
                {
                    if (parts.Length < 4) return "Error: Channel ID required for 'remove' action.";
                    if (!ulong.TryParse(parts[3], out ulong removeId)) return "Error: Invalid channel ID. Must be numeric.";
                    result = _gifService.RemoveAllowedChannel(commandKey, removeId);
                    success = !result.Contains("Error", StringComparison.OrdinalIgnoreCase);
                    if (success) _auditLogger.Log(userId, username, "REMOVE_CHANNEL_RESTRICTION", "GIF_COMMAND", commandKey, $"Removed allowed channel: {removeId}", channelName, guildId, guildName, true);
                }
                else if (action == "list")
                {
                    result = _gifService.ListAllowedChannels(commandKey);
                    success = !result.Contains("Error", StringComparison.OrdinalIgnoreCase);
                    if (success) _auditLogger.Log(userId, username, "LIST_CHANNEL_RESTRICTIONS", "GIF_COMMAND", commandKey, "Listed channel restrictions", channelName, guildId, guildName, true);
                }
                else if (action == "clear")
                {
                    result = _gifService.ClearAllowedChannels(commandKey);
                    success = !result.Contains("Error", StringComparison.OrdinalIgnoreCase);
                    if (success) _auditLogger.Log(userId, username, "CLEAR_CHANNEL_RESTRICTIONS", "GIF_COMMAND", commandKey, "Cleared all channel restrictions", channelName, guildId, guildName, true);
                }
                else
                {
                    return "Invalid action. Use 'add', 'remove', 'list', or 'clear'.";
                }

                _statisticsTracker.TrackCommand("GIF-CHANNEL", "ADMIN", userId, username, channelName, success, guildId, guildName);
                return result;
            }
            catch (Exception ex)
            {
                _statisticsTracker.TrackCommand("GIF-CHANNEL", "ADMIN", userId, username, channelName, false, guildId, guildName);
                return $"Error managing channel restrictions: {ex.Message}";
            }
        }

        private async Task<string> GifValidate(string command, string userId, string username, string channelName, ulong guildId, string guildName)
        {
            try
            {
                string[] parts = command.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                bool checkAccessibility = parts.Any(p => p.Equals("--check-access", StringComparison.OrdinalIgnoreCase));
                string? commandKey = parts.Length > 1
                    ? parts.FirstOrDefault(p => !p.Equals("GIF-VALIDATE", StringComparison.OrdinalIgnoreCase) && !p.StartsWith("--", StringComparison.OrdinalIgnoreCase))
                    : null;

                string result;
                if (!string.IsNullOrEmpty(commandKey))
                {
                    string normalizedCommandKey = commandKey.ToUpperInvariant();
                    result = await _gifService.ValidateCommandUrlsAsync(normalizedCommandKey, checkAccessibility);
                    _auditLogger.Log(userId, username, "VALIDATE", "GIF_COMMAND", normalizedCommandKey, $"Validated URLs (Accessibility check: {checkAccessibility})", channelName, guildId, guildName, true);
                }
                else
                {
                    result = await _gifService.ValidateAllUrlsAsync(checkAccessibility);
                    _auditLogger.Log(userId, username, "VALIDATE", "GIF_COMMAND", "ALL", $"Validated all URLs (Accessibility check: {checkAccessibility})", channelName, guildId, guildName, true);
                }

                _statisticsTracker.TrackCommand("GIF-VALIDATE", "ADMIN", userId, username, channelName, true, guildId, guildName);
                return result;
            }
            catch (Exception ex)
            {
                _statisticsTracker.TrackCommand("GIF-VALIDATE", "ADMIN", userId, username, channelName, false, guildId, guildName);
                return $"Error validating URLs: {ex.Message}";
            }
        }

        public string GetHelp(CommandContext context)
        {
            if (!context.IsAdminChannel)
            {
                return string.Empty;
            }

            StringBuilder sb = new StringBuilder();
            sb.AppendLine("**GIF Administrative Commands:**");
            sb.AppendLine("```");
            sb.AppendLine("!gif-add COMMANDNAME URL [channel] [pattern] [isRegex] [aliases]");
            sb.AppendLine("!gif-remove COMMANDNAME URL");
            sb.AppendLine("!gif-refresh - Reload commands from file");
            sb.AppendLine("!gif-list [COMMANDNAME] - List all or specific command");
            sb.AppendLine("!gif-alias COMMANDNAME add|remove ALIAS");
            sb.AppendLine("!gif-channel COMMANDNAME add|remove|list|clear [CHANNELID]");
            sb.AppendLine("!gif-validate [COMMANDNAME] [--check-access]");
            sb.AppendLine("```");
            return sb.ToString();
        }

        public void Dispose()
        {
            // Shared services are owned elsewhere.
        }
    }
}