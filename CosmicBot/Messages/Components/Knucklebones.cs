using CosmicBot.Models;
using CosmicBot.Models.Enums;
using Discord;
using System.Text;


namespace CosmicBot.Messages.Components
{
    public class Knucklebones : EmbedMessage
    {
        private Dice?[] _player1Board = new Dice?[9];
        private Dice?[] _player2Board = new Dice?[9];
        private readonly string _username;
        private readonly string _player2Username;
        private readonly string _iconUrl;
        private readonly string _player2IconUrl;
        private readonly ulong _userId;
        private readonly ulong _player2Id;
        private GameStatus Status = GameStatus.Pending;
        private readonly long _bet;
        private int _turn = 0;
        private Dice _activeDice = new Dice();
        private MessageButton? _column1;
        private MessageButton? _column2;
        private MessageButton? _column3;

        public Knucklebones(IInteractionContext context, IUser player2, long bet) : base([context.User.Id, player2.Id])
        {
            _bet = bet;

            _userId = context.User.Id;
            _player2Id = player2.Id;

            _username = context.User.GlobalName;
            _player2Username = player2.GlobalName;

            _iconUrl = context.User.GetAvatarUrl();
            _player2IconUrl = player2.GetAvatarUrl();

            var acceptButton = new MessageButton("Accept", ButtonStyle.Success);
            acceptButton.OnPress += Accept;
            Buttons.Add(acceptButton);

            var denyButton = new MessageButton("Deny", ButtonStyle.Danger);
            denyButton.OnPress += Deny;
            Buttons.Add(denyButton);
        }

        private Task PlayAgain(IInteractionContext? context = null)
        {
            Status = GameStatus.InProgress;
            _player1Board = new Dice?[9];
            _player2Board = new Dice?[9];
            _turn = 1;
            RollDice();
            AddGameControls();

            return Task.CompletedTask;
        }

        public override Embed GetEmbed()
        {
            string activeUser, activeUrl;
            if (_turn == 0 || (Status == GameStatus.InProgress && _turn == 1) || Status == GameStatus.Won)
            {
                activeUser = _username;
                activeUrl = _iconUrl;
            }
            else
            {
                activeUser = _player2Username;
                activeUrl = _player2IconUrl;
            }

            var embedBuilder = new EmbedBuilder()
                .WithAuthor(new EmbedAuthorBuilder()
                    .WithName($"Knucklebones - Bet {_bet} Star(s)").WithIconUrl(activeUrl))
                .WithDescription(GetGameWindow());
            if(Status == GameStatus.InProgress)
                embedBuilder.WithFooter($"{activeUser}'s turn");

            return embedBuilder.Build();
        }

        private Task Accept(IInteractionContext? context)
        {
            if (context == null)
                return Task.CompletedTask;

            if(context.User.Id == _player2Id)
            {
                Status = GameStatus.InProgress;
                _turn = 1;
                RollDice();
                AddGameControls();
            }
            return Task.CompletedTask;
        }

        private Task Deny(IInteractionContext? context)
        {
            if (context == null)
                return Task.CompletedTask;

            if (context.User.Id == _player2Id)
            {
                Status = GameStatus.Rejected;
                Expired = true;
            }
            return Task.CompletedTask;
        }

        private void AddGameControls()
        {
            Buttons.Clear();

            _column1 = new MessageButton("1", ButtonStyle.Primary);
            _column1.OnPress += Column1;
            Buttons.Add(_column1);

            _column2 = new MessageButton("2", ButtonStyle.Primary);
            _column2.OnPress += Column2;
            Buttons.Add(_column2);

            _column3 = new MessageButton("3", ButtonStyle.Primary);
            _column3.OnPress += Column3;
            Buttons.Add(_column3);
        }

        private Task Column1(IInteractionContext? context)
        {
            ColumnAction(context, 0);
            return Task.CompletedTask;
        }

        private Task Column2(IInteractionContext? context)
        {
            ColumnAction(context, 1);
            return Task.CompletedTask;
        }

        private Task Column3(IInteractionContext? context)
        {
            ColumnAction(context, 2);
            return Task.CompletedTask;
        }

