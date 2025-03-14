using CosmicBot.Helpers;
using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using Microsoft.Extensions.Configuration;
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
    }
}
