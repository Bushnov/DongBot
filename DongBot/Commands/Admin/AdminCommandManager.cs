using System;
using System.Text;
using System.Threading.Tasks;

namespace DongBot
{
    /// <summary>
    /// Handles non-GIF admin-only commands such as audit and statistics.
    /// </summary>
    internal class AdminCommandManager : ICommandManager
    {
        private readonly AdminReportingService _reportingService;
        private readonly UserErrorReportLogger? _userErrorReportLogger;
        private readonly string _adminChannelName;

        public AdminCommandManager(AdminReportingService reportingService, string adminChannelName, UserErrorReportLogger? userErrorReportLogger = null)
        {
            _reportingService = reportingService;
            _adminChannelName = adminChannelName;
            _userErrorReportLogger = userErrorReportLogger;
        }

        public bool CanHandle(string command)
        {
            string upper = command.ToUpperInvariant();
            return (upper.StartsWith("AUDIT") && !upper.StartsWith("AUDIT-TRAIL"))
                || upper.StartsWith("BADBOT-LIST")
                || upper.Equals("STATS")
                || upper.StartsWith("STATS-TOP")
                || upper.StartsWith("STATS-USER")
                || upper.StartsWith("STATS-COMMAND");
        }

        public Task<string> ProcessCommandAsync(string command, CommandContext context)
        {
            if (!context.IsAdminChannel)
            {
                return Task.FromResult($"Error: This command can only be used in #{_adminChannelName}");
            }

            string upper = command.ToUpperInvariant();

            if (upper.StartsWith("AUDIT") && !upper.StartsWith("AUDIT-STATS"))
            {
                return Task.FromResult(_reportingService.GetAuditLog(command, context.UserId, context.Username, context.ChannelName, context.GuildId));
            }

            if (upper.StartsWith("BADBOT-LIST"))
            {
                return Task.FromResult(GetBadBotReports(command, context.GuildId));
            }

            if (upper.Equals("AUDIT-STATS"))
            {
                return Task.FromResult(_reportingService.GetAuditStats(context.UserId, context.Username, context.ChannelName, context.GuildId));
            }

            if (upper.Equals("STATS"))
            {
                return Task.FromResult(_reportingService.GetStats(context.UserId, context.Username, context.ChannelName, context.GuildId));
            }

            if (upper.StartsWith("STATS-TOP"))
            {
                return Task.FromResult(_reportingService.GetTopCommands(command, context.UserId, context.Username, context.ChannelName, context.GuildId));
            }

            if (upper.StartsWith("STATS-USER"))
            {
                return Task.FromResult(_reportingService.GetUserStatistics(command, context.UserId, context.Username, context.ChannelName, context.GuildId));
            }

            if (upper.StartsWith("STATS-COMMAND"))
            {
                return Task.FromResult(_reportingService.GetCommandStatistics(command, context.UserId, context.Username, context.ChannelName, context.GuildId));
            }

            return Task.FromResult(string.Empty);
        }

        public string GetHelp(CommandContext context)
        {
            if (!context.IsAdminChannel)
            {
                return string.Empty;
            }

            StringBuilder sb = new StringBuilder();
            sb.AppendLine("**Audit & Statistics Commands:**");
            sb.AppendLine("```");
            sb.AppendLine("!audit [limit] - Show recent audit log entries");
            sb.AppendLine("!audit-stats - Show audit statistics");
            sb.AppendLine("!badbot-list [N] - Show recent user error reports");
            sb.AppendLine("!release-notes [version|range] - Post user release notes to #dongbot");
            sb.AppendLine("!stats - Show overall bot statistics");
            sb.AppendLine("!stats-top [N] - Show top N commands");
            sb.AppendLine("!stats-user [USER_ID] - Show user statistics");
            sb.AppendLine("!stats-command COMMANDNAME - Show command statistics");
            sb.AppendLine("```");
            return sb.ToString();
        }

        private string GetBadBotReports(string command, ulong guildId)
        {
            if (_userErrorReportLogger == null)
            {
                return "User error reporting is not configured.";
            }

            string[] parts = command.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            int count = 20;
            if (parts.Length > 1 && int.TryParse(parts[1], out int parsed))
            {
                count = Math.Clamp(parsed, 1, 100);
            }

            var reports = _userErrorReportLogger.GetRecentReports(count, guildId);
            if (reports.Count == 0)
            {
                return "No user error reports found for this server.";
            }

            StringBuilder sb = new StringBuilder();
            sb.AppendLine($"**Recent User Error Reports (latest {reports.Count})**");
            sb.AppendLine("```");
            foreach (var report in reports)
            {
                string previous = string.IsNullOrWhiteSpace(report.PreviousCommand) ? "(none)" : report.PreviousCommand;
                string comment = string.IsNullOrWhiteSpace(report.Comment) ? "(no comment)" : report.Comment;
                sb.AppendLine($"[{report.Timestamp:yyyy-MM-dd HH:mm}] {report.Username} in #{report.ChannelName}");
                sb.AppendLine($"  Previous: {previous}");
                sb.AppendLine($"  Comment: {comment}");
            }
            sb.AppendLine("```");
            return sb.ToString();
        }

        public void Dispose()
        {
            // No owned resources.
        }
    }
}