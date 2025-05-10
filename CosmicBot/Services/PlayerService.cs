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

        public async Task<MessageResponse> SetBirthday(ulong guildId, ulong userId, int month, int day, int year, IGuildUser? user)
        {
            if (year < 1900 || year > DateTime.UtcNow.Year || month < 1 || month > 12 || day < 1 || day > 31)
                return new MessageResponse("Invalid date entered", ephemeral: true);

            var playerStats = await GetPlayerStatsAsync(guildId, userId);
            try
            {
                var bday = new DateTime(year, month, day);
                playerStats.Birthday = bday;
                _context.Update(playerStats);
                await _context.SaveChangesAsync();

                var adultRole = _guildSettings.GetAdultRole(guildId);
                if (adultRole != null && user != null)
                {
                    if ((DateTime.UtcNow - bday).TotalDays > 364)
                    {
                        await user.AddRoleAsync((ulong)adultRole);
                    }
                }

                return new MessageResponse($"Sucessfully set birthday to {bday.ToShortDateString()}", ephemeral: true);
            }
            catch
            {
                return new MessageResponse("Error setting birthday. Please make sure the date you entered is correct!", ephemeral: true);
            }
        }

        public List<PlayerStats> GetUsersWithBirthdays(ulong guildId)
        {
            return _context.PlayerStats.Where(u => u.GuildId == guildId && u.Birthday != null).ToList();
        }

        public async Task<MessageResponse> Daily(ulong guildId, ulong userId)
        {
            var player = await GetPlayerStatsAsync(guildId, userId);
            var guildTimeNow = _guildSettings.GetGuildTime(guildId);
            if (player.LastDaily != null && guildTimeNow - player.LastDaily < TimeSpan.FromDays(1))
            {
                var timeLeft = TimeSpan.FromDays(1) - (guildTimeNow - (DateTime)player.LastDaily);
                var sb = new StringBuilder();
                if (timeLeft.Hours > 0)
                    sb.Append($"{timeLeft.Hours} hours ");
                if(timeLeft.Minutes > 0)
                    sb.Append($"{timeLeft.Minutes} minutes ");
                sb.Append($"{timeLeft.Seconds} seconds left.");
                return new MessageResponse($"You have already claimed your daily reward today! {sb}", ephemeral: true);
            }

            var rng = new Random();
            var oldLevel = GetLevelFromXp(player.Experience);
            var pointsEarned = Convert.ToInt64(rng.Next(100 * oldLevel) + (100 * oldLevel));
            var experienceEarned = Convert.ToInt64(rng.Next(100 * oldLevel) + (100 * oldLevel));

            await Award(guildId, userId, pointsEarned, experienceEarned);

            var newLevel = GetLevelFromXp(player.Experience + experienceEarned);
            var leveledUp = newLevel > oldLevel ? $"\nYou leveled up! You are now level **{newLevel}**" : string.Empty;

            player.LastDaily = guildTimeNow;
            _context.Update(player);
            await _context.SaveChangesAsync();

            return new MessageResponse($"You gained **{pointsEarned}** stars and gained **{experienceEarned}** xp!{leveledUp}");
        }

        public async Task<MessageResponse> TransferPoints(ulong guildId, ulong fromId, ulong toId, int amount)
        {
            var from = await GetPlayerStatsAsync(guildId, fromId);

            if (from.Points < amount)
                return new MessageResponse("You do not have enough stars for that.", ephemeral: true);

            var to = await GetPlayerStatsAsync(guildId, toId);

            from.Points -= amount;
            to.Points += amount;
            await _context.SaveChangesAsync();
            return new MessageResponse("Stars successfully sent!", ephemeral: true);
        }

        public async Task<List<string>> StarLeaderboard(ulong guildId, IInteractionContext interactionContext)
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
                stars = stars.PadRight(16);
                playerStats.Add($"{i+1}. {stars} {name}");
            }
            return playerStats;
        }

        public async Task<List<string>> LevelLeaderboard(ulong guildId, IInteractionContext interactionContext)
        {
            var playersInGuild = (await _context.PlayerStats.Where(p => p.GuildId == guildId).ToListAsync())
                .OrderByDescending(l => GetLevelFromXp(l.Experience)).ToList();
            var playerStats = new List<string>();
            for (var i = 0; i < playersInGuild.Count; i++)
            {
                var player = playersInGuild[i];
                var user = await interactionContext.Client.GetUserAsync(player.UserId);
                var level = GetLevelFromXp(player.Experience);
                var name = user.GlobalName.Length > 15 ? user.GlobalName.Substring(0, 12) + "..." : user.GlobalName;
                var stars = $"Lvl. {level}";
                stars = stars.PadRight(16);
                playerStats.Add($"{i + 1}. {stars} {name}");
            }
            return playerStats;
        }

        public async Task<MessageResponse> StatCard(ulong guildId, IUser user)
        {
            var playerStats = await GetPlayerStatsAsync(guildId, user.Id);
            var level = GetLevelFromXp(playerStats.Experience);
            var playerCard = new EmbedBuilder()
                .WithThumbnailUrl(user.GetDisplayAvatarUrl())
                .WithDescription($"**Level:** {level}\n**XP:** {playerStats.Experience}/{GetXpForLevel(level+1)}\n**Stars:** {playerStats.Points}\n**Games Won:** {playerStats.GamesWon}\n**Games Lost:** {playerStats.GamesLost}")
                .WithAuthor(new EmbedAuthorBuilder().WithName(user.GlobalName).WithIconUrl(user.GetAvatarUrl()))
                .Build();

            return new MessageResponse(embed: playerCard);
        }

        public async Task<bool> Award(ulong guildId, ulong userId, long? points = 0, long? xp = 0, int? gamesWon = 0, int? gamesLost = 0)
        {
            var pointsLeft = true;
            var player = await GetPlayerStatsAsync(guildId, userId);
            if (player != null)
            {
                player.Points += points ?? 0;
                player.Experience += xp ?? 0;
                player.GamesWon += gamesWon ?? 0;
                player.GamesLost += gamesLost ?? 0;

                if (player.Points < 0)
                    player.Points = 0;

                if (player.Points + points < 0)
                    pointsLeft = false;

                _context.Update(player);
                await _context.SaveChangesAsync();

                return pointsLeft;
            }
            return false;
        }

        public static int GetLevelFromXp(long experience)
        {
            return Convert.ToInt32(Math.Floor(Math.Sqrt(experience / 100))) + 1;
        }

        private static int GetXpForLevel(int level)
        {
            return (level - 1) * (level - 1) * 100;
        }
    }
}
