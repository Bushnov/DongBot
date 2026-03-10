# DongBot

A Discord bot for managing and serving GIF commands, with comprehensive administrative tools and analytics. Written in C# with Discord.NET.

## Features

### 🎯 Dynamic GIF System
- **JSON-based command storage**: All commands stored in `gifcommands.json` for easy management
- **Channel-aware**: Commands respect Discord channel permissions
- **Multi-channel restrictions**: Restrict commands to specific channels by ID
- **Regex support**: Advanced pattern matching for flexible command triggers
- **Command aliases**: Multiple triggers for the same command
- **Automatic backups**: All data files backed up on load and before save

### 📊 Analytics & Monitoring
- **Statistics tracking**: Track command usage, user activity, and trends
- **Audit logging**: Complete audit trail of all administrative actions
- **URL validation**: Verify GIF URLs are valid and accessible
- **Channel-based help**: Context-aware help system

### 🔐 Permission Management
- **Admin channel**: All administrative commands restricted to `#dongbot-admin`
- **Channel-based access**: Users can only use commands in channels they have access to
- **Safe permissions**: No role requirements - access controlled by Discord channel permissions

## Quick Start

### Using GIF Commands
Simply type `!<command>` in any Discord channel where the command is available:
```
!dong     - Random home run celebration GIF
!ding     - Random "ding" GIF  
!sweep    - Random sweeping GIF
!boop     - Random boop GIF
```

### Getting Help
```
!help     - Show all commands available in the current channel
```
When used in `#dongbot-admin`, shows all administrative commands as well.

## Administrative Commands

**⚠️ All administrative commands can only be used in `#dongbot-admin` channel**

### GIF Management
```
!gif-add COMMANDNAME URL [channel] [pattern] [isRegex] [aliases]
    Add a new GIF to a command or create a new command
    Example: !gif-add DONG https://giphy.com/example.gif

!gif-remove COMMANDNAME [URL]
    Remove a specific GIF or entire command
    Example: !gif-remove DONG https://giphy.com/example.gif

!gif-refresh
    Reload all commands from gifcommands.json

!gif-list [COMMANDNAME]
    List all commands or details for a specific command
    Example: !gif-list DONG

!gif-alias COMMANDNAME add|remove ALIAS
    Manage command aliases
    Example: !gif-alias DONG add DINGER

!gif-channel COMMANDNAME add|remove|list|clear [CHANNELID]
    Manage channel restrictions for commands
    Example: !gif-channel DONG add 123456789012345678

!gif-validate [COMMANDNAME] [--check-access]
    Validate GIF URLs (optionally check accessibility)
    Example: !gif-validate DONG --check-access
```

### Statistics & Analytics
```
!stats
    Show overall bot usage statistics

!stats-top [N]
    Show top N most used commands (default: 10)

!stats-user [USERNAME]
    Show statistics for a specific user

!stats-command COMMANDNAME
    Show detailed statistics for a command
```

### Audit Logging
```
!audit [limit]
    Show recent audit log entries (default: 20)

!audit-stats
    Show audit log statistics and summary
```

## Installation

### Prerequisites
- .NET 8.0 SDK or later
- Discord Bot Token

### Setup
1. Clone the repository
2. Create a `token.txt` file in `DongBot/bin/Debug/net6.0/` with your Discord bot token
3. Build the solution:
   ```
   dotnet build
   ```
4. Run the bot:
   ```
   dotnet run --project DongBot
   ```

## Configuration

### Required Files

#### Bot Token
Store your Discord bot token in `DongBot/bin/Debug/net6.0/token.txt`

**⚠️ Never commit your token to version control!**

#### GIF Commands (gifcommands.json)
All GIF commands are stored in `gifcommands.json`. You can edit this file directly, then use `!gif-refresh` to reload.

**Example structure:**
```json
{
  "commands": {
    "DONG": {
      "channel": "baseball",
      "pattern": "DONG",
      "isRegex": false,
      "gifs": [
        "https://media.giphy.com/media/example1.gif",
        "https://media.giphy.com/media/example2.gif"
      ],
      "aliases": ["DINGER", "HOMERUN"],
      "allowedChannels": [123456789012345678, 987654321098765432]
    }
  }
}
```

**Properties:**
- `channel` (string): Legacy channel restriction by name (optional)
- `pattern` (string): Command pattern to match
- `isRegex` (bool): Whether pattern is a regular expression
- `gifs` (array): List of GIF URLs
- `aliases` (array): Alternative command names
- `allowedChannels` (array): Channel IDs where command is allowed (null/empty = all channels)

### Data Files
The bot creates and manages several data files:
- `gifcommands.json` - GIF command definitions
- `bot_audit.json` - Audit log entries
- `bot_statistics.json` - Usage statistics
- `backups/` - Automatic backups of all data files

### Bot Token
Store your Discord bot token in `DongBot/bin/Debug/net6.0/token.txt`

**⚠️ Never commit your token to version control!**

