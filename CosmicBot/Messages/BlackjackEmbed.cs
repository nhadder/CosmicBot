using Discord.WebSocket;
using Discord;
using CosmicBot.Helpers;
using CosmicBot.Models;
using CosmicBot.Models.Enums;
using System.Text;

namespace CosmicBot.DiscordResponse
{
    public class BlackjackEmbed
    {
        private List<PlayingCard> _dealerCards = new List<PlayingCard>();
        private List<PlayingCard> _userCards = new List<PlayingCard>();
        private bool _expired = false;

        public DateTime Expires { get; set; } = DateTime.UtcNow.AddMinutes(1);
        public GameStatus Status { get; set; } = GameStatus.InProgress;
        public long Bet { get; set; }
        public ulong UserId { get; set; }

        public BlackjackEmbed(ulong userId, long bet)
        {
            UserId = userId;
            Bet = bet;

            _userCards = DealCards(2);
            _dealerCards = DealCards(1);
        }

        public async Task SendAsync(IInteractionContext context)
        {
            await context.Interaction.DeferAsync();

            var embed = GetPageEmbed(context.User);
            var components = GetPageButtons();

            var message = await context.Interaction.FollowupAsync(embed: embed, components: components);
            GameMessageStore.AddMessage(message.Id, this);
        }

        public async Task HandleButtonAsync(SocketMessageComponent component, IUser user)
        {
            Expires = DateTime.UtcNow.AddMinutes(1);
            if (component.Data.CustomId.StartsWith("game_bj_hit"))
                Hit();
            else if (component.Data.CustomId.StartsWith("game_bj_stand"))
                Stand();

            var embed = GetPageEmbed(user);
            var components = GetPageButtons();
            await component.UpdateAsync(msg => { msg.Embed = embed; msg.Components = components; });
        }

        private void Hit()
        {
            _userCards.AddRange(DealCards(1));

            var total = GetTotal(_userCards);
            if (total > 21)
            {
                Status = GameStatus.Lost;
                _expired = true;
            }
        }

        private void Stand()
        {
            _expired = true;
            var dealerTotal = GetTotal(_dealerCards);
            while (dealerTotal < 17)
            {
                _dealerCards.AddRange(DealCards(1));
                dealerTotal = GetTotal(_dealerCards);
            }

            var userTotal = GetTotal(_userCards);
            if (dealerTotal > 21 || dealerTotal < userTotal)
            {
                Status = GameStatus.Won;
                return;
            }
            if (dealerTotal > userTotal)
            {
                Status = GameStatus.Lost;
                return;
            }
            Status = GameStatus.Tie;
        }

        private Embed GetPageEmbed(IUser user)
        {
            var embedBuilder = new EmbedBuilder()
                .WithAuthor(new EmbedAuthorBuilder()
                    .WithName($"Blackjack - Bet {Bet} Star(s)").WithIconUrl(user.GetAvatarUrl()))
                .WithDescription(GetGameWindow())
                .WithFooter(user.GlobalName);

            return embedBuilder.Build();
        }

        private string GetGameWindow()
        {
            var playerCards = string.Join(" ", _userCards.Select(c => c.ToString()));
            var playerCardsHeader = "Your Cards";
            var userTotal = $"Total: {GetTotal(_userCards)}";

            var dealerCards = Status == GameStatus.InProgress ?
                _dealerCards.First().ToString() + " ?" :
                string.Join(" ", _dealerCards.Select(c => c.ToString()));
            var dealerCardsHeader = "Dealer's Cards";
            var dealerTotal = "Total: " + (Status == GameStatus.InProgress ?
                GetTotal(_dealerCards.Take(1).ToList()).ToString() :
                GetTotal(_dealerCards).ToString());

            var result = string.Empty;
            if (Status == GameStatus.Won)
                result = $"You won! You gained **{Bet}** stars!";
            if (Status == GameStatus.Lost)
                result = $"You lose! You lost **{Bet}** stars!";
            if (Status == GameStatus.Tie)
                result = $"Push";

            var playerWidth = Math.Max(playerCards.Length+1, 13);
            var sb = new StringBuilder();
            sb.AppendLine("```" + playerCardsHeader.PadRight(playerWidth) + " " + dealerCardsHeader);
            sb.AppendLine(playerCards.PadRight(playerWidth) + " " + dealerCards);
            sb.AppendLine(userTotal.PadRight(playerWidth) + " " + dealerTotal + "```");
            if (!string.IsNullOrWhiteSpace(result))
            {
                sb.AppendLine("**Result**");
                sb.AppendLine(result);
            }
            return sb.ToString();
        }

        private static List<PlayingCard> DealCards(int n)
        {
            var cards = new List<PlayingCard>();
            var rng = new Random();
            for(var i = 0; i < n; i++)
            {
                cards.Add(new PlayingCard()
                {
                    Suit = rng.GetItems(Enum.GetValues<PlayingCardSuit>(), 1).First(),
                    Number = rng.GetItems(Enum.GetValues<PlayingCardNumber>(), 1).First()
                });
            }
            return cards;
        }

        private static int GetTotal(List<PlayingCard> cards)
        {
            var total = 0;
            //add non-aces
            foreach(var card in cards.Where(c => c.Number != PlayingCardNumber.Ace))
            {
                if ((int)card.Number >= 2 && (int)card.Number <= 10)
                {
                    total += (int)card.Number;
                    continue;
                }

                if ((int)card.Number >= 11)
                    total += 10;
            }

            //add aces last to try and soft total if over 21
            foreach(var aces in cards.Where(c => c.Number == PlayingCardNumber.Ace))
            {
                var tempTotal = total + 11;
                if (tempTotal > 21)
                    total += 1;
                else
                    total += 11;

                continue;
            }

            return total;
        }

        private MessageComponent GetPageButtons()
        {
            var builder = new ComponentBuilder();
            builder.WithButton("Hit", $"game_bj_hit", ButtonStyle.Success, disabled: _expired);
            builder.WithButton("Stand", $"game_bj_stand", ButtonStyle.Danger, disabled: _expired);
            return builder.Build();
        }       
    }
}
