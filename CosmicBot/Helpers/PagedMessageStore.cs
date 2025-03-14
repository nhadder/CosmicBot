using CosmicBot.DiscordResponse;

namespace CosmicBot.Helpers
{
    public static class PagedMessageStore
    {
        private static readonly Dictionary<ulong, PagedListEmbed> _pagedMessages = new();

        public static void AddMessage(ulong messageId, PagedListEmbed pagedList)
        {
            CheckForExpiredGames();
            _pagedMessages[messageId] = pagedList;
        }

        public static bool TryGetMessage(ulong messageId, out PagedListEmbed? pagedList)
        {
            CheckForExpiredGames();
            return _pagedMessages.TryGetValue(messageId, out pagedList);
        }

        public static bool RemoveMessage(ulong messageId)
        {
            return _pagedMessages.Remove(messageId);
        }

        private static void CheckForExpiredGames()
        {
            foreach (var pagedList in _pagedMessages)
            {
                if (DateTime.UtcNow > pagedList.Value.Expires)
                    RemoveMessage(pagedList.Key);
            }
        }
    }
}
