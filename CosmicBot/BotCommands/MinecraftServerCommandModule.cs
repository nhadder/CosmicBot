using CosmicBot.DAL;
using CosmicBot.Models;
using Discord.Interactions;
using Discord;
using Discord.WebSocket;
using Microsoft.EntityFrameworkCore;

namespace CosmicBot.BotCommands
{
    [RequireUserPermission(GuildPermission.Administrator)]
    [Group("server", "Minecraft Server")]
    public class MinecraftServerCommandModule : InteractionModuleBase<SocketInteractionContext>
    {
        private readonly DataContext _context;
        public MinecraftServerCommandModule(DataContext context)
        {
            _context = context;
        }

        [SlashCommand("add", "Add a Minecraft Server")]
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

        [SlashCommand("list", "List your minecraft servers")]
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

        [SlashCommand("remove", "Delete a minecraft server")]
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

        [SlashCommand("update", "Update a minecraft server")]
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
