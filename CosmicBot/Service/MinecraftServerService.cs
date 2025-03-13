using CoreRCON;
using CosmicBot.DAL;
using CosmicBot.DiscordResponse;
using CosmicBot.Helpers;
using CosmicBot.Models;
using Microsoft.EntityFrameworkCore;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;

namespace CosmicBot.Service
{
    public class MinecraftServerService
    {
        private readonly DataContext _context;

        public MinecraftServerService(DataContext context)
        {
            _context = context;
        }

        public async Task<MinecraftServer?> GetServerById(Guid serverId)
        {
            return await _context.MinecraftServers.FirstOrDefaultAsync(s => s.ServerId == serverId);
        }

        public async Task<MessageResponse> ListPlayers(ulong guildId)
        {
            var servers = await GetServerByGuild(guildId);
            if (servers.Count == 0)
                return new MessageResponse("No servers added", ephemeral: true);

            var sb = new StringBuilder();
            foreach (var server in servers)
            {
                var response = await SendCommand(server.ServerId, "list");
                if (!string.IsNullOrWhiteSpace(response))
                    sb.AppendLine($"*{server.Name}*\n{response}\n");
            }
            if (!string.IsNullOrWhiteSpace(sb.ToString()))
                return new MessageResponse(sb.ToString());

            return new MessageResponse("No servers responded", ephemeral: true);
        }

        #region Server Management
        public async Task<MessageResponse> AddServer(ulong guildId, string serverName, string ipAddress, int rconPort = 25575, string rconPassword = "", ServerType serverType = ServerType.Vanilla)
        {
            var server = new MinecraftServer()
            {
                GuildId = guildId,
                Name = serverName,
                IpAddress = ipAddress,
                ServerType = serverType,
                RconPassword = rconPassword,
                RconPort = (ushort)rconPort
            };

            try
            {
                _context.Add(server);
                await _context.SaveChangesAsync();
                return new MessageResponse($"Added new minecraft ({server.ServerType}) server: {server.Name}\nServer Id: {server.ServerId}", ephemeral: true);
            }
            catch(Exception ex)
            {
                return new MessageResponse($"Failed to add server: {ex.Message}", ephemeral: true);
            }
        }

        public async Task<MessageResponse> ListServers(ulong guildId)
        {
            try
            {
                var servers = await _context.MinecraftServers.Where(s => s.GuildId == guildId).ToListAsync();

                if (servers.Count == 0)
                    return new MessageResponse($"No servers available to list.", ephemeral: true);

                var serverList = string.Join("\n", servers.Select(s => $"[{s.ServerType}] {s.Name} ({s.ServerId})"));
                return new MessageResponse($"Minecraft Servers:\n{serverList}", ephemeral: true);
            }
            catch (Exception ex)
            {
                return new MessageResponse($"Error: {ex.Message}", ephemeral: true);
            }
        }

        public async Task<MessageResponse> RemoveServer(string serverId)
        {
            var server = await GetServerById(StrToGuid(serverId));

            if (server == null)
                return new MessageResponse($"Unknown server: {serverId}", ephemeral: true);

            var tasks = await _context.MinecraftScheduledTasks.Where(t => t.ServerId == server.ServerId).ToListAsync();

            if (tasks.Count > 0)
            {
                var commands = await _context.MinecraftCommands.Where(c => tasks.Any(t => t.ScheduledTaskId == c.ScheduledTaskId)).ToListAsync();
                _context.MinecraftCommands.RemoveRange(commands);
                _context.MinecraftScheduledTasks.RemoveRange(tasks);
            }
            _context.MinecraftServers.Remove(server);
            await _context.SaveChangesAsync();

            return new MessageResponse($"Removed minecraft {server.ServerType} server: {server.Name}", ephemeral: true);
        }

