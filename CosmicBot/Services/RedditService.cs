using CosmicBot.DAL;
using CosmicBot.DiscordResponse;
using CosmicBot.Models;
using CosmicBot.Models.Enums;
using Discord;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using System.Text;

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

        public async Task<MessageResponse> Post(string subreddit, RedditCategory category = RedditCategory.Hot)
        {
            subreddit = subreddit.StartsWith(("r/")) ? subreddit.Substring(2) : subreddit;
            var post  = await GetRandomPostAsync(subreddit, category);
            return post != null ? post : new MessageResponse("Unable to find a suitable post on that subreddit", ephemeral: true);
        }

        #region AutoPosts

        public async Task<MessageResponse> AddAutoPost(ulong guildId, string subreddit, RedditCategory category, IMessageChannel channel, string startTime, string interval)
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
                Interval = period,
                Category = category
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
                sb.AppendLine($"**{post.Id}**\nr/{post.Subreddit} [{post.Category}] to Channel <#{post.ChannelId}>\n");

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

        private async Task<MessageResponse?> GetRandomPostAsync(string subreddit, RedditCategory category = RedditCategory.Hot)
        {
            string url = $"https://www.reddit.com/r/{subreddit}/{category.ToString().ToLower()}.json?limit=50&t=day";

            try
            {
                HttpResponseMessage response = await _httpClient.GetAsync(url);
                response.EnsureSuccessStatusCode();
                string json = await response.Content.ReadAsStringAsync();
                var redditResponse = JsonConvert.DeserializeObject<RedditData<RedditListingData>>(json);
                if(redditResponse != null)
                {
                    var posts = redditResponse.Data.Children.Select(x => x.Data);
                    Random rng = new Random();
                    var randomPost = posts.OrderBy(_ => rng.Next()).FirstOrDefault();
                    if (randomPost != null)
                    {
                        if (randomPost.IsImage)
                        {
                            return new MessageResponse(embed: new EmbedBuilder()
                                .WithTitle(randomPost.Title ?? string.Empty)
                                .WithImageUrl(randomPost.Url)
                                .WithFooter($"r/{subreddit}").Build());
                        }
                        else if (randomPost.Is_gallery != null && randomPost.Is_gallery == true)
                        {
                            return new MessageResponse(text: randomPost?.Url ?? string.Empty);
                        }
                        else if (!string.IsNullOrWhiteSpace(randomPost?.Media?.Reddit_video?.Fallback_url))
                        {
                            return new MessageResponse(text: randomPost?.Media?.Reddit_video?.Fallback_url ?? string.Empty);
                        }
                        else if (!string.IsNullOrWhiteSpace(randomPost?.Preview?.Reddit_video_preview?.Fallback_url))
                        {
                            return new MessageResponse(text: randomPost?.Preview?.Reddit_video_preview?.Fallback_url ?? string.Empty);
                        }
                        else if (randomPost?.Url != null && !randomPost.Url.ToLower().Contains("reddit"))
                        {
                            return new MessageResponse(text: randomPost?.Url ?? string.Empty);
                        }

                        if (randomPost.Selftext != null)
                            randomPost.Selftext = randomPost.Selftext.Length > 4093 ? randomPost.Selftext.Substring(0, 4093) + "..." : randomPost.Selftext;

                        var builder = new EmbedBuilder()
                            .WithTitle(randomPost?.Title ?? string.Empty)
                            .WithDescription(randomPost?.Selftext ?? string.Empty)
                            .WithUrl(randomPost?.Url ?? string.Empty)
                            .WithFooter($"r/{subreddit}");
                        return new MessageResponse(embed: builder.Build());
                        
                    }
                }
                return null;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error fetching top image post: {ex.Message}\n{ex.StackTrace}");
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
