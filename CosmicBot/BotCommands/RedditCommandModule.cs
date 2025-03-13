using CosmicBot.Service;
using Discord.Interactions;

namespace CosmicBot.BotCommands
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
            await Respond(await _service.PostTop(subreddit));
        }
    }
}