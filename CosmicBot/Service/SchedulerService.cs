using CosmicBot.DAL;
using CosmicBot.Helpers;
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
            Console.WriteLine($"Scheduler service started");
            while (!cancellationToken.IsCancellationRequested)
            {
                using (var scope = _scopeFactory.CreateScope())
                {
                    var context = scope.ServiceProvider.GetRequiredService<DataContext>();
                    var discordClient = scope.ServiceProvider.GetRequiredService<DiscordSocketClient>();
                    await HandleMinecraftScheduledTasks(context, cancellationToken);
                    await HandleRedditPostTasks(context, discordClient, cancellationToken);
                }
                    Thread.Sleep(TimeSpan.FromMinutes(1));
            }
        }

        private async Task HandleRedditPostTasks(DataContext context, DiscordSocketClient discordClient, CancellationToken cancellationToken)
        {
            var posts = await context.RedditAutoPosts.AsNoTracking().ToListAsync(cancellationToken);

            foreach (var post in posts)
            {
                var guildTime = GetGuildTime(context, post.GuildId);
                if (post.LastRan == null)
                {
                    post.LastRan = GetLastRanIfNull(guildTime, post.StartTime, post.Interval);
                    context.RedditAutoPosts.Update(post);
                    await context.SaveChangesAsync(cancellationToken);
                }

                if (CheckTask(guildTime, post.LastRan, post.StartTime, post.Interval))
                {
                    Console.WriteLine($"Running scheduled task: Reddit auto post {post.Subreddit} now at {guildTime}");
                    await RedditApiService.PostTop(post.Subreddit, post.ChannelId, discordClient);

                    post.LastRan = guildTime;
                    context.Update(post);
                    await context.SaveChangesAsync(cancellationToken);
                }

                if (cancellationToken.IsCancellationRequested)
                    break;
            }
        }

        private async Task HandleMinecraftScheduledTasks(DataContext context, CancellationToken cancellationToken)
        {
            var tasks = await context.MinecraftScheduledTasks.AsNoTracking().ToListAsync(cancellationToken);

            foreach (var task in tasks)
            {
                var server = context.MinecraftServers.FirstOrDefault(s => s.ServerId == task.ServerId);

                if (server == null)
                    continue;

                var guildTime = GetGuildTime(context, server.GuildId);
                if (task.LastRan == null)
                {
                    task.LastRan = GetLastRanIfNull(guildTime, task.StartTime, task.Interval);
                    context.MinecraftScheduledTasks.Update(task);
                    await context.SaveChangesAsync(cancellationToken);
                }
                
                if (CheckTask(guildTime, task.LastRan, task.StartTime, task.Interval))
                {
                    Console.WriteLine($"Running scheduled task: {task.Name} now at {guildTime}");
                    var commands = await context.MinecraftCommands.Where(c => c.ScheduledTaskId == task.ScheduledTaskId).ToListAsync();

                    if (commands.Count > 0)
                    {
                        var service = new MinecraftServerService(server);
                        await service.SendCommands(commands);
                    }

                    task.LastRan = guildTime;
                    context.Update(task);
                    await context.SaveChangesAsync(cancellationToken);
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

        private static DateTime GetGuildTime(DataContext context, ulong guildId)
        {
            var timeZoneSetting = context.GuildSettings.FirstOrDefault(s => s.GuildId == guildId && s.SettingKey == "Timezone");
            if (timeZoneSetting != null)
                return TimeZoneHelper.GetGuildTimeNow(timeZoneSetting.SettingValue);
            return DateTime.UtcNow;
                
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            Console.WriteLine($"Scheduler service stopped");
            return Task.CompletedTask;
        }
    }
}