## Project Structure
```
DongBot/
├── MainDong.cs              - Main bot entry point and command router
├── DBActions.cs             - Command handlers and business logic
├── GifCommandManager.cs     - GIF command management (load/save/process)
├── AuditLogger.cs           - Audit logging system
├── StatisticsTracker.cs     - Usage statistics and analytics
├── BackupManager.cs         - Automatic backup system
├── UrlValidator.cs          - URL validation with domain whitelist
├── gifcommands.json         - GIF command configuration
├── bot_audit.json           - Audit log data
├── bot_statistics.json      - Statistics data
├── backups/                 - Automatic backups directory
└── token.txt               - Discord bot token (not in repo)
```

## System Architecture

### Core Components

**MainDong.cs**
- Discord client initialization and event handling
- Command routing and permission checking
- Admin channel enforcement (`#dongbot-admin`)

**DBActions.cs**
- Business logic layer for all commands
- Coordinates between managers
- Handles command parsing and validation

**GifCommandManager.cs**
- JSON-based command storage
- Pattern matching (exact and regex)
- Channel restriction checking
- Alias resolution
- Thread-safe file operations

**AuditLogger.cs**
- Tracks all administrative actions
- Maintains limited history (configurable)
- Provides audit queries and statistics

**StatisticsTracker.cs**
- Command usage tracking
- User and channel analytics
- Daily statistics
- Trend analysis

**BackupManager.cs**
- Automatic backups on load and save
- Configurable retention (default: 10 backups)
- Framework for timed backups (disabled by default)

**UrlValidator.cs**
- URL format validation
- Domain whitelist (17 trusted GIF domains)
- Optional HTTP accessibility checks

## Development

### Recent Improvements (March 2026)
- ✅ Externalized GIF commands to JSON file system
- ✅ Added comprehensive audit logging
- ✅ Implemented statistics tracking
- ✅ Added command aliases support
- ✅ Implemented URL validation with domain whitelist
- ✅ Added automatic backup system
- ✅ Implemented multi-channel restrictions
- ✅ Added admin channel permissions (`#dongbot-admin`)
- ✅ Created context-aware help system
- ✅ Removed legacy hardcoded GIF arrays
- ✅ Thread-safe file operations throughout

### Key Features Implemented
1. **Audit Logging**: Generalized system tracks all bot operations
2. **Statistics Tracking**: Command usage, user activity, and trends
3. **Command Aliases**: Multiple triggers for same command
4. **URL Validation**: Format checks, domain whitelist, optional accessibility tests
5. **Auto-Backup**: On-load and pre-save backups with configurable retention
6. **Multi-Channel Support**: Commands can be restricted to multiple channels
7. **Permission Management**: Admin commands restricted to `#dongbot-admin`
8. **Help System**: Channel-aware help showing relevant commands only

### Contributing
1. Fork the repository
2. Create a feature branch
3. Make your changes
4. Test thoroughly
5. Submit a pull request

## Technical Details

### Thread Safety
All file operations use lock mechanisms to ensure thread-safe reads and writes across concurrent Discord message handlers.

### Backup Strategy
- **On Load**: Creates backup when loading data files
- **Before Save**: Creates backup before writing changes
- **Retention**: Keeps 10 most recent backups per file (configurable)
- **Naming**: Format `{filename}_{reason}_{timestamp}.json`

### Channel Restrictions
Commands can be restricted in two ways:
1. **Legacy**: Single channel by name (string)
2. **Modern**: Multiple channels by ID (List<ulong>)

When both are set, the modern AllowedChannels takes priority.

### URL Validation
Three validation levels:
1. **Format**: Basic URI validation (always on)
2. **Domain**: Whitelist of 17 trusted GIF domains (warns if not whitelisted)
3. **Accessibility**: HTTP HEAD request to verify URL is accessible (optional)

Whitelisted domains include: giphy.com, tenor.com, imgur.com, gfycat.com, and more.

## Potential Future Enhancements

### Not Yet Implemented
- **Command Categories**: Organize commands by theme/category
- **Rate Limiting**: Prevent command spam
- **Scheduled Tasks**: Timed automatic backups
- **Web Dashboard**: Web interface for bot management
- **Database Integration**: Move from JSON to SQL/NoSQL database
- **Embed Support**: Rich Discord embeds for responses
- **Multi-Server**: Per-server command configurations

See [IMPROVEMENTS.md](IMPROVEMENTS.md) for detailed recommendations.

## Troubleshooting

### Bot not responding
- Check `token.txt` exists and contains valid token
- Verify bot has proper Discord permissions
- Check console output for errors

### Commands not working in channel
- Use `!help` to see commands available in that channel
- Check channel restrictions with `!gif-list COMMANDNAME` in `#dongbot-admin`
- Verify channel ID is in AllowedChannels list if restricted

### Administrative commands not working
- Ensure you're in the `#dongbot-admin` channel
- Check bot has permission to send messages in that channel

## License

[Specify your license here]

## Support

For questions or issues, please use the GitHub issue tracker.

---

**Last Updated**: March 2026  
**Version**: 2.0 - Complete refactor with JSON system, analytics, and permissions
