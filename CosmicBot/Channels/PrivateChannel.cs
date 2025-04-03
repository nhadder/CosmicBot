using Discord;

namespace CosmicBot.Channels
{
    public class PrivateChannel
    {
        private ulong _channelId;

        public PrivateChannel(ulong channelId)
        {
            _channelId = channelId;
        }
    }
}
