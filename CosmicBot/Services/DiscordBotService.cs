using CosmicBot.Helpers;
using CosmicBot.Services;
using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

namespace CosmicBot.Service
{
    public class DiscordBotService
    {
        private readonly DiscordSocketClient _client;
        private readonly InteractionService _handler;
        private readonly IServiceProvider _services;
        private readonly IConfiguration _configuration;

        public DiscordBotService(DiscordSocketClient client, 
            InteractionService handler,
            IServiceProvider services, 
            IConfiguration configuration)
        {
            _client = client;
            _handler = handler;
            _services = services;
            _configuration = configuration;
        }

        public async Task InitializeAsync()
        {
            await _handler.AddModulesAsync(Assembly.GetEntryAssembly(), _services);
            _handler.InteractionExecuted += HandleInteractionExecute;
            _handler.Log += Logger.LogAsync;

            _client.Log += Logger.LogAsync;
            _client.Ready += ReadyAsync;
            _client.InteractionCreated += HandleInteraction;
            _client.UserVoiceStateUpdated += HandleVoiceStateUpdated;
            _client.UserLeft += HandleUserLeft;
            _client.MessageReceived += HandleMessageReceived;

            await _client.LoginAsync(TokenType.Bot, _configuration["DISCORD_BOT_TOKEN"]);
            await _client.StartAsync();
        }

        public async Task ReadyAsync()
        {
            await _handler.RegisterCommandsGloballyAsync();        
        }

        private async Task HandleInteraction(SocketInteraction interaction)
        {
            try
            {
                var context = new SocketInteractionContext(_client, interaction);
                var result = await _handler.ExecuteCommandAsync(context, _services);

                if (!result.IsSuccess)
                    switch (result.Error)
                    {
                        case InteractionCommandError.UnmetPrecondition:
                            Logger.Log($"Unmet Precondition {result.ErrorReason}");
                            break;
                        default:
                            Logger.Log($"Other error: {result.ErrorReason}");
                            break;
                    }
            }
            catch(Exception ex)
            {
                Logger.Log($"Exception occured: {ex.Message}\n{ex.StackTrace}");
                if (interaction.Type is InteractionType.ApplicationCommand)
                    await interaction.GetOriginalResponseAsync().ContinueWith(async (msg) => await msg.Result.DeleteAsync());
            }
        }

        private async Task HandleInteractionExecute(ICommandInfo commandInfo, IInteractionContext context, IResult result)
        {
            if (!result.IsSuccess)
                switch (result.Error)
                {
                    case InteractionCommandError.UnmetPrecondition:
                        await Logger.LogAsync($"Unmet Precondition {result.ErrorReason}");
                        break;
                    default:
                        await Logger.LogAsync($"Unknown error: {result.ErrorReason}");
                        break;
                }
        }

        private async Task HandleVoiceStateUpdated(SocketUser user, SocketVoiceState before, SocketVoiceState after)
        {
            await ChannelStore.CheckForExpiredMessages(_client);
            if (after.VoiceChannel != null)
            {
                var guildSettings = _services.GetRequiredService<GuildSettingsService>();
                await ChannelStore.HandleUserJoinedVoiceChannel(_client, guildSettings, user, after.VoiceChannel);
            }
            else if (before.VoiceChannel != null && after.VoiceChannel == null)
            {
                await ChannelStore.CheckForExpiredMessages(_client);
            }
        }

        private async Task HandleUserLeft(SocketGuild guild, SocketUser user)
        {
            var guildSettings = _services.GetRequiredService<GuildSettingsService>();
            var setting = guildSettings.GetModBotChannel(guild.Id);
            if(setting != null)
            {
                var channel = await _client.GetChannelAsync((ulong)setting) as ITextChannel;
                if (channel != null)
                {
                    var builder = new EmbedBuilder()
                        .WithAuthor(new EmbedAuthorBuilder()
                            .WithName($"{user.GlobalName}")
                            .WithIconUrl(user.GetAvatarUrl()))
                        .WithDescription($"<@{user.Id}> (username: {user.Username}) has left!\n{guild.MemberCount} members remain...")
                        .WithImageUrl(user.GetAvatarUrl());

                    await channel.SendMessageAsync(embed: builder.Build());
                }
                else
                    await guildSettings.RemoveModBotChannel(guild.Id);
            }
        }

        private async Task HandleMessageReceived(SocketMessage message)
        {
            if (message is not SocketUserMessage userMessage) return;
            if (message.Channel is not SocketGuildChannel channel) return;
            if (userMessage.Author.IsBot) return;

            var guildId = channel.Guild.Id;
            var guildSettings = _services.GetRequiredService<GuildSettingsService>();
            var countingChannelId = guildSettings.GetCountingChannel(guildId);
            if (countingChannelId == null) return;
            if (userMessage.Channel.Id != countingChannelId) return;

            var countingService = _services.GetRequiredService<CountingService>();
            await countingService.TryAddCount(guildId, (ulong)countingChannelId, userMessage);
        }
    }
}
