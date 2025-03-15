using CosmicBot.Models;
using Discord.Interactions;
using Discord;
using CosmicBot.Service;
using CosmicBot.DiscordResponse;

namespace CosmicBot.Commands
{
    [DefaultMemberPermissions(GuildPermission.Administrator)]
    [Group("server", "Minecraft Server")]
    public class MinecraftServerCommandModule : CommandModule
    {
        private readonly MinecraftServerService _service;
        public MinecraftServerCommandModule(MinecraftServerService service)
        {
            _service = service;
        }

        [SlashCommand("add", "Add a Minecraft Server")]
        public async Task AddServer(string serverName, string ipAddress, int rconPort = 25575, string rconPassword = "", ServerType serverType = ServerType.Vanilla)
        {
            if (!HasChannelPermissions())
            {
                await Respond(new MessageResponse("I don't have valid permissions in this channel", ephemeral: true));
                return;
            }

            await Respond(await _service.AddServer(Context.Guild.Id, serverName, ipAddress, rconPort, rconPassword, serverType));
        }

        [SlashCommand("list", "List your minecraft servers")]
        public async Task Servers()
        {
            if (!HasChannelPermissions())
            {
                await Respond(new MessageResponse("I don't have valid permissions in this channel", ephemeral: true));
                return;
            }

            await Respond(await _service.ListServers(Context.Guild.Id));
        }

        [SlashCommand("remove", "Delete a minecraft server")]
        public async Task DeleteServer(string serverId)
        {
            if (!HasChannelPermissions())
            {
                await Respond(new MessageResponse("I don't have valid permissions in this channel", ephemeral: true));
                return;
            }

            await Respond(await _service.RemoveServer(serverId));
        }

        [SlashCommand("update", "Update a minecraft server")]
        public async Task UpdateServer(string serverId, string? name, string? ipAddress, int? rconPort, string? rconPassword, ServerType? serverType)
        {
            if (!HasChannelPermissions())
            {
                await Respond(new MessageResponse("I don't have valid permissions in this channel", ephemeral: true));
                return;
            }

            await Respond(await _service.UpdateServer(serverId, name, ipAddress, rconPort, rconPassword, serverType));
        }
    }
}
