# DongBot User Release Notes

## v2.0.3 (2026-03-27)

### Highlights
- 

### Improvements
- 

### Fixes
- 

---



Audience: all Discord users of DongBot commands.

## v2.0.1 (2026-03-12)

This update improves MLB command reliability and makes player output more accurate.

### Highlights
- `!mlb-player-stats` now shows stat sections based on player role more reliably.
- `!mlb-player` now includes a health/status line when roster status is available.
- MLB lookups now handle off-season API gaps more gracefully.

### Improvements
- Pitchers now show pitching + fielding sections instead of incorrectly surfacing hitter-first output.
- Position players continue to show batting, baserunning, and fielding sections.
- Shohei Ohtani is handled as a two-way player and shows both hitting and pitching sections.
- `!mlb-team`, `!mlb-player`, and standings-related commands now make a best effort to use historical data when current-season MLB API responses are sparse.

### Fixes
- Fixed cases where pitcher lookups could display batting output when current stats happened to include hitting data.
- Fixed missing player health information when active-roster status is present in roster data.
- Reduced off-season failures for standings and profile lookups.

---

## v2.0.0 (2026-03-11)

This is a major update that introduces MLB features to users for the first time and adds a direct user feedback command.

### Highlights
- First public MLB feature release in DongBot.
- New MLB commands for scores, schedules, standings, teams, players, venues, divisions, leagues, and sport scope.
- New user feedback command: `!badbot [comment]`.
- Updated help and command documentation.

---

## New Commands and Features

### 1) Report issues directly from chat
Use this if a response is wrong, incomplete, or broken.

```text
!badbot the gif link is broken
```

What gets captured:
- your comment
- the previous command you ran (if available)
- channel + timestamp

Example flow:
```text
User: !dong
Bot:  (returns GIF)
User: !badbot the gif link is broken
Bot: ⚠️ Report logged for investigation.
     Previous command: !dong
     Comment: the gif link is broken
```

### 2) MLB commands are now available (first public release)

There was no previously released MLB integration for users. Everything below is brand new in `v2.0.0`.

Start with:

```text
!mlb-help
```

General MLB:

```text
!mlb-scores
!mlb-schedule
!mlb-standings
!mlb-standings AL East
```

Team, player, and venue lookups:

```text
!mlb-team Braves
!mlb-player Ronald Acuna
!mlb-player-stats Max Fried 2023
!mlb-venue Truist Park
```

New filters/scope commands:

```text
!mlb-division NL East
!mlb-league National League
!mlb-sport MLB
```

Braves-focused quick commands:

```text
!braves-score
!braves-schedule
!braves-standings
!braves-roster
```

---

## Quick User Reference

### Core help
```text
!help
!mlb-help
```

### GIF command usage
```text
!dong
!boop
!sweep
```
(Actual command list varies by channel permissions/config.)

### MLB quick picks
```text
!braves-score
!braves-schedule
!mlb-scores
!mlb-schedule
!mlb-team Braves
!mlb-player Ronald Acuna
```

### Issue reporting
```text
!badbot [your comment]
```

---

## Notes for Users
- If something looks wrong, use `!badbot` immediately after the issue so previous-command context is captured.
- You can include short reproduction comments (for example: "shows wrong team", "missing stats", "broken URL").
