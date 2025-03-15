using Discord.Interactions;
using CosmicBot.Service;
using CosmicBot.DiscordResponse;
using CosmicBot.Messages.Components;
using Discord;

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
            var playerStats = await _playerService.Leaderboard(Context.Guild.Id, Context);
            await new PagedList(playerStats, 10, "Leaderboard").SendAsync(Context);
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
            {
                await Respond(new MessageResponse("You do not have enough points for that bet", ephemeral: true));
                return;
            }

            await new Blackjack(Context, bet).SendAsync(Context);
        }

        [SlashCommand("knucklebones", "Play a game of blackjack against another player")]
        public async Task Blackjack(int bet, IUser opponent)
        {
            var player = await _playerService.GetPlayerStatsAsync(Context.Guild.Id, Context.User.Id);
            if (player.Points < bet)
            {
                await Respond(new MessageResponse("You do not have enough points for that bet", ephemeral: true));
                return;
            }
            var player2 = await _playerService.GetPlayerStatsAsync(Context.Guild.Id, opponent.Id);
            if (player2.Points < bet)
            {
                await Respond(new MessageResponse($"{opponent.GlobalName} does not have enough points for that bet", ephemeral: true));
                return;
            }

            await new Knucklebones(Context, opponent, bet).SendAsync(Context);
        }
    }
}
