using CosmicBot.DiscordResponse;
using Discord;

namespace CosmicBot.Messages.Components
{
    public class PagedList : EmbedMessage
    {
        private readonly List<string> _items;
        private readonly int _pageSize;
        private int _currentPage = 0;
        private string _title;
        private int _totalPages => (int)Math.Ceiling((double)_items.Count / _pageSize);
        private MessageButton _next;
        private MessageButton _prev;

        public PagedList(List<string> items, int pageSize, string title) : base()
        {
            _items = items;
            _pageSize = pageSize;
            _title = title;

            _prev = new MessageButton("◀ Previous", ButtonStyle.Primary);
            _prev.OnPress = Prev;
            Buttons.Add(_prev);

            _next = new MessageButton("Next ▶", ButtonStyle.Primary);
            _next.OnPress = Next;
            Buttons.Add(_next);

            DisableButtonsIfNecessary();
        }

        public override Embed[] GetEmbeds()
        {
            var pagedItems = _items.Skip(_currentPage * _pageSize).Take(_pageSize);

            var expiredMessage = Expired ? "\nThis message component has expired." : "";

            var embedBuilder = new EmbedBuilder()
                .WithTitle(_title)
                .WithDescription($"```{string.Join("\n", pagedItems)}```{expiredMessage}")
                .WithFooter($"Page {_currentPage + 1} / {_totalPages}");

            return [embedBuilder.Build()];
        }

        private void DisableButtonsIfNecessary()
        {
            _prev.Disabled = _currentPage == 0;
            _next.Disabled = _currentPage == _totalPages - 1;
        }

        private MessageResponse? Next(IInteractionContext context)
        {
            _currentPage++;
            DisableButtonsIfNecessary();
            return null;
        }

        private MessageResponse? Prev(IInteractionContext context)
        {
            _currentPage--;
            DisableButtonsIfNecessary();
            return null;
        }
    }
}