        public async Task<MessageResponse> UpdateServer(string serverId, string? name, string? ipAddress, int? rconPort, string? rconPassword, ServerType? serverType)
        {
            var server = await GetServerById(StrToGuid(serverId));

            if (server == null)
                return new MessageResponse($"Unknown server: {serverId}", ephemeral: true);

            server.Name = name ?? server.Name;
            server.IpAddress = ipAddress ?? server.IpAddress;
            server.RconPort = (ushort)(rconPort ?? server.RconPort);
            server.RconPassword = rconPassword ?? server.RconPassword;
            server.ServerType = serverType ?? server.ServerType;

            _context.MinecraftServers.Update(server);
            await _context.SaveChangesAsync();
            return new MessageResponse($"Updated minecraft {server.ServerType} server {server.ServerId}", ephemeral: true);
        }

        #endregion

        #region Scheduled Tasks

        public async Task<List<MinecraftScheduledTask>> GetAllTasks()
        {
            return await _context.MinecraftScheduledTasks.AsNoTracking().ToListAsync();
        }

        public async Task<MessageResponse> AddTask(string serverId, string taskName, string commands, string startTime, string interval)
        {
            var commandList = commands.Split(';').ToList() ?? new List<string>();
            var start = TimeOnly.Parse(startTime);
            var period = TimeSpan.Parse(interval);

            var server = await GetServerById(StrToGuid(serverId));

            if (server == null)
                return new MessageResponse($"Unknown server: {serverId}", ephemeral: true);

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
            return new MessageResponse($"Added new minecraft scheduled task {taskName} ({newTask.ScheduledTaskId}) for server: {server.Name}", ephemeral: true);
        }

        public async Task<MessageResponse> UpdateTask(string taskId, string? taskName, string? commands, string? startTime, string? interval)
        {
            List<string>? commandList = string.IsNullOrWhiteSpace(commands) ? null : commands.Split(';').ToList();
            TimeOnly? start = TimeOnly.TryParse(startTime, out TimeOnly result) ? result : null;
            TimeSpan? period = TimeSpan.TryParse(interval, out TimeSpan result2) ? result2 : null;

            var task = _context.MinecraftScheduledTasks.FirstOrDefault(t => t.ScheduledTaskId == StrToGuid(taskId));

            if (task == null)
                return new MessageResponse($"Unknown task: {taskId}", ephemeral: true);


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

            if (newCommands.Count > 0)
            {
                _context.MinecraftCommands.RemoveRange(oldCommands);
                _context.MinecraftCommands.AddRange(newCommands);
            }

            await _context.SaveChangesAsync();
            return new MessageResponse($"Updated minecraft scheduled task {task.Name} ({task.ScheduledTaskId}) for task: {task.ScheduledTaskId}", ephemeral: true);
        }

        public async Task<MessageResponse> RemoveTask(string taskId)
        {
            var task = _context.MinecraftScheduledTasks.FirstOrDefault(t => t.ScheduledTaskId == StrToGuid(taskId));

            if (task == null)
                return new MessageResponse($"Unknown task: {taskId}", ephemeral: true);

            var oldCommands = await _context.MinecraftCommands.Where(c => c.ScheduledTaskId == task.ScheduledTaskId).ToListAsync();

            if (oldCommands.Count > 0)
                _context.MinecraftCommands.RemoveRange(oldCommands);

            _context.MinecraftScheduledTasks.Remove(task);

            await _context.SaveChangesAsync();
            return new MessageResponse($"Removed minecraft scheduled task {task.Name} ({task.ScheduledTaskId})", ephemeral: true);
        }

        public async Task<MessageResponse> ListTasks(string serverId)
        {
            var server = await GetServerById(StrToGuid(serverId));

            if (server == null)
                return new MessageResponse($"Unknown server: {serverId}", ephemeral: true);

            var tasks = await _context.MinecraftScheduledTasks.Where(t => t.ServerId == server.ServerId).ToListAsync();

            if (tasks.Count == 0)
                return new MessageResponse($"No tasks found for {serverId}", ephemeral: true);

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
            return new MessageResponse(sb.ToString(), ephemeral: true);
        }

