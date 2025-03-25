using CosmicBot.DAL;
using CosmicBot.DiscordResponse;
using CosmicBot.Helpers;
using CosmicBot.Models;
using CosmicBot.Models.Enums;
using System.Runtime.CompilerServices;

namespace CosmicBot.Service
{
    public class GuildSettingsService
    {
        private readonly DataContext _context;

        public GuildSettingsService(DataContext context)
        {
            _context = context;
        }

        public async Task<MessageResponse> SetTimezone(ulong guildId, TimeZoneEnum timezone)
        {
            var timezoneStr = TimeZoneHelper.GetTimeZoneId(timezone);
            await SetSetting(guildId, GuildSettingNames.Timezone, timezoneStr);
            return new MessageResponse($"Timezone updated to {timezoneStr}", ephemeral: true);
        }

        public DateTime GetGuildTime(ulong guildId)
        {
            var timeZoneSetting = GetSetting(guildId, GuildSettingNames.Timezone);
            if (timeZoneSetting != null)
                return TimeZoneHelper.GetGuildTimeNow(timeZoneSetting);
            return DateTime.UtcNow;
        }

        public async Task RemoveDanceBattleSetting(ulong guildId)
        {
            await RemoveSetting(guildId, GuildSettingNames.DanceBattleChannel);
        }

        public async Task<MessageResponse> SetDanceBattleChannel(ulong guildId, ulong channelId)
        {
            await SetSetting(guildId, GuildSettingNames.DanceBattleChannel, channelId.ToString());
            return new MessageResponse($"Dance Battle Channel Set to <#{channelId}>", ephemeral: true);
        }

        public ulong? GetDanceBattleChannel(ulong guildId)
        {
            var setting = GetSetting(guildId, GuildSettingNames.DanceBattleChannel);
            if (string.IsNullOrEmpty(setting)) return null;
            return Convert.ToUInt64(setting);
        }

        public async Task SetDanceBattleMessageId(ulong guildId, ulong messageId)
        {
            await SetSetting(guildId, GuildSettingNames.DanceBattleMessageId, messageId.ToString());
        }

        public ulong? GetDanceBattleMessageId(ulong guildId)
        {
            var setting = GetSetting(guildId, GuildSettingNames.DanceBattleMessageId);
            if (string.IsNullOrEmpty(setting)) return null;
            return Convert.ToUInt64(setting);
        }

        public async Task RemoveDanceBattleMessageId(ulong guildId)
        {
            await RemoveSetting(guildId, GuildSettingNames.DanceBattleMessageId);
        }

        public async Task RemoveBotChannel(ulong guildId, ulong channelId)
        {
            var channels = GetBotChannels(guildId);
            if (channels == null)
                return;

            channels.Remove(channelId);
            await SetSetting(guildId, GuildSettingNames.BotChannel, string.Join(";", channels));
        }

        public async Task<MessageResponse> SetBotChannel(ulong guildId, ulong channelId)
        {
            var channels = GetBotChannels(guildId);
            if (channels == null)
                channels = new List<ulong>();

            channels.Add(channelId);

            await SetSetting(guildId, GuildSettingNames.BotChannel, string.Join(";", channels));
            return new MessageResponse($"Bot Channel added <#{channelId}>", ephemeral: true);
        }

        public List<ulong>? GetBotChannels(ulong guildId)
        {
            var setting = GetSetting(guildId, GuildSettingNames.BotChannel);
            if (string.IsNullOrEmpty(setting)) return null;
            return setting.Split(";").Select(c => Convert.ToUInt64(c)).ToList();
        }

        public async Task SetDanceBattleStartTime(ulong guildId, DateTime startTime)
        {
            await SetSetting(guildId, GuildSettingNames.DanceBattleStartTime, startTime.ToString());
        }

        public DateTime GetDanceBattleStartTime(ulong guildId)
        {
            var setting = GetSetting(guildId, GuildSettingNames.DanceBattleStartTime);
            if (DateTime.TryParse(setting, out var value))
                return value;
            return DateTime.UtcNow.AddHours(12);
        }

        #region Private Methods

        private async Task SetSetting(ulong guildId, string key, string value)
        {
            var setting = _context.GuildSettings.FirstOrDefault(s => s.GuildId == guildId && s.SettingKey.Equals(key));
            if (setting == null)
            {
                setting = new GuildSetting()
                {
                    GuildId = guildId,
                    SettingKey = key,
                    SettingValue = value
                };
                _context.Add(setting);
            }
            else
            {
                setting.SettingValue = value;
                _context.GuildSettings.Update(setting);
            }
            await _context.SaveChangesAsync();
        }

        private string? GetSetting(ulong guildId, string key)
        {
            var setting = _context.GuildSettings.FirstOrDefault(s => s.GuildId == guildId && s.SettingKey == key);
            return setting?.SettingValue;
        }

        private async Task RemoveSetting(ulong guildId, string key)
        {
            var setting = _context.GuildSettings.FirstOrDefault(s => s.GuildId == guildId && s.SettingKey.Equals(key));
            if (setting != null)
            {
                _context.GuildSettings.Remove(setting);
                await _context.SaveChangesAsync();
            }
        }

        #endregion
    }
}
