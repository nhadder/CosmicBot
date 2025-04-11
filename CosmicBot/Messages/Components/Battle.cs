using CosmicBot.DiscordResponse;
using CosmicBot.Models;
using CosmicBot.Models.Enums;
using Discord;
using System.Text;


namespace CosmicBot.Messages.Components
{
    public class Battle : EmbedMessage
    {
        private enum AttackType
        {
            Slash,
            Stab,
            Block,
            Rest
        }

        private class AttackResult
        {
            public int Damage { get; set; } = 0;
            public AttackType Attack { get; set; } = AttackType.Slash;
            public bool Crit { get; set; } = false;
            public bool Stunned { get; set; } = false;
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

        public Battle(IInteractionContext context, IUser player2, int player1Level, int player2Level, long bet) : base([context.User.Id, player2.Id])
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
                    .WithName($"Battle - Bet {_bet} Star(s)").WithIconUrl(activeUrl))
                .WithDescription(GetGameWindow());
            if(Status == GameStatus.InProgress)
                embedBuilder.WithFooter($"{activeUser}'s turn");

            return [embedBuilder.Build()];
        }

        private MessageResponse? Accept(IInteractionContext context)
        {
            if (context.User.Id == _player2Id)
            {
                Status = GameStatus.InProgress;
                _turn = 1;
                AddGameControls();
            }
            else
                return new MessageResponse("You can not accept your own game invitation...", ephemeral: true);

            return null;
        }

        private MessageResponse? Deny(IInteractionContext context)
        {
            if (context.User.Id == _player2Id)
            {
                Status = GameStatus.Rejected;
                Expired = true;
            }
            else
                return new MessageResponse("You can not deny your own game invitation...", ephemeral: true);

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
            return ColumnAction(context, AttackType.Slash);
        }

        private MessageResponse? Stab(IInteractionContext context)
        {
            return ColumnAction(context, AttackType.Stab);
        }

        private MessageResponse? Block(IInteractionContext context)
        {
            return ColumnAction(context, AttackType.Block);
        }

        private MessageResponse? ColumnAction(IInteractionContext context, AttackType attack)
        {
            if (_turn == 1 && context.User.Id == _userId)
            {
                _player1Attack = attack;
                if (_player2Result != null && _player2Result.Stunned)
                {
                    _player2Attack = AttackType.Rest;
                    TurnOver();
                }
                else
                    _turn = 2;

                return null;
            }
            else if (_turn == 2 && context.User.Id == _player2Id)
            {
                _player2Attack = attack;
                TurnOver();
                if (_player1Result != null && !_player1Result.Stunned)
                    _turn = 1;
                else
                {
                    _player1Attack = AttackType.Rest;
                    _turn = 2;
                }

                return null;
            }
            else
                return new MessageResponse("It is not your turn yet...", ephemeral: true);
        }
        
