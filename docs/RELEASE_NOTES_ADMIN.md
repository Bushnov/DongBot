# DongBot Admin Release Notes

Audience: bot operators/admin-channel users.

## v2.0.0 (2026-03-11)

This release introduces a major refactor, stronger release governance, and new operational reporting workflows.

---

## Operational Changes

### 1) New user issue reporting workflow

Users can now submit issues directly from Discord:
```text
!badbot the gif link is broken
```

Admin-facing review command:
```text
!badbot-list [N]
```

Examples:
```text
!badbot-list
!badbot-list 50
```

What `!badbot-list` shows:
- timestamp
- username/channel
- previous command context
- user comment

Data persistence:
- dedicated file: `user_error_reports.json`
- mirrored audit trail entry in standard audit log

### 1b) New release publication command

Admins can post user-facing release notes directly to `#dongdot`:

```text
!release-notes
!release-notes 2.0.0
!release-notes v2.0.0
!release-notes 2.0.0..2.0.2
!release-notes 2.0.0-2.0.2
```

Behavior:
- Uses `docs/RELEASE_NOTES_USER.md` as the source file.
- With no version argument, posts the latest release section.
- With a version argument, posts only that version block.
- With a range, posts all matching versions inclusively (useful for bundling several small releases).
- Ranges are sparse-safe: if some versions in the requested range do not exist yet, the command still posts any versions that do exist inside that range.
- Automatically splits long content into multiple Discord messages when needed.

### 2) Command and MLB refactor outcomes
- MLB lookup behavior moved to dedicated services (name resolver, standings filter, entity lookup orchestration).
- Added commands:
  - `!mlb-venue [name]`
  - `!mlb-division [name]`
  - `!mlb-league [name]`
  - `!mlb-sport [name]`
- Error semantics improved for player/team/venue paths:
  - “not found” vs “data unavailable”

### 3) Repository and file layout reorganization
- Source now grouped by role (`App`, `Commands`, `Core`, `Scheduling`, `Services`).
- Tests reorganized to parallel source domains.
- Legacy/stale root-level implementation/docs removed.

---

## Release Governance / Versioning

### 1) Version source of truth
- `DongBot/DongBot.csproj`
- Current version: `2.0.0`

### 2) Enforced version bump workflow
- `.github/workflows/version-bump-required.yml`
- Fails CI if code changes are pushed without incrementing `<Version>`.

### 3) Auto-tagging on main
- `.github/workflows/auto-tag-from-version.yml`
- Creates annotated `vX.Y.Z` tag from csproj version.

### 4) Conventional PR titles
- `.github/workflows/conventional-pr-title.yml`
- PR titles must follow conventional format (`feat:`, `fix:`, `docs:`, etc.)

### 5) PR checklist and release hygiene
- `.github/pull_request_template.md`
- Includes version bump + changelog/release-note checklist.

### 6) Version bump helper
Use:
```powershell
./scripts/bump-version.ps1 -Version 2.0.1
```

Script behavior:
- updates csproj version metadata
- prepends starter sections to:
  - `docs/CHANGELOG_INTERNAL.md`
  - `docs/RELEASE_NOTES_DISCORD.md`

---

## Admin Quick Reference (v2.0.0)

### User report review
```text
!badbot-list [N]
!release-notes [version|range]
```

### Existing admin reporting
```text
!audit [limit]
!audit-stats
!stats
!stats-top [N]
!stats-user [USERNAME]
!stats-command COMMAND
```

### GIF administration
```text
!gif-add COMMAND URL [channel] [pattern] [isRegex] [aliases]
!gif-remove COMMAND [URL]
!gif-refresh
!gif-list [COMMAND]
!gif-alias COMMAND add|remove ALIAS
!gif-channel COMMAND add|remove|list|clear [CHANNELID]
!gif-validate [COMMAND] [--check-access]
```

### Scheduler controls
```text
!braves-scheduler-status
!braves-scheduler-enable
!braves-scheduler-disable
!braves-scheduler-test-daily
!braves-scheduler-test-weekly
```

---

## Recommended Admin Rollout Message
```text
DongBot v2.0.0 is now live.
If users see any bad output, ask them to run:
!badbot [short comment]

Admins can review submissions with:
!badbot-list 50
```
