using CosmicBot.DiscordResponse;
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
            if (!HasChannelPermissions())
            {
                await Respond(new MessageResponse("I don't have valid permissions in this channel", ephemeral: true));
                return;
            }

            MessageResponse? response = null;

            try
            {
                response = await MessageStore.HandleMessageButtons(Context, _playerService);
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
