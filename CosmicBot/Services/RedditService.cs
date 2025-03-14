using CosmicBot.DAL;
using CosmicBot.DiscordResponse;
using CosmicBot.Models;
using Discord;
using Discord.WebSocket;
using Microsoft.EntityFrameworkCore;
using System.Text;
using System.Text.Json;

namespace CosmicBot.Service
{
    public class RedditService
    {

        private readonly DataContext _context;
        private readonly HttpClient _httpClient;

        public RedditService(DataContext context)
        {
            _context = context;
            _httpClient = new HttpClient
            {
                DefaultRequestHeaders = { { "User-Agent", "CSharpRedditBot/1.0" } }
            };
        }

        public async Task<MessageResponse> PostTop(string subreddit)
        {
            subreddit = subreddit.StartsWith(("r/")) ? subreddit.Substring(2) : subreddit;
            var imageUrl = await GetRandomImagePostAsync(subreddit);
            if (!string.IsNullOrWhiteSpace(imageUrl))
            {
                var embed = new EmbedBuilder()
                    .WithImageUrl(imageUrl)
                    .WithColor(Color.Blue)
                    .WithFooter($"r/{subreddit}")
                    .Build();
                return new MessageResponse(embed: embed);
            }
            return new MessageResponse("Unable to find image on that subreddit", ephemeral: true);
        }

        #region AutoPosts

        public async Task<MessageResponse> AddAutoPost(ulong guildId, string subreddit, IMessageChannel channel, string startTime, string interval)
        {
            subreddit = subreddit.StartsWith(("r/")) ? subreddit.Substring(2) : subreddit;
            var start = TimeOnly.Parse(startTime);
            var period = TimeSpan.Parse(interval);

            var newAutoPost = new RedditAutoPost()
            {
                GuildId = guildId,
                Subreddit = subreddit,
                ChannelId = channel.Id,
                StartTime = start,
                Interval = period
            };

            _context.Add(newAutoPost);
            await _context.SaveChangesAsync();
            return new MessageResponse($"Added new auto post for r/{subreddit} to channel {channel.Name} every {period} starting at {start}", ephemeral: true);
        }

        public async Task<MessageResponse> RemoveAutoPost(string autopostId)
        {
            var autopost = _context.RedditAutoPosts.FirstOrDefault(a => a.Id == StrToGuid(autopostId));

            if (autopost == null)
                return new MessageResponse("Auto post not found.", ephemeral: true);

            _context.Remove(autopost);
            await _context.SaveChangesAsync();
            return new MessageResponse($"Removed auto post id {autopostId}", ephemeral: true);
        }

        public async Task<MessageResponse> ListAutoPosts(ulong guildId)
        {
            var autopost = await _context.RedditAutoPosts.Where(a => a.GuildId == guildId).ToListAsync();

            if (autopost.Count == 0)
                return new MessageResponse("No auto posts yet.", ephemeral: true);

            var sb = new StringBuilder();
            foreach (var post in autopost)
                sb.AppendLine($"({post.Id}) r/{post.Subreddit} to Channel @{post.ChannelId}");

            return new MessageResponse(sb.ToString(), ephemeral: true);
        }

        public async Task<List<RedditAutoPost>> GetAllAutoPosts()
        {
            return await _context.RedditAutoPosts.AsNoTracking().ToListAsync();
        }

        public async Task UpdateAutoPost(RedditAutoPost post)
        {
            _context.RedditAutoPosts.Update(post);
            await _context.SaveChangesAsync();
        }

        #endregion

        #region Private Methods

        private async Task<string?> GetRandomImagePostAsync(string subreddit)
        {
            string url = $"https://www.reddit.com/r/{subreddit}/hot.json?limit=30&t=day";

            try
            {
                HttpResponseMessage response = await _httpClient.GetAsync(url);
                response.EnsureSuccessStatusCode();
                string json = await response.Content.ReadAsStringAsync();

                using JsonDocument doc = JsonDocument.Parse(json);
                var posts = doc.RootElement
                    .GetProperty("data").GetProperty("children")
                    .EnumerateArray()
                    .Select(p => p.GetProperty("data"))
                    .Where(p =>
                    {
                        string url = p.GetProperty("url").GetString() ?? "";
                        return url.EndsWith(".jpg") || url.EndsWith(".png") || url.EndsWith(".gif") || url.EndsWith(".gifv");
                    });

                Random rng = new Random();
                var topPost = posts.OrderBy(_ => rng.Next()).FirstOrDefault();
                return topPost.ValueKind != JsonValueKind.Undefined ? topPost.GetProperty("url").GetString() : null;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error fetching top image post: {ex.Message}");
                return null;
            }
        }

        private static Guid StrToGuid(string? str)
        {
            if (str == null)
                return Guid.Empty;

            return Guid.TryParse(str, out Guid result) ? result : Guid.Empty;
        }

        #endregion
    }
}
