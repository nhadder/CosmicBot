using CosmicBot.DiscordResponse;

namespace CosmicBot.Helpers
{
    public static class GameMessageStore
    {
        private static readonly Dictionary<ulong, BlackjackEmbed> _blackjackGames = new();

        public static void AddMessage(ulong messageId, BlackjackEmbed game)
        {
            CheckForExpiredGames();
            _blackjackGames[messageId] = game;
        }

        public static bool TryGetMessage(ulong messageId, out BlackjackEmbed? game)
        {
            CheckForExpiredGames();
            return _blackjackGames.TryGetValue(messageId, out game);
        }

        public static bool RemoveMessage(ulong messageId)
        {
            return _blackjackGames.Remove(messageId);
        }

        private static void CheckForExpiredGames()
        {
            foreach(var game in _blackjackGames)
            {
                if (DateTime.UtcNow > game.Value.Expires)
                    RemoveMessage(game.Key);
            }
        }
    }
}
