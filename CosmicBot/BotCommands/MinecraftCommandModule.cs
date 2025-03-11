using CosmicBot.DAL;
using CosmicBot.Helpers;
using CosmicBot.Models;
using CosmicBot.Service;
using Discord.Interactions;
using Discord.WebSocket;
using Microsoft.EntityFrameworkCore;
using System.Text;

namespace CosmicBot.BotCommands
{
    public class MinecraftCommandModule : InteractionModuleBase<SocketInteractionContext>
    {
        private readonly DataContext _context;
        public MinecraftCommandModule(DataContext context)
        {
            _context = context;
        }

        #region Minecraft Server Commands

        [SlashCommand("addserver", "[Administrator] Add a Minecraft Server")]
        public async Task AddServer(string serverName, string ipAddress, int rconPort = 25575, string rconPassword = "", ServerType serverType = ServerType.Vanilla)
        {
            if (!UserIsMod(Context.User))
            {
                await RespondAsync("You don't have permission to use this command.", ephemeral: true);
                return;
            }

            var newServer = new MinecraftServer()
            {
                GuildId = Context.Guild.Id,
                Name = serverName,
                IpAddress = ipAddress,
                ServerType = serverType,
                RconPassword = rconPassword,
                RconPort = (ushort)rconPort
            };
            _context.Add(newServer);
            await _context.SaveChangesAsync();
            await RespondAsync($"Added new minecraft ({serverType}) server: {serverName}\nServer Id: {newServer.ServerId}", ephemeral: true);
        }

        [SlashCommand("servers", "[Administrator] List your minecraft servers")]
        public async Task Servers()
        {
            if (!UserIsMod(Context.User))
            {
                await RespondAsync("You don't have permission to use this command.", ephemeral: true);
                return;
            }

            try
            {
                var servers = await _context.MinecraftServers.Where(s => s.GuildId == Context.Guild.Id).ToListAsync();       

                if (servers.Count == 0)
                {
                    await RespondAsync($"No servers available to list.", ephemeral: true);
                    return;
                }

                var serverList = string.Join("\n", servers.Select(s => $"[{s.ServerType}] {s.Name} ({s.ServerId})"));
                await RespondAsync($"Minecraft Servers:\n{serverList}", ephemeral: true);
            }
            catch (Exception ex)
            {
                await RespondAsync($"Error: {ex.Message}", ephemeral: true);
                return;
            }
        }

        [SlashCommand("deleteserver", "[Administrator] Delete a minecraft server")]
        public async Task DeleteServer(string server_id)
        {
            var serverId = StrToGuid(server_id);
            var server = GetServer(serverId);

            if (server == null)
            {
                await RespondAsync($"Unknown server: {serverId}", ephemeral: true);
                return;
            }

            var tasks = await _context.MinecraftScheduledTasks.Where(t => t.ServerId == server.ServerId).ToListAsync();

            if (tasks.Count > 0)
            {
                var commands = await _context.MinecraftCommands.Where(c => tasks.Any(t => t.ScheduledTaskId == c.ScheduledTaskId)).ToListAsync();
                _context.MinecraftCommands.RemoveRange(commands);
                _context.MinecraftScheduledTasks.RemoveRange(tasks);
            }
            _context.MinecraftServers.Remove(server);

            await _context.SaveChangesAsync();
            await RespondAsync($"Removed minecraft {server.ServerType} server: {server.Name}", ephemeral: true);
        }

        [SlashCommand("updateserver", "[Administrator] Update a minecraft server")]
        public async Task DeleteServer(string server_id, string? name, string? ipAddress, int? rconPort, string? rconPassword, ServerType? serverType)
        {
            var serverId = StrToGuid(server_id);
            var server = GetServer(serverId);

            if (server == null)
            {
                await RespondAsync($"Unknown server: {serverId}", ephemeral: true);
                return;
            }

            server.Name = name ?? server.Name;
            server.IpAddress = ipAddress ?? server.IpAddress;
            server.RconPort = (ushort)(rconPort ?? server.RconPort);
            server.RconPassword = rconPassword ?? server.RconPassword;
            server.ServerType = serverType ?? server.ServerType;

            _context.MinecraftServers.Update(server);
            await _context.SaveChangesAsync();
            await RespondAsync($"Updated minecraft {serverType} server {server.ServerId}", ephemeral: true);
        }

