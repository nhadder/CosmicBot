using CosmicBot.DiscordResponse;
using CosmicBot.Models.Enums;
using CosmicBot.Service;
using Discord;
using Discord.Interactions;

namespace CosmicBot.Commands
{
    [DefaultMemberPermissions(GuildPermission.Administrator)]
    [Group("autopost", "Reddit Commands")]
    public class RedditAutopostCommandModule : CommandModule
    {
        private readonly RedditService _service;
        public RedditAutopostCommandModule(RedditService service)
        {
            _service = service;
        }

        [SlashCommand("add", "Auto post the top reddit post. Start time: hh:mm:ss. Interval [d.]hh:mm:ss.")]
        public async Task AddAutoPost(string subreddit, RedditCategory category, IMessageChannel channel, string startTime, string interval)
        {
            if (!HasChannelPermissions())
            {
                await Respond(new MessageResponse("I don't have valid permissions in this channel", ephemeral: true));
                return;

            }
            await Respond(await _service.AddAutoPost(Context.Guild.Id, subreddit, category, channel, startTime, interval));
        }

        [SlashCommand("remove", "Remove Auto post")]
        public async Task RemoveAutoPost(string autopostId)
        {
            if (!HasChannelPermissions())
            {
                await Respond(new MessageResponse("I don't have valid permissions in this channel", ephemeral: true));
                return;
            }

            await Respond(await _service.RemoveAutoPost(autopostId));
        }

        [SlashCommand("list", "List your auto posts")]
        public async Task ListAutoPosts()
        {
            if (!HasChannelPermissions())
            {
                await Respond(new MessageResponse("I don't have valid permissions in this channel", ephemeral: true));
                return;

            }
            await Respond(await _service.ListAutoPosts(Context.Guild.Id));
        }
    }
}