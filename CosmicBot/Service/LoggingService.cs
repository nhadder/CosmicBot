using Discord.Commands;
using Discord;

namespace CosmicBot.Service
{
    public static class LoggingService
    {
        public static Task LogAsync(LogMessage message)
        {
            if (message.Exception is CommandException cmdException)
            {
                Console.WriteLine($"[Command/{message.Severity}] {cmdException.Command.Aliases.First()}"
                    + $" failed to execute in {cmdException.Context.Channel}.");
                Console.WriteLine(cmdException);
            }
            else
                Console.WriteLine($"[General/{message.Severity}] {message}");

            return Task.CompletedTask;
        }

        public static Task LogAsync(string message)
        {
            Console.WriteLine($"[General/Info] {DateTime.Now.ToShortTimeString()} {message}");
            return Task.CompletedTask;
        }
    }
}
