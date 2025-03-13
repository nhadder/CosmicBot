using CosmicBot.Service;
using Discord;
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

        [RequireUserPermission(GuildPermission.Administrator)]
        [Group("autopost", "Reddit AutoPost Commands")]
        public class RedditAutoPostCommandModule : CommandModule
        {

            private readonly RedditService _service;
            public RedditAutoPostCommandModule(RedditService service)
            {
                _service = service;
            }

            [SlashCommand("add", "Auto post the top reddit post. Start time: hh:mm:ss. Interval [d.]hh:mm:ss.")]
            public async Task AddAutoPost(string subreddit, IMessageChannel channel, string startTime, string interval)
            {
                await Respond(await _service.AddAutoPost(Context.Guild.Id, subreddit, channel, startTime, interval));
            }

            [SlashCommand("remove", "Remove Auto post")]
            public async Task RemoveAutoPost(string autopostId)
            {
                await Respond(await _service.RemoveAutoPost(autopostId));
            }

            [SlashCommand("list", "List your auto posts")]
            public async Task ListAutoPosts()
            {
                await Respond(await _service.ListAutoPosts(Context.Guild.Id));
            }
        }
    }
}