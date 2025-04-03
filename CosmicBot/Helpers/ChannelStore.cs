using CosmicBot.Channels;
using CosmicBot.Service;
using Discord;
using Discord.WebSocket;

namespace CosmicBot.Helpers
{
    public static class ChannelStore
    {
        private static readonly Dictionary<ulong, ulong> _channels = [];
        private static object _lock = new object();

        public static void AddMessage(IDiscordClient client, ulong channelId, ulong userId)
        {
            lock (_lock)
            {
                _channels[userId] = channelId;
            }
        }

        public static async Task HandleUserJoinedVoiceChannel(IDiscordClient client, GuildSettingsService guildSettingsService, SocketUser user, SocketVoiceChannel joinedChannel)
        {
            await CheckForExpiredMessages(client);
            if (user is not SocketGuildUser guildUser) return;

            var guild = joinedChannel.Guild;

            var configuredVoiceChannelId = guildSettingsService.GetCreatePrivateVoiceChannelId(guild.Id);
            if (joinedChannel.Id != configuredVoiceChannelId) return;

            var category = joinedChannel.Category;

            if (_channels.ContainsKey(user.Id))
            {
                var existingVc = await client.GetChannelAsync(_channels[user.Id]) as IVoiceChannel;
                if (existingVc != null)
                {
                    await guildUser.ModifyAsync(properties => properties.Channel = Optional.Create(existingVc));
                    return;
                }
            }

            var newChannel = await guild.CreateVoiceChannelAsync($"🎧{user.GlobalName}'s Studio🎧", properties =>
            {
                properties.CategoryId = category?.Id;
                properties.Position = joinedChannel.Position + 1;
            });

            AddMessage(client, newChannel.Id, user.Id);

            await guildUser.ModifyAsync(properties => properties.Channel = newChannel);
        }

        public static bool RemoveMessage(ulong userId)
        {
            lock (_lock)
            {
                return _channels.Remove(userId);
            }
        }

        public static async Task CheckForExpiredMessages(IDiscordClient client)
        {
            foreach(var channel in _channels)
            {
                var vc = await client.GetChannelAsync(channel.Value) as IVoiceChannel;
                if (vc == null)
                {
                    RemoveMessage(channel.Key);
                }
                else
                {
                    var users = await vc.GetUsersAsync().FlattenAsync();
                    if (!users.Any(u => u.VoiceChannel?.Id == vc.Id))
                    {
                        await vc.DeleteAsync();
                        RemoveMessage(channel.Key);
                    }
                }
            }
        }

        public static async Task ExpireAllMessages(IDiscordClient client)
        {
            foreach (var channel in _channels)
            {
                var vc = await client.GetChannelAsync(channel.Value) as IVoiceChannel;
                if (vc == null)
                {
                    RemoveMessage(channel.Key);
                }
                else
                {
                    await vc.DeleteAsync();
                    RemoveMessage(channel.Key);
                }
            }
        }
    }
}
