using CosmicBot.BotCommands;
using Discord;
using Discord.Interactions;
using Discord.WebSocket;

namespace CosmicBot.Service
{
    public class DiscordBotService
    {
        private readonly DiscordSocketClient _client;
        private readonly InteractionService _handler;
        private readonly IServiceProvider _services;

        public DiscordBotService(DiscordSocketClient client, InteractionService handler, IServiceProvider services)
        {
            _client = client;
            _handler = handler;
            _services = services;
        }

        public async Task InitializeAsync()
        {
            await _handler.AddModuleAsync<MinecraftCommandModule>(_services);
            await _handler.AddModuleAsync<RedditCommandModule>(_services);
            await _handler.AddModuleAsync<SettingsCommandModule>(_services);
            _client.Ready += ReadyAsync;
            _client.InteractionCreated += HandleInteraction;
            _handler.InteractionExecuted += HandleInteractionExecute;
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
                            Console.WriteLine($"Unmet Precondition {result.ErrorReason}");
                            break;
                        default:
                            Console.WriteLine($"Other error: {result.ErrorReason}");
                            break;
                    }
            }
            catch(Exception ex)
            {
                Console.WriteLine($"Exception occured: {ex.Message}\n{ex.StackTrace}");
                if (interaction.Type is InteractionType.ApplicationCommand)
                    await interaction.GetOriginalResponseAsync().ContinueWith(async (msg) => await msg.Result.DeleteAsync());
            }
        }

        private Task HandleInteractionExecute(ICommandInfo commandInfo, IInteractionContext context, IResult result)
        {
            if (!result.IsSuccess)
                switch (result.Error)
                {
                    case InteractionCommandError.UnmetPrecondition:
                        Console.WriteLine($"Unmet Precondition {result.ErrorReason}");
                        break;
                    default:
                        Console.WriteLine($"Unknown error: {result.ErrorReason}");
                        break;
                }

            return Task.CompletedTask;
        }
    }
}
