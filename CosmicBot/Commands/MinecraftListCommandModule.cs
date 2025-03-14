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
            await Respond(await _service.ListPlayers(Context.Guild.Id));
        }
    }
}
