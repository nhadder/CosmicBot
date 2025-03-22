using CosmicBot.DiscordResponse;
using CosmicBot.Models;
using CosmicBot.Models.Enums;
using Discord;
using System.Text;


namespace CosmicBot.Messages.Components
{
    public class GrooveBattle : EmbedMessage
    {
        private enum AttackType
        {
            Slash,
            Stab,
            Block
        }

        private class AttackResult
        {
            public int Damage { get; set; } = 0;
            public AttackType Attack { get; set; } = AttackType.Slash;
            public bool Crit { get; set; } = false;
        }

        private int _player1Health = 100;
        private int _player2Health = 100;
        private AttackType _player1Attack = AttackType.Slash;
        private AttackType _player2Attack = AttackType.Slash;
        private int _player1Level = 1;
        private int _player2Level = 1;
        private readonly string _username;
        private readonly string _player2Username;
        private readonly string _iconUrl;
        private readonly string _player2IconUrl;
        private readonly ulong _userId;
        private readonly ulong _player2Id;
        private GameStatus Status = GameStatus.Pending;
        private readonly long _bet;
        private int _turn = 0;
        private AttackResult? _player1Result = null;
        private AttackResult? _player2Result = null;

        public GrooveBattle(IInteractionContext context, IUser player2, int player1Level, int player2Level, long bet) : base([context.User.Id, player2.Id])
        {
            _bet = bet;

            _userId = context.User.Id;
            _player2Id = player2.Id;

            _username = context.User.GlobalName;
            _player2Username = player2.GlobalName;

            _iconUrl = context.User.GetAvatarUrl();
            _player2IconUrl = player2.GetAvatarUrl();

            _player1Level = player1Level;
            _player2Level = player2Level;

            var acceptButton = new MessageButton("Accept", ButtonStyle.Success);
            acceptButton.OnPress = Accept;
            Buttons.Add(acceptButton);

            var denyButton = new MessageButton("Deny", ButtonStyle.Danger);
            denyButton.OnPress = Deny;
            Buttons.Add(denyButton);
        }

        private MessageResponse? PlayAgain(IInteractionContext context)
        {
            Status = GameStatus.InProgress;
            _player1Health = 100;
            _player2Health = 100;
            _turn = 1;
            _player1Result = null;
            _player2Result = null;
            AddGameControls();

            return null;
        }

        public override Embed[] GetEmbeds()
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
                    .WithName($"Groove Battle - Bet {_bet} Star(s)").WithIconUrl(activeUrl))
                .WithDescription(GetGameWindow());
            if(Status == GameStatus.InProgress)
                embedBuilder.WithFooter($"{activeUser}'s turn");