        private void ColumnAction(IInteractionContext? context, int column)
        {
            if (context == null)
                return;

            if (_turn == 1 && context.User.Id == _userId)
            {
                AddToColumn(_player1Board, column);
                RemoveFromColumn(_player2Board, column);

                if (!HasOpenSlots(_player1Board))
                    CalculateResults();
                else
                {
                    _turn = 2;
                    RollDice();
                    DisableButtonsIfFull(_player2Board);
                }
            }
            else if (_turn == 2 && context.User.Id == _player2Id)
            {
                AddToColumn(_player2Board, column);
                RemoveFromColumn(_player1Board, column);

                if (!HasOpenSlots(_player2Board))
                    CalculateResults();
                else
                {
                    _turn = 1;
                    RollDice();
                    DisableButtonsIfFull(_player1Board);
                }
            }
        }

        private void DisableButtonsIfFull(Dice?[] board)
        {
            if (_column1 != null)
                _column1.Disabled = IsColumnFull(board, 0);
            if (_column2 != null)
                _column2.Disabled = IsColumnFull(board, 1);
            if (_column3 != null)
                _column3.Disabled = IsColumnFull(board, 2);
        }

        private void RollDice()
        {
            var rng = new Random();
            _activeDice = new Dice()
            {
                Value = rng.GetItems(Enum.GetValues<DiceNumber>(), 1).First()
            };
        }

        private void AddToColumn(Dice?[] board, int column)
        {
            for (var i = column * 3; i < 3 + (3*column); i++)
            {
                if (board[i] == null)
                {
                    board[i] = _activeDice;
                    break;
                }
            }
        }

        private void RemoveFromColumn(Dice?[] otherBoard, int column)
        {
            var j = -1;
            for (var i = column * 3; i < 3 + (3 * column); i++)
            {
                if (otherBoard[i]?.Value == _activeDice.Value)
                {
                    otherBoard[i] = null;
                    if(j == -1)
                        j = i;
                }
                else if (otherBoard[i] != null && j != -1)
                {
                    otherBoard[j] = otherBoard[i];
                    otherBoard[i] = null;
                    j = i;
                }
            }
        }

        private void CalculateResults()
        {
            var player1Score = CalculateBoard(_player1Board);
            var player2Score = CalculateBoard(_player2Board);
            if (player1Score > player2Score)
                GameOver(GameStatus.Won);
            else if (player2Score > player1Score)
                GameOver(GameStatus.Lost);
            else
                GameOver(GameStatus.Tie);
        }

        private static int CalculateBoard(Dice?[] board)
        {
            var total = 0;
            for(var column = 0; column < 3; column++)
            {
                total += CalculateColumn(board, column);
            }
            return total;
        }

        private static int CalculateColumn(Dice?[] board, int column)
        {
            var total = 0;
            for (var i = column * 3; i < 3 + (3 * column); i++)
            {
                if (board[i] != null)
                {
                    var value = (int)(board[i]?.Value ?? 0);
                    total += value * CountSameInColumn(board, column, value);
                }
            }
            return total;
        }

        private static int CountSameInColumn(Dice?[] board, int column, int value)
        {
            var count = 0;
            for (var i = column * 3; i < 3 + (3 * column); i++)
            {
                var val = (int)(board[i]?.Value ?? 0);
                if (value == val)
                    count++;
            }
            return count;
        }

        private static bool IsColumnFull(Dice?[] board, int column)
        {
            var foundEmpty = false;
            for (var i = column * 3; i < 3 + (3 * column); i++)
            {
                if (board[i] == null)
                {
                    foundEmpty = true;
                    break;
                }
            }

            return !foundEmpty;
        }

        private void GameOver(GameStatus status)
        {
            if (status == GameStatus.Won)
            {
                Awards.Add(new PlayerAward(_userId, _bet, 20, 1, 0));
                Awards.Add(new PlayerAward(_player2Id, -_bet, 10, 0, 1));
            }
            if (status == GameStatus.Lost)
            {
                Awards.Add(new PlayerAward(_player2Id, _bet, 20, 1, 0));
                Awards.Add(new PlayerAward(_userId, -_bet, 10, 0, 1));
            }
            if (status == GameStatus.Tie)
            {
                Awards.Add(new PlayerAward(_userId, 0, 10, 0, 0));
                Awards.Add(new PlayerAward(_player2Id, 0, 10, 0, 0));
            }

            Status = status;

            Buttons.Clear();

            var playAgainButton = new MessageButton("Play again?", ButtonStyle.Secondary);
            playAgainButton.OnPress += PlayAgain;
            Buttons.Add(playAgainButton);
        }