        private static AttackResult GetAttackResult(AttackType attack, AttackType other, int levelDifference)
        {
            var rng = new Random();
            var same = attack == other;
            int damage = 0;
            bool stunned = false;
            if(attack == AttackType.Slash && other != AttackType.Stab)
            {
                damage = 15;
            }
            if (attack == AttackType.Stab)
            {
                if (other != AttackType.Block)
                    damage = 10;
                else
                    stunned = true;
            }
            if(attack == AttackType.Block && same)
            {
                damage = 5;
            }

            var crit = false;
            if (damage > 0 && rng.Next(100) <= 10)
            {
                crit = true;
                damage += 5;
            }

            return new AttackResult()
            {
                Attack = attack,
                Crit = crit,
                Damage = damage,
                Stunned = stunned
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

        private void GameOver(GameStatus status)
        {
            if (status == GameStatus.Won)
            {
                Awards.Add(new PlayerAward(_userId, _bet, 500, 1, 0));
                Awards.Add(new PlayerAward(_player2Id, -_bet, 100, 0, 1));
            }
            if (status == GameStatus.Lost)
            {
                Awards.Add(new PlayerAward(_player2Id, _bet, 500, 1, 0));
                Awards.Add(new PlayerAward(_userId, -_bet, 100, 0, 1));
            }
            if (status == GameStatus.Tie)
            {
                Awards.Add(new PlayerAward(_userId, 0, 250, 0, 0));
                Awards.Add(new PlayerAward(_player2Id, 0, 250, 0, 0));
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
                sb.AppendLine($"{_username} has challenged {_player2Username}!");
                sb.AppendLine("Game: Battle");
                sb.AppendLine($"Bet: {_bet} stars");
                sb.AppendLine();
                sb.AppendLine($"<@{_player2Id}>, do you accept?");
            }
            else if(Status == GameStatus.Rejected)
            {
                sb.AppendLine($"{_username} has challenged {_player2Username}!");
                sb.AppendLine("Game: Battle");
                sb.AppendLine($"Bet: {_bet} stars");
                sb.AppendLine();
                sb.AppendLine($"<@{_player2Id}> has rejected!");
            }
            else
            {          
                sb.Append("```");
                var player1Length = Math.Max(10, _username.Length + 2);
                var player2Length = Math.Max(10, _player2Username.Length + 2);
                sb.Append($"Lvl. {_player1Level}".PadRight(player1Length)); sb.AppendLine($"Lvl. {_player2Level}".PadLeft(player2Length));
                if (_turn == 1)
                {
                    sb.Append($">{_username}<".PadRight(player1Length)); sb.AppendLine($" {_player2Username} ".PadLeft(player2Length));
                }
                else
                {
                    sb.Append($" {_username} ".PadRight(player1Length)); sb.AppendLine($">{_player2Username}<".PadLeft(player2Length));
                }
                sb.AppendLine();
                sb.Append($"[={_player1Health}=]".PadRight(player1Length)); sb.AppendLine($"[={_player2Health}=]".PadLeft(player2Length));
                var player1Frame = GetFrame(1, _player1Result?.Attack ?? null);
                var player2Frame = GetFrame(2, _player2Result?.Attack ?? null);

                for (var i = 0; i < player1Frame.Count; i++)
                {
                    sb.Append(player1Frame[i].PadRight(player1Length)); sb.AppendLine(player2Frame[i].PadLeft(player2Length));
                }
                sb.Append((_player1Result?.Attack.ToString() ?? string.Empty).PadRight(player1Length)); sb.AppendLine((_player2Result?.Attack.ToString() ?? string.Empty).PadLeft(player2Length));
                var status1 = (_player2Result?.Crit ?? false ? "Crit!" : string.Empty) + (_player1Result?.Stunned ?? false ? "Stunned!" : string.Empty);
                var status2 = (_player1Result?.Crit ?? false ? "Crit!" : string.Empty) + (_player2Result?.Stunned ?? false ? "Stunned!" : string.Empty);
                sb.Append(status1.PadRight(player1Length)); sb.AppendLine(status2.PadLeft(player2Length));
                sb.Append(("-" + _player2Result?.Damage.ToString() ?? string.Empty).PadRight(player1Length)); sb.AppendLine(("-" + _player1Result?.Damage.ToString() ?? string.Empty).PadLeft(player2Length));
                sb.AppendLine(GetTurnUpdate());
                sb.Append("```");
                var result = string.Empty;
                if (Status == GameStatus.Won)
                {
                    result = $"{_username} won! They gained **{_bet}** stars and **{500}** XP!\n";
                    result += $"{_player2Username} lost. They lost **{_bet}** stars and gained **{100}** XP!";
                }

                if (Status == GameStatus.Lost)
                {
                    result = $"{_player2Username} won! They gained **{_bet}** stars and **{500}** XP!\n";
                    result += $"{_username} lost. They lost **{_bet}** stars and gained **{100}** XP!";
                }
                if (Status == GameStatus.Tie)
                    result = $"Tie!\n You both gained **{250}** XP!";

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

        private string GetTurnUpdate()
        {
            if (_player1Result == null || _player2Result == null)
                return "";

            if (_player1Result.Attack == AttackType.Slash && _player2Result.Attack == AttackType.Slash)
                return "Clank!\nNothing happens.";
            if (_player1Result.Attack == AttackType.Stab && _player2Result.Attack == AttackType.Block)
                return $"{_player2Username} blocked {_username}'s stab!\n{_username} is stunned!";
            if (_player1Result.Attack == AttackType.Block && _player2Result.Attack == AttackType.Stab)
                return $"{_username} blocked {_player2Username}'s stab!\n{_player2Username} is stunned!";
            if (_player1Result.Attack == AttackType.Slash && _player2Result.Attack == AttackType.Block)
                return $"{_username}'s slash\nwent around {_player2Username}'s block!";
            if (_player1Result.Attack == AttackType.Block && _player2Result.Attack == AttackType.Slash)
                return $"{_player2Username}'s slash\nwent around {_username}'s block!";
            if (_player1Result.Attack == AttackType.Block && _player2Result.Attack == AttackType.Block)
                return $"You bump shields!";
            if (_player1Result.Attack == AttackType.Stab && _player2Result.Attack == AttackType.Stab)
                return $"You both stab each other!";
            if (_player1Result.Attack == AttackType.Slash && _player2Result.Attack == AttackType.Stab)
                return $"{_player2Username} stabbed {_username}!";
            if (_player1Result.Attack == AttackType.Stab && _player2Result.Attack == AttackType.Slash)
                return $"{_username} stabbed {_player2Username}!";
            if (_player1Result.Attack == AttackType.Slash)
                return $"{_username} slashed!";
            if (_player1Result.Attack == AttackType.Stab)
                return $"{_username} stabbed!";
            if (_player1Result.Attack == AttackType.Block)
                return $"{_username} blocked!\n...For some reason";
            if (_player2Result.Attack == AttackType.Slash)
                return $"{_player2Username} slashed!";
            if (_player2Result.Attack == AttackType.Stab)
                return $"{_player2Username} stabbed!";
            if (_player2Result.Attack == AttackType.Block)
                return $"{_player2Username} blocked!\n...For some reason";
            return "";
        }

        private static List<string> GetFrame(int player, AttackType? attack)
        {
            var frame = new List<string>();
            switch(attack)
            {
                case AttackType.Slash: frame = CharacterSlash; break;
                case AttackType.Block: frame = CharacterBlock; break;
                case AttackType.Stab: frame = CharacterStab; break;
                case AttackType.Rest: frame = CharacterStunned; break;
                default: frame = CharacterIdle; break;
            }

            if (player == 2)
                return frame.Select(Reverse).ToList();

            return frame;
        }

        private static string Reverse(string str)
        {
            var newStr = string.Empty;
            foreach (var c in str.ToCharArray())
            {
                var newChar = c;
                switch(c)
                {
                    case '(': newChar = ')'; break;
                    case ')': newChar = '('; break;
                    case '/': newChar = '\\'; break;
                    case '\\': newChar = '/'; break;
                    case '[': newChar = ']'; break;
                    case ']': newChar = '['; break;
                    case '>': newChar = '<'; break;
                    case '<': newChar = '>'; break;
                    default: newChar = c; break;
                }
                newStr = newChar + newStr;
            }
            return newStr;
        }

        private static List<string> CharacterIdle = new List<string>()
        {
           @"   _    ^ ",
           @"  ( )  /  ",
           @"  /|\+    ",
           @"[ ]|      ",
           @" v/ \     ",
           @" /   \    ",
        };

        private static List<string> CharacterBlock = new List<string>()
        {
           @"  _       ",
           @" ( )  ___ ",
           @"+/|--[   ]",
           @" \|   \_/ ",
           @" /\\      ",
           @"|  v|     ",
        };

        private static List<string> CharacterStab = new List<string>()
        {
           @"    _     ",
           @"   ( )    ",
           @"  /|-+--->",
           @"[ ]|      ",
           @" v/ \     ",
           @" /   \    ",
        };

        private static List<string> CharacterSlash = new List<string>()
        {
           @"    _     ",
           @"   ( ) ^  ",
           @"  /|\ ~ `)",
           @"[ ]| -+~`>",
           @" v/ \ ~ ( ",
           @" /   | v` ",
        };

        private static List<string> CharacterStunned = new List<string>()
        {
           @"   _*     ",
           @" *(~)*    ",
           @"  /|\     ",
           @"[ ]|_+    ",
           @" _/  |\   ",
           @"       \  ",
        };
    }
}
