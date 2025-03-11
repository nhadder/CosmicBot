using CosmicBot.DAL;
using CosmicBot.Models;
using Discord.Interactions;
using Discord;
using Discord.WebSocket;
using CosmicBot.Helpers;
using CosmicBot.Service;
using System.Text;
using Microsoft.EntityFrameworkCore;

namespace CosmicBot.BotCommands
{
    [Group("whitelist", "Minecraft Server")]
    public class MinecraftWhitelistCommandModule : InteractionModuleBase<SocketInteractionContext>
    {
        private readonly DataContext _context;
        public MinecraftWhitelistCommandModule(DataContext context)
        {
            _context = context;
        }

        [SlashCommand("add", "Add a username to the whitelist")]
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

            await RespondAsync(sb.ToString());
        }

        [RequireUserPermission(GuildPermission.Administrator)]
        [SlashCommand("remove", "Remove a username from the whitelist")]
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

        private static bool UserIsMod(SocketUser user)
        {
            var guildUser = user as SocketGuildUser;
            return guildUser?.GuildPermissions.Administrator ?? false;
        }
    }
}
