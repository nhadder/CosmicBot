using CosmicBot.DAL;
using CosmicBot.Helpers;
using CosmicBot.Messages.Components;
using Discord;
using Discord.WebSocket;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace CosmicBot.Service
{
    public class SchedulerService : IHostedService
    {
        private readonly IServiceScopeFactory _scopeFactory;

        public SchedulerService(IServiceScopeFactory scopeFactory)
        {
            _scopeFactory = scopeFactory;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            Logger.Log("Scheduler service started");

            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    using (var scope = _scopeFactory.CreateScope())
                    {
                        var context = scope.ServiceProvider.GetRequiredService<DataContext>();
                        var socketClient = scope.ServiceProvider.GetRequiredService<DiscordSocketClient>();

                        var minecraftService = new MinecraftServerService(context);
                        var redditService = new RedditService(context);
                        var guildSettingsService = new GuildSettingsService(context);
                        var playerService = new PlayerService(context, guildSettingsService);

                        await HandleMinecraftScheduledTasks(minecraftService, guildSettingsService, cancellationToken);
                        await HandleRedditPostTasks(redditService, guildSettingsService, socketClient, cancellationToken);
                        await BotChannelGifts(guildSettingsService, socketClient);
                        await DanceBattle(guildSettingsService, socketClient, playerService, context);
                        await MessageStore.CheckForExpiredMessages(socketClient);
                        await ChannelStore.CheckForExpiredMessages(socketClient);
                    }
                    Thread.Sleep(TimeSpan.FromMinutes(1));
                }
                catch (Exception ex)
                {
                    Logger.Log($"{ex.Message}: {ex.StackTrace}");
                    while(ex.InnerException != null)
                    {
                        ex = ex.InnerException;
                        Logger.Log($"{ex.Message}: {ex.StackTrace}");
                    }
                }
            }
        }

        private static async Task BotChannelGifts(GuildSettingsService settings, DiscordSocketClient socketClient)
        {
            if (DateTime.UtcNow.Minute == 0 || DateTime.UtcNow.Minute == 30)
            {
                var guilds = socketClient.Guilds;
                foreach (var guild in guilds)
                {
                    var botChannels = settings.GetBotChannels(guild.Id);
                    var activeChests = MessageStore.GetMessagesOfType(typeof(Chest)).Count;
                    if (botChannels != null && botChannels.Any() && activeChests < botChannels.Count)
                    {
                        var rng = new Random();
                        var chosen = botChannels.OrderBy(_ => rng.Next()).First();
                        var channel = guild.Channels.FirstOrDefault(c => c.Id == chosen) as IMessageChannel;
                        if (channel != null)
                        {
                            await new Chest().SendAsync(socketClient, channel);
                        }
                    }
                }
            }
        }

        private static async Task DanceBattle(GuildSettingsService settings, DiscordSocketClient socketClient, PlayerService playerService, DataContext context)
        {
            var guilds = socketClient.Guilds;
            foreach(var guild in guilds)
            {
                var channelId = settings.GetDanceBattleChannel(guild.Id);
                if(channelId != null)
                {
                    var channel = guild.Channels.FirstOrDefault(c => c.Id == channelId) as IMessageChannel;
                    if (channel == null)
                        continue;

                    var messageId = settings.GetDanceBattleMessageId(guild.Id);
                    if(messageId == null)
                    {
                        var startTime = DateTime.UtcNow.AddHours(12);
                        await settings.SetDanceBattleStartTime(guild.Id, startTime);
                        var danceBattle = await new DanceOff(null, startTime).SendAsync(socketClient, channel);
                        await settings.SetDanceBattleMessageId(guild.Id, danceBattle);
                    }
                    else
                    {
                        var previousMembers = await context.DanceBattleMembers.Where(d => d.GuildId == guild.Id).ToListAsync();
                        if (!MessageStore.MessageExists((ulong)messageId))
                        {
                            var startTime = settings.GetDanceBattleStartTime(guild.Id);
                            var newGame = await new DanceOff(previousMembers, startTime).SendAsync(socketClient, channel);
                            await settings.SetDanceBattleMessageId(guild.Id, newGame);
                        }
                        else
                        {
                            var message = MessageStore.GetMessage((ulong)messageId) as DanceOff;
                            if (message != null)
                            {
                                await message.Next(channel);
                                if (message.Status == Models.Enums.GameStatus.Won)
                                {
                                    if (previousMembers.Count > 0)
                                    {
                                        context.DanceBattleMembers.RemoveRange(previousMembers);
                                        await context.SaveChangesAsync();
                                    }

                                    await settings.RemoveDanceBattleMessageId(guild.Id);

                                    if (message.Awards.Count > 0)
                                        foreach (var award in message.Awards)
                                            await playerService.Award(guild.Id, award.UserId, award.Points, award.Experience, award.GamesWon, award.GamesLost);
                                }
                                else if (message.Status == Models.Enums.GameStatus.Pending)
                                {
                                    var newMembers = message.Dancers.Where(d => !previousMembers.Any(m => m.UserId == d.UserId));
                                    await context.DanceBattleMembers.AddRangeAsync(newMembers);
                                    await context.SaveChangesAsync();
                                }
                                await message.UpdateAsync(socketClient);
                            }
                        }
                    }
                }
            }
        }

        private static async Task HandleRedditPostTasks(RedditService redditService, GuildSettingsService guildSettings, DiscordSocketClient socketClient, CancellationToken cancellationToken)
        {
            var posts = await redditService.GetAllAutoPosts();

            foreach (var post in posts)
            {
                var guildTime = guildSettings.GetGuildTime(post.GuildId);
                if (post.LastRan == null)
                {
                    post.LastRan = GetLastRanIfNull(guildTime, post.StartTime, post.Interval);
                    await redditService.UpdateAutoPost(post);
                }

                if (CheckTask(guildTime, post.LastRan, post.StartTime, post.Interval))
                {
                    Logger.Log($"Running scheduled task: Reddit auto post {post.Subreddit} now at {guildTime}");

                    try
                    {
                        var response = await redditService.Post(post.Subreddit);
                        var channel = socketClient.GetChannel(post.ChannelId) as IMessageChannel;

                        if (channel != null)
                            await channel.SendMessageAsync(response.Text, embed: response.Embed);
                    }
                    catch(Exception ex)
                    {
                        Logger.Log($"Exception occured: {ex.Message}\n{ex.StackTrace}");
                    }

                    post.LastRan = guildTime;
                    await redditService.UpdateAutoPost(post);
                }

                if (cancellationToken.IsCancellationRequested)
                    break;
            }
        }

        private static async Task HandleMinecraftScheduledTasks(MinecraftServerService minecraftService, GuildSettingsService guildSettings, CancellationToken cancellationToken)
        {
            var tasks = await minecraftService.GetAllTasks();

            foreach (var task in tasks)
            {
                var server = await minecraftService.GetServerById(task.ServerId);

                if (server == null)
                    continue;

                var guildTime = guildSettings.GetGuildTime(server.GuildId);
                if (task.LastRan == null)
                {
                    task.LastRan = GetLastRanIfNull(guildTime, task.StartTime, task.Interval);
                    await minecraftService.UpdateTask(task);
                }
                
                if (CheckTask(guildTime, task.LastRan, task.StartTime, task.Interval))
                {
                    Logger.Log($"Running scheduled task: {task.Name} now at {guildTime}");

                    try
                    {
                        var commands = await minecraftService.GetCommands(task.ScheduledTaskId);
                        if (commands.Count > 0)
                            await minecraftService.SendCommands(server.ServerId, commands);
                    }
                    catch(Exception ex)
                    {
                        Logger.Log($"Exception occured: {ex.Message}\n{ex.StackTrace}");
                    }

                    task.LastRan = guildTime;
                    await minecraftService.UpdateTask(task);
                }

                if (cancellationToken.IsCancellationRequested)
                    break;
            }
        }

        private static DateTime GetLastRanIfNull(DateTime guildTime, TimeOnly start, TimeSpan interval)
        {
            DateTime anchor = new DateTime(guildTime.Year, guildTime.Month, guildTime.Day, start.Hour, start.Minute, start.Second);
            if (guildTime < anchor)
                anchor = anchor.AddDays(-1);
            var elapsed = guildTime - anchor;
            var intervalsPassed = (int)Math.Floor(elapsed.TotalSeconds / interval.TotalSeconds);
            var nextScheduled = anchor.AddTicks(interval.Ticks * (intervalsPassed + 1));
            return nextScheduled - interval;
        }

        private static bool CheckTask(DateTime guildTime, DateTime? lastRan, TimeOnly start, TimeSpan interval)
        {
            DateTime anchor = new DateTime(guildTime.Year, guildTime.Month, guildTime.Day, start.Hour, start.Minute, start.Second);
            if (guildTime < anchor)
                anchor = anchor.AddDays(-1);

            DateTime nextScheduled;
            if (lastRan.HasValue)
                nextScheduled = lastRan.Value.Add(interval);
            else
            {
                var elapsed = guildTime - anchor;
                var intervalsPassed = (int)Math.Floor(elapsed.TotalSeconds / interval.TotalSeconds);
                nextScheduled = anchor.AddTicks(interval.Ticks * (intervalsPassed + 1));
            }

            return guildTime >= nextScheduled;
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            await Logger.LogAsync("Scheduler service stopped");
        }
    }
}
