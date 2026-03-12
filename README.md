# DongBot

Discord bot for GIF command delivery, MLB/Braves commands, admin auditing, and usage analytics.

## Highlights

- JSON-backed GIF command system with aliases, regex support, and channel restrictions
- MLB command suite (scores, schedule, standings, team/player/venue lookups)
- Braves scheduler for daily/weekly automated posts across all matching servers
- Admin-only audit and statistics reporting with per-server filtering and server-partitioned breakdowns
- Backups, URL validation, and command usage tracking

## Prerequisites

- .NET 8 SDK
- Discord bot token

### SDK troubleshooting (`NETSDK1045`)

If build output shows `NETSDK1045` (for example: "current .NET SDK does not support targeting .NET 8.0"), install a .NET 8 SDK and confirm with:

```powershell
dotnet --list-sdks
```

This repo includes `global.json` and expects an `8.x` SDK.

## Run Locally

1. Create `DongBot/token.txt` with your bot token (or set `TokenFilePath` in `botconfig.json`).
2. Build:

   ```bash
   dotnet build
   ```

3. Run:

   ```bash
   dotnet run --project DongBot
   ```

## Configuration

Primary settings are loaded from `DongBot/botconfig.json` via `BotConfig`:

- `AdminChannelName` (default: `dongbot-admin`)
- `BravesChannelName` (default: `baseball`)
- `TokenFilePath` (default: `token.txt`)
- `GifCommandsFilePath` (default: `gifcommands.json`)
- `AuditLogFilePath` (default: `bot_audit.json`)
- `StatisticsFilePath` (default: `bot_statistics.json`)
- `UserErrorReportsFilePath` (default: `user_error_reports.json`)

## Commands

Use `!help` for channel-aware help output.

### User-facing

- `!<gif-command>` (example: `!dong`)
- `!badbot [comment]` (report a bad response with optional user context)
- `!braves-schedule`, `!braves-score`, `!braves-standings`, `!braves-roster`
- `!mlb-schedule`, `!mlb-scores`, `!mlb-standings [filter]`
- `!mlb-division [name]`, `!mlb-league [name]`, `!mlb-sport [name]`
- `!mlb-team [name]`, `!mlb-venue [name]`
- `!mlb-player [name]`, `!mlb-player-stats [name] [season]`
- `!mlb-help`

### Admin-only (in configured admin channel)

- GIF admin: `!gif-add`, `!gif-remove`, `!gif-refresh`, `!gif-list`, `!gif-alias`, `!gif-channel`, `!gif-validate`
- Reporting: `!audit`, `!audit-stats`, `!badbot-list [N]`, `!release-notes [version|range]` (sparse ranges allowed), `!stats`, `!stats-top`, `!stats-user`, `!stats-command`
- Scheduler control: `!braves-scheduler-status`, `!braves-scheduler-enable`, `!braves-scheduler-disable`, `!braves-scheduler-test-daily`, `!braves-scheduler-test-weekly`

Scheduler and reporting behavior is guild-aware:

- Scheduler posts to every guild that has the configured Braves channel name, with enable/disable state tracked independently per server.
- `!audit`, `!audit-stats`, `!badbot-list`, `!stats`, and `!stats-top` return data for the current server.
- `!stats-user` and `!stats-command` return global totals with a per-server breakdown.

## Project Layout

```text
DongBot/
├─ App/                  # Bot startup and Discord event handling
├─ Commands/
│  ├─ Admin/             # Admin/reporting command managers
│  ├─ Gif/               # User GIF command manager
│  └─ MLB/               # MLB + Braves command manager
├─ Core/
│  ├─ Config/            # BotConfig
│  └─ Interfaces/        # ICommandManager, IMLBDataClient
├─ Scheduling/           # BravesScheduler
├─ Services/
│  ├─ Gif/               # GifCommandService
│  ├─ Lookup/            # Name/entity/standings resolver services
│  ├─ Operations/        # Audit, backup, stats, URL validation, MLB API client
│  └─ Reporting/         # AdminReportingService
├─ gifcommands.json
└─ botconfig.json

DongBot.Tests/
├─ App/
├─ Commands/
├─ Core/
├─ Scheduling/
├─ Services/
└─ Shared/
```

## Documentation

- Quick command reference: `QUICKREF.md`
- Braves scheduler details: `docs/BRAVES_SCHEDULER.md`
- Internal changelog: `docs/CHANGELOG_INTERNAL.md`
- User release notes: `docs/RELEASE_NOTES_USER.md`
- Admin release notes: `docs/RELEASE_NOTES_ADMIN.md`
- Discord announcement template: `docs/RELEASE_NOTES_DISCORD.md`

## Versioning

- Current app version: `2.0.1`
- Version source of truth: `DongBot/DongBot.csproj` `<Version>`
- CI guard: `.github/workflows/version-bump-required.yml`
- Auto-tag workflow: `.github/workflows/auto-tag-from-version.yml`
- Conventional PR title check: `.github/workflows/conventional-pr-title.yml`
- PR checklist template: `.github/pull_request_template.md`
- Version bump helper: `scripts/bump-version.ps1`
- Policy: each push/PR must include a version increment in `DongBot/DongBot.csproj`

### Release Process (recommended)

1. Bump `DongBot/DongBot.csproj` version.
2. Update `docs/CHANGELOG_INTERNAL.md` with technical details.
3. Update `docs/RELEASE_NOTES_USER.md` and `docs/RELEASE_NOTES_ADMIN.md` with newest-first sections.
4. Update `docs/RELEASE_NOTES_DISCORD.md` if you need a separate announcement draft.
5. Post notes in Discord with `!release-notes [version|range]` from admin channel.
6. Run `dotnet test`.
7. Push and verify CI passes.

### Quick bump command

```powershell
./scripts/bump-version.ps1 -Version 2.0.1
```

## Testing

### Standard unit tests

```powershell
dotnet test DongBot.Tests/DongBot.Tests.csproj
```

### Live MLB API integration tests

A separate set of tests makes real requests through the MLBStatsAPI SDK and are skipped by default.
To run them, pass `DongBot.Tests/live.runsettings`:

```powershell
dotnet test DongBot.Tests/DongBot.Tests.csproj --settings DongBot.Tests/live.runsettings
```

Or set the environment variable manually for the current shell session:

```powershell
$env:DONGBOT_RUN_LIVE_MLB_TESTS = "1"
dotnet test DongBot.Tests/DongBot.Tests.csproj
```

Live tests use the 2023 season and verify:
- Pitchers → pitching + fielding sections only
- Hitters → batting + baserunning + fielding sections only
- Shohei Ohtani → all four sections

## Notes

- Keep secrets out of source control (`token.txt`, custom config values).
- Test suite currently runs with `dotnet test` against `.NET 8`.
- Contribution conventions: `CONTRIBUTING.md`

## Maintainer: Private MLBStatsAPI Package

`DongBot` consumes `MLBStatsAPI` from a private GitHub Packages NuGet feed.

### Required PAT scopes (for consumers)
- `read:packages`
- `repo` (required for private repositories/packages)

### Local restore setup
`nuget.config` already includes these package sources:
- `nuget.org`
- `github-bushnov` => `https://nuget.pkg.github.com/Bushnov/index.json`

Before restore/build/test locally, authenticate once:

```powershell
dotnet nuget remove source github-bushnov
dotnet nuget add source "https://nuget.pkg.github.com/Bushnov/index.json" --name github-bushnov --username "Bushnov" --password "<YOUR_PAT>" --store-password-in-clear-text
```

Then run:

```powershell
dotnet restore DongBot.sln
dotnet build DongBot.sln
dotnet test DongBot.Tests/DongBot.Tests.csproj
```

### CI secret setup
In the GitHub repository settings for `DongBot`, add secret:
- `GH_PACKAGES_PAT` = PAT with `read:packages` + `repo`

`build-and-test.yml` uses this secret before `dotnet restore`.

### Common auth failures and quick fixes
- `401 Unauthorized`
   - PAT missing/expired/incorrect
   - regenerate PAT and update local source or CI secret
- `403 Forbidden`
   - PAT lacks permissions (`read:packages`, `repo`) or package visibility access missing
   - verify package owner/org access and token scopes
- `NU1301`
   - feed URL/auth/source configuration problem
   - verify `nuget.config` source URL and run `dotnet nuget list source`

For questions or issues, please use the GitHub issue tracker.

---

**Last Updated**: March 2026  
**Version**: 2.0.1 - MLB reliability and release note ordering improvements