            return [embedBuilder.Build()];
        }

        private MessageResponse? Accept(IInteractionContext context)
        {
            if(context.User.Id == _player2Id)
            {
                Status = GameStatus.InProgress;
                _turn = 1;
                AddGameControls();
            }
            return null;
        }

        private MessageResponse? Deny(IInteractionContext context)
        {
            if (context.User.Id == _player2Id)
            {
                Status = GameStatus.Rejected;
                Expired = true;
            }
            return null;
        }

        private void AddGameControls()
        {
            Buttons.Clear();

            var slash = new MessageButton("Slash", ButtonStyle.Danger);
            slash.OnPress = Slash;
            Buttons.Add(slash);

            var stab = new MessageButton("Stab", ButtonStyle.Success);
            stab.OnPress = Stab;
            Buttons.Add(stab);

            var block = new MessageButton("Block", ButtonStyle.Primary);
            block.OnPress = Block;
            Buttons.Add(block);
        }

        private MessageResponse? Slash(IInteractionContext context)
        {
            ColumnAction(context, AttackType.Slash);
            return null;
        }

        private MessageResponse? Stab(IInteractionContext context)
        {
            ColumnAction(context, AttackType.Stab);
            return null;
        }

        private MessageResponse? Block(IInteractionContext context)
        {
            ColumnAction(context, AttackType.Block);
            return null;
        }

        private void ColumnAction(IInteractionContext context, AttackType attack)
        {
            if (_turn == 1 && context.User.Id == _userId)
            {
                _player1Attack = attack;
                _player1Result = null;
                _player2Result = null;
                _turn = 2;
            }
            else if (_turn == 2 && context.User.Id == _player2Id)
            {
                _player2Attack = attack;
                TurnOver();
                _turn = 1;
            }
        }
        
        private static AttackResult GetAttackResult(AttackType attack, AttackType other, int levelDifference)
        {
            var rng = new Random();
            var same = attack == other;
            int damage = 0;
            if(attack == AttackType.Slash && other != AttackType.Stab && !same)
            {
                damage = Math.Max(10, 10 + rng.Next(Math.Max(10, levelDifference)));
            }
            if (attack == AttackType.Stab && other != AttackType.Block)
            {
                damage = Math.Max(5, 5 + rng.Next(Math.Max(5, levelDifference)));
            }
            if(attack == AttackType.Block)
            {
                if (same)
                    damage = 1;
            }

            var crit = false;
            if ((attack == AttackType.Stab || (attack == AttackType.Slash && !same)) && rng.Next(100) <= 10)
            {
                crit = true;
                damage += 1;
                damage *= 2;
            }

            return new AttackResult()
            {
                Attack = attack,
                Crit = crit,
                Damage = damage
            };
        }

        private void TurnOver()
        {
            _player1Result = GetAttackResult(_player1Attack, _player2Attack, _player1Level - _player2Level);
            _player2Result = GetAttackResult(_player2Attack, _player1Attack, _player2Level - _player1Level);
            _player1Health -= _player2Result.Damage;
            _player2Health -= _player1Result.Damage;

            if (_player1Health < 0)
                _player1Health = 0;

            if (_player2Health < 0)
                _player2Health = 0;

            CalculateResults();
        }

        private void CalculateResults()
        {
            if (_player1Health > 0 && _player2Health <= 0)
                GameOver(GameStatus.Won);
            else if (_player2Health > 0 && _player1Health <= 0)
                GameOver(GameStatus.Lost);
            else if (_player1Health <= 0 && _player2Health <= 0)
                GameOver(GameStatus.Tie);
        }

        private static int CalculateBonusXp(int myLevel, int opponentLevel)
        {
            if (myLevel >= opponentLevel)
                return 1;

            return (opponentLevel - myLevel) * 100;
        }

        private void GameOver(GameStatus status)
        {
            if (status == GameStatus.Won)
            {
                var bonus = CalculateBonusXp(_player1Level, _player2Level);
                Awards.Add(new PlayerAward(_userId, _bet, 20*bonus, 1, 0));
                Awards.Add(new PlayerAward(_player2Id, -_bet, 10, 0, 1));
            }
            if (status == GameStatus.Lost)
            {
                var bonus = CalculateBonusXp(_player2Level, _player1Level);
                Awards.Add(new PlayerAward(_player2Id, _bet, 20*bonus, 1, 0));
                Awards.Add(new PlayerAward(_userId, -_bet, 10, 0, 1));
            }
            if (status == GameStatus.Tie)
            {
                var bonus1 = CalculateBonusXp(_player1Level, _player2Level);
                var bonus2 = CalculateBonusXp(_player2Level, _player1Level);
                Awards.Add(new PlayerAward(_userId, 0, 10*bonus1, 0, 0));
                Awards.Add(new PlayerAward(_player2Id, 0, 10*bonus2, 0, 0));
            }

            Status = status;

            Buttons.Clear();

            var playAgainButton = new MessageButton("Play again?", ButtonStyle.Secondary);
            playAgainButton.OnPress = PlayAgain;
            Buttons.Add(playAgainButton);
        }

        private string GetGameWindow()
        {
            var sb = new StringBuilder();
            if (Status == GameStatus.Pending)
            {
                sb.AppendLine($"{_username} (Lvl. {_player1Level}) has challenged {_player2Username} (Lvl. {_player2Level})!");
                sb.AppendLine("Game: Groove Battle");
                sb.AppendLine($"Bet: {_bet} stars");
                sb.AppendLine();
                sb.AppendLine($"<@{_player2Id}>, do you accept?");
            }
            else if(Status == GameStatus.Rejected)
            {
                sb.AppendLine($"{_username} (Lvl. {_player1Level}) has challenged {_player2Username} (Lvl. {_player2Level})!");
                sb.AppendLine("Game: Groove Battle");
                sb.AppendLine($"Bet: {_bet} stars");
                sb.AppendLine();
                sb.AppendLine($"<@{_player2Id}> has rejected!");
            }
            else
            {
                sb.AppendLine(Rules());
                var player1Hearts = new List<string>();
                for(var i = 10; i > 0; i--)
                {
                    if (_player1Health >= i * 10)
                        player1Hearts.Add("<3 ");
                    else
                        player1Hearts.Add("   ");
                }
                var player2Hearts = new List<string>();
                for (var i = 10; i > 0; i--)
                {
                    if (_player2Health >= i * 10)
                        player2Hearts.Add(" <3");
                    else
                        player2Hearts.Add("   ");
                }
                var p1DamageColumn = new List<string>();
                var damage1ColumnLength = 5;
                if (_player1Result != null)
                {
                    var damageStr = $"-{_player1Result.Damage}";
                    damage1ColumnLength = Math.Max(5, damageStr.Length);
                    p1DamageColumn.Add(_player1Result.Crit 
                        ? "Crit!".PadRight(damage1ColumnLength) 
                        : " ".PadRight(damage1ColumnLength));
                    p1DamageColumn.Add(damageStr.PadRight(damage1ColumnLength));
                }
                else
                {
                    p1DamageColumn.Add("     ");
                    p1DamageColumn.Add("     ");
                }
                var p2DamageColumn = new List<string>();
                var damage2ColumnLength = 5;
                if (_player2Result != null)
                {
                    var damageStr = $"-{_player2Result.Damage}";
                    damage2ColumnLength = Math.Max(5, damageStr.Length);
                    p2DamageColumn.Add(_player2Result.Crit
                        ? "Crit!".PadLeft(damage2ColumnLength)
                        : " ".PadLeft(damage2ColumnLength));
                    p2DamageColumn.Add(damageStr.PadLeft(damage2ColumnLength));
                }
                else
                {
                    p2DamageColumn.Add("     ");
                    p2DamageColumn.Add("     ");
                }
                sb.Append("```");

                if (_turn == 1)
                    sb.AppendLine("Your turn".PadRight(2 + damage1ColumnLength + 15 + 1 + 15 + damage2ColumnLength + 2));
                else
                    sb.AppendLine("Your turn".PadLeft(2 + damage1ColumnLength + 15 + 1 + 15 + damage2ColumnLength + 2));

                var Player1Character = Player1CharacterIdle;
                if(_player1Result != null)
                {
                    if (_player1Result.Attack == AttackType.Slash)
                        Player1Character = Player1CharacterSlash;
                    if (_player1Result.Attack == AttackType.Stab)
                        Player1Character = Player1CharacterStab;
                    if (_player1Result.Attack == AttackType.Block)
                        Player1Character = Player1CharacterBlock;
                }

                var Player2Character = Player2CharacterIdle;
                if (_player2Result != null)
                {
                    if (_player2Result.Attack == AttackType.Slash)
                        Player2Character = Player2CharacterSlash;
                    if (_player2Result.Attack == AttackType.Stab)
                        Player2Character = Player2CharacterStab;
                    if (_player2Result.Attack == AttackType.Block)
                        Player2Character = Player2CharacterBlock;
                }

                sb.Append($"{player1Hearts[0]}{" ".PadRight(damage1ColumnLength)}{$" Lvl. {_player1Level}".PadRight(14)} ");
                sb.AppendLine($"{$" Lvl. {_player2Level}".PadLeft(14)}{" ".PadLeft(damage2ColumnLength)}{player2Hearts[0]}");

                sb.Append($"{player1Hearts[1]}{" ".PadRight(damage1ColumnLength)}{Player1Character[0]} ");
                sb.AppendLine($"{Player2Character[0]}{" ".PadLeft(damage2ColumnLength)}{player2Hearts[1]}");

                sb.Append($"{player1Hearts[2]}{p2DamageColumn[0]}{Player1Character[1]} ");
                sb.AppendLine($"{Player2Character[1]}{p1DamageColumn[0]}{player2Hearts[2]}");

                sb.Append($"{player1Hearts[3]}{p2DamageColumn[1]}{Player1Character[2]} ");
                sb.AppendLine($"{Player2Character[2]}{p1DamageColumn[1]}{player2Hearts[3]}");

                sb.Append($"{player1Hearts[4]}{" ".PadRight(damage1ColumnLength)}{Player1Character[3]} ");
                sb.AppendLine($"{Player2Character[3]}{" ".PadLeft(damage2ColumnLength)}{player2Hearts[4]}");

                sb.Append($"{player1Hearts[5]}{" ".PadRight(damage1ColumnLength)}{Player1Character[4]} ");
                sb.AppendLine($"{Player2Character[4]}{" ".PadLeft(damage2ColumnLength)}{player2Hearts[5]}");

                sb.Append($"{player1Hearts[6]}{" ".PadRight(damage1ColumnLength)}{Player1Character[5]} ");
                sb.AppendLine($"{Player2Character[5]}{" ".PadLeft(damage2ColumnLength)}{player2Hearts[6]}");

                sb.Append($"{player1Hearts[7]}{" ".PadRight(damage1ColumnLength)}{Player1Character[6]} ");
                sb.AppendLine($"{Player2Character[6]}{" ".PadLeft(damage2ColumnLength)}{player2Hearts[7]}");

                sb.Append($"{player1Hearts[8]}{" ".PadRight(damage1ColumnLength)}{Player1Character[7]} ");
                sb.AppendLine($"{Player2Character[7]}{" ".PadLeft(damage2ColumnLength)}{player2Hearts[8]}");

                sb.Append($"{player1Hearts[9]}{" ".PadRight(damage1ColumnLength)}{Player1Character[8]} ");
                sb.AppendLine($"{Player2Character[8]}{" ".PadLeft(damage2ColumnLength)}{player2Hearts[9]}");


                sb.Append($"{_player1Health.ToString().PadRight(3)}{" ".PadRight(damage1ColumnLength)}{_username.PadRight(14)} ");
                sb.AppendLine($"{_player2Username.PadLeft(14)}{" ".PadLeft(damage2ColumnLength)}{_player2Health.ToString().PadLeft(3)}");

                if (_player1Result != null)
                    sb.Append(_player1Result.Attack.ToString().PadLeft(3 + damage1ColumnLength + 14) + "|");
                if (_player2Result != null)
                    sb.AppendLine(_player2Result.Attack.ToString().PadRight(14 + damage2ColumnLength + 3));
                sb.AppendLine(GetTurnUpdate());
                sb.Append("```");
                var result = string.Empty;
                if (Status == GameStatus.Won)
                {
                    result = $"{_username} won! They gained **{_bet}** stars and **{20*CalculateBonusXp(_player1Level, _player2Level)}** XP!\n";
                    result += $"{_player2Username} lost. They lost **{_bet}** stars and gained **10** XP!";
                }

                if (Status == GameStatus.Lost)
                {
                    result = $"{_player2Username} won! They gained **{_bet}** stars and **{20 * CalculateBonusXp(_player2Level, _player1Level)}** XP!\n";
                    result += $"{_username} lost. They lost **{_bet}** stars and gained **10** XP!";
                }
                if (Status == GameStatus.Tie)
                    result = $"Tie!\n{_username} gained {10*CalculateBonusXp(_player1Level, _player2Level)} XP!\n{_player2Username} gained {10 * CalculateBonusXp(_player2Level, _player1Level)} XP!";

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

        private static string Rules()
        {
            return @"**Rules:** 
Slash > Block, Stab > Slash, Block > Stab
Both Block - take 1 dmg each
Both Slash - Nothing happens
Both Stab - take lesser dammage each";
        }

        private string GetTurnUpdate()
        {
            if (_player1Result == null || _player2Result == null)
                return "";

            if (_player1Result.Attack == AttackType.Slash && _player2Result.Attack == AttackType.Slash)
                return "Clank! Nothing happens.";
            if (_player1Result.Attack == AttackType.Stab && _player2Result.Attack == AttackType.Block)
                return $"{_username}'s stab was blocked by {_player2Username}'s block!";
            if (_player1Result.Attack == AttackType.Block && _player2Result.Attack == AttackType.Stab)
                return $"{_player2Username}'s stab was blocked by {_username}'s block!";
            if (_player1Result.Attack == AttackType.Slash && _player2Result.Attack == AttackType.Block)
                return $"{_username}'s slash went around {_player2Username}'s block!";
            if (_player1Result.Attack == AttackType.Block && _player2Result.Attack == AttackType.Slash)
                return $"{_player2Username}'s slash went around {_username}'s block!";
            if (_player1Result.Attack == AttackType.Block && _player2Result.Attack == AttackType.Block)
                return $"You both shield bash each other for 1 damage!";
            if (_player1Result.Attack == AttackType.Stab && _player2Result.Attack == AttackType.Stab)
                return $"You both stab each other!";
            if (_player1Result.Attack == AttackType.Slash && _player2Result.Attack == AttackType.Stab)
                return $"{_player2Username} stabbed {_username}!";
            if (_player1Result.Attack == AttackType.Stab && _player2Result.Attack == AttackType.Slash)
                return $"{_username} stabbed {_player2Username}!";
            return "";
        }

        private static List<string> Player1CharacterIdle = new List<string>()
        {
           @"    ~~~~  ~   ",
           @"   /  ~~~~    ",
           @"  | -_- |     ",
           @"   \---/      ",
           @"  /` |`\   __ ",
           @" -\+-|--\/|> |",
           @"     |    |  |",
           @"    / \    \/ ",
           @"   /   \      ",
        };

        private static List<string> Player1CharacterBlock = new List<string>()
        {
           @"    ~~~~  ~   ",
           @"   /  ~~~~    ",
           @"  | >_< |     ",
           @"   \---/   __ ",
           @"  /` |`- /|  |",
           @"-/+-|---> |  |",
           @"     |     \/ ",
           @"    / \       ",
           @"   /   \      ",
        };

        private static List<string> Player1CharacterStab = new List<string>()
        {
           @"    ~~~~  ~   ",
           @"   /  ~~~~    ",
           @"  | -_- |     ",
           @"   \---/      ",
           @"   `\|`\      ",
           @"    -|\+\---->",
           @"     |  /--\  ",
           @"    / \ |  |  ",
           @"   /   \ \/   ",
        };

        private static List<string> Player1CharacterSlash = new List<string>()
        {
           @"    ~~~~  ~ ^ ",
           @"   /  ~~~~ /  ",
           @"  | >_> | -   ",
           @"   \---/ /    ",
           @"  /` |`-      ",
           @"  \  |/ \  __ ",
           @"   -+|   \|  |",
           @"    / \   |  |",
           @"   /   \   \/ ",
        };

        private static List<string> Player2CharacterIdle = new List<string>()
        {
           @"       _t_    ",
           @"     ~/_~_\~  ",
           @"     |  v  |  ",
           @"      \---/   ",
           @" __   /`| `\  ",
           @"| <|\/--|-+/- ",
           @"|  |    |     ",
           @" \/    / \    ",
           @"      /   \   ",
        };

        private static List<string> Player2CharacterBlock = new List<string>()
        {
           @"       _t_    ",
           @"     ~/> <\~  ",
           @"     |  o  |  ",
           @" --   \---/   ",
           @"|  |\ -`| `\  ",
           @"|  |  <-|--+\-",
           @" \/     |     ",
           @"       / \    ",
           @"      /   \   ",
        };

        private static List<string> Player2CharacterStab = new List<string>()
        {
           @"       _t_    ",
           @"     ~/_~_\~  ",
           @"     |  >  |  ",
           @"      \---/   ",
           @"      /`|/`   ",
           @"<----/+/|-    ",
           @" /--\   |     ",
           @" |  |  / \    ",
           @"  \/  /   \   ", 
        };

        private static List<string> Player2CharacterSlash = new List<string>()
        {
           @"  ^    _t_    ",
           @"   \ ~/>v<\~  ",
           @"    -|  x  |  ",
           @"     \\---/   ",
           @"      -`| `\  ",
           @" __  / \|  /  ",
           @"|  |    |+-   ",
           @"|  |   / \    ",
           @" \/   /   \   ",
        };
    }
}
