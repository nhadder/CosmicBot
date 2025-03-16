using CosmicBot.DiscordResponse;
using CosmicBot.Messages;
using CosmicBot.Service;
using Discord;
using Discord.WebSocket;

namespace CosmicBot.Helpers
{
    public static class MessageStore
    {
        private static readonly Dictionary<ulong, EmbedMessage> _messages = [];

        public static async Task AddMessage(IInteractionContext context, ulong messageId, EmbedMessage message)
        {
            await CheckForExpiredMessages(context.Client);
            _messages[messageId] = message;
        }

        public static async Task<MessageResponse?> HandleMessageButtons(IInteractionContext context, PlayerService playerService)
        {
            await CheckForExpiredMessages(context.Client);
            if (context.Interaction is SocketMessageComponent messageComponent)
            {
                var result = _messages.TryGetValue(messageComponent.Message.Id, out EmbedMessage? message);
                if (result)
                {
                    if (message == null)
                    {
                        RemoveMessage(messageComponent.Message.Id);
                        return new MessageResponse("That message component has expired", ephemeral: true);
                    }

                    if (message.Participants != null && !message.Participants.Contains(context.Interaction.User.Id))
                        return new MessageResponse("You are not a participant to interact with that", ephemeral: true);

                    await message.HandleButtons(context, messageComponent);

                    if (message.Awards.Count > 0)
                    {
                        foreach (var award in message.Awards)
                            await playerService.Award(context.Guild.Id,
                                award.UserId,
                                award.Points,
                                award.Experience,
                                award.GamesWon,
                                award.GamesLost);

                        message.Awards.Clear();
                    }
                    
                    if(message.Expired)
                        RemoveMessage(messageComponent.Message.Id);

                    return null;
                }
                else
                {
                    return new MessageResponse("That message component has expired", ephemeral: true);
                }
            }
            return null;
        }

        public static bool RemoveMessage(ulong messageId)
        {
            return _messages.Remove(messageId);
        }

        public static async Task CheckForExpiredMessages(IDiscordClient client)
        {
            foreach(var message in _messages)
            {
                if (DateTime.UtcNow > message.Value.Expires)
                {
                    await message.Value.Expire(client);
                    RemoveMessage(message.Key);
                }
            }
        }
    }
}
