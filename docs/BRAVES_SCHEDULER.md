# Braves Automated Scheduler

## Overview

`BravesScheduler` posts automated Braves updates to the configured Braves channel and exposes admin controls for enable/disable and test execution.

## Scheduled Posts

- Daily schedule post around 10:00 AM Eastern
- Game preview about 30 minutes before first pitch
- Weekly NL East standings update on Monday morning

The scheduler includes duplicate guards (`_lastDailyPost`, `_lastWeeklyPost`, `_lastGamePreviewId`) so the same cycle is not posted repeatedly.

## Admin Commands

These commands are processed through the command pipeline and are restricted to the configured admin channel.

- `!braves-scheduler-status`
- `!braves-scheduler-enable`
- `!braves-scheduler-disable`
- `!braves-scheduler-test-daily`
- `!braves-scheduler-test-weekly`

## Configuration

Use `DongBot/botconfig.json`:

- `BravesChannelName` controls target channel lookup (case-insensitive)
- `AdminChannelName` controls where scheduler admin commands are allowed

The scheduler uses Windows time zone ID `Eastern Standard Time` for scheduling windows.

## Runtime Behavior

- Starts from `MainDong` when the bot is ready
- Uses periodic background loops for daily, preview, and weekly checks
- Skips posting when disabled
- Logs warnings when target channel cannot be found

## Troubleshooting

- Run `!braves-scheduler-status` to confirm current state and target channel
- Verify the configured Braves channel exists and bot has send permissions
- Use `!braves-scheduler-test-daily` and `!braves-scheduler-test-weekly` to validate delivery
