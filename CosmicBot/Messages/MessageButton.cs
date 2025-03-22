using CosmicBot.DiscordResponse;
using Discord;

namespace CosmicBot.Messages
{
    public class MessageButton
    {
        public string Text { get; set; } = string.Empty;
        public ButtonStyle Style { get; set; } = ButtonStyle.Primary;
        public int Row { get; set; } = 0;
        public IEmote? Emote { get; set; } = null;
        public bool Disabled { get; set; } = false;

        public Func<IInteractionContext, MessageResponse?> OnPress { get; set; } = (context) => { return null; };

        public readonly string Id = $"button_{Guid.NewGuid()}";

        public MessageButton(string text, ButtonStyle style = ButtonStyle.Primary, int row = 0, IEmote? emote = null, bool disabled = false)
        {
            Text = text;
            Style = style;
            Row = row;
            Emote = emote;
            Disabled = disabled;
        }

        public MessageResponse? Press(IInteractionContext context)
        {
            return OnPress(context);
        }
    }
}
