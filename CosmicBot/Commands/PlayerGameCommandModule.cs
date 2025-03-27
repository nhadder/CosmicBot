using Discord.Interactions;
using CosmicBot.Service;
using CosmicBot.DiscordResponse;
using CosmicBot.Messages.Components;
using Discord;
using CosmicBot.Services;
using System.Text.RegularExpressions;

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
            if (!HasChannelPermissions())
            {
                await Respond(new MessageResponse("I don't have valid permissions in this channel", ephemeral: true));
                return;
            }

            await Respond(await _playerService.Daily(Context.Guild.Id, Context.User.Id));
        }

        [SlashCommand("gift", "Gift stars to another player")]
        public async Task Gift(IUser to, int amount)
        {
            if (!HasChannelPermissions())
            {
                await Respond(new MessageResponse("I don't have valid permissions in this channel", ephemeral: true));
                return;
            }

            if (amount <= 0)
            {
                await Respond(new MessageResponse("Amount must be a positive number", ephemeral: true));
                return;
            }

            await Respond(await _playerService.TransferPoints(Context.Guild.Id, Context.User.Id, to.Id, amount));
        }

        [Group("leaderboard", "See current leaderboards")]
        public class LeaderboardCommandModule : CommandModule
        {
            private readonly PlayerService _playerService;

            public LeaderboardCommandModule(PlayerService playerService)
            {
                _playerService = playerService;
            }

            [SlashCommand("stars", "Show the star leadboard of the guild")]
            public async Task Stars()
            {
                if (!HasChannelPermissions())
                {
                    await Respond(new MessageResponse("I don't have valid permissions in this channel", ephemeral: true));
                    return;
                }

                var playerStats = await _playerService.StarLeaderboard(Context.Guild.Id, Context);
                await new PagedList(playerStats, 10, "Star Leaderboard").SendAsync(Context);
            }

            [SlashCommand("levels", "Show the star leadboard of the guild")]
            public async Task Levels()
            {
                if (!HasChannelPermissions())
                {
                    await Respond(new MessageResponse("I don't have valid permissions in this channel", ephemeral: true));
                    return;
                }

                var playerStats = await _playerService.LevelLeaderboard(Context.Guild.Id, Context);
                await new PagedList(playerStats, 10, "Level Leaderboard").SendAsync(Context);
            }
        }

        [SlashCommand("stats", "Show the star leadboard of the guild")]
        public async Task Stats()
        {
            if (!HasChannelPermissions())
            {
                await Respond(new MessageResponse("I don't have valid permissions in this channel", ephemeral: true));
                return;
            }

            await Respond(await _playerService.StatCard(Context.Guild.Id, Context.User));
        }

        [SlashCommand("blackjack", "Play a game of blackjack")]
        public async Task Blackjack(int bet)
        {
            if (!HasChannelPermissions())
            {
                await Respond(new MessageResponse("I don't have valid permissions in this channel", ephemeral: true));
                return;
            }

            if (bet < 0)
            {
                await Respond(new MessageResponse("Bet must be 0 or higher.", ephemeral: true));
                return;
            }

            var player = await _playerService.GetPlayerStatsAsync(Context.Guild.Id, Context.User.Id);
            if (player.Points < bet)
            {
                await Respond(new MessageResponse("You do not have enough points for that bet", ephemeral: true));
                return;
            }

            if (player.Points < bet*2)
            {
                await Respond(new MessageResponse("You can not bet more than half of your stars", ephemeral: true));
                return;
            }

            await new Blackjack(Context, bet).SendAsync(Context);
        }

        [SlashCommand("higherlower", "Play a game of higher lower")]
        public async Task HigherLower()
        {
            if (!HasChannelPermissions())
            {
                await Respond(new MessageResponse("I don't have valid permissions in this channel", ephemeral: true));
                return;
            }

            await new HigherLower(Context).SendAsync(Context);
        }

        [SlashCommand("knucklebones", "Play a game of blackjack against another player")]
        public async Task Knucklebones(int bet, IUser opponent)
        {
            if (!HasChannelPermissions())
            {
                await Respond(new MessageResponse("I don't have valid permissions in this channel", ephemeral: true));
                return;
            }

            if (bet < 0)
            {
                await Respond(new MessageResponse("Bet must be 0 or higher.", ephemeral: true));
                return;
            }

            var player = await _playerService.GetPlayerStatsAsync(Context.Guild.Id, Context.User.Id);
            if (player.Points < bet)
            {
                await Respond(new MessageResponse("You do not have enough points for that bet", ephemeral: true));
                return;
            }

            if (player.Points < bet * 2)
            {
                await Respond(new MessageResponse("You can not bet more than half of your stars", ephemeral: true));
                return;
            }

            var player2 = await _playerService.GetPlayerStatsAsync(Context.Guild.Id, opponent.Id);
            if (player2.Points < bet)
            {
                await Respond(new MessageResponse($"{opponent.GlobalName} does not have enough points for that bet", ephemeral: true));
                return;
            }

            if (player2.Points < bet * 2)
            {
                await Respond(new MessageResponse($"{opponent.GlobalName} can not bet more than half of their stars", ephemeral: true));
                return;
            }

            await new Knucklebones(Context, opponent, bet).SendAsync(Context);
        }

        [SlashCommand("battle", "Battle another player")]
        public async Task Battle(int bet, IUser opponent)
        {
            if (!HasChannelPermissions())
            {
                await Respond(new MessageResponse("I don't have valid permissions in this channel", ephemeral: true));
                return;
            }

            if (bet < 0)
            {
                await Respond(new MessageResponse("Bet must be 0 or higher.", ephemeral: true));
                return;
            }

            var player = await _playerService.GetPlayerStatsAsync(Context.Guild.Id, Context.User.Id);
            if (player.Points < bet)
            {
                await Respond(new MessageResponse("You do not have enough points for that bet", ephemeral: true));
                return;
            }

            if (player.Points < bet * 2)
            {
                await Respond(new MessageResponse("You can not bet more than half of your stars", ephemeral: true));
                return;
            }

            var player2 = await _playerService.GetPlayerStatsAsync(Context.Guild.Id, opponent.Id);
            if (player2.Points < bet)
            {
                await Respond(new MessageResponse($"{opponent.GlobalName} does not have enough points for that bet", ephemeral: true));
                return;
            }

            if (player2.Points < bet * 2)
            {
                await Respond(new MessageResponse($"{opponent.GlobalName} can not bet more than half of their stars", ephemeral: true));
                return;
            }

            await new Battle(Context, opponent, player.Level, player2.Level, bet).SendAsync(Context);
        }

        [Group("pet", "Buy and raise a pet!")]
        public class PetCommands : CommandModule 
        {
            private readonly PlayerService _playerService;
            private readonly PetService _petService;

            public PetCommands(PlayerService playerService, PetService petService)
            {
                _playerService = playerService;
                _petService = petService;
            }

            [SlashCommand("buy", "Buy a pet")]
            public async Task Buy(string name)
            {
                if (!HasChannelPermissions())
                {
                    await Respond(new MessageResponse("I don't have valid permissions in this channel", ephemeral: true));
                    return;
                }

                name = Regex.Replace(name, "[^a-zA-Z]", "");
                if (string.IsNullOrWhiteSpace(name))
                {
                    await Respond(new MessageResponse("Invalid name", ephemeral: true));
                    return;
                }

                if (name.Length <= 3 || name.Length >= 20)
                {
                    await Respond(new MessageResponse("Name must be more than 3 characters and less than 20 characters", ephemeral: true));
                    return;
                }

                var player = await _playerService.GetPlayerStatsAsync(Context.Guild.Id, Context.User.Id);
                if (player.Points < 500)
                {
                    await Respond(new MessageResponse("You must have **500** Stars to buy a pet", ephemeral: true));
                    return;
                }

                var response = await _petService.Create(Context.Guild.Id, Context.User.Id, name);

                if (response == null)
                {
                    await Respond(new MessageResponse("You already have a pet.\nDo `/pet sell` to sell them for stars. The older and happier they are, the more valuable!", ephemeral: true));
                    return;
                }

                await _playerService.Award(Context.Guild.Id, Context.User.Id, -10_000);
                await Respond(response);
            }

            [SlashCommand("sell", "Sell your pet")]
            public async Task Sell()
            {
                if (!HasChannelPermissions())
                {
                    await Respond(new MessageResponse("I don't have valid permissions in this channel", ephemeral: true));
                    return;
                }

                var price = await _petService.Sell(Context.Guild.Id, Context.User.Id);
                if (price == 0)
                {
                    await Respond(new MessageResponse("Unable to sell pet", ephemeral: true));
                    return;
                }

                await _playerService.Award(Context.Guild.Id, Context.User.Id, price);
                await Respond(new MessageResponse($"You sold your pet for {price} stars!", ephemeral: true));
            }

            [SlashCommand("pet", "Pet your pet!")]
            public async Task Pet()
            {
                if (!HasChannelPermissions())
                {
                    await Respond(new MessageResponse("I don't have valid permissions in this channel", ephemeral: true));
                    return;
                }

                var pet = _petService.Get(Context.Guild.Id, Context.User.Id);
                if(pet == null)
                {
                    await Respond(new MessageResponse("You do not have a pet yet.\nUse `/pet buy` to buy a pet (requires **500** stars)", ephemeral: true));
                    return;
                }

                if (pet.Sold)
                {
                    await Respond(new MessageResponse("You do not have a pet.\nUse `/pet buy` to buy a pet (requires **500** stars)", ephemeral: true));
                    return;
                }

                await new PetGame(Context.User, pet).SendAsync(Context);
            }

            [SlashCommand("rename", "Pet your pet!")]
            public async Task Rename(string name)
            {
                if (!HasChannelPermissions())
                {
                    await Respond(new MessageResponse("I don't have valid permissions in this channel", ephemeral: true));
                    return;
                }

                var pet = _petService.Get(Context.Guild.Id, Context.User.Id);
                if (pet == null)
                {
                    await Respond(new MessageResponse("You do not have a pet yet.\nUse `/pet buy` to buy a pet (requires **500** stars)", ephemeral: true));
                    return;
                }

                name = string.Join("", name.ToCharArray().Where(c => (c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z')));
                if (string.IsNullOrWhiteSpace(name))
                {
                    await Respond(new MessageResponse("Invalid name", ephemeral: true));
                    return;
                }

                if (name.Length <= 3 || name.Length >= 20)
                {
                    await Respond(new MessageResponse("Name must be more than 3 characters and less than 20 characters", ephemeral: true));
                    return;
                }

                pet.Name = name;
                await _petService.Update(pet);

                await Respond(new MessageResponse($"Successfully renamed your pet to {name}", ephemeral: true));
            }
        }
    }
}
