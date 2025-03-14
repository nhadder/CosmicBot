using CosmicBot.DAL;
using CosmicBot.Models;
using CosmicBot.DiscordResponse;
using Microsoft.EntityFrameworkCore;
using Discord;
using System.Text;

namespace CosmicBot.Service
{
    public class PlayerService
    {
        private readonly DataContext _context;
        private readonly GuildSettingsService _guildSettings;

        public PlayerService(DataContext context, GuildSettingsService guildSettings)
        {
            _context = context;
            _guildSettings = guildSettings;
        }

        public async Task<PlayerStats> GetPlayerStatsAsync(ulong guildId, ulong userId)
        {
            var playerStats = _context.PlayerStats.FirstOrDefault(p => p.GuildId == guildId && p.UserId == userId);
            if (playerStats is null)
            {
                playerStats = new PlayerStats { UserId = userId, GuildId = guildId };
                await _context.PlayerStats.AddAsync(playerStats);
                await _context.SaveChangesAsync();
            }
            return playerStats;
        }

        public async Task<MessageResponse> Daily(ulong guildId, ulong userId)
        {
            var player = await GetPlayerStatsAsync(guildId, userId);
            var guildTimeNow = _guildSettings.GetGuildTime(guildId);

            if (player.LastDaily != null && guildTimeNow - player.LastDaily < TimeSpan.FromDays(1))
            {
                var timeLeft = guildTimeNow - (DateTime)player.LastDaily;
                var sb = new StringBuilder();
                if(timeLeft.Minutes > 0)
                    sb.Append($"{timeLeft.Minutes} minutes ");
                sb.Append($"{timeLeft.Seconds} seconds left.");
                return new MessageResponse($"You have already claimed your daily reward today! {sb}", ephemeral: true);
            }

            var rng = new Random();
            var pointsEarned = Convert.ToInt64(Math.Floor((rng.NextDouble()*50 + 50) * player.Level));
            var experienceEarned = 10 * player.Level;

            player.Points += pointsEarned;
            player.Experience += experienceEarned;

            var oldLevel = player.Level;
            var newLevel = GetLevelFromXp(player.Experience);
            var leveledUp = newLevel > oldLevel ? $"\nYou leveled up! You are now level **{newLevel}**" : string.Empty;

            player.Level = newLevel;

            player.LastDaily = guildTimeNow;
            _context.Update(player);
            await _context.SaveChangesAsync();

            return new MessageResponse($"You gained **{pointsEarned}** stars and gained **{experienceEarned}** xp!{leveledUp}");
        }

        public async Task<PagedListEmbed> Leaderboard(ulong guildId, IInteractionContext interactionContext)
        {
            var playersInGuild = (await _context.PlayerStats.Where(p => p.GuildId == guildId).ToListAsync())
                .OrderByDescending(l => l.Points).ToList();
            var playerStats = new List<string>();
            for (var i = 0; i < playersInGuild.Count; i++)
            {
                var player = playersInGuild[i];
                var user = await interactionContext.Client.GetUserAsync(player.UserId);
                var name = user.GlobalName.Length > 15 ? user.GlobalName.Substring(0, 12) + "..." : user.GlobalName;
                var stars = $"{player.Points} stars";
                stars = stars.PadRight(20);
                playerStats.Add($"{i+1}. {stars} {name}");
            }
            return new PagedListEmbed(playerStats, 10, "Leaderboard");
        }

        public async Task<MessageResponse> StatCard(ulong guildId, IUser user)
        {
            var playerStats = await GetPlayerStatsAsync(guildId, user.Id);
            var playerCard = new EmbedBuilder()
                .WithThumbnailUrl(user.GetDisplayAvatarUrl())
                .WithDescription($"**Level:** {playerStats.Level}\n**XP:** {playerStats.Experience}/{GetXpForLevel(playerStats.Level)}\n**Stars:** {playerStats.Points}\n**Games Won:** {playerStats.GamesWon}\n**Games Lost:** {playerStats.GamesLost}")
                .WithAuthor(new EmbedAuthorBuilder().WithName(user.GlobalName).WithIconUrl(user.GetAvatarUrl()))
                .Build();

            return new MessageResponse(embed: playerCard);
        }

        public async Task Award(ulong guildId, ulong userId, long points = 0, long xp = 0, int gamesWon = 0, int gamesLost = 0)
        {
            var player = await GetPlayerStatsAsync(guildId, userId);
            if (player != null)
            {
                player.Points += points;
                player.Experience += xp;
                player.GamesWon += gamesWon;
                player.GamesLost += gamesLost;

                player.Level = GetLevelFromXp(player.Experience);

                if (player.Points < 0)
                    player.Points = 0;

                _context.Update(player);
                await _context.SaveChangesAsync();
            }
        }

        private static int GetLevelFromXp(long experience)
        {
            int level = 1;
            int threshold = 20;

            while (experience >= threshold)
            {
                level++;
                threshold *= 2;
                experience -= threshold / 2;
            }

            return level;
        }

        private static int GetXpForLevel(int level)
        {
            if (level < 1) return 0;

            int xpRequired = 0;
            int threshold = 20;

            for (int currentLevel = 1; currentLevel <= level; currentLevel++)
            {
                xpRequired += threshold;
                threshold *= 2;
            }

            return xpRequired;
        }
    }
}
