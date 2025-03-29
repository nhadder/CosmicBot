using CosmicBot.Commands;
using CosmicBot.DAL;
using CosmicBot.Helpers;
using CosmicBot.Service;
using CosmicBot.Services;
using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace CosmicBot
{
    public static class Program
    {      
        public static async Task Main(string[] args)
        {
            var host = Host.CreateDefaultBuilder()
                .ConfigureLogging((context, logging) =>
                {
                    logging.ClearProviders();
                    logging.AddConsole();
                    logging.AddDebug();

                    logging.SetMinimumLevel(LogLevel.Information);
                    logging.AddFilter("Microsoft.EntityFrameworkCore", LogLevel.Warning);
                })
                .ConfigureAppConfiguration((context, config) =>
                {
                    config.AddJsonFile("appsettings.json", optional: true)
                        .AddEnvironmentVariables();
                })
                .ConfigureServices((context, services) =>
                {
                    services.AddDbContext<DataContext>(options =>
                    {
                        var connectionString = new SqlConnectionStringBuilder()
                        {
                            DataSource = context.Configuration["SQL_SERVER"],
                            UserID = context.Configuration["SQL_USER"],
                            Password = context.Configuration["SQL_PASS"],
                            InitialCatalog = context.Configuration["SQL_DB"],
                            TrustServerCertificate = true
                        }.ToString();

                        options.UseSqlServer(connectionString);
                    })
                    //Discord Services
                    .AddSingleton(_socketConfig)
                    .AddSingleton<DiscordSocketClient>()
                    .AddSingleton( sp =>
                    {
                        var scopeFactory = sp.GetRequiredService<IServiceScopeFactory>();
                        var scope = scopeFactory.CreateScope();
                        var socketClient = sp.GetRequiredService<DiscordSocketClient>();
                        var interactionService = new InteractionService(socketClient, _interactionServiceConfig);
                        return new DiscordBotService(socketClient,
                            interactionService,
                            scope.ServiceProvider,
                            scope.ServiceProvider.GetRequiredService<IConfiguration>());
                    })
                    // Domain Services
                    .AddScoped<RedditService>()
                    .AddScoped<MinecraftServerService>()
                    .AddScoped<GuildSettingsService>()
                    .AddScoped<PlayerService>()
                    .AddScoped<PetService>()
                    //Command Modules
                    .AddScoped<MinecraftListCommandModule>()
                    .AddScoped<MinecraftServerCommandModule>()
                    .AddScoped<MinecraftTaskCommandModule>()
                    .AddScoped<MinecraftWhitelistCommandModule>()
                    .AddScoped<RedditCommandModule>()
                    .AddScoped<RedditAutopostCommandModule>()
                    .AddScoped<PlayerGameCommandModule>()
                    .AddScoped<SettingsCommandModule>()
                    .AddScoped<EmbedMessageInteractionModule>()
                    .AddScoped<GifCommandModule>()
                    //Hosted Services
                    .AddHostedService<SchedulerService>();
                })
                .Build();

            using (var scope = host.Services.CreateScope())
            {
                var services = scope.ServiceProvider;
                try
                {
                    var dbContext = services.GetRequiredService<DataContext>();
                    dbContext.Database.Migrate();
                }
                catch (Exception ex)
                {
                    Logger.Log($"An error occurred while migrating the database.\n{ex.Message}\n{ex.StackTrace}");
                    return;
                }
            }

            var discordBotService = host.Services.GetRequiredService<DiscordBotService>();
            await discordBotService.InitializeAsync();

            if (Environment.GetCommandLineArgs().Any(arg => arg.Contains("ef")))
                return;

            var discordClient = host.Services.GetRequiredService<DiscordSocketClient>();
            Console.CancelKeyPress += async (sender, e) =>
            {
                e.Cancel = true;
                await ApplicationClosing(discordClient);
                Environment.Exit(0);
            };

            host.Run();
        }

        private async static Task ApplicationClosing(IDiscordClient client)
        {
            Logger.Log("Exit command initiated. Expiring all my known messages");
            await MessageStore.ExpireAllMessages(client);
        }

        private static readonly DiscordSocketConfig _socketConfig = new()
        {
            GatewayIntents = GatewayIntents.GuildMembers
            | GatewayIntents.GuildMessages
            | GatewayIntents.GuildMessageReactions
            | GatewayIntents.GuildEmojis
            | GatewayIntents.Guilds,
            AlwaysDownloadUsers = true,
        };

        private static readonly InteractionServiceConfig _interactionServiceConfig = new();
    }
}