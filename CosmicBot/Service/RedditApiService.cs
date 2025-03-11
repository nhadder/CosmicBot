using CosmicBot.DAL;
using CosmicBot.Models;
using Discord;
using Discord.WebSocket;
using System.Text.Json;

namespace CosmicBot.Service
{
    public static class RedditApiService
    {
        private static readonly HttpClient _httpClient = new HttpClient
        {
            DefaultRequestHeaders = { { "User-Agent", "CSharpRedditBot/1.0" } }
        };

        public static async Task PostTop(string subreddit, ulong channelId, DiscordSocketClient discordClient)
        {             
            var imageUrl = await GetRandomImagePostAsync(subreddit);
            var channel = discordClient.GetChannel(channelId) as IMessageChannel;
            if (channel != null && !string.IsNullOrWhiteSpace(imageUrl))
            {
                var embed = new EmbedBuilder()
                    .WithImageUrl(imageUrl)
                    .WithColor(Color.Blue)
                    .WithFooter($"r/{subreddit}")
                    .Build();
                await channel.SendMessageAsync(embed: embed);
            }
        }

        public static async Task<string?> GetRandomImagePostAsync(string subreddit)
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
    }
}
