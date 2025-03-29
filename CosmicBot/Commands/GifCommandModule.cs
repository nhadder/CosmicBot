using CosmicBot.DiscordResponse;
using CosmicBot.Helpers;
using Discord.Interactions;

namespace CosmicBot.Commands
{
    public class GifCommandModule : CommandModule
    {
        public GifCommandModule() { }

        [SlashCommand("gif", "Send a gif")]
        public async Task Gif(string tags)
        {
            if (!HasChannelPermissions())
            {
                await Respond(new MessageResponse("I don't have valid permissions in this channel", ephemeral: true));
                return;
            }

            await Respond(await GifFetcher.GetGifMessage(tags));
        }
    }
}
