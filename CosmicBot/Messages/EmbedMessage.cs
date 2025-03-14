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

        private ulong _messageId;
        private ulong _channelId;

        public EmbedMessage(List<ulong>? userParticipants = null)
        {
            Expires = DateTime.UtcNow.AddMinutes(1);
            Participants = userParticipants;
        }
        public abstract Embed GetEmbed();
        public MessageComponent? GetButtons()
        {
            if (Buttons.Count == 0)
                return null;

            var builder = new ComponentBuilder();
            foreach (var button in Buttons)
                builder.WithButton(button.Text, button.Id, button.Style, button.Emote, disabled: button.Disabled || Expired, row: button.Row);
            return builder.Build();
        }
        public async Task Expire(IInteractionContext context)
        {
            Expired = true;
            await UpdateAsync(context);
        }

        public async Task SendAsync(IInteractionContext context)
        {
            await context.Interaction.DeferAsync();

            var embed = GetEmbed();
            var components = GetButtons();

            var message = await context.Interaction.FollowupAsync(embed: embed, components: components);
            _messageId = message.Id;
            _channelId = message.Channel.Id;
            await MessageStore.AddMessage(context, message.Id, this);
        }

        public async Task UpdateAsync(IInteractionContext context)
        {
            var embed = GetEmbed();
            var components = GetButtons();

            if (await context.Client.GetChannelAsync(_channelId) is not IMessageChannel channel)
                return;
            if (await channel.GetMessageAsync(_messageId) is not IUserMessage message)
                return;

            await message.ModifyAsync(msg => { msg.Embed = embed; msg.Components = components; });
        }

        public async Task HandleButtons(IInteractionContext context, SocketMessageComponent messageComponent)
        {
            if (Expired)
            {
                await UpdateAsync(context);
                return;
            }

            var button = Buttons.FirstOrDefault(b => messageComponent.Data.CustomId.StartsWith(b.Id));
            if (button != null)
            {
                Expires = DateTime.UtcNow.AddMinutes(1);
                await button.Press();
                await UpdateAsync(context);
            }
        }

    }
}
