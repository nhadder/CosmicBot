using Discord.WebSocket;
using Discord;
using CosmicBot.Helpers;

namespace CosmicBot.DiscordResponse
{
    public class PagedListEmbed
    {
        private readonly List<string> _items;
        private readonly int _pageSize;
        private int _currentPage = 0;

        public string Title { get; set; }

        public PagedListEmbed(List<string> items, int pageSize, string title)
        {
            _items = items;
            _pageSize = pageSize;
            Title = title;
        }

        public async Task SendAsync(IInteractionContext context)
        {
            await context.Interaction.DeferAsync();

            var embed = GetPageEmbed();
            var components = GetPageButtons();

            var message = await context.Interaction.FollowupAsync(embed: embed, components: components);
            PagedMessageStore.AddMessage(message.Id, this);
        }

        private Embed GetPageEmbed()
        {
            var pagedItems = _items.Skip(_currentPage * _pageSize).Take(_pageSize);
            var embedBuilder = new EmbedBuilder()
                .WithTitle(Title)
                .WithDescription($"```{string.Join("\n", pagedItems)}```")
                .WithFooter($"Page {_currentPage + 1} / {TotalPages}");

            return embedBuilder.Build();
        }

        private MessageComponent GetPageButtons()
        {
            var builder = new ComponentBuilder();
            if (_currentPage > 0)
                builder.WithButton("◀ Previous", $"page_prev_{_currentPage}", ButtonStyle.Primary);

            if (_currentPage < TotalPages - 1)
                builder.WithButton("Next ▶", $"page_next_{_currentPage}", ButtonStyle.Primary);

            return builder.Build();
        }

        private int TotalPages => (int)Math.Ceiling((double)_items.Count / _pageSize);

        public async Task HandleButtonAsync(SocketMessageComponent component)
        {
            if (component.Data.CustomId.StartsWith("page_prev"))
                _currentPage--;
            else if (component.Data.CustomId.StartsWith("page_next"))
                _currentPage++;

            var embed = GetPageEmbed();
            var components = GetPageButtons();
            await component.UpdateAsync(msg => { msg.Embed = embed; msg.Components = components; });
        }
    }
}
