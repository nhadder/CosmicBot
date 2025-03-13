using CosmicBot.DiscordResponse;

namespace CosmicBot.Helpers
{
    public static class PagedMessageStore
    {
        private static readonly Dictionary<ulong, PagedListEmbed> _pagedMessages = new();

        public static void AddMessage(ulong messageId, PagedListEmbed pagedList)
        {
            _pagedMessages[messageId] = pagedList;
        }

        public static bool TryGetMessage(ulong messageId, out PagedListEmbed? pagedList)
        {
            return _pagedMessages.TryGetValue(messageId, out pagedList);
        }
    }
}