        #endregion

        #region Scheduled Task Commands

        [SlashCommand("addtask", "Add a scheduled command to run on your server. Start time: hh:mm:ss. Interval [d.]hh:mm:ss.")]
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

        [SlashCommand("updatetask", "Update a scheduled command on your server. Start time: hh:mm:ss. Interval [d.]hh:mm:ss.")]
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

        [SlashCommand("deletetask", "Delete a scheduled command on your server.")]
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


        [SlashCommand("tasks", "List your current scheduled tasks for a server")]
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

        #endregion

        #region Whitelist Commands
        [SlashCommand("whitelistadd", "Add a username to the whitelist")]
        public async Task WhitelistAdd(string username)
        {

            var guildId = Context.Guild.Id;
            var servers = await _context.MinecraftServers.Where(s => s.GuildId == guildId).ToListAsync();

            if (servers.Count == 0)
            {
                await RespondAsync($"No servers added yet.", ephemeral: true);
                return;
            }

            var sb = new StringBuilder();

            foreach (var server in servers)
            {
                var service = new MinecraftServerService(server);
                var response = await service.SendCommand($"whitelist add {username}");
                if (response.Contains("That player does not exist"))
                {
                    if (server.ServerType == ServerType.Vanilla)
                    {
                        try
                        {
                            var xuid = await BedrockXUIDHelper.GetXUID(username);
                            response = await service.SendCommand($"fwhitelist add {xuid}");
                            if(!string.IsNullOrWhiteSpace(response))
                                sb.AppendLine($"{server.Name}: {response}");
                        }
                        catch
                        {
                            sb.AppendLine($"{server.Name}: Unable to reach https://www.cxkes.me/xbox/xuid to get bedrock player id...");
                        }
                    }
                    else
                    {
                        if (!string.IsNullOrWhiteSpace(response))
                            sb.AppendLine($"{server.Name}: {response}");
                    }
                }
                else
                    if (!string.IsNullOrWhiteSpace(response))
                        sb.AppendLine($"{server.Name}: {response}");
            }

            await RespondAsync(sb.ToString());
        }
        [SlashCommand("whitelistremove", "Remove a username from the whitelist")]
        public async Task WhitelistRemove(string username)
        {
            if (!UserIsMod(Context.User))
            {
                await RespondAsync("You don't have permission to use this command.", ephemeral: true);
                return;
            }

            var guildId = Context.Guild.Id;
            var servers = await _context.MinecraftServers.Where(s => s.GuildId == guildId).ToListAsync();

            if (servers.Count == 0)
            {
                await RespondAsync($"No servers added yet.", ephemeral: true);
                return;
            }

            var sb = new StringBuilder();

            foreach (var server in servers)
            {
                var service = new MinecraftServerService(server);
                var response = await service.SendCommand($"whitelist remove {username}");
                if (response.Contains("That player does not exist"))
                {
                    if (server.ServerType == ServerType.Vanilla)
                    {
                        try
                        {
                            var xuid = await BedrockXUIDHelper.GetXUID(username);
                            response = await service.SendCommand($"fwhitelist remove {xuid}");
                            sb.AppendLine($"{server.Name}: {response}");
                        }
                        catch
                        {
                            sb.AppendLine($"{server.Name}: Unable to reach https://www.cxkes.me/xbox/xuid to get bedrock player id...");
                        }
                    }
                }
                else
                    sb.AppendLine($"{server.Name}: {response}");
            }

            await RespondAsync(sb.ToString(), ephemeral: true);
        }

        #endregion

        #region Other

        [SlashCommand("list", "Lists players on your servers")]
        public async Task List()
        {
            var guildId = Context.Guild.Id;
            var servers = await _context.MinecraftServers.Where(s => s.GuildId == guildId).ToListAsync();
            if (servers.Count == 0)
            {
                await RespondAsync("No servers added", ephemeral: true);
                return;
            }

            var sb = new StringBuilder();
            foreach (var server in servers)
            {
                var service = new MinecraftServerService(server);
                var response = await service.SendCommand("list");
                if(!string.IsNullOrWhiteSpace(response))
                    sb.AppendLine($"*{server.Name}*\n{response}\n");
            }
            if (!string.IsNullOrWhiteSpace(sb.ToString()))
                await RespondAsync(sb.ToString());
        }

        #endregion

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
