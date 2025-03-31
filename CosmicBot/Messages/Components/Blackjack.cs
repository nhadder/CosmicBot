using CosmicBot.DiscordResponse;
using CosmicBot.Models;
using CosmicBot.Models.Enums;
using Discord;
using System.Text;


namespace CosmicBot.Messages.Components
{
    public class Blackjack : EmbedMessage
    {
        private List<PlayingCard> _dealerCards = [];
        private List<Hand> _userHands = [];
        private int _activeHand = 0;
        private readonly string _username;
        private readonly string _iconUrl;
        private readonly ulong _userId;
        private readonly long _bet;

        private class Hand
        {
            public List<PlayingCard> Cards { get; set; } = new List<PlayingCard>();
            public bool Doubled { get; set; } = false;
            public GameStatus Status { get; set; } = GameStatus.InProgress;
        }

        public Blackjack(IInteractionContext context, long bet) : base([context.User.Id])
        {
            _bet = bet;
            _userId = context.User.Id;
            _username = context.User.GlobalName;
            _iconUrl = context.User.GetAvatarUrl();

            SetupGame(context);
        }

        private void SetupGame(IInteractionContext context)
        {
            _userHands.Clear();
            _dealerCards.Clear();
            _userHands.Add(new Hand { Cards = DealCards(2) });
            _dealerCards = DealCards(1);
            _activeHand = 0;
            DoButtons();
        }

        private void DoButtons()
        {
            Buttons.Clear();
            if (GetTotal(_userHands[_activeHand].Cards) != 21)
            {
                var hitButton = new MessageButton("Hit", ButtonStyle.Success, 0);
                hitButton.OnPress = Hit;
                Buttons.Add(hitButton);

                var doubleButton = new MessageButton("Double Down", ButtonStyle.Primary, 1);
                doubleButton.OnPress = DoubleDown;
                Buttons.Add(doubleButton);

                if (CanSplit(_userHands[_activeHand].Cards))
                {
                    var splitButton = new MessageButton("Split", ButtonStyle.Primary, 1);
                    splitButton.OnPress = Split;
                    Buttons.Add(splitButton);
                }
            }

            var standButton = new MessageButton("Stand", ButtonStyle.Danger, 0);
            standButton.OnPress = Stand;
            Buttons.Add(standButton);
        }

        public override Embed[] GetEmbeds()
        {
            var embedBuilder = new EmbedBuilder()
                .WithAuthor(new EmbedAuthorBuilder()
                    .WithName($"Blackjack - Bet {_bet} Star(s)").WithIconUrl(_iconUrl))
                .WithDescription(GetGameWindow())
                .WithFooter(_username);

            return [embedBuilder.Build()];
        }

        private MessageResponse? PlayAgain(IInteractionContext context)
        {
            SetupGame(context);
            return null;
        }

        private MessageResponse? Hit(IInteractionContext context)
        {
            _userHands[_activeHand].Cards.AddRange(DealCards(1));

            var total = GetTotal(_userHands[_activeHand].Cards);
            if (total > 21)
            {
                if(_activeHand <  _userHands.Count - 1)
                {
                    _activeHand++;
                    Hit(context);
                }
                else
                    GameOver();
            }
            else if (total == 21)
                return Stand(context);

            DoButtons();
            return null;
        }

        private MessageResponse? Split(IInteractionContext context)
        {
            var firstHand = new List<PlayingCard>
            {
                _userHands[_activeHand].Cards[0]
            };
            var secondHand = new List<PlayingCard>
            {
                _userHands[_activeHand].Cards[1]
            };
            _userHands[_activeHand].Cards = firstHand;
            _userHands.Add(new Hand { Cards = secondHand });

            return Hit(context);
        }

        private MessageResponse? DoubleDown(IInteractionContext context)
        {
            _userHands[_activeHand].Doubled = true;
            _userHands[_activeHand].Cards.AddRange(DealCards(1));
            return Stand(context);
        }

