using Discord;

namespace CosmicBot.DiscordResponse
{
    public class MessageResponse
    {
        public string? Text { get; set; } = null;
        public Embed[]? Embeds { get; set; } = null;
        public bool TTS { get; set; } = false;
        public bool Ephemeral { get; set; } = false;
        public AllowedMentions? AllowedMentions { get; set; } = null;
        public RequestOptions? RequestOptions { get; set; } = null;
        public MessageComponent? Components { get; set; } = null;
        public Embed? Embed { get; set; } = null;
        public PollProperties? Poll { get; set; } = null;

        public MessageResponse(string? text = null, Embed[]? embeds = null, bool isTTS = false, bool ephemeral = false,
            AllowedMentions? allowedMentions = null, RequestOptions? options = null, MessageComponent? components = null, Embed? embed = null, PollProperties? poll = null)
        {
            Text = text;
            Embeds = embeds;
            TTS = isTTS;
            Ephemeral = ephemeral;
            AllowedMentions = allowedMentions;
            RequestOptions = options;
            Components = components;
            Embed = embed;
            Poll = poll;
        }
    }
}
