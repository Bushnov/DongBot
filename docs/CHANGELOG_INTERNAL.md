# DongBot Internal Changelog

Detailed, developer-oriented release history.

## 2.0.0 - 2026-03-11

### Release Scope Summary (local vs `origin/main`)
- This release is a major refactor + governance baseline.
- Diff scan performed against `origin/main` before release-note finalization.
- Net shape of changes:
  - legacy flat source files removed from `DongBot/` root
  - new structured source/test trees added
  - command behavior expanded (`MLB`, `BADBOT`, admin reporting)
  - versioning/release automation introduced under `.github/` and `scripts/`

### Architecture and Repository Structure
- Main project reorganized into feature folders:
  - `DongBot/App`
  - `DongBot/Commands/{Admin,Gif,MLB}`
  - `DongBot/Core/{Config,Interfaces}`
  - `DongBot/Scheduling`
  - `DongBot/Services/{Gif,Lookup,Operations,Reporting}`
- Tests reorganized to mirror source domains under `DongBot.Tests/`.
- Removed stale/legacy top-level implementation files and reports that no longer matched current architecture.

### Command System and Runtime Behavior
- `MainDong` updated to:
  - centralize command dispatch through managers
  - preserve per-user last successful command context
  - handle `!badbot [comment]` directly for user error reporting
- `AdminCommandManager` expanded to support `!badbot-list [N]`.
- Help surface updated across managers and docs for the expanded command set.

### MLB Refactor (Major)
- Introduced reusable lookup/filter services:
  - `INameIdResolver` / `DefaultNameIdResolver`
  - `IStandingsFilterService` / `StandingsFilterService`
  - `IEntityLookupService` / `EntityLookupService`
- Added typed lookup outcomes via `EntityLookupStatus` and typed results for team/player/venue resolution.
- `MLBCommandManager` now delegates:
  - name/ID resolution
  - standings filter parsing/matching
  - entity lookup orchestration
- Added/expanded command coverage for:
  - `!mlb-venue`
  - `!mlb-division`
  - `!mlb-league`
  - `!mlb-sport`
- Behavior improvements:
  - better best-match handling for real-name queries
  - clear separation of “not found” vs “data unavailable” responses

### User Error Reporting (New)
- Added new report command:
  - `!badbot [comment]`
- Report payload captures:
  - user identity
  - channel
  - previous command context (if available)
  - optional user free-text comment
- Added dedicated persistence:
  - `UserErrorReportLogger`
  - default file: `user_error_reports.json`
- Dual-write model for traceability:
  - dedicated user error report file
  - standard audit log (`AuditLogger`)
- Added admin report viewer:
  - `!badbot-list [N]` (bounded request size, newest-first view)

### Versioning, Release, and Governance (New Baseline)
- Application version metadata formalized in `DongBot/DongBot.csproj`:
  - `Version = 2.0.0`
  - `AssemblyVersion = 2.0.0.0`
  - `FileVersion = 2.0.0.0`
  - `InformationalVersion = 2.0.0`
- Added CI guard workflow:
  - `.github/workflows/version-bump-required.yml`
  - enforces `<Version>` bump on pushes/PRs with code changes
- Added auto-tag workflow:
  - `.github/workflows/auto-tag-from-version.yml`
  - creates `vX.Y.Z` tags from csproj version on main pushes
- Added conventional PR title enforcement:
  - `.github/workflows/conventional-pr-title.yml`
- Added PR checklist template:
  - `.github/pull_request_template.md`
- Added version bump helper script:
  - `scripts/bump-version.ps1`
- Added contribution standards:
  - `CONTRIBUTING.md`

### Documentation Refresh
- Reworked docs to align with current architecture and commands:
  - `README.md`
  - `QUICKREF.md`
  - `docs/BRAVES_SCHEDULER.md`
- Added release artifacts:
  - `docs/RELEASE_NOTES_USER.md`
  - `docs/RELEASE_NOTES_ADMIN.md`
  - `docs/RELEASE_NOTES_DISCORD.md` (announcement template/index)

### Testing and Validation
- Expanded test coverage across:
  - MLB command branches
  - admin routing and report commands
  - badbot/report logger behavior
  - config binding for new settings
- Latest validated suite state before release packaging:
  - 158 tests passing
- Coverage pass includes targeted `MLBCommandManager` branch increases versus earlier baseline.

---

## Changelog Guidelines
- Keep this file technical and implementation-focused.
- Include architecture changes, behavior changes, persistence/schema changes, and test impact.
- Mirror each public release with:
  - `docs/RELEASE_NOTES_USER.md` (end-user)
  - `docs/RELEASE_NOTES_ADMIN.md` (operators/admins)
