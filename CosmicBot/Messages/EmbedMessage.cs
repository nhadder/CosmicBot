using CosmicBot.DiscordResponse;
using CosmicBot.Helpers;
using CosmicBot.Models;
using Discord;
using Discord.WebSocket;

namespace CosmicBot.Messages
{
    public abstract class EmbedMessage
    {
        public DateTime Expires { get; protected set; }
        public List<ulong>? Participants { get; set; }
        public List<MessageButton> Buttons { get; set; } = [];
        public bool Expired { get; protected set; }
        public List<PlayerAward> Awards { get; protected set; } = [];
        public bool DisableAutoExpiring { get; set; } = false;

        private ulong _messageId;
        private ulong _channelId;

        public EmbedMessage(List<ulong>? userParticipants = null, bool disableAutoExpiring = false)
        {
            DisableAutoExpiring = disableAutoExpiring;
            if (!DisableAutoExpiring)
                Expires = DateTime.UtcNow.AddMinutes(2);
            else
                Expires = DateTime.UtcNow.AddDays(8);

            Participants = userParticipants;
        }
        public abstract Embed[] GetEmbeds();
        public MessageComponent? GetButtons()
        {
            if (Buttons.Count == 0)
                return null;

            var builder = new ComponentBuilder();
            foreach (var button in Buttons)
                builder.WithButton(button.Text, button.Id, button.Style, button.Emote, disabled: button.Disabled || Expired, row: button.Row);
            return builder.Build();
        }

        public async Task Expire(IDiscordClient client)
        {
            Expired = true;
            await UpdateAsync(client);
        }

        public async Task SendAsync(IInteractionContext context)
        {
            await context.Interaction.DeferAsync();

            var embeds = GetEmbeds();
            var components = GetButtons();

            var message = await context.Interaction.FollowupAsync(embeds: embeds, components: components);
            _messageId = message.Id;
            _channelId = message.Channel.Id;
            await MessageStore.AddMessage(context.Client, message.Id, this);
        }

        public async Task<ulong> SendAsync(IDiscordClient client, IMessageChannel channel)
        {
            var embeds = GetEmbeds();
            var components = GetButtons();

            var message = await channel.SendMessageAsync(embeds: embeds, components: components);
            _messageId = message.Id;
            _channelId = message.Channel.Id;
            await MessageStore.AddMessage(client, message.Id, this);
            return message.Id;
        }

        public async Task UpdateAsync(IDiscordClient client)
        {
            var embeds = GetEmbeds();
            var components = GetButtons();

            if (await client.GetChannelAsync(_channelId) is not IMessageChannel channel)
                return;
            if (await channel.GetMessageAsync(_messageId) is not IUserMessage message)
                return;

            await message.ModifyAsync(msg => { msg.Embeds = embeds; msg.Components = components; });
        }

        public async Task<MessageResponse?> HandleButtons(IInteractionContext context, SocketMessageComponent messageComponent)
        {
            if (Expired)
            {
                await UpdateAsync(context.Client);
                return null;
            }

            var button = Buttons.FirstOrDefault(b => messageComponent.Data.CustomId.StartsWith(b.Id));
            if (button != null)
            {
                if(!DisableAutoExpiring)
                    Expires = DateTime.UtcNow.AddMinutes(2);
                var response = button.Press(context);
                await UpdateAsync(context.Client);
                return response;
            }

            return null;
        }

    }
}
