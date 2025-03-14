using CosmicBot.DiscordResponse;
using Discord.Interactions;

namespace CosmicBot.Commands
{
    public class CommandModule : InteractionModuleBase<SocketInteractionContext>
    {
        public async Task Respond(MessageResponse response)
        {
            await base.RespondAsync(response.Text,
                response.Embeds,
                response.TTS,
                response.Ephemeral,
                response.AllowedMentions,
                response.RequestOptions,
                response.Components,
                response.Embed,
                response.Poll);
        }
    }
}
