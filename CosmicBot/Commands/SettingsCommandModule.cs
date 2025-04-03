using CosmicBot.DiscordResponse;
using CosmicBot.Helpers;
using CosmicBot.Messages.Components;
using CosmicBot.Service;
using Discord;
using Discord.Interactions;

namespace CosmicBot.Commands
{
    [DefaultMemberPermissions(GuildPermission.Administrator)]
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
            if (!HasChannelPermissions())
            {
                await Respond(new MessageResponse("I don't have valid permissions in this channel", ephemeral: true));
                return;
            }

            await Respond(await _service.SetTimezone(Context.Guild.Id, timezone));
        }

        [Group("botchannel", "Designate Bot Channels")]
        public class BotChannelSettings : CommandModule
        {
            public readonly GuildSettingsService _service;

            public BotChannelSettings(GuildSettingsService service)
            {
                _service = service;
            }

            [SlashCommand("add", "Add a bot channel")]
            public async Task AddBotChannel(IChannel channel)
            {
                if (!HasChannelPermissions())
                {
                    await Respond(new MessageResponse("I don't have valid permissions in this channel", ephemeral: true));
                    return;
                }

                await Respond(await _service.SetBotChannel(Context.Guild.Id, channel.Id));
            }

            [SlashCommand("remove", "Remove a bot channel")]
            public async Task RemoveBotChannel(IChannel channel)
            {
                if (!HasChannelPermissions())
                {
                    await Respond(new MessageResponse("I don't have valid permissions in this channel", ephemeral: true));
                    return;
                }
                await _service.RemoveBotChannel(Context.Guild.Id, channel.Id);

                await Respond(new MessageResponse("Bot channel successfully removed", ephemeral: true));
            }

        }

        [Group("studiospawner", "Designate Studio Spawner")]
        public class CreatePrivateChannelSettings : CommandModule
        {
            public readonly GuildSettingsService _service;

            public CreatePrivateChannelSettings(GuildSettingsService service)
            {
                _service = service;
            }

            [SlashCommand("add", "Add a voice channel creator")]
            public async Task AddVoiceChannelSpawner(IVoiceChannel channel)
            {
                if (!HasChannelPermissions())
                {
                    await Respond(new MessageResponse("I don't have valid permissions in this channel", ephemeral: true));
                    return;
                }
                await _service.SetCreatePrivateVoiceChannelId(Context.Guild.Id, channel.Id);
                await Respond(new MessageResponse("Voice Channel Spawner Created", ephemeral: true));
            }

            [SlashCommand("remove", "Remove a voice channel creator")]
            public async Task RemoveVoiceChannelSpawner()
            {
                if (!HasChannelPermissions())
                {
                    await Respond(new MessageResponse("I don't have valid permissions in this channel", ephemeral: true));
                    return;
                }
                await _service.RemoveCreatePrivateVoiceChannelId(Context.Guild.Id);
                await Respond(new MessageResponse("Voice Channel Spawner successfully removed", ephemeral: true));
            }
        }

        [Group("logchannel", "Designate Channel for announcing member departures")]
        public class ModBotChannelSettings : CommandModule
        {
            public readonly GuildSettingsService _service;

            public ModBotChannelSettings(GuildSettingsService service)
            {
                _service = service;
            }

            [SlashCommand("set", "Add a mod bot channel")]
            public async Task AddModBotChannel(ITextChannel channel)
            {
                if (!HasChannelPermissions())
                {
                    await Respond(new MessageResponse("I don't have valid permissions in this channel", ephemeral: true));
                    return;
                }
                await _service.SetModBotChannel(Context.Guild.Id, channel.Id);
                await Respond(new MessageResponse("Log channel specified", ephemeral: true));
            }

            [SlashCommand("remove", "stop announcing member departures")]
            public async Task RemoveModBotChannel()
            {
                if (!HasChannelPermissions())
                {
                    await Respond(new MessageResponse("I don't have valid permissions in this channel", ephemeral: true));
                    return;
                }
                await _service.RemoveModBotChannel(Context.Guild.Id);
                await Respond(new MessageResponse("Log channel disconnected successfully", ephemeral: true));
            }
        }

        [Group("dancebattle","Dance battle settings")]
        public class DanceBattleSettings : CommandModule
        {
            public readonly GuildSettingsService _service;

            public DanceBattleSettings(GuildSettingsService service)
            {
                _service= service;
            }

            [SlashCommand("on", "Choose a channel to hold daily dance battles in")]
            public async Task SetDanceBattleChannel(IMessageChannel channel)
            {
                if (!HasChannelPermissions())
                {
                    await Respond(new MessageResponse("I don't have valid permissions in this channel", ephemeral: true));
                    return;
                }

                await Respond(await _service.SetDanceBattleChannel(Context.Guild.Id, channel.Id));
            }

            [SlashCommand("off", "Stop dance battles from automatically happening")]
            public async Task SetDanceBattleChannel()
            {
                if (!HasChannelPermissions())
                {
                    await Respond(new MessageResponse("I don't have valid permissions in this channel", ephemeral: true));
                    return;
                }

                await _service.RemoveDanceBattleSetting(Context.Guild.Id);
                await Respond(new MessageResponse("Successful", ephemeral: true));
            }

            [SlashCommand("start", "Start a dance battle immediately")]
            public async Task StartDanceBattle()
            {
                if (!HasChannelPermissions())
                {
                    await Respond(new MessageResponse("I don't have valid permissions in this channel", ephemeral: true));
                    return;
                }

                await new DanceOff(null, null).SendAsync(Context);
            }
        }
    }
}
