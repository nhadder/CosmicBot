using Discord;
using Discord.WebSocket;

namespace CosmicBot.Messages
{
    public class MessageButton
    {
        public string Text { get; set; } = string.Empty;
        public ButtonStyle Style { get; set; } = ButtonStyle.Primary;
        public int Row { get; set; } = 0;
        public IEmote? Emote { get; set; } = null;
        public bool Disabled { get; set; } = false;

        public event Func<IInteractionContext?, Task> OnPress 
        { 
            add { _pressEvents.Add(value); } 
            remove { _pressEvents.Remove(value); } 
        }

        private readonly List<Func<IInteractionContext?, Task>> _pressEvents = new();
        public readonly string Id = $"button_{Guid.NewGuid()}";

        public MessageButton(string text, ButtonStyle style = ButtonStyle.Primary, int row = 0, IEmote? emote = null, bool disabled = false)
        {
            Text = text;
            Style = style;
            Row = row;
            Emote = emote;
            Disabled = disabled;
        }

        public async Task Press(IInteractionContext? context = null)
        {
            foreach (var e in _pressEvents)
                await e(context);
        }
    }
}
