using CosmicBot.DAL;
using CosmicBot.Helpers;
using CosmicBot.Models;
using Discord.Interactions;
using Discord.WebSocket;

namespace CosmicBot.BotCommands
{
    public class SettingsCommandModule : InteractionModuleBase<SocketInteractionContext>
    {
        private readonly DataContext _context;
        public SettingsCommandModule(DataContext context)
        {
            _context = context;
        }

        #region Reddit Server Commands

        [SlashCommand("settimezone", "Choose a timezone for scheduled tasks to go off of (Default UTC +0)")]
        public async Task SetTimezone(TimeZoneEnum timezone)
        {
            if (!UserIsMod(Context.User))
            {
                await RespondAsync("You don't have permission to use this command.", ephemeral: true);
                return;
            }

            var timezoneStr = TimeZoneHelper.GetTimeZoneId(timezone);
            await CreateOrUpdateSetting("Timezone", timezoneStr);          
            
            await RespondAsync($"Timezone updated to {timezoneStr}", ephemeral: true);
        }

        #endregion

        private static bool UserIsMod(SocketUser user)
        {
            var guildUser = user as SocketGuildUser;
            return guildUser?.GuildPermissions.Administrator ?? false;
        }

        private async Task CreateOrUpdateSetting(string key, string value)
        {
            var guildId = Context.Guild.Id;
            var setting = _context.GuildSettings.FirstOrDefault(s => s.GuildId == guildId &&  s.SettingKey.Equals(key));
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

        private static Guid StrToGuid(string? str)
        {
            if (str == null)
                return Guid.Empty;

            return Guid.TryParse(str, out Guid result) ? result : Guid.Empty;
        }
    }
}
