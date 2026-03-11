using System;
using System.IO;
using System.Text.Json;

namespace DongBot
{
    public class BotConfig
    {
        public string AdminChannelName { get; set; } = "dongbot-admin";
        public string BravesChannelName { get; set; } = "baseball";
        public string TokenFilePath { get; set; } = "token.txt";
        public string GifCommandsFilePath { get; set; } = "gifcommands.json";
        public string AuditLogFilePath { get; set; } = "bot_audit.json";
        public string StatisticsFilePath { get; set; } = "bot_statistics.json";
        public string UserErrorReportsFilePath { get; set; } = "user_error_reports.json";
        public bool AuditVerboseConsoleLogging { get; set; } = false;

        public static BotConfig Load()
        {
            string baseDirPath = Path.Combine(AppContext.BaseDirectory, "botconfig.json");
            string workingDirPath = Path.Combine(Directory.GetCurrentDirectory(), "botconfig.json");
            string configPath = File.Exists(baseDirPath) ? baseDirPath : workingDirPath;

            if (!File.Exists(configPath))
            {
                return new BotConfig();
            }

            try
            {
                string json = File.ReadAllText(configPath);
                BotConfig? config = JsonSerializer.Deserialize<BotConfig>(json, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                return config ?? new BotConfig();
            }
            catch
            {
                return new BotConfig();
            }
        }
    }
}
