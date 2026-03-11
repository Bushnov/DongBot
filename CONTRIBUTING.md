# Contributing

## Branch and PR Flow
- Create a feature branch from `main`.
- Open a PR with a Conventional Commit-style title.
- Ensure required checks pass before merge.

## Conventional Commit PR Titles
Use one of these types:
- `feat:` new feature
- `fix:` bug fix
- `docs:` documentation-only
- `refactor:` code restructuring with no behavior change
- `test:` tests only
- `chore:` maintenance/tooling
- `ci:` CI/CD changes
- `perf:` performance improvement
- `revert:` revert a previous change

Examples:
- `feat: add badbot user report command`
- `fix: handle missing scheduler in status command`
- `docs: update release process for v2.0.0`

## Versioning Rules
- Every PR/push that changes code must bump version in `DongBot/DongBot.csproj`.
- Keep release notes updated:
  - Internal: `docs/CHANGELOG_INTERNAL.md`
  - External: `docs/RELEASE_NOTES_DISCORD.md`

## Helpful Script
Use:

```powershell
./scripts/bump-version.ps1 -Version 2.0.1
```

Then review generated note templates, run tests, and commit.
