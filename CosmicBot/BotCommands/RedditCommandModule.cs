using CosmicBot.DAL;
using CosmicBot.Models;
using CosmicBot.Service;
using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using Microsoft.EntityFrameworkCore;
using System.Text;

namespace CosmicBot.BotCommands
{
    [Group("reddit", "Reddit Commands")]
    public class RedditCommandModule : InteractionModuleBase<SocketInteractionContext>
    {
        private readonly DataContext _context;
        private readonly DiscordSocketClient _discord;
        public RedditCommandModule(DataContext context, DiscordSocketClient discord)
        {
            _context = context;
            _discord = discord;
        }

        [SlashCommand("post", "Show a random image in a subreddit")]
        public async Task Post(string subreddit)
        {
            subreddit = subreddit.StartsWith(("r/")) ? subreddit.Substring(2) : subreddit;
            await RedditApiService.PostTop(subreddit, Context.Channel.Id, _discord);
        }

        [RequireUserPermission(GuildPermission.Administrator)]
        [Group("autopost", "Reddit AutoPost Commands")]
        public class RedditAutoPostCommandModule : InteractionModuleBase<SocketInteractionContext>
        {

            private readonly DataContext _context;
            public RedditAutoPostCommandModule(DataContext context)
            {
                _context = context;
            }

            [SlashCommand("add", "Auto post the top reddit post. Start time: hh:mm:ss. Interval [d.]hh:mm:ss.")]
            public async Task AddAutoPost(string subreddit, IMessageChannel channel, string startTime, string interval)
            {

                if (!UserIsMod(Context.User))
                {
                    await RespondAsync("You don't have permission to use this command.", ephemeral: true);
                    return;
                }

                subreddit = subreddit.StartsWith(("r/")) ? subreddit.Substring(2) : subreddit;
                var start = TimeOnly.Parse(startTime);
                var period = TimeSpan.Parse(interval);

                var newAutoPost = new RedditAutoPost()
                {
                    GuildId = Context.Guild.Id,
                    Subreddit = subreddit,
                    ChannelId = channel.Id,
                    StartTime = start,
                    Interval = period
                };

                _context.Add(newAutoPost);
                await _context.SaveChangesAsync();
                await RespondAsync($"Added new auto post for r/{subreddit} to channel {channel.Name} every {period} starting at {start}", ephemeral: true);
            }

            [SlashCommand("remove", "Remove Auto post")]
            public async Task RemoveAutoPost(string autopost_id)
            {
                var autopostId = StrToGuid(autopost_id);

                if (!UserIsMod(Context.User))
                {
                    await RespondAsync("You don't have permission to use this command.", ephemeral: true);
                    return;
                }

                var autopost = _context.RedditAutoPosts.FirstOrDefault(a => a.Id == autopostId);

                if (autopost == null)
                {
                    await RespondAsync("Auto post not found.", ephemeral: true);
                    return;
                }

                _context.Remove(autopost);
                await _context.SaveChangesAsync();
                await RespondAsync($"Removed auto post id {autopostId}", ephemeral: true);
            }

            [SlashCommand("list", "List your auto posts")]
            public async Task ListAutoPosts()
            {
                if (!UserIsMod(Context.User))
                {
                    await RespondAsync("You don't have permission to use this command.", ephemeral: true);
                    return;
                }

                var autopost = await _context.RedditAutoPosts.Where(a => a.GuildId == Context.Guild.Id).ToListAsync();

                if (autopost.Count == 0)
                {
                    await RespondAsync("No auto posts yet.", ephemeral: true);
                    return;
                }

                var sb = new StringBuilder();
                foreach (var post in autopost)
                {
                    sb.AppendLine($"({post.Id}) r/{post.Subreddit} to Channel @{post.ChannelId}");
                }

                await RespondAsync(sb.ToString(), ephemeral: true);
            }
        }

        private static bool UserIsMod(SocketUser user)
        {
            var guildUser = user as SocketGuildUser;
            return guildUser?.GuildPermissions.Administrator ?? false;
        }

        private static Guid StrToGuid(string? str)
        {
            if (str == null)
                return Guid.Empty;

            return Guid.TryParse(str, out Guid result) ? result : Guid.Empty;
        }
    }
}