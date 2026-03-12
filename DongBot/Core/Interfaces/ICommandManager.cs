using System;
using System.Threading.Tasks;

namespace DongBot
{
    /// <summary>
    /// Contextual information about the Discord message that triggered a command.
    /// Includes guild/server information to enable server-specific operations and tracking.
    /// </summary>
    public record CommandContext(
        string ChannelName,
        ulong ChannelId,
        bool IsAdminChannel,
        string UserId,
        string Username,
        ulong GuildId = 0,
        string GuildName = ""
    );

    /// <summary>
    /// Contract for all command managers in DongBot
    /// </summary>
    public interface ICommandManager : IDisposable
    {
        /// <summary>
        /// Returns true if this manager is able to process the given command string.
        /// Used to short-circuit dispatch without calling ProcessCommandAsync.
        /// </summary>
        bool CanHandle(string command);

        /// <summary>
        /// Process the command and return a Discord response string,
        /// or an empty string if the command was not handled.
        /// </summary>
        Task<string> ProcessCommandAsync(string command, CommandContext context);

        /// <summary>
        /// Returns human-readable help text for this manager's commands.
        /// Implementations should use context.IsAdminChannel to optionally surface admin commands.
        /// </summary>
        string GetHelp(CommandContext context);
    }
}
