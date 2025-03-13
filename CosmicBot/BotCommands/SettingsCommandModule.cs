using CosmicBot.Helpers;
using CosmicBot.Service;
using Discord;
using Discord.Interactions;

namespace CosmicBot.BotCommands
{
    [RequireUserPermission(GuildPermission.Administrator)]
    [Group("settings", "Guild Setting Commands")]
    public class SettingsCommandModule : CommandModule
    {
        private readonly GuildSettingsService _service;
        public SettingsCommandModule(GuildSettingsService service)
        {
            _service = service;
        }

        [SlashCommand("timezone", "Choose a timezone for scheduled tasks to go off of (Default UTC +0)")]
        public async Task SetTimezone(TimeZoneEnum timezone)
        {
            await Respond(await _service.SetTimezone(Context.Guild.Id, timezone));
        }
    }
}
