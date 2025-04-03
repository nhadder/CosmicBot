using CosmicBot.DiscordResponse;
using CosmicBot.Messages;
using CosmicBot.Messages.Components;
using CosmicBot.Service;
using CosmicBot.Services;
using Discord;
using Discord.WebSocket;

namespace CosmicBot.Helpers
{
    public static class MessageStore
    {
        private static readonly Dictionary<ulong, EmbedMessage> _messages = [];
        private static object _lock = new object();

        public static void AddMessage(IDiscordClient client, ulong messageId, EmbedMessage message)
        {
            lock (_lock)
            {
                _messages[messageId] = message;
            }
        }

        public static EmbedMessage GetMessage(ulong messageId)
        {
            lock (_lock)
            {
                return _messages[messageId];
            }
        }

        public static List<EmbedMessage> GetMessagesOfType(Type type)
        {
            lock (_lock)
            {
                return _messages.Where(m => m.GetType() == type)?.Select(m => m.Value).ToList() ?? new List<EmbedMessage>();
            }
        }

        public static bool MessageExists(ulong messageId)
        {
            lock (_lock)
            {
                return _messages.ContainsKey(messageId);
            }
        }

        public static async Task<MessageResponse?> HandleMessageButtons(IInteractionContext context, PlayerService playerService, PetService petService)
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

                    var response = await message.HandleButtons(context, messageComponent);

                    if (message.Awards.Count > 0)
                    {
                        bool pointsLeft = true;
                        foreach (var award in message.Awards)
                        {
                            pointsLeft = await playerService.Award(context.Guild.Id,
                                award.UserId,
                                award.Points,
                                award.Experience,
                                award.GamesWon,
                                award.GamesLost) && pointsLeft;
                        }

                        message.Awards.Clear();

                        if(!pointsLeft)
                        {
                            await message.Expire(context.Client);
                            RemoveMessage(messageComponent.Message.Id);
                            return new MessageResponse("Not enough stars to play again...", ephemeral: true);
                        }
                    }

                    if (message is PetGame)
                    {
                        var petGame = message as PetGame;
                        if(petGame != null) 
                            await petService.Update(petGame.Pet);
                    }
                    
                    if(message.Expired)
                        RemoveMessage(messageComponent.Message.Id);

                    return response;
                }
                else
                {
                    if (await context.Client.GetChannelAsync(context.Channel.Id) is IMessageChannel channel)
                    {
                        if (await channel.GetMessageAsync(messageComponent.Message.Id) is IUserMessage userMessage)
                        {
                            await userMessage.DeleteAsync();
                        }
                    }

                    return new MessageResponse("That message component has expired", ephemeral: true);
                }
            }
            return null;
        }

        public static bool RemoveMessage(ulong messageId)
        {
            lock (_lock)
            {
                return _messages.Remove(messageId);
            }
        }

        public static async Task CheckForExpiredMessages(IDiscordClient client)
        {
            foreach(var message in _messages)
            {
                if (DateTime.UtcNow > message.Value.Expires || message.Value.Expired)
                {
                    await message.Value.Expire(client);
                    RemoveMessage(message.Key);
                }
            }
        }

        public static async Task ExpireAllMessages(IDiscordClient client)
        {
            foreach (var message in _messages)
            {
                await message.Value.Expire(client);
                RemoveMessage(message.Key);
            }
        }
    }
}
