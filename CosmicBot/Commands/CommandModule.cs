using CosmicBot.DiscordResponse;
using Discord;
using Discord.Interactions;

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

        public bool HasChannelPermissions()
        {
            if (Context.Guild == null || Context.Channel == null)
                return false;

            var botUser = Context.Guild.CurrentUser;
            if (botUser == null)
                return false;

            if (Context.Channel is not IGuildChannel channel)
                return false;

            var botPermissions = botUser.GetPermissions(channel);

            return botPermissions.Has(ChannelPermission.ViewChannel) && botPermissions.Has(ChannelPermission.SendMessages);
        }
    }
}
