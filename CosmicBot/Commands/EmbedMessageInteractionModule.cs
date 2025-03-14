using CosmicBot.Helpers;
using CosmicBot.Service;
using Discord.Interactions;
namespace CosmicBot.Commands
{
    public class EmbedMessageInteractionModule : CommandModule
    {
        private readonly PlayerService _playerService;
        public EmbedMessageInteractionModule(PlayerService playerService) 
        {
            _playerService = playerService;
        }

        [ComponentInteraction("button_*")]
        public async Task HandleButtons()
        {
            await Respond(await MessageStore.HandleMessageButtons(Context, _playerService));
        }
    }
}
