using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace DongBot
{
    internal sealed class GifCommandService
    {
        private readonly string _filePath;
        private readonly Random _random = new Random();
        private readonly object _lock = new object();
        private readonly BackupManager _backupManager;
        private GifCommandData _data = new GifCommandData();
        private readonly Dictionary<string, string> _aliasLookup = new Dictionary<string, string>();

        public GifCommandService(string filePath = "gifcommands.json")
        {
            _filePath = filePath;
            string backupDir = Path.Combine(Path.GetDirectoryName(_filePath) ?? ".", "backups");
            _backupManager = new BackupManager(backupDir, maxBackupsToKeep: 10);
            LoadCommands();
        }

        public void LoadCommands()
        {
            lock (_lock)
            {
                try
                {
                    if (!File.Exists(_filePath))
                    {
                        Console.WriteLine($"GIF commands file not found: {_filePath}. Creating default file.");
                        _data = new GifCommandData();
                        SaveCommands();
                        return;
                    }

                    _backupManager.BackupOnLoad(_filePath);

                    string json = File.ReadAllText(_filePath);
                    _data = JsonSerializer.Deserialize<GifCommandData>(json, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    }) ?? new GifCommandData();

                    if (_data.Commands == null)
                    {
                        _data = new GifCommandData();
                    }

                    BuildAliasLookup();
                    Console.WriteLine($"Successfully loaded {_data.Commands.Count} GIF commands from {_filePath}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error loading GIF commands: {ex.Message}");
                    _data = new GifCommandData();
                }
            }
        }

        public string ProcessCommand(string commandText, string channelName, ulong channelId = 0)
        {
            if (string.IsNullOrWhiteSpace(commandText))
            {
                return string.Empty;
            }

            lock (_lock)
            {
                string upperCommand = commandText.ToUpperInvariant();

                if (_aliasLookup.TryGetValue(upperCommand, out string? primaryCommandKey)
                    && _data.Commands.TryGetValue(primaryCommandKey, out GifCommand? aliasCommand))
                {
                    if (!IsCommandAllowedInChannel(aliasCommand, channelName, channelId))
                    {
                        return string.Empty;
                    }

                    if (aliasCommand.Gifs.Count > 0)
                    {
                        return aliasCommand.Gifs[_random.Next(aliasCommand.Gifs.Count)];
                    }
                }

                foreach (KeyValuePair<string, GifCommand> kvp in _data.Commands)
                {
                    GifCommand command = kvp.Value;
                    if (!IsCommandAllowedInChannel(command, channelName, channelId))
                    {
                        continue;
                    }

                    bool matches;
                    if (command.IsRegex)
                    {
                        try
                        {
                            matches = Regex.IsMatch(upperCommand, command.Pattern, RegexOptions.IgnoreCase);
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Error matching regex pattern '{command.Pattern}': {ex.Message}");
                            continue;
                        }
                    }
                    else
                    {
                        matches = upperCommand.Equals(command.Pattern, StringComparison.OrdinalIgnoreCase);
                    }

                    if (matches && command.Gifs.Count > 0)
                    {
                        return command.Gifs[_random.Next(command.Gifs.Count)];
                    }
                }
            }

            return string.Empty;
        }

        public List<GifCommandDisplayInfo> GetAvailableCommands(string channelName, ulong channelId)
        {
            lock (_lock)
            {
                return _data.Commands
                    .Where(kvp => IsCommandAllowedInChannel(kvp.Value, channelName, channelId))
                    .OrderBy(kvp => kvp.Key)
                    .Select(kvp => new GifCommandDisplayInfo(
                        kvp.Key,
                        kvp.Value.Pattern,
                        kvp.Value.IsRegex,
                        kvp.Value.Aliases.ToList(),
                        kvp.Value.Gifs.Count))
                    .ToList();
            }
        }

        public string AddOrUpdateCommand(string commandKey, string gifUrl, string channel = "", string? pattern = null, bool isRegex = false, string? aliases = null)
        {
            if (string.IsNullOrWhiteSpace(commandKey))
            {
                return "Error: Command key cannot be empty.";
            }

            if (string.IsNullOrWhiteSpace(gifUrl))
            {
                return "Error: GIF URL cannot be empty.";
            }

            UrlValidationResult validation = UrlValidator.ValidateGifUrl(gifUrl);
            if (!validation.IsValid && !validation.WarningOnly)
            {
                return $"Error: {validation.ErrorMessage}";
            }

            lock (_lock)
            {
                commandKey = commandKey.ToUpperInvariant();
                pattern ??= commandKey;
                string warningMessage = validation.WarningOnly ? $"\n⚠ {validation.ErrorMessage}" : string.Empty;

                if (_data.Commands.TryGetValue(commandKey, out GifCommand? existingCommand))
                {
                    if (!existingCommand.Gifs.Contains(gifUrl))
                    {
                        existingCommand.Gifs.Add(gifUrl);
                        SaveCommands();
                        return $"Added new GIF to existing command '{commandKey}'. Total GIFs: {existingCommand.Gifs.Count}{warningMessage}";
                    }

                    return $"GIF already exists in command '{commandKey}'.";
                }

                List<string> aliasList = ParseAliases(aliases);
                _data.Commands[commandKey] = new GifCommand
                {
                    Channel = channel ?? string.Empty,
                    Pattern = pattern,
                    IsRegex = isRegex,
                    Gifs = new List<string> { gifUrl },
                    Aliases = aliasList
                };

                SaveCommands();
                BuildAliasLookup();

                string aliasInfo = aliasList.Count > 0
                    ? $" with aliases: {string.Join(", ", aliasList)}"
                    : string.Empty;
                return $"Created new command '{commandKey}' with 1 GIF{aliasInfo}.{warningMessage}";
            }
        }

        public string RemoveCommand(string commandKey, string? gifUrl = null)
        {
            if (string.IsNullOrWhiteSpace(commandKey))
            {
                return "Error: Command key cannot be empty.";
            }

            lock (_lock)
            {
                commandKey = commandKey.ToUpperInvariant();

                if (!_data.Commands.TryGetValue(commandKey, out GifCommand? command))
                {
                    return $"Error: Command '{commandKey}' not found.";
                }

                if (string.IsNullOrWhiteSpace(gifUrl))
                {
                    _data.Commands.Remove(commandKey);
                    SaveCommands();
                    BuildAliasLookup();
                    return $"Removed command '{commandKey}' entirely.";
                }

                if (command.Gifs.Remove(gifUrl))
                {
                    if (command.Gifs.Count == 0)
                    {
                        _data.Commands.Remove(commandKey);
                        SaveCommands();
                        BuildAliasLookup();
                        return $"Removed last GIF from '{commandKey}'. Command has been deleted.";
                    }

                    SaveCommands();
                    return $"Removed GIF from '{commandKey}'. Remaining GIFs: {command.Gifs.Count}";
                }

                return $"Error: GIF not found in command '{commandKey}'.";
            }
        }

        public string ListCommands(string? commandKey = null)
        {
            lock (_lock)
            {
                if (string.IsNullOrWhiteSpace(commandKey))
                {
                    if (_data.Commands.Count == 0)
                    {
                        return "No commands configured.";
                    }

                    string commandList = string.Join(", ", _data.Commands.Keys.OrderBy(k => k));
                    return $"Available commands ({_data.Commands.Count}): {commandList}";
                }

                commandKey = commandKey.ToUpperInvariant();
                if (!_data.Commands.TryGetValue(commandKey, out GifCommand? command))
                {
                    return $"Error: Command '{commandKey}' not found.";
                }

                string channelInfo = string.IsNullOrWhiteSpace(command.Channel) ? "All channels" : $"Channel: {command.Channel}";
                string patternInfo = command.IsRegex ? $"Regex: {command.Pattern}" : $"Pattern: {command.Pattern}";
                string aliasInfo = command.Aliases.Count > 0 ? $"\nAliases: {string.Join(", ", command.Aliases)}" : string.Empty;
                string gifList = string.Join("\n", command.Gifs.Select((g, i) => $"  {i + 1}. {g}"));

                return $"Command '{commandKey}':\n{channelInfo}\n{patternInfo}{aliasInfo}\nGIFs ({command.Gifs.Count}):\n{gifList}";
            }
        }

        public int GetCommandCount()
        {
            lock (_lock)
            {
                return _data.Commands.Count;
            }
        }

        public string AddAlias(string commandKey, string alias)
        {
            if (string.IsNullOrWhiteSpace(commandKey))
            {
                return "Error: Command key cannot be empty.";
            }

            if (string.IsNullOrWhiteSpace(alias))
            {
                return "Error: Alias cannot be empty.";
            }

            lock (_lock)
            {
                commandKey = commandKey.ToUpperInvariant();
                alias = alias.ToUpperInvariant();

                if (!_data.Commands.TryGetValue(commandKey, out GifCommand? command))
                {
                    return $"Error: Command '{commandKey}' not found.";
                }

                if (_data.Commands.ContainsKey(alias))
                {
                    return $"Error: '{alias}' is already a primary command.";
                }

                if (_aliasLookup.TryGetValue(alias, out string? existingAliasTarget))
                {
                    return $"Error: '{alias}' is already an alias for '{existingAliasTarget}'.";
                }

                if (command.Aliases.Contains(alias))
                {
                    return $"Error: '{alias}' is already an alias for '{commandKey}'.";
                }

                command.Aliases.Add(alias);
                SaveCommands();
                BuildAliasLookup();
                return $"Added alias '{alias}' to command '{commandKey}'.";
            }
        }

        public string RemoveAlias(string commandKey, string alias)
        {
            if (string.IsNullOrWhiteSpace(commandKey))
            {
                return "Error: Command key cannot be empty.";
            }

            if (string.IsNullOrWhiteSpace(alias))
            {
                return "Error: Alias cannot be empty.";
            }

            lock (_lock)
            {
                commandKey = commandKey.ToUpperInvariant();
                alias = alias.ToUpperInvariant();

                if (!_data.Commands.TryGetValue(commandKey, out GifCommand? command))
                {
                    return $"Error: Command '{commandKey}' not found.";
                }

                if (!command.Aliases.Contains(alias))
                {
                    return $"Error: '{alias}' is not an alias for '{commandKey}'.";
                }

                command.Aliases.Remove(alias);
                SaveCommands();
                BuildAliasLookup();
                return $"Removed alias '{alias}' from command '{commandKey}'.";
            }
        }

        public string AddAllowedChannel(string commandKey, ulong channelId)
        {
            if (string.IsNullOrWhiteSpace(commandKey))
            {
                return "Error: Command key cannot be empty.";
            }

            if (channelId == 0)
            {
                return "Error: Invalid channel ID.";
            }

            lock (_lock)
            {
                commandKey = commandKey.ToUpperInvariant();
                if (!_data.Commands.TryGetValue(commandKey, out GifCommand? command))
                {
                    return $"Error: Command '{commandKey}' not found.";
                }

                command.AllowedChannels ??= new List<ulong>();
                if (command.AllowedChannels.Contains(channelId))
                {
                    return $"Channel {channelId} is already allowed for command '{commandKey}'.";
                }

                command.AllowedChannels.Add(channelId);
                SaveCommands();
                return $"Added channel {channelId} to allowed channels for command '{commandKey}'.";
            }
        }

        public string RemoveAllowedChannel(string commandKey, ulong channelId)
        {
            if (string.IsNullOrWhiteSpace(commandKey))
            {
                return "Error: Command key cannot be empty.";
            }

            if (channelId == 0)
            {
                return "Error: Invalid channel ID.";
            }

            lock (_lock)
            {
                commandKey = commandKey.ToUpperInvariant();
                if (!_data.Commands.TryGetValue(commandKey, out GifCommand? command))
                {
                    return $"Error: Command '{commandKey}' not found.";
                }

                if (command.AllowedChannels == null || !command.AllowedChannels.Contains(channelId))
                {
                    return $"Channel {channelId} is not in the allowed channels for command '{commandKey}'.";
                }

                command.AllowedChannels.Remove(channelId);
                SaveCommands();
                return $"Removed channel {channelId} from allowed channels for command '{commandKey}'.";
            }
        }

        public string ListAllowedChannels(string commandKey)
        {
            if (string.IsNullOrWhiteSpace(commandKey))
            {
                return "Error: Command key cannot be empty.";
            }

            lock (_lock)
            {
                commandKey = commandKey.ToUpperInvariant();
                if (!_data.Commands.TryGetValue(commandKey, out GifCommand? command))
                {
                    return $"Error: Command '{commandKey}' not found.";
                }

                if (command.AllowedChannels == null || command.AllowedChannels.Count == 0)
                {
                    return $"Command '{commandKey}' has no channel restrictions (available in all channels).";
                }

                List<string> lines = command.AllowedChannels.Select(id => $"  - {id}").ToList();
                return $"Allowed channels for command '{commandKey}':\n{string.Join("\n", lines)}";
            }
        }

        public string ClearAllowedChannels(string commandKey)
        {
            if (string.IsNullOrWhiteSpace(commandKey))
            {
                return "Error: Command key cannot be empty.";
            }

            lock (_lock)
            {
                commandKey = commandKey.ToUpperInvariant();
                if (!_data.Commands.TryGetValue(commandKey, out GifCommand? command))
                {
                    return $"Error: Command '{commandKey}' not found.";
                }

                if (command.AllowedChannels == null || command.AllowedChannels.Count == 0)
                {
                    return $"Command '{commandKey}' already has no channel restrictions.";
                }

                int count = command.AllowedChannels.Count;
                command.AllowedChannels.Clear();
                SaveCommands();
                return $"Cleared all channel restrictions for command '{commandKey}' (removed {count} channels).";
            }
        }

        public async Task<string> ValidateCommandUrlsAsync(string commandKey, bool checkAccessibility = false)
        {
            if (string.IsNullOrWhiteSpace(commandKey))
            {
                return "Error: Command key cannot be empty.";
            }

            List<string> gifUrls;
            lock (_lock)
            {
                commandKey = commandKey.ToUpperInvariant();
                if (!_data.Commands.TryGetValue(commandKey, out GifCommand? command))
                {
                    return $"Error: Command '{commandKey}' not found.";
                }

                gifUrls = command.Gifs.ToList();
            }

            return await ValidateUrlsAsync(commandKey, gifUrls, checkAccessibility);
        }

        public async Task<string> ValidateAllUrlsAsync(bool checkAccessibility = false)
        {
            Dictionary<string, List<string>> commands;
            lock (_lock)
            {
                commands = _data.Commands.ToDictionary(
                    kvp => kvp.Key,
                    kvp => kvp.Value.Gifs.ToList());
            }

            int totalUrls = 0;
            int validCount = 0;
            int warningCount = 0;
            int errorCount = 0;
            List<string> problemCommands = new List<string>();

            foreach (KeyValuePair<string, List<string>> kvp in commands)
            {
                bool hasIssues = false;
                foreach (string gifUrl in kvp.Value)
                {
                    totalUrls++;
                    UrlValidationResult result = checkAccessibility
                        ? await UrlValidator.ValidateGifUrlAsync(gifUrl, true)
                        : UrlValidator.ValidateGifUrl(gifUrl);

                    if (!result.IsValid)
                    {
                        errorCount++;
                        hasIssues = true;
                    }
                    else if (result.WarningOnly)
                    {
                        warningCount++;
                        hasIssues = true;
                    }
                    else
                    {
                        validCount++;
                    }
                }

                if (hasIssues)
                {
                    problemCommands.Add(kvp.Key);
                }
            }

            List<string> lines = new List<string>
            {
                "**Global URL Validation Results**",
                "```",
                $"Total Commands: {commands.Count}",
                $"Total URLs: {totalUrls}",
                $"✓ Valid: {validCount}",
                $"⚠ Warnings: {warningCount}",
                $"❌ Invalid: {errorCount}"
            };

            if (problemCommands.Count > 0)
            {
                lines.Add(string.Empty);
                lines.Add($"Commands with issues ({problemCommands.Count}):");
                lines.AddRange(problemCommands.Take(10).Select(cmd => $"  - {cmd}"));
                if (problemCommands.Count > 10)
                {
                    lines.Add($"  ... and {problemCommands.Count - 10} more");
                }
                lines.Add(string.Empty);
                lines.Add("Use !gif-validate COMMANDNAME for details");
            }

            lines.Add("```");
            return string.Join("\n", lines);
        }

        private async Task<string> ValidateUrlsAsync(string commandKey, List<string> gifUrls, bool checkAccessibility)
        {
            List<string> issues = new List<string>();
            int validCount = 0;
            int warningCount = 0;

            foreach (string gifUrl in gifUrls)
            {
                UrlValidationResult result = checkAccessibility
                    ? await UrlValidator.ValidateGifUrlAsync(gifUrl, true)
                    : UrlValidator.ValidateGifUrl(gifUrl);

                if (!result.IsValid)
                {
                    issues.Add($"❌ {gifUrl}\n   {result.ErrorMessage}");
                }
                else if (result.WarningOnly)
                {
                    issues.Add($"⚠ {gifUrl}\n   {result.ErrorMessage}");
                    warningCount++;
                }
                else
                {
                    validCount++;
                }
            }

            List<string> lines = new List<string>
            {
                $"**Validation Results for '{commandKey}'**",
                "```",
                $"Total URLs: {gifUrls.Count}",
                $"✓ Valid: {validCount}",
                $"⚠ Warnings: {warningCount}",
                $"❌ Invalid: {issues.Count - warningCount}"
            };

            if (issues.Count > 0)
            {
                lines.Add(string.Empty);
                lines.Add("Issues Found:");
                lines.AddRange(issues);
            }

            lines.Add("```");
            return string.Join("\n", lines);
        }

        private void SaveCommands()
        {
            lock (_lock)
            {
                try
                {
                    _backupManager.BackupBeforeSave(_filePath);

                    JsonSerializerOptions options = new JsonSerializerOptions
                    {
                        WriteIndented = true,
                        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                    };

                    string json = JsonSerializer.Serialize(_data, options);
                    File.WriteAllText(_filePath, json);
                    Console.WriteLine($"Successfully saved GIF commands to {_filePath}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error saving GIF commands: {ex.Message}");
                    throw;
                }
            }
        }

        private void BuildAliasLookup()
        {
            _aliasLookup.Clear();
            foreach (KeyValuePair<string, GifCommand> kvp in _data.Commands)
            {
                foreach (string alias in kvp.Value.Aliases)
                {
                    string upperAlias = alias.ToUpperInvariant();
                    if (!_aliasLookup.ContainsKey(upperAlias))
                    {
                        _aliasLookup[upperAlias] = kvp.Key;
                    }
                }
            }
        }

        private static List<string> ParseAliases(string? aliases)
        {
            if (string.IsNullOrWhiteSpace(aliases))
            {
                return new List<string>();
            }

            return aliases.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(a => a.Trim().ToUpperInvariant())
                .Where(a => !string.IsNullOrWhiteSpace(a))
                .ToList();
        }

        private static bool IsCommandAllowedInChannel(GifCommand command, string channelName, ulong channelId)
        {
            if (command.AllowedChannels != null && command.AllowedChannels.Count > 0)
            {
                return channelId != 0 && command.AllowedChannels.Contains(channelId);
            }

            if (!string.IsNullOrWhiteSpace(command.Channel)
                && !command.Channel.Equals(channelName, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            return true;
        }
    }

    internal sealed record GifCommandDisplayInfo(
        string CommandKey,
        string Pattern,
        bool IsRegex,
        IReadOnlyList<string> Aliases,
        int GifCount);

    public class GifCommandData
    {
        public Dictionary<string, GifCommand> Commands { get; set; } = new Dictionary<string, GifCommand>();
    }

    public class GifCommand
    {
        public string Channel { get; set; } = string.Empty;
        public string Pattern { get; set; } = string.Empty;
        public bool IsRegex { get; set; }
        public List<string> Gifs { get; set; } = new List<string>();
        public List<string> Aliases { get; set; } = new List<string>();
        public List<ulong>? AllowedChannels { get; set; }
    }
}