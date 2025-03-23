using CosmicBot.DiscordResponse;
using CosmicBot.Helpers;
using CosmicBot.Models;
using CosmicBot.Models.Enums;
using Discord;
using System.Text;

namespace CosmicBot.Messages.Components
{
    public class DanceOff : EmbedMessage
    {
        private class UserInfo
        {
            public ulong Id { get; set; }
            public string Name { get; set; } = string.Empty;
            public string ImageUrl { get; set; } = string.Empty;
        }

        private List<UserInfo> _dancers = new List<UserInfo>();
        private List<UserInfo> _survivors = new List<UserInfo>();
        private UserInfo? _player1;
        private UserInfo? _player2;
        private UserInfo? _winner;
        private DateTime _startTime;
        private GameStatus Status = GameStatus.Pending;
        private string _startingGifUrl;
        private IDisposable? _typing;
        public DanceOff() : base(null, true) 
        {
            var button = new MessageButton("Join Dance Battle", ButtonStyle.Success);
            button.OnPress += Join;
            Buttons.Add(button);
            _startingGifUrl = Task.Run(async () => await TenorGifFetcher.GetRandomGifUrl("dance battle")).Result;
            _startTime = DateTime.UtcNow.AddMinutes(3);
        }

        public async Task Next(IMessageChannel channel)
        {
            if(Status == GameStatus.Pending)
            {
                if (DateTime.UtcNow > _startTime)
                {
                    if (_dancers.Count < 2)
                        await GameOver(GameStatus.Rejected, channel);
                    else
                    {
                        Status = GameStatus.InProgress;
                        Buttons.Clear();
                        await PickOpponents(channel);
                    }
                }
            } 
            else if (Status == GameStatus.InProgress)
            {
                if (_survivors.Count == 1 && _dancers.Count <= 1)
                    await GameOver(GameStatus.Won, channel);
                else
                    await PickOpponents(channel);
            }
        }

        private async Task PickOpponents(IMessageChannel channel)
        {
            var rng = new Random();
            if (_dancers.Count <= 1)
            {
                _dancers.AddRange(_survivors);
                _survivors.Clear();
            }

            _dancers = _dancers.OrderBy(_ => rng.Next()).ToList();

            if (_dancers.Count < 2) return;

            _player1 = _dancers[0];
            _player2 = _dancers[1];

            _survivors.Add(_player1);
            _dancers.RemoveAt(0);
            _dancers.RemoveAt(0);

            var randomGif = Task.Run(async () => await TenorGifFetcher.GetRandomGifUrl("party dance")).Result;
            var embedBuilder1 = new EmbedBuilder()
                .WithAuthor(new EmbedAuthorBuilder()
                    .WithUrl(_player1.ImageUrl)
                    .WithName($"{_player1.Name} won!")
                    .WithIconUrl(_player1.ImageUrl))
                .WithDescription($"<@{_player1.Id}> {_winnerMessages.OrderBy(_ => rng.Next()).First()}")
                .WithImageUrl(randomGif);

            var randomGif2 = Task.Run(async () => await TenorGifFetcher.GetRandomGifUrl("bad dance")).Result;
            var embedBuilder2 = new EmbedBuilder()
                .WithAuthor(new EmbedAuthorBuilder()
                    .WithUrl(_player2.ImageUrl)
                    .WithName($"{_player2.Name} lost")
                    .WithIconUrl(_player2.ImageUrl))
                .WithDescription($"<@{_player2.Id}> {_loseMessages.OrderBy(_ => rng.Next()).First()}")
                .WithImageUrl(randomGif2);

            await channel.SendMessageAsync(embeds: [embedBuilder1.Build(), embedBuilder2.Build()]);
            _typing = channel.EnterTypingState();
        }

        private async Task GameOver(GameStatus status, IMessageChannel channel)
        {
            if (status == GameStatus.Won)
            {
                _winner = _survivors.First();
                Awards.Add(new PlayerAward(_winner.Id, 10_000, 10_000, 1, 0));
                var embedBuilder = new EmbedBuilder()
                    .WithTitle($"Dance Battle - Winner {_winner.Name}")
                    .WithImageUrl(_winner.ImageUrl)
                    .WithDescription($"<@{_winner.Id}> was the last one standing!\n They won **10,000** stars and **10,000** XP!");

                await channel.SendMessageAsync(embed: embedBuilder.Build());
                await channel.SendMessageAsync($"<@{_winner.Id}>");
            }
            Buttons.Clear();
            Expired = true;
            Status = status;
            if(_typing != null)
                _typing.Dispose();
        }

        private MessageResponse? Join(IInteractionContext context)
        {
            if (!_dancers.Exists(d => d.Id == context.User.Id))
            {
                _dancers.Add(new UserInfo
                {
                    Id = context.User.Id,
                    Name = context.User.GlobalName,
                    ImageUrl = context.User.GetAvatarUrl()
                });
                return new MessageResponse(ephemeral: true, text: "You have joined the dance battle!");
            }
            return new MessageResponse(ephemeral: true, text: "You have already joined.");
        }

        public override Embed[] GetEmbeds()
        {
            if (Status == GameStatus.Rejected)
            {
                return [new EmbedBuilder()
                    .WithTitle("Dance Battle")
                    .WithDescription("Not enough people joined this time...")
                    .Build()];
            }
            else if (Status == GameStatus.Pending)
            {

                var sb = new StringBuilder();
                sb.AppendLine("@everyone Join the party and face off on the dance floor!");
                var timeLeft = _startTime - DateTime.UtcNow;
                var embedBuilder = new EmbedBuilder()
                    .WithTitle("Dance Battle")
                    .WithDescription(sb.ToString())
                    .WithImageUrl(_startingGifUrl)
                    .WithFields(new EmbedFieldBuilder()
                        .WithName("Prize")
                        .WithValue($"**10,000** stars and **10,000** XP"),
                    new EmbedFieldBuilder()
                        .WithName("Time Left To Join")
                        .WithValue($"{timeLeft.Hours} hours {timeLeft.Minutes} minutes"),
                    new EmbedFieldBuilder()
                        .WithName("Dancers Joined")
                        .WithValue($"{_dancers.Count}"));

                return [embedBuilder.Build()];
            }
            else
            {
                return [new EmbedBuilder()
                    .WithTitle("Dance Battle")
                    .WithDescription("The dance battle is underway!")
                    .Build()];
            }
        }

        private List<string> _loseMessages = new List<string>()
        {
            "lost embarssingly!",
            "litteraly **BROKE** their leg...",
            "tripped over their own feet.",
            "couldn't keep up to the beat.",
            "danced like a grandma.",
            "was no match against the master.",
            "lost but had some...*interesting* moves at least."
        };

        private List<string> _winnerMessages = new List<string>()
        {
            "**KILLED** it on the dance floor!",
            "showed off some gnarly moves!",
            "invented a new dance move. It's **SUPER** effective!",
            "shuffled all the way to the top!",
            "set the dance floor on fire!",
            "had some fresh moves. Nobody had seen any dancing this cool before.",
            "*REALLY* knows what they're doing!"
        };
    }   
}
