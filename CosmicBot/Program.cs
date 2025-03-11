using CosmicBot.BotCommands;
using CosmicBot.DAL;
using CosmicBot.Service;
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
                    .AddSingleton(_socketConfig)
                    .AddSingleton(_interactionServiceConfig)
                    .AddSingleton<DiscordSocketClient>()
                    .AddSingleton(x => new InteractionService(x.GetRequiredService<DiscordSocketClient>(), _interactionServiceConfig))
                    .AddSingleton<DiscordBotService>()
                    .AddSingleton<MinecraftCommandModule>()
                    .AddSingleton<RedditCommandModule>()
                    .AddSingleton<SettingsCommandModule>()
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
                    Console.WriteLine($"An error occurred while migrating the database.\n{ex.Message}\n{ex.StackTrace}");
                }
            }

            var client = host.Services.GetRequiredService<DiscordSocketClient>();
            client.Log += LogAsync;

            var discordBotService = host.Services.GetRequiredService<DiscordBotService>();
            await discordBotService.InitializeAsync();

            var configuration = host.Services.GetRequiredService<IConfiguration>();

            await client.LoginAsync(TokenType.Bot, configuration["DISCORD_BOT_TOKEN"]);
            await client.StartAsync();

            if (Environment.GetCommandLineArgs().Any(arg => arg.Contains("ef")))
                return;

            host.Run();
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

        private static Task LogAsync(LogMessage message)
        {
            Console.WriteLine(message.ToString());
            return Task.CompletedTask;
        }
    }
}