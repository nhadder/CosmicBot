using CosmicBot.DiscordResponse;
using Discord.Interactions;
using Microsoft.VisualBasic;

namespace CosmicBot.Commands
{
    public class CommandModule : InteractionModuleBase<SocketInteractionContext>
    {
        public async Task Respond(MessageResponse? response)
        {
            if (response != null)
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
            else
                await Context.Interaction.DeferAsync();
        }
    }
}
