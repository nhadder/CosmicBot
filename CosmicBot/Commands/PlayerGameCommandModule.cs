using Discord.Interactions;
using CosmicBot.Service;
using CosmicBot.DiscordResponse;

namespace CosmicBot.Commands
{
    public class PlayerGameCommandModule : CommandModule
    {
        private readonly PlayerService _playerService;
        public PlayerGameCommandModule(PlayerService playerService)
        {
            _playerService = playerService;
        }

        [SlashCommand("daily", "Get your daily reward")]
        public async Task Daily()
        {
            await Respond(await _playerService.Daily(Context.Guild.Id, Context.User.Id));
        }

        [SlashCommand("leaderboard", "Show the star leadboard of the guild")]
        public async Task Leaderboard()
        {
            var pagedList = await _playerService.Leaderboard(Context.Guild.Id, Context);
            await pagedList.SendAsync(Context);
        }

        [SlashCommand("stats", "Show the star leadboard of the guild")]
        public async Task Stats()
        {
            await Respond(await _playerService.StatCard(Context.Guild.Id, Context.User));
        }

        [SlashCommand("blackjack", "Play a game of blackjack")]
        public async Task Blackjack(int bet)
        {
            var player = await _playerService.GetPlayerStatsAsync(Context.Guild.Id, Context.User.Id);
            if (player.Points < bet)
                await Respond(new MessageResponse("You do not have enough points for that bet", ephemeral: true));

            await new BlackjackEmbed(Context.User.Id, bet).SendAsync(Context);
        }
    }
}
