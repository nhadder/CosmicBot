using CosmicBot.DAL;
using CosmicBot.Models;
using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using Microsoft.EntityFrameworkCore;
using System.Text;

namespace CosmicBot.BotCommands
{
    [RequireUserPermission(GuildPermission.Administrator)]
    [Group("tasks", "Minecraft Server Tasks")]
    public class MinecraftTaskCommandModule : InteractionModuleBase<SocketInteractionContext>
    {
        private readonly DataContext _context;
        public MinecraftTaskCommandModule(DataContext context)
        {
            _context = context;
        }

        [SlashCommand("add", "Add a scheduled command to run on your server. Start time: hh:mm:ss. Interval [d.]hh:mm:ss.")]
        public async Task AddTask(string server_id, string taskName, string commands, string startTime, string interval)
        {
            var serverId = StrToGuid(server_id);

            if (!UserIsMod(Context.User))
            {
                await RespondAsync("You don't have permission to use this command.", ephemeral: true);
                return;
            }

            var commandList = commands.Split(';').ToList() ?? new List<string>();
            var start = TimeOnly.Parse(startTime);
            var period = TimeSpan.Parse(interval);

            var server = GetServer(serverId);

            if (server == null)
            {
                await RespondAsync($"Unknown server: {serverId}", ephemeral: true);
                return;
            }

            var newTask = new MinecraftScheduledTask()
            {
                Name = taskName,
                StartTime = start,
                Interval = period,
                ServerId = server.ServerId
            };

            _context.Add(newTask);
            await _context.SaveChangesAsync();

            var newCommands = commandList.Select((c, i) => new MinecraftCommand()
            {
                Command = c,
                ScheduledTaskId = newTask.ScheduledTaskId,
                Order = i
            });

            _context.AddRange(newCommands);
            await _context.SaveChangesAsync();
            await RespondAsync($"Added new minecraft scheduled task {taskName} ({newTask.ScheduledTaskId}) for server: {server.Name}", ephemeral: true);
        }

        [SlashCommand("update", "Update a scheduled command on your server. Start time: hh:mm:ss. Interval [d.]hh:mm:ss.")]
        public async Task UpdateTask(string task_id, string? taskName, string? commands, string? startTime, string? interval)
        {
            var taskId = StrToGuid(task_id);

            if (!UserIsMod(Context.User))
            {
                await RespondAsync("You don't have permission to use this command.", ephemeral: true);
                return;
            }

            List<string>? commandList = string.IsNullOrWhiteSpace(commands) ? null : commands.Split(';').ToList();
            TimeOnly? start = TimeOnly.TryParse(startTime, out TimeOnly result) ? result : null;
            TimeSpan? period = TimeSpan.TryParse(interval, out TimeSpan result2) ? result2 : null;

            var task = _context.MinecraftScheduledTasks.FirstOrDefault(t => t.ScheduledTaskId == taskId);

            if (task == null)
            {
                await RespondAsync($"Unknown task: {taskId}", ephemeral: true);
                return;
            }


            task.Name = taskName ?? task.Name;
            task.StartTime = start ?? task.StartTime;
            task.Interval = period ?? task.Interval;

            _context.MinecraftScheduledTasks.Update(task);
            await _context.SaveChangesAsync();

            var oldCommands = await _context.MinecraftCommands.Where(c => c.ScheduledTaskId == task.ScheduledTaskId).ToListAsync();
            var newCommands = commandList is null ? [] : commandList.Select((c, i) => new MinecraftCommand()
            {
                Command = c,
                ScheduledTaskId = task.ScheduledTaskId,
                Order = i
            }).ToList();

            if(newCommands.Count > 0)
            {
                _context.MinecraftCommands.RemoveRange(oldCommands);
                _context.MinecraftCommands.AddRange(newCommands);
            }

            await _context.SaveChangesAsync();
            await RespondAsync($"Updated minecraft scheduled task {task.Name} ({task.ScheduledTaskId}) for task: {task.ScheduledTaskId}", ephemeral: true);
        }

        [SlashCommand("remove", "Delete a scheduled command on your server.")]
        public async Task DeleteTask(string task_id)
        {
            var taskId = StrToGuid(task_id);

            if (!UserIsMod(Context.User))
            {
                await RespondAsync("You don't have permission to use this command.", ephemeral: true);
                return;
            }

            var task = _context.MinecraftScheduledTasks.FirstOrDefault(t => t.ScheduledTaskId == taskId);

            if (task == null)
            {
                await RespondAsync($"Unknown task: {taskId}", ephemeral: true);
                return;
            }

            var oldCommands = await _context.MinecraftCommands.Where(c => c.ScheduledTaskId == task.ScheduledTaskId).ToListAsync();

            if(oldCommands.Count > 0)
                _context.MinecraftCommands.RemoveRange(oldCommands);

            _context.MinecraftScheduledTasks.Remove(task);

            await _context.SaveChangesAsync();
            await RespondAsync($"Removed minecraft scheduled task {task.Name} ({task.ScheduledTaskId})", ephemeral: true);
        }

        [SlashCommand("list", "List your current scheduled tasks for a server")]
        public async Task ListTasks(string server_id)
        {
            var serverId = StrToGuid(server_id);
            if (!UserIsMod(Context.User))
            {
                await RespondAsync("You don't have permission to use this command.", ephemeral: true);
                return;
            }

            var server = GetServer(serverId);

            if(server == null)
            {
                await RespondAsync($"Unknown server: {serverId}", ephemeral: true);
                return;
            }

            var tasks = await _context.MinecraftScheduledTasks.Where(t => t.ServerId == serverId).ToListAsync();

            if (tasks.Count == 0)
            {
                await RespondAsync($"No tasks found for {serverId}", ephemeral: true);
                return;
            }

            var sb = new StringBuilder();
            sb.AppendLine($"*{server.Name} ({serverId}) Tasks*");
            foreach (var task in tasks)
            {
                sb.AppendLine($"(Id: {task.ScheduledTaskId}) {task.Name}: {task.StartTime} every {task.Interval}");
                var commands = await _context.MinecraftCommands.Where(c => c.ScheduledTaskId == task.ScheduledTaskId)
                    .OrderBy(c => c.Order).ToListAsync();

                foreach (var command in commands)
                {
                    sb.AppendLine($"> {command.Command}");
                }
                sb.AppendLine();
            }
            await RespondAsync(sb.ToString(), ephemeral: true);
        }

        private static bool UserIsMod(SocketUser user)
        {
            var guildUser = user as SocketGuildUser;
            return guildUser?.GuildPermissions.Administrator ?? false;
        }

        private MinecraftServer? GetServer(Guid serverId)
        {
            return _context.MinecraftServers.FirstOrDefault(s =>
            s.ServerId == serverId && s.GuildId == Context.Guild.Id);
        }

        private static Guid StrToGuid(string? str)
        {
            if (str == null)
                return Guid.Empty;

            return Guid.TryParse(str, out Guid result) ? result : Guid.Empty;
        }
    }
}
