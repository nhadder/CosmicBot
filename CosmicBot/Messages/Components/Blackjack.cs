using CosmicBot.Models;
using CosmicBot.Models.Enums;
using Discord;
using System.Text;


namespace CosmicBot.Messages.Components
{
    public class Blackjack : EmbedMessage
    {
        private List<PlayingCard> _dealerCards = [];
        private List<PlayingCard> _userCards = [];
        private readonly string _username;
        private readonly string _iconUrl;
        private readonly ulong _userId;
        private GameStatus Status;
        private readonly long _bet;

        public Blackjack(IInteractionContext context, long bet) : base([context.User.Id])
        {
            _bet = bet;

            _userId = context.User.Id;
            _username = context.User.GlobalName;
            _iconUrl = context.User.GetAvatarUrl();

            SetupGame();
        }

        private void SetupGame()
        {
            Status = GameStatus.InProgress;
            _userCards.Clear();
            _dealerCards.Clear();

            _userCards = DealCards(2);
            _dealerCards = DealCards(1);

            if (GetTotal(_userCards) == 21)
            {
                Stand();
                return;
            }

            var hitButton = new MessageButton("Hit", ButtonStyle.Success);
            hitButton.OnPress += Hit;
            Buttons.Add(hitButton);

            var standButton = new MessageButton("Stand", ButtonStyle.Danger);
            standButton.OnPress += Stand;
            Buttons.Add(standButton);
        }

        public override Embed GetEmbed()
        {
            var embedBuilder = new EmbedBuilder()
                .WithAuthor(new EmbedAuthorBuilder()
                    .WithName($"Blackjack - Bet {_bet} Star(s)").WithIconUrl(_iconUrl))
                .WithDescription(GetGameWindow())
                .WithFooter(_username);

            return embedBuilder.Build();
        }

        private Task PlayAgain(IInteractionContext? context = null)
        {
            Buttons.Clear();
            SetupGame();
            return Task.CompletedTask;
        }

        private async Task Hit(IInteractionContext? context = null)
        {
            _userCards.AddRange(DealCards(1));

            var total = GetTotal(_userCards);
            if (total > 21)
                GameOver(GameStatus.Lost);
            else if(total == 21)
                await Stand();
        }

        private Task Stand(IInteractionContext? context = null)
        {
            var dealerTotal = GetTotal(_dealerCards);
            while (dealerTotal < 17)
            {
                _dealerCards.AddRange(DealCards(1));
                dealerTotal = GetTotal(_dealerCards);
            }

            var userTotal = GetTotal(_userCards);

            if (dealerTotal > 21 || dealerTotal < userTotal)
                GameOver(GameStatus.Won);
            else if (dealerTotal > userTotal)
                GameOver(GameStatus.Lost);
            else
                GameOver(GameStatus.Tie);

            return Task.CompletedTask;
        }

        private void GameOver(GameStatus status)
        {
            if(status == GameStatus.Won)
                Awards.Add(new PlayerAward(_userId, _bet, 20, 1, 0));
            if(status == GameStatus.Lost)
                Awards.Add(new PlayerAward(_userId, -_bet, 5, 0, 1));
            if(status == GameStatus.Tie)
                Awards.Add(new PlayerAward(_userId, 0, 10, 0, 0));

            Status = status;

            Buttons.Clear();
            var tryAgainButton = new MessageButton("Play Again?", ButtonStyle.Secondary);
            tryAgainButton.OnPress += PlayAgain;
            Buttons.Add(tryAgainButton);
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
                result = $"You won! You gained **{_bet}** stars and **20** XP!";
            if (Status == GameStatus.Lost)
                result = $"You lose! You lost **{_bet}** stars and gained **5** XP!";
            if (Status == GameStatus.Tie)
                result = $"Push. You gained **10** XP!";

            var playerWidth = Math.Max(playerCards.Length + 1, 13);
            var sb = new StringBuilder();
            sb.AppendLine("```" + playerCardsHeader.PadRight(playerWidth) + " " + dealerCardsHeader);
            sb.AppendLine(playerCards.PadRight(playerWidth) + " " + dealerCards);
            sb.AppendLine(userTotal.PadRight(playerWidth) + " " + dealerTotal + "```");
            if (!string.IsNullOrWhiteSpace(result))
            {
                sb.AppendLine("**Result**");
                sb.AppendLine(result);
            }

            if (Expired)
                sb.AppendLine("\nThis game has expired!");

            return sb.ToString();
        }

        private static List<PlayingCard> DealCards(int n)
        {
            var cards = new List<PlayingCard>();
            var rng = new Random();
            for (var i = 0; i < n; i++)
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

            foreach (var card in cards.Where(c => c.Number != PlayingCardNumber.Ace))
            {
                if ((int)card.Number >= 2 && (int)card.Number <= 10)
                {
                    total += (int)card.Number;
                    continue;
                }

                if ((int)card.Number >= 11)
                    total += 10;
            }

            foreach (var aces in cards.Where(c => c.Number == PlayingCardNumber.Ace))
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
    }
}
