using CosmicBot.DiscordResponse;
using CosmicBot.Models;
using CosmicBot.Models.Enums;
using Discord;
using System.Text;


namespace CosmicBot.Messages.Components
{
    public class HigherLower : EmbedMessage
    {
        private List<PlayingCard> _cards = [];
        private int _lives = 0;
        private int _streak = 0;
        private readonly string _username;
        private readonly string _iconUrl;
        private readonly ulong _userId;
        private GameStatus Status;

        public HigherLower(IInteractionContext context) : base([context.User.Id])
        {
            _userId = context.User.Id;
            _username = context.User.GlobalName;
            _iconUrl = context.User.GetAvatarUrl();

            SetupGame(context);
        }

        private void SetupGame(IInteractionContext context)
        {
            Status = GameStatus.InProgress;
            _streak = 0;
            _lives = 0;
            _cards = DealCards(1);

            var higherButton = new MessageButton("Higher", ButtonStyle.Success);
            higherButton.OnPress = Higher;
            Buttons.Add(higherButton);

            var lowerButton = new MessageButton("Lower", ButtonStyle.Danger);
            lowerButton.OnPress = Lower;
            Buttons.Add(lowerButton);
        }

        public void AddSurrenderOption()
        {
            var SurrenderButton = new MessageButton("Surrender", ButtonStyle.Primary);
            SurrenderButton.OnPress = Surrender;
            Buttons.Add(SurrenderButton);
        }

        public override Embed[] GetEmbeds()
        {
            var embedBuilder = new EmbedBuilder()
                .WithAuthor(new EmbedAuthorBuilder()
                    .WithName($"Higher Lower - Streak {_streak}").WithIconUrl(_iconUrl))
                .WithDescription(GetGameWindow())
                .WithFooter(_username);

            return [embedBuilder.Build()];
        }

        private MessageResponse? PlayAgain(IInteractionContext context)
        {
            Buttons.Clear();
            SetupGame(context);
            return null;
        }

        private MessageResponse? Higher(IInteractionContext context)
        {
            if (_streak == 0)
                AddSurrenderOption();

            var lastCard =_cards.Last();
            var newCard = DealCards(1).First();
            _cards.Add(newCard);

            if (((int)newCard.Number) < ((int)lastCard.Number))
            {
                if (lastCard.Number == PlayingCardNumber.Seven && _lives > 0)
                {
                    _streak++;
                    _lives--;
                }
                else
                    GameOver(GameStatus.Lost);
            }
            else
            {
                if (lastCard.Number == PlayingCardNumber.Seven)
                    _lives++;

                _streak++;
            }

            return null;
        }

        private MessageResponse? Lower(IInteractionContext context)
        {
            if (_streak == 0)
                AddSurrenderOption();

            var lastCard = _cards.Last();
            var newCard = DealCards(1).First();
            _cards.Add(newCard);

            if (((int)newCard.Number) > ((int)lastCard.Number))
            {
                if (lastCard.Number == PlayingCardNumber.Seven && _lives > 0)
                {
                    _streak++;
                    _lives--;
                }
                else
                    GameOver(GameStatus.Lost);
            }
            else
            {
                if (lastCard.Number == PlayingCardNumber.Seven)
                    _lives++;
                _streak++;
            }

            return null;
        }

        private MessageResponse? Surrender(IInteractionContext context)
        {
            GameOver(GameStatus.Won);
            return null;
        }

        private void GameOver(GameStatus status)
        {
            if(status == GameStatus.Won)
                Awards.Add(new PlayerAward(_userId, Convert.ToInt64(Math.Pow(2, _streak)), 20 * _streak, 1, 0));

            Status = status;

            Buttons.Clear();
            var tryAgainButton = new MessageButton("Play Again?", ButtonStyle.Secondary);
            tryAgainButton.OnPress = PlayAgain;
            Buttons.Add(tryAgainButton);
        }

        private string GetGameWindow()
        {
            var cards = string.Join(" ", _cards.Select(c => c.ToString()));

            var result = string.Empty;
            if (Status == GameStatus.Won)
                result = $"**Result**\nYou won! You gained **{Math.Pow(2, _streak)}** stars and **{_streak * 20}** XP!";
            if (Status == GameStatus.Lost)
                result = $"**Result**\nYou lose!";
            if (Status == GameStatus.InProgress && _streak > 0)
                result = $"Surrender for **{Math.Pow(2, _streak)}** stars and **{_streak * 20}** XP";
                var sb = new StringBuilder();
            sb.Append("```");
            sb.AppendLine(cards);
            if (_lives > 0)
                sb.AppendLine($"Seven Shields: {_lives}");
            sb.Append("```");

            if (!string.IsNullOrWhiteSpace(result))
                sb.AppendLine(result);

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
    }
}
