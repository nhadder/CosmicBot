using CosmicBot.Service;
using Discord;
using Discord.Interactions;

namespace CosmicBot.BotCommands
{
    [RequireUserPermission(GuildPermission.Administrator)]
    [Group("autopost", "Reddit Commands")]
    public class RedditAutopostCommandModule : CommandModule
    {
        private readonly RedditService _service;
        public RedditAutopostCommandModule(RedditService service)
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