        private MessageResponse? Stand(IInteractionContext context)
        {
            if (_activeHand < _userHands.Count - 1)
            {
                _activeHand++;
                Hit(context);
            }
            else
                GameOver();

            return null;
        }

        private void GameOver()
        {
            var dealerTotal = GetTotal(_dealerCards);
            while (dealerTotal < 17)
            {
                _dealerCards.AddRange(DealCards(1));
                dealerTotal = GetTotal(_dealerCards);
            }

            foreach (var hand in _userHands)
            {
                var userTotal = GetTotal(hand.Cards);

                if (userTotal > 21)
                    hand.Status = GameStatus.Lost;
                else if (dealerTotal > 21 || dealerTotal < userTotal)
                    hand.Status = GameStatus.Won;
                else if (dealerTotal > userTotal)
                    hand.Status = GameStatus.Lost;
                else
                    hand.Status = GameStatus.Tie;

                var bonus = hand.Doubled ? _bet : 0;
                if (hand.Status == GameStatus.Won)
                    Awards.Add(new PlayerAward(_userId, _bet + bonus, 20, 1, 0));
                if (hand.Status == GameStatus.Lost)
                    Awards.Add(new PlayerAward(_userId, -_bet - bonus, 5, 0, 1));
                if (hand.Status == GameStatus.Tie)
                    Awards.Add(new PlayerAward(_userId, 0, 10, 0, 0));
            }

            Buttons.Clear();
            var tryAgainButton = new MessageButton("Play Again?", ButtonStyle.Secondary);
            tryAgainButton.OnPress = PlayAgain;
            Buttons.Add(tryAgainButton);
        }

        private string GetGameWindow()
        {
            var playerCardsHeader = "Your Cards";
            var playerWidth = 13;
            var handCardStrings = new List<string>();
            var handTotalStrings = new List<string>();
            var results = new List<string>();
            foreach (var hand in _userHands)
            {
                var playerCards = string.Join(" ", hand.Cards.Select(c => c.ToString()));
                var userTotal = $"Total: {GetTotal(hand.Cards)}";
                playerWidth = Math.Max(playerCards.Length + 2, 13);
                handCardStrings.Add(playerCards);
                handTotalStrings.Add(userTotal);
                var bonus = hand.Doubled ? _bet : 0;
                if (hand.Status == GameStatus.Won)
                    results.Add($"You won! You gained **{_bet + bonus}** stars and **20** XP!");
                if (hand.Status == GameStatus.Lost)
                    results.Add($"You lose! You lost **{_bet + bonus}** stars and gained **5** XP!");
                if (hand.Status == GameStatus.Tie)
                    results.Add($"Push. You gained **10** XP!");
            }

            var dealerCards = _dealerCards.Count == 1 ?
                _dealerCards.First().ToString() + " ?" :
                string.Join(" ", _dealerCards.Select(c => c.ToString()));
            var dealerCardsHeader = "Dealer's Cards";
            var dealerTotal = "Total: " + GetTotal(_dealerCards).ToString();

            var sb = new StringBuilder();
            sb.Append("```");
            sb.AppendLine(playerCardsHeader.PadRight(playerWidth) + " " + dealerCardsHeader);
            var firstActive = _userHands.Count > 1 && _activeHand == 0 ? ">" : string.Empty;
            sb.AppendLine(firstActive + handCardStrings[0].PadRight(playerWidth) + " " + dealerCards);
            sb.AppendLine(handTotalStrings[0].PadRight(playerWidth) + " " + dealerTotal);
            if(_userHands.Count > 1)
            {
                for(var i = 1; i < handCardStrings.Count; i++)
                {
                    sb.AppendLine();
                    if (_activeHand == i)
                        sb.Append('>');
                    sb.AppendLine(handCardStrings[i]);
                    sb.AppendLine(handTotalStrings[i]);
                }
            }
            sb.Append("```");
            if (results.Any())
            {
                sb.AppendLine("**Result**");
                foreach(var result in results)
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

        private static bool CanSplit(List<PlayingCard> cards)
        {
            return (cards.Count == 2 && (int)cards[0].Number == (int)cards[1].Number);
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
