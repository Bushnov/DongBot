using DongBot;

namespace DongBot.Tests;

[Collection("BotConfig Serial")]
public class BotConfigTests
{
    [Fact]
    public void Load_ReturnsDefaults_WhenFileMissing()
    {
        using TestWorkspace workspace = new();
        string originalCurrentDirectory = Directory.GetCurrentDirectory();
        Directory.SetCurrentDirectory(workspace.RootPath);
        try
        {
            BotConfig config = BotConfig.Load();

            Assert.Equal("dongbot-admin", config.AdminChannelName);
            Assert.Equal("baseball", config.BravesChannelName);
            Assert.Equal("user_error_reports.json", config.UserErrorReportsFilePath);
        }
        finally
        {
            Directory.SetCurrentDirectory(originalCurrentDirectory);
        }
    }

    [Fact]
    public void Load_ReadsValues_FromBaseDirectoryConfig()
    {
        string configPath = Path.Combine(AppContext.BaseDirectory, "botconfig.json");
        bool existed = File.Exists(configPath);
        string? originalContent = existed ? File.ReadAllText(configPath) : null;

        try
        {
            File.WriteAllText(configPath, """
{
  "adminChannelName": "ops-admin",
  "bravesChannelName": "braves-room",
    "tokenFilePath": "secrets/token.txt",
    "userErrorReportsFilePath": "data/user-errors.json"
}
""");

            BotConfig config = BotConfig.Load();

            Assert.Equal("ops-admin", config.AdminChannelName);
            Assert.Equal("braves-room", config.BravesChannelName);
            Assert.Equal("secrets/token.txt", config.TokenFilePath);
            Assert.Equal("data/user-errors.json", config.UserErrorReportsFilePath);
        }
        finally
        {
            if (existed && originalContent != null)
            {
                File.WriteAllText(configPath, originalContent);
            }
            else if (File.Exists(configPath))
            {
                File.Delete(configPath);
            }
        }
    }
}