        public async Task UpdateTask(MinecraftScheduledTask task)
        {
            _context.MinecraftScheduledTasks.Update(task);
            await _context.SaveChangesAsync();
        }

        public async Task<List<MinecraftCommand>> GetCommands(Guid scheduledTaskId)
        {
            return await _context.MinecraftCommands.Where(c => c.ScheduledTaskId == scheduledTaskId).ToListAsync();
        }

        public async Task SendCommands(Guid serverId, List<MinecraftCommand> commands)
        {
            commands = commands.OrderBy(c => c.Order).ToList();
            foreach (var command in commands)
            {
                var response = await SendCommand(serverId, command.Command);
                if (!string.IsNullOrWhiteSpace(response))
                    Console.WriteLine($"Response: {response}");
            }
        }

        #endregion

        #region Whitelisting

        public async Task<MessageResponse> WhitelistAdd(ulong guildId, string username)
        {
            var servers = await _context.MinecraftServers.Where(s => s.GuildId == guildId).ToListAsync();

            if (servers.Count == 0)
                return new MessageResponse($"No servers added yet.", ephemeral: true);

            var sb = new StringBuilder();

            foreach (var server in servers)
            {
                var response = await SendCommand(server.ServerId, $"whitelist add {username}");
                if (response.Contains("That player does not exist"))
                {
                    if (server.ServerType == ServerType.Vanilla)
                    {
                        try
                        {
                            var xuid = await BedrockXUIDHelper.GetXUID(username);
                            response = await SendCommand(server.ServerId, $"fwhitelist add {xuid}");
                            if (!string.IsNullOrWhiteSpace(response))
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

            return new MessageResponse(sb.ToString());
        }

        public async Task<MessageResponse> WhitelistRemove(ulong guildId, string username)
        {
            var servers = await _context.MinecraftServers.Where(s => s.GuildId == guildId).ToListAsync();

            if (servers.Count == 0)
                return new MessageResponse($"No servers added yet.", ephemeral: true);

            var sb = new StringBuilder();

            foreach (var server in servers)
            {
                var response = await SendCommand(server.ServerId, $"whitelist remove {username}");
                if (response.Contains("That player does not exist"))
                {
                    if (server.ServerType == ServerType.Vanilla)
                    {
                        try
                        {
                            var xuid = await BedrockXUIDHelper.GetXUID(username);
                            response = await SendCommand(server.ServerId, $"fwhitelist remove {xuid}");
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

            return new MessageResponse(sb.ToString(), ephemeral: true);
        }

        #endregion

        #region Private Methods

        private async Task<List<MinecraftServer>> GetServerByGuild(ulong guildId)
        {
            return await _context.MinecraftServers.Where(s => s.GuildId == guildId).ToListAsync();
        }

        private async Task<string> SendCommand(Guid serverId, string command)
        {
            var server = _context.MinecraftServers.FirstOrDefault(s => s.ServerId == serverId);
            if (server == null)
                return string.Empty;

            try
            {
                using (var rcon = new RCON(IPAddress.Parse(server.IpAddress), server.RconPort, server.RconPassword))
                {
                    await rcon.ConnectAsync();
                    string response = await rcon.SendCommandAsync(command);
                    return RemoveColorCodes(response);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to send command: `{command}` to {server.IpAddress}:{server.RconPort}");
                Console.WriteLine(ex.Message);
                return string.Empty;
            }
        }

        private static string RemoveColorCodes(string input)
        {
            string pattern = "§[0-9A-Za-z]";
            return Regex.Replace(input, pattern, string.Empty);
        }

        private static Guid StrToGuid(string? str)
        {
            if (str == null)
                return Guid.Empty;

            return Guid.TryParse(str, out Guid result) ? result : Guid.Empty;
        }

        #endregion
    }
}
