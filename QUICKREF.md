# DongBot Quick Reference

## GIF Commands (User)

### Using Commands
Format: `!<command>`

**Popular Commands:**
- `!dong` - Home run celebrations
- `!ding` - Ding sounds/reactions
- `!sweep` - Sweeping animations
- `!boop` - Booping GIFs
- `!wash` / `!windmill` - Ron Washington GIFs
- `!thicc` - Thicc memes
- `!salami` - Grand salami references
- `!gameday` - Game day hype
- `!dumpsterfire` - Dumpster fire GIFs
- `!noice` - Nice reactions
- `!myman` - "My man" reactions
- `!toot` - Toot horn GIFs

**See all commands:**
```
!gif-list
```

**See details for a specific command:**
```
!gif-list DONG
```

## GIF Management (Admin)

### Add New GIF to Existing Command
```
!gif-add COMMANDNAME https://url-to-gif.com
```

### Add New GIF with Channel Restriction
```
!gif-add COMMANDNAME https://url-to-gif.com baseball
```

### Create Entirely New Command
```
!gif-add NEWCOMMAND https://url-to-gif.com channel-name
```

### Remove Specific GIF from Command
```
!gif-remove COMMANDNAME https://url-to-gif-to-remove.com
```

### Remove Entire Command
```
!gif-remove COMMANDNAME
```

### Reload Commands from File
Useful after manually editing gifcommands.json:
```
!gif-refresh
```

## Command Syntax Reference

### gif-add
```
!gif-add <COMMAND> <URL> [CHANNEL] [PATTERN] [ISREGEX]
```
- `COMMAND` - Command name (required)
- `URL` - GIF URL (required)
- `CHANNEL` - Channel restriction (optional, "" for all channels)
- `PATTERN` - Custom regex pattern (optional, defaults to COMMAND)
- `ISREGEX` - true/false for regex patterns (optional, defaults to false)

**Examples:**
```
!gif-add EXCITED https://giphy.com/excited.gif
!gif-add HYPE https://tenor.com/hype.gif baseball
!gif-add FUZZY https://example.com/fuzzy.gif "" "FU+Z+Y$" true
```

### gif-remove
```
!gif-remove <COMMAND> [URL]
```
- `COMMAND` - Command name (required)
- `URL` - Specific GIF to remove (optional, if omitted removes entire command)

**Examples:**
```
!gif-remove OLDCOMMAND
!gif-remove DONG https://broken-link.com/gif.gif
```

### gif-list
```
!gif-list [COMMAND]
```
- `COMMAND` - Specific command to view (optional, if omitted lists all commands)

**Examples:**
```
!gif-list
!gif-list DONG
```

### gif-refresh
```
!gif-refresh
```
No parameters needed.

## Tips & Tricks

### Testing New Commands
1. Add the command with `!gif-add`
2. Test it immediately with `!<command>`
3. If it doesn't work, check with `!gif-list <command>`
4. Remove with `!gif-remove` if needed

### Bulk Editing
For adding many GIFs at once:
1. Edit `gifcommands.json` directly
2. Use `!gif-refresh` to reload

### Channel Restrictions
- Use `baseball` for baseball channel only
- Use `mls` for MLS/soccer channel only
- Use `college-sports` for college sports channel
- Use `""` or leave empty for all channels

### Regular Expressions
For advanced pattern matching:
```
!gif-add BRAVOS https://example.com/braves.gif baseball "BRAV(O|E)S$" true
```
This matches both "BRAVOS" and "BRAVES"

### Finding Broken Links
```
!gif-list COMMAND
```
Then manually check each URL or use the output to identify broken links.

## Troubleshooting

### Command Not Working
1. Check if command exists: `!gif-list`
2. Check command details: `!gif-list COMMAND`
3. Verify channel restrictions
4. Try refreshing: `!gif-refresh`

### GIF Not Adding
- Ensure URL is valid and accessible
- Check for typos in command name
- Verify you have admin permissions
- Make sure URL doesn't already exist

### Changes Not Appearing
- Use `!gif-refresh` after manual file edits
- Check console for error messages
- Verify JSON file is valid (use JSON validator)

## File Locations

- **GIF Commands**: `DongBot/gifcommands.json`
- **Bot Token**: `DongBot/bin/Debug/net6.0/token.txt`
- **Backups**: Manually backup `gifcommands.json` before major changes

## Best Practices

### For Admins
- Test new commands in a test channel first
- Keep backup of gifcommands.json
- Document unusual regex patterns
- Periodically review and remove broken GIF links
- Use descriptive command names

### For Users
- Use commands in appropriate channels
- Check `!gif-list` to discover new commands
- Report broken GIF links to admins

---

**Last Updated**: March 9, 2026