        private static bool HasOpenSlots(Dice?[] board)
        {
            return board.Any(d => d == null);
        }

        private static string DiceStrValue(Dice? die)
        {
            if (die == null)
                return "/";
            else
                return die.ToString();
        }

        private string GetGameWindow()
        {
            var sb = new StringBuilder();
            if (Status == GameStatus.Pending)
            {
                sb.AppendLine($"{_username} has challened {_player2Username}!");
                sb.AppendLine("Game: Knucklebones");
                sb.AppendLine($"Bet: {_bet} stars");
                sb.AppendLine();
                sb.AppendLine($"<@{_player2Id}>, do you accept?");
            }
            else if(Status == GameStatus.Rejected)
            {
                sb.AppendLine($"{_username} has challened {_player2Username}!");
                sb.AppendLine("Game: Knucklebones");
                sb.AppendLine($"Bet: {_bet} stars");
                sb.AppendLine();
                sb.AppendLine($"<@{_player2Id}> has rejected!");
            }
            else
            {
                var yourTurn = _turn == 1 ? " - Your turn!" : "";
                sb.AppendLine($"```{_username}'s Board{yourTurn}");
                sb.AppendLine($"[{DiceStrValue(_player1Board[2])}] [{DiceStrValue(_player1Board[5])}] [{DiceStrValue(_player1Board[8])}]");
                sb.Append($"[{DiceStrValue(_player1Board[1])}] [{DiceStrValue(_player1Board[4])}] [{DiceStrValue(_player1Board[7])}]");
                sb.AppendLine($"   Total: {CalculateBoard(_player1Board)}");
                sb.AppendLine($"[{DiceStrValue(_player1Board[0])}] [{DiceStrValue(_player1Board[3])}] [{DiceStrValue(_player1Board[6])}]");
                sb.AppendLine($"{CalculateColumn(_player1Board, 0).ToString().PadLeft(3)} {CalculateColumn(_player1Board, 1).ToString().PadLeft(3)} {CalculateColumn(_player1Board, 2).ToString().PadLeft(3)}");
                sb.AppendLine();
                yourTurn = _turn == 2 ? " - Your turn!" : "";
                sb.AppendLine($"{_player2Username}'s Board{yourTurn}");
                sb.AppendLine($"[{DiceStrValue(_player2Board[2])}] [{DiceStrValue(_player2Board[5])}] [{DiceStrValue(_player2Board[8])}]");
                sb.Append($"[{DiceStrValue(_player2Board[1])}] [{DiceStrValue(_player2Board[4])}] [{DiceStrValue(_player2Board[7])}]");
                sb.AppendLine($"   Total: {CalculateBoard(_player2Board)}");
                sb.AppendLine($"[{DiceStrValue(_player2Board[0])}] [{DiceStrValue(_player2Board[3])}] [{DiceStrValue(_player2Board[6])}]");
                sb.AppendLine($"{CalculateColumn(_player2Board, 0).ToString().PadLeft(3)} {CalculateColumn(_player2Board, 1).ToString().PadLeft(3)} {CalculateColumn(_player2Board, 2).ToString().PadLeft(3)}");
                sb.AppendLine();
                sb.AppendLine($"Active Dice: [{_activeDice}]```");

                var result = string.Empty;
                if (Status == GameStatus.Won)
                {
                    result = $"{_username} won! They gained **{_bet}** stars and **20** XP!\n";
                    result += $"{_player2Username} lost. They lost **{_bet}** stars and gained **10** XP!";
                }

                if (Status == GameStatus.Lost)
                {
                    result = $"{_player2Username} won! They gained **{_bet}** stars and **20** XP!\n";
                    result += $"{_username} lost. They lost **{_bet}** stars and gained **10** XP!";
                }
                if (Status == GameStatus.Tie)
                    result = $"Tie! You both gained **10** XP!";

                if (!string.IsNullOrWhiteSpace(result))
                {
                    sb.AppendLine("**Result**");
                    sb.AppendLine(result);
                }
            }

            if (Expired)
                sb.AppendLine("\nThis game has expired!");

            return sb.ToString();
        }   
    }
}
