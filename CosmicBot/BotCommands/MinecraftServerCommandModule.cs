using CosmicBot.Models;
using Discord.Interactions;
using Discord;
using CosmicBot.Service;

namespace CosmicBot.BotCommands
{
    [RequireUserPermission(GuildPermission.Administrator)]
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
            await Respond(await _service.AddServer(Context.Guild.Id, serverName, ipAddress, rconPort, rconPassword, serverType));
        }

        [SlashCommand("list", "List your minecraft servers")]
        public async Task Servers()
        {
            await Respond(await _service.ListServers(Context.Guild.Id));
        }

        [SlashCommand("remove", "Delete a minecraft server")]
        public async Task DeleteServer(string serverId)
        {
            await Respond(await _service.RemoveServer(serverId));
        }

        [SlashCommand("update", "Update a minecraft server")]
        public async Task UpdateServer(string serverId, string? name, string? ipAddress, int? rconPort, string? rconPassword, ServerType? serverType)
        {
            await Respond(await _service.UpdateServer(serverId, name, ipAddress, rconPort, rconPassword, serverType));
        }
    }
}
