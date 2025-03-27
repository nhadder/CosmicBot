using CosmicBot.DiscordResponse;
using CosmicBot.Helpers;
using CosmicBot.Service;
using CosmicBot.Services;
using Discord.Interactions;
namespace CosmicBot.Commands
{
    public class EmbedMessageInteractionModule : CommandModule
    {
        private readonly PlayerService _playerService;
        private readonly PetService _petService;
        public EmbedMessageInteractionModule(PlayerService playerService, PetService petService) 
        {
            _playerService = playerService;
            _petService = petService;
        }

        [ComponentInteraction("button_*")]
        public async Task HandleButtons()
        {
            if (!HasChannelPermissions())
            {
                await Respond(new MessageResponse("I don't have valid permissions in this channel", ephemeral: true));
                return;
            }

            MessageResponse? response = null;

            try
            {
                response = await MessageStore.HandleMessageButtons(Context, _playerService, _petService);
            }
            catch (Exception ex)
            {
                Logger.Log($"{ex.Message}: {ex.StackTrace}");
                while (ex.InnerException != null)
                {
                    ex = ex.InnerException;
                    Logger.Log($"{ex.Message}: {ex.StackTrace}");
                }
            }

            await Respond(response);
        }
    }
}
