using CosmicBot.DiscordResponse;
using CosmicBot.Service;
using Discord.Interactions;

namespace CosmicBot.Commands
{
    public class MinecraftListCommandModule : CommandModule
    {
        private readonly MinecraftServerService _service;
        public MinecraftListCommandModule(MinecraftServerService service)
        {
            _service = service;
        }

        [SlashCommand("list", "Lists players on servers")]
        public async Task List()
        {
            if (!HasChannelPermissions())
            {
                await Respond(new MessageResponse("I don't have valid permissions in this channel", ephemeral: true));
                return;
            }

            await Respond(await _service.ListPlayers(Context.Guild.Id));
        }
    }
}
