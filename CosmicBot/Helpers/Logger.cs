using Discord.Commands;
using Discord;

namespace CosmicBot.Helpers
{
    public static class Logger
    {
        public static Task LogAsync(LogMessage message)
        {
            Log(message);
            return Task.CompletedTask;
        }

        public static Task LogAsync(string message)
        {
            Log(message);
            return Task.CompletedTask;
        }

        public static void Log(LogMessage message)
        {
            if (message.Exception is CommandException cmdException)
            {
                Console.WriteLine($"[Command/{message.Severity}] {cmdException.Command.Aliases.First()}"
                    + $" failed to execute in {cmdException.Context.Channel}.");
                Console.WriteLine(cmdException);
            }
            else
                Console.WriteLine($"[General/{message.Severity}] {message}");
        }

        public static void Log(string message)
        {
            Console.WriteLine($"[General/Info] {DateTime.Now.ToShortTimeString()} {message}");
        }
    }
}
