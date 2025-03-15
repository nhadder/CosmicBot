using CosmicBot.DiscordResponse;
using CosmicBot.Service;
using Discord.Interactions;

namespace CosmicBot.Commands
{
    [Group("reddit", "Reddit Commands")]
    public class RedditCommandModule : CommandModule
    {
        private readonly RedditService _service;
        public RedditCommandModule(RedditService service)
        {
            _service = service;
        }

        [SlashCommand("post", "Show a random image in a subreddit")]
        public async Task Post(string subreddit)
        {
            if (!HasChannelPermissions())
            {
                await Respond(new MessageResponse("I don't have valid permissions in this channel", ephemeral: true));
                return;
            }

            await Respond(await _service.PostTop(subreddit));
        }
    }
}