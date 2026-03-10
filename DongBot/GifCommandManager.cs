using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace DongBot
{
    /// <summary>
    /// Manages GIF commands from an external JSON file.
    /// Supports loading, saving, adding, updating, and removing commands.
    /// </summary>
    public class GifCommandManager
    {
        private readonly string _filePath;
        private readonly Random _random;
        private GifCommandData _data;
        private readonly object _lock = new object();
        private Dictionary<string, string> _aliasLookup = new Dictionary<string, string>(); // alias -> primary command
        private readonly BackupManager _backupManager;

        public GifCommandManager(string filePath = "gifcommands.json")
        {
            _filePath = filePath;
            _random = new Random();
            
            // Initialize backup manager with backups subdirectory
            string backupDir = Path.Combine(Path.GetDirectoryName(_filePath) ?? ".", "backups");
            _backupManager = new BackupManager(backupDir, maxBackupsToKeep: 10, enableTimedBackups: false);
            
            LoadCommands();
        }

        /// <summary>
        /// Load commands from the JSON file
        /// </summary>
        public void LoadCommands()
        {
            lock (_lock)
            {
                try
                {
                    if (!File.Exists(_filePath))
                    {
                        Console.WriteLine($"GIF commands file not found: {_filePath}. Creating default file.");
                        _data = new GifCommandData { Commands = new Dictionary<string, GifCommand>() };
                        SaveCommands();
                        return;
                    }

                    // Create backup before loading
                    _backupManager.BackupOnLoad(_filePath);

                    string json = File.ReadAllText(_filePath);
                    _data = JsonSerializer.Deserialize<GifCommandData>(json, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });

                    if (_data == null || _data.Commands == null)
                    {
                        _data = new GifCommandData { Commands = new Dictionary<string, GifCommand>() };
                    }

                    // Build alias lookup dictionary
                    BuildAliasLookup();

                    Console.WriteLine($"Successfully loaded {_data.Commands.Count} GIF commands from {_filePath}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error loading GIF commands: {ex.Message}");
                    _data = new GifCommandData { Commands = new Dictionary<string, GifCommand>() };
                }
            }
        }

        /// <summary>
        /// Build the alias lookup dictionary from loaded commands
        /// </summary>
        private void BuildAliasLookup()
        {
            _aliasLookup.Clear();
            foreach ( KeyValuePair<string, GifCommand> kvp in _data.Commands)
            {
                if (kvp.Value.Aliases != null)
                {
                    foreach (string alias in kvp.Value.Aliases)
                    {
                        string upperAlias = alias.ToUpper();
                        if (!_aliasLookup.ContainsKey(upperAlias))
                        {
                            _aliasLookup[upperAlias] = kvp.Key;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Save commands to the JSON file
        /// </summary>
        public void SaveCommands()
        {
            lock (_lock)
            {
                try
                {
                    // Create backup before saving
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

        /// <summary>
        /// Process a command and return a random GIF if it matches
        /// </summary>
        /// <param name="commandText">The command text (after the ! prefix)</param>
        /// <param name="channelName">The Discord channel name</param>
        /// <returns>A random GIF URL if the command matches, empty string otherwise</returns>
        /// <summary>
        /// Process a command and return a GIF URL if found
        /// </summary>
        /// <param name="commandText">The command text to process</param>
        /// <param name="channelName">Channel name (for legacy Channel property checks and logging)</param>
        /// <param name="channelId">Channel ID (for AllowedChannels restrictions)</param>
        /// <returns>GIF URL if command matched and channel allowed, empty string otherwise</returns>
        public string ProcessCommand(string commandText, string channelName, ulong channelId = 0)
        {
            if (string.IsNullOrWhiteSpace(commandText))
            {
                return string.Empty;
            }

            lock (_lock)
            {
                string upperCommand = commandText.ToUpper();

                // First, check if this is an alias and resolve to primary command
                string primaryCommandKey = null;
                if (_aliasLookup.ContainsKey(upperCommand))
                {
                    primaryCommandKey = _aliasLookup[upperCommand];
                    if (_data.Commands.ContainsKey(primaryCommandKey))
                    {
                        GifCommand aliasCommand = _data.Commands[primaryCommandKey];
                        
                        // Check channel restrictions
                        if (!IsCommandAllowedInChannel(aliasCommand, channelName, channelId))
                        {
                            return string.Empty;
                        }
                        
                        if (aliasCommand.Gifs != null && aliasCommand.Gifs.Count > 0)
                        {
                            int randomIndex = _random.Next(aliasCommand.Gifs.Count);
                            return aliasCommand.Gifs[randomIndex];
                        }
                    }
                }

                // Then check pattern-based matching
                foreach (KeyValuePair<string, GifCommand> kvp in _data.Commands)
                {
                    GifCommand command = kvp.Value;

                    // Check channel restrictions
                    if (!IsCommandAllowedInChannel(command, channelName, channelId))
                    {
                        continue;
                    }

                    // Check if command matches
                    bool matches = false;
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

                    if (matches && command.Gifs != null && command.Gifs.Count > 0)
                    {
                        int randomIndex = _random.Next(command.Gifs.Count);
                        return command.Gifs[randomIndex];
                    }
                }
            }

            return string.Empty;
        }

        /// <summary>
        /// Check if a command is allowed in the specified channel
        /// </summary>
        /// <param name="command">The command to check</param>
        /// <param name="channelName">Channel name (for legacy Channel property)</param>
        /// <param name="channelId">Channel ID (for AllowedChannels property)</param>
        /// <returns>True if command is allowed in channel, false otherwise</returns>
        private bool IsCommandAllowedInChannel(GifCommand command, string channelName, ulong channelId)
        {
            // First check new AllowedChannels property (takes priority)
            if (command.AllowedChannels != null && command.AllowedChannels.Count > 0)
            {
                // If AllowedChannels is specified and channelId is provided, check if channel is in the list
                if (channelId != 0)
                {
                    return command.AllowedChannels.Contains(channelId);
                }
                // If channelId not provided but AllowedChannels is set, deny access
                return false;
            }

            // Fall back to legacy Channel property (string-based channel name restriction)
            if (!string.IsNullOrWhiteSpace(command.Channel) && 
                !command.Channel.Equals(channelName, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            // No restrictions, allow the command
            return true;
        }

        /// <summary>
        /// Add or update a command
        /// </summary>
        /// <param name="commandKey">The command key (e.g., "DONG")</param>
        /// <param name="gifUrl">The GIF URL to add</param>
        /// <param name="channel">Optional channel restriction</param>
        /// <param name="pattern">Optional custom pattern (defaults to commandKey)</param>
        /// <param name="isRegex">Whether the pattern is a regex</param>
        /// <param name="aliases">Optional comma-separated list of aliases</param>
        /// <returns>A success message</returns>
        public string AddOrUpdateCommand(string commandKey, string gifUrl, string channel = "", string pattern = null, bool isRegex = false, string aliases = null)
        {
            if (string.IsNullOrWhiteSpace(commandKey))
            {
                return "Error: Command key cannot be empty.";
            }

            if (string.IsNullOrWhiteSpace(gifUrl))
            {
                return "Error: GIF URL cannot be empty.";
            }

            // Validate URL
            UrlValidationResult validation = UrlValidator.ValidateGifUrl(gifUrl);
            if (!validation.IsValid && !validation.WarningOnly)
            {
                return $"Error: {validation.ErrorMessage}";
            }

            lock (_lock)
            {
                commandKey = commandKey.ToUpper();
                pattern = pattern ?? commandKey;

                string warningMessage = validation.WarningOnly ? $"\n⚠ {validation.ErrorMessage}" : "";

                if (_data.Commands.ContainsKey(commandKey))
                {
                    // Command exists - add the GIF to the list
                    if (!_data.Commands[commandKey].Gifs.Contains(gifUrl))
                    {
                        _data.Commands[commandKey].Gifs.Add(gifUrl);
                        SaveCommands();
                        return $"Added new GIF to existing command '{commandKey}'. Total GIFs: {_data.Commands[commandKey].Gifs.Count}{warningMessage}";
                    }
                    else
                    {
                        return $"GIF already exists in command '{commandKey}'.";
                    }
                }
                else
                {
                    // Parse aliases if provided
                    List<string> aliasList = null;
                    if (!string.IsNullOrWhiteSpace(aliases))
                    {
                        aliasList = aliases.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                            .Select(a => a.Trim().ToUpper())
                            .Where(a => !string.IsNullOrWhiteSpace(a))
                            .ToList();
                    }

                    // Create new command
                    _data.Commands[commandKey] = new GifCommand
                    {
                        Channel = channel ?? string.Empty,
                        Pattern = pattern,
                        IsRegex = isRegex,
                        Gifs = new List<string> { gifUrl },
                        Aliases = aliasList
                    };
                    SaveCommands();
                    BuildAliasLookup(); // Rebuild alias lookup
                    
                    string aliasInfo = aliasList != null && aliasList.Count > 0 
                        ? $" with aliases: {string.Join(", ", aliasList)}" 
                        : "";
                    return $"Created new command '{commandKey}' with 1 GIF{aliasInfo}.{warningMessage}";
                }
            }
        }

        /// <summary>
        /// Remove a GIF from a command or remove the entire command
        /// </summary>
        /// <param name="commandKey">The command key</param>
        /// <param name="gifUrl">Optional: specific GIF URL to remove. If null, removes entire command.</param>
        /// <returns>A success message</returns>
        public string RemoveCommand(string commandKey, string gifUrl = null)
        {
            if (string.IsNullOrWhiteSpace(commandKey))
            {
                return "Error: Command key cannot be empty.";
            }

            lock (_lock)
            {
                commandKey = commandKey.ToUpper();

                if (!_data.Commands.ContainsKey(commandKey))
                {
                    return $"Error: Command '{commandKey}' not found.";
                }

                if (string.IsNullOrWhiteSpace(gifUrl))
                {
                    // Remove entire command
                    _data.Commands.Remove(commandKey);
                    SaveCommands();
                    return $"Removed command '{commandKey}' entirely.";
                }
                else
                {
                    // Remove specific GIF
                    GifCommand command = _data.Commands[commandKey];
                    if (command.Gifs.Remove(gifUrl))
                    {
                        if (command.Gifs.Count == 0)
                        {
                            // No GIFs left, remove the command entirely
                            _data.Commands.Remove(commandKey);
                            SaveCommands();
                            return $"Removed last GIF from '{commandKey}'. Command has been deleted.";
                        }
                        else
                        {
                            SaveCommands();
                            return $"Removed GIF from '{commandKey}'. Remaining GIFs: {command.Gifs.Count}";
                        }
                    }
                    else
                    {
                        return $"Error: GIF not found in command '{commandKey}'.";
                    }
                }
            }
        }

        /// <summary>
        /// List all commands or get details about a specific command
        /// </summary>
        /// <param name="commandKey">Optional: specific command to get details for</param>
        /// <returns>Command list or details</returns>
        public string ListCommands(string commandKey = null)
        {
            lock (_lock)
            {
                if (string.IsNullOrWhiteSpace(commandKey))
                {
                    // List all commands
                    if (_data.Commands.Count == 0)
                    {
                        return "No commands configured.";
                    }

                    string commandList = string.Join(", ", _data.Commands.Keys.OrderBy(k => k));
                    return $"Available commands ({_data.Commands.Count}): {commandList}";
                }
                else
                {
                    // Get details for specific command
                    commandKey = commandKey.ToUpper();
                    if (!_data.Commands.ContainsKey(commandKey))
                    {
                        return $"Error: Command '{commandKey}' not found.";
                    }

                    GifCommand command = _data.Commands[commandKey];
                    string channelInfo = string.IsNullOrWhiteSpace(command.Channel) ? "All channels" : $"Channel: {command.Channel}";
                    string patternInfo = command.IsRegex ? $"Regex: {command.Pattern}" : $"Pattern: {command.Pattern}";
                    string aliasInfo = command.Aliases != null && command.Aliases.Count > 0 
                        ? $"\nAliases: {string.Join(", ", command.Aliases)}" 
                        : "";
                    string gifList = string.Join("\n", command.Gifs.Select((g, i) => $"  {i + 1}. {g}"));

                    return $"Command '{commandKey}':\n{channelInfo}\n{patternInfo}{aliasInfo}\nGIFs ({command.Gifs.Count}):\n{gifList}";
                }
            }
        }

        /// <summary>
        /// Get the count of commands currently loaded
        /// </summary>
        public int GetCommandCount()
        {
            lock (_lock)
            {
                return _data?.Commands?.Count ?? 0;
            }
        }

        /// <summary>
        /// Add an alias to an existing command
        /// </summary>
        /// <param name="commandKey">The command key</param>
        /// <param name="alias">The alias to add</param>
        /// <returns>A success message</returns>
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
                commandKey = commandKey.ToUpper();
                alias = alias.ToUpper();

                if (!_data.Commands.ContainsKey(commandKey))
                {
                    return $"Error: Command '{commandKey}' not found.";
                }

                // Check if alias is already used by another command
                if (_data.Commands.ContainsKey(alias))
                {
                    return $"Error: '{alias}' is already a primary command.";
                }

                if (_aliasLookup.ContainsKey(alias))
                {
                    return $"Error: '{alias}' is already an alias for '{_aliasLookup[alias]}'.";
                }

                GifCommand command = _data.Commands[commandKey];
                if (command.Aliases == null)
                {
                    command.Aliases = new List<string>();
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

        /// <summary>
        /// Remove an alias from a command
        /// </summary>
        /// <param name="commandKey">The command key</param>
        /// <param name="alias">The alias to remove</param>
        /// <returns>A success message</returns>
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
                commandKey = commandKey.ToUpper();
                alias = alias.ToUpper();

                if (!_data.Commands.ContainsKey(commandKey))
                {
                    return $"Error: Command '{commandKey}' not found.";
                }

                GifCommand command = _data.Commands[commandKey];
                if (command.Aliases == null || !command.Aliases.Contains(alias))
                {
                    return $"Error: '{alias}' is not an alias for '{commandKey}'.";
                }

                command.Aliases.Remove(alias);
                SaveCommands();
                BuildAliasLookup();
                return $"Removed alias '{alias}' from command '{commandKey}'.";
            }
        }

        // Channel Restriction Management Methods

        /// <summary>
        /// Add a channel to the allowed channels list for a command
        /// </summary>
        /// <param name="commandKey">The command key</param>
        /// <param name="channelId">The channel ID to add</param>
        /// <returns>Result message</returns>
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
                commandKey = commandKey.ToUpper();

                if (!_data.Commands.ContainsKey(commandKey))
                {
                    return $"Error: Command '{commandKey}' not found.";
                }

                GifCommand command = _data.Commands[commandKey];
                
                // Initialize AllowedChannels list if null
                if (command.AllowedChannels == null)
                {
                    command.AllowedChannels = new List<ulong>();
                }

                // Check if channel is already in the list
                if (command.AllowedChannels.Contains(channelId))
                {
                    return $"Channel {channelId} is already allowed for command '{commandKey}'.";
                }

                command.AllowedChannels.Add(channelId);
                SaveCommands();
                return $"Added channel {channelId} to allowed channels for command '{commandKey}'.";
            }
        }

        /// <summary>
        /// Remove a channel from the allowed channels list for a command
        /// </summary>
        /// <param name="commandKey">The command key</param>
        /// <param name="channelId">The channel ID to remove</param>
        /// <returns>Result message</returns>
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
                commandKey = commandKey.ToUpper();

                if (!_data.Commands.ContainsKey(commandKey))
                {
                    return $"Error: Command '{commandKey}' not found.";
                }

                GifCommand command = _data.Commands[commandKey];
                
                if (command.AllowedChannels == null || !command.AllowedChannels.Contains(channelId))
                {
                    return $"Channel {channelId} is not in the allowed channels for command '{commandKey}'.";
                }

                command.AllowedChannels.Remove(channelId);
                SaveCommands();
                return $"Removed channel {channelId} from allowed channels for command '{commandKey}'.";
            }
        }

        /// <summary>
        /// List all allowed channels for a command
        /// </summary>
        /// <param name="commandKey">The command key</param>
        /// <returns>List of allowed channels or status message</returns>
        public string ListAllowedChannels(string commandKey)
        {
            if (string.IsNullOrWhiteSpace(commandKey))
            {
                return "Error: Command key cannot be empty.";
            }

            lock (_lock)
            {
                commandKey = commandKey.ToUpper();

                if (!_data.Commands.ContainsKey(commandKey))
                {
                    return $"Error: Command '{commandKey}' not found.";
                }

                GifCommand command = _data.Commands[commandKey];
                
                if (command.AllowedChannels == null || command.AllowedChannels.Count == 0)
                {
                    return $"Command '{commandKey}' has no channel restrictions (available in all channels).";
                }

                StringBuilder sb = new StringBuilder();
                sb.AppendLine($"Allowed channels for command '{commandKey}':");
                foreach (ulong channelId in command.AllowedChannels)
                {
                    sb.AppendLine($"  - {channelId}");
                }
                return sb.ToString().TrimEnd();
            }
        }

        /// <summary>
        /// Clear all channel restrictions for a command (allow in all channels)
        /// </summary>
        /// <param name="commandKey">The command key</param>
        /// <returns>Result message</returns>
        public string ClearAllowedChannels(string commandKey)
        {
            if (string.IsNullOrWhiteSpace(commandKey))
            {
                return "Error: Command key cannot be empty.";
            }

            lock (_lock)
            {
                commandKey = commandKey.ToUpper();

                if (!_data.Commands.ContainsKey(commandKey))
                {
                    return $"Error: Command '{commandKey}' not found.";
                }

                GifCommand command = _data.Commands[commandKey];
                
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

        /// <summary>
        /// Validate all URLs in a specific command
        /// </summary>
        /// <param name="commandKey">The command key to validate</param>
        /// <param name="checkAccessibility">Whether to check URL accessibility (slower)</param>
        /// <returns>Validation results</returns>
        public async Task<string> ValidateCommandUrlsAsync(string commandKey, bool checkAccessibility = false)
        {
            if (string.IsNullOrWhiteSpace(commandKey))
            {
                return "Error: Command key cannot be empty.";
            }

            lock (_lock)
            {
                commandKey = commandKey.ToUpper();

                if (!_data.Commands.ContainsKey(commandKey))
                {
                    return $"Error: Command '{commandKey}' not found.";
                }
            }

            // Release lock before async operations
            GifCommand command;
            lock (_lock)
            {
                command = _data.Commands[commandKey];
            }

            List<string> issues = new List<string>();
            int validCount = 0;
            int warningCount = 0;

            foreach (string gifUrl in command.Gifs)
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

            StringBuilder sb = new StringBuilder();
            sb.AppendLine($"**Validation Results for '{commandKey}'**");
            sb.AppendLine($"```");
            sb.AppendLine($"Total URLs: {command.Gifs.Count}");
            sb.AppendLine($"✓ Valid: {validCount}");
            sb.AppendLine($"⚠ Warnings: {warningCount}");
            sb.AppendLine($"❌ Invalid: {issues.Count - warningCount}");

            if (issues.Count > 0)
            {
                sb.AppendLine();
                sb.AppendLine("Issues Found:");
                foreach (string issue in issues)
                {
                    sb.AppendLine(issue);
                }
            }

            sb.AppendLine($"```");
            return sb.ToString();
        }

        /// <summary>
        /// Validate all URLs in all commands
        /// </summary>
        /// <param name="checkAccessibility">Whether to check URL accessibility (slower)</param>
        /// <returns>Validation results summary</returns>
        public async Task<string> ValidateAllUrlsAsync(bool checkAccessibility = false)
        {
            Dictionary<string, GifCommand> commands;
            lock (_lock)
            {
                commands = new Dictionary<string, GifCommand>(_data.Commands);
            }

            int totalUrls = 0;
            int validCount = 0;
            int warningCount = 0;
            int errorCount = 0;
            List<string> problemCommands = new List<string>();

            foreach ( KeyValuePair<string, GifCommand> kvp in commands)
            {
                string commandKey = kvp.Key;
                GifCommand command = kvp.Value;
                bool hasIssues = false;

                foreach (string gifUrl in command.Gifs)
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
                    problemCommands.Add(commandKey);
                }
            }

            StringBuilder sb = new StringBuilder();
            sb.AppendLine($"**Global URL Validation Results**");
            sb.AppendLine($"```");
            sb.AppendLine($"Total Commands: {commands.Count}");
            sb.AppendLine($"Total URLs: {totalUrls}");
            sb.AppendLine($"✓ Valid: {validCount}");
            sb.AppendLine($"⚠ Warnings: {warningCount}");
            sb.AppendLine($"❌ Invalid: {errorCount}");

            if (problemCommands.Count > 0)
            {
                sb.AppendLine();
                sb.AppendLine($"Commands with issues ({problemCommands.Count}):");
                foreach (string cmd in problemCommands.Take(10))
                {
                    sb.AppendLine($"  - {cmd}");
                }
                if (problemCommands.Count > 10)
                {
                    sb.AppendLine($"  ... and {problemCommands.Count - 10} more");
                }
                sb.AppendLine();
                sb.AppendLine("Use !gif-validate COMMANDNAME for details");
            }

            sb.AppendLine($"```");
            return sb.ToString();
        }

        /// <summary>
        /// Get help information for commands available in a specific channel
        /// </summary>
        /// <param name="channelName">The channel name</param>
        /// <param name="channelId">The channel ID</param>
        /// <param name="isAdminChannel">Whether this is the admin channel</param>
        /// <returns>Help text showing available commands</returns>
        public string GetChannelHelp(string channelName, ulong channelId, bool isAdminChannel)
        {
            lock (_lock)
            {
                // Get commands available in this channel
                List<KeyValuePair<string, GifCommand>> availableCommands = _data.Commands
                    .Where(kvp => IsCommandAllowedInChannel(kvp.Value, channelName, channelId))
                    .OrderBy(kvp => kvp.Key)
                    .ToList();

                StringBuilder sb = new StringBuilder();
                sb.AppendLine($"**DongBot Help - {channelName}**");
                sb.AppendLine();

                if (availableCommands.Count > 0)
                {
                    sb.AppendLine($"**Available GIF Commands ({availableCommands.Count}):**");
                    sb.AppendLine("```");
                    
                    foreach (KeyValuePair<string, GifCommand> kvp in availableCommands)
                    {
                        string commandKey = kvp.Key;
                        GifCommand command = kvp.Value;
                        
                        // Show command pattern
                        string pattern = command.IsRegex ? $"(regex: {command.Pattern})" : $"!{command.Pattern.ToLower()}";
                        sb.Append($"  {pattern}");
                        
                        // Show aliases if any
                        if (command.Aliases != null && command.Aliases.Count > 0)
                        {
                            sb.Append($" [Aliases: {string.Join(", ", command.Aliases.Select(a => "!" + a.ToLower()))}]");
                        }
                        
                        // Show GIF count
                        sb.AppendLine($" - {command.Gifs.Count} GIF(s)");
                    }
                    
                    sb.AppendLine("```");
                }
                else
                {
                    sb.AppendLine("*No GIF commands are available in this channel.*");
                    sb.AppendLine();
                }

                // Show admin commands if in admin channel
                if (isAdminChannel)
                {
                    sb.AppendLine();
                    sb.AppendLine("**Administrative Commands:**");
                    sb.AppendLine("```");
                    sb.AppendLine("GIF Management:");
                    sb.AppendLine("  !gif-add COMMANDNAME URL [channel] [pattern] [isRegex] [aliases]");
                    sb.AppendLine("  !gif-remove COMMANDNAME URL");
                    sb.AppendLine("  !gif-refresh - Reload commands from file");
                    sb.AppendLine("  !gif-list [COMMANDNAME] - List all or specific command");
                    sb.AppendLine("  !gif-alias COMMANDNAME add|remove ALIAS");
                    sb.AppendLine("  !gif-channel COMMANDNAME add|remove|list|clear [CHANNELID]");
                    sb.AppendLine("  !gif-validate [COMMANDNAME] [--check-access]");
                    sb.AppendLine();
                    sb.AppendLine("Audit & Statistics:");
                    sb.AppendLine("  !audit [limit] - Show recent audit log entries");
                    sb.AppendLine("  !audit-stats - Show audit statistics");
                    sb.AppendLine("  !stats - Show overall bot statistics");
                    sb.AppendLine("  !stats-top [N] - Show top N commands");
                    sb.AppendLine("  !stats-user [USERNAME] - Show user statistics");
                    sb.AppendLine("  !stats-command COMMANDNAME - Show command statistics");
                    sb.AppendLine("```");
                    sb.AppendLine();
                    sb.AppendLine("*Note: All administrative commands can only be used in #dongbot-admin*");
                }
                else
                {
                    sb.AppendLine();
                    sb.AppendLine("*For administrative commands, use !help in #dongbot-admin*");
                }

                sb.AppendLine();
                sb.AppendLine("**General Usage:**");
                sb.AppendLine("Type `!COMMANDNAME` to get a random GIF from that command.");
                sb.AppendLine("Multiple GIFs per command are selected randomly each time.");

                return sb.ToString();
            }
        }
    }

    /// <summary>
    /// Root data structure for the JSON file
    /// </summary>
    public class GifCommandData
    {
        public Dictionary<string, GifCommand> Commands { get; set; }
    }

    /// <summary>
    /// Represents a single GIF command
    /// </summary>
    public class GifCommand
    {
        public string Channel { get; set; }
        public string Pattern { get; set; }
        public bool IsRegex { get; set; }
        public List<string> Gifs { get; set; }
        public List<string> Aliases { get; set; }
        public List<ulong> AllowedChannels { get; set; } // null or empty = unrestricted (all channels)
    }
}
