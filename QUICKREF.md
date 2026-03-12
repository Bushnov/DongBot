# DongBot Quick Reference

## Everyday Commands

- `!help` — show commands available in the current channel
- `!<gif-command>` — run a GIF command (example: `!dong`)
- `!badbot [comment]` — report a bad bot response and include optional details
- `!mlb-help` — MLB command help

## MLB Commands

- `!braves-schedule`
- `!braves-score`
- `!braves-standings`
- `!braves-roster`
- `!mlb-schedule`
- `!mlb-scores`
- `!mlb-standings [filter]`
- `!mlb-division [name]`
- `!mlb-league [name]`
- `!mlb-sport [name]`
- `!mlb-team [name]`
- `!mlb-venue [name]`
- `!mlb-player [name]`
- `!mlb-player-stats [name] [season]`

## Admin Commands

These commands are restricted to the configured admin channel (`AdminChannelName` in `botconfig.json`).

### GIF Admin

- `!gif-add COMMAND URL [channel] [pattern] [isRegex] [aliases]`
- `!gif-remove COMMAND URL`
- `!gif-refresh`
- `!gif-list [COMMAND]`
- `!gif-alias COMMAND add|remove ALIAS`
- `!gif-channel COMMAND add|remove|list|clear [CHANNELID]`
- `!gif-validate [COMMAND] [--check-access]`

### Reporting

- `!audit [limit]`
- `!audit-stats`
- `!badbot-list [N]`
- `!release-notes [version|range]`
- `!stats`
- `!stats-top [N]`
- `!stats-user [USER_ID]`
- `!stats-command COMMAND`

### Scheduler Control

- `!braves-scheduler-status`
- `!braves-scheduler-enable`
- `!braves-scheduler-disable`
- `!braves-scheduler-test-daily`
- `!braves-scheduler-test-weekly`

## Key Files

- GIF command data: `DongBot/gifcommands.json`
- Runtime config: `DongBot/botconfig.json`
- Token file (default): `DongBot/token.txt`
- Braves scheduler docs: `docs/BRAVES_SCHEDULER.md`

## Useful Dev Commands

```bash
dotnet build
dotnet test
dotnet run --project DongBot
```
