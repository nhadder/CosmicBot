using Discord.Interactions;
using Discord;
using CosmicBot.Service;

namespace CosmicBot.Commands
{
    [Group("whitelist", "Minecraft Server")]
    public class MinecraftWhitelistCommandModule : CommandModule
    {
        private readonly MinecraftServerService _service;
        public MinecraftWhitelistCommandModule(MinecraftServerService service)
        {
            _service = service;
        }

        [SlashCommand("add", "Add a username to the whitelist")]
        public async Task WhitelistAdd(string username)
        {
            await Respond(await _service.WhitelistAdd(Context.Guild.Id, username));
        }

        [DefaultMemberPermissions(GuildPermission.Administrator)]
        [SlashCommand("remove", "Remove a username from the whitelist")]
        public async Task WhitelistRemove(string username)
        {
            await Respond(await _service.WhitelistRemove(Context.Guild.Id, username));
        }
    }
}
