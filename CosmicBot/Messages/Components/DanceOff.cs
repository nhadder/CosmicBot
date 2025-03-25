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
        public List<DanceBattleMember> Dancers = new List<DanceBattleMember>();
        private List<DanceBattleMember> _survivors = new List<DanceBattleMember>();
        private DanceBattleMember? _player1;
        private DanceBattleMember? _player2;
        private DanceBattleMember? _winner;
        private DateTime? _startTime;
        public GameStatus Status = GameStatus.Pending;
        private string _startingGifUrl;
        public DanceOff(List<DanceBattleMember>? previousJoiners, DateTime? start) : base(null, true) 
        {
            if (previousJoiners != null && previousJoiners.Count > 0)
            {
                Dancers.AddRange(previousJoiners);
            }
            var button = new MessageButton("Join Dance Battle", ButtonStyle.Success);
            button.OnPress += Join;
            Buttons.Add(button);

            if (start == null)
            {
                _startTime = null;
                var startButton = new MessageButton("Start", ButtonStyle.Secondary);
                startButton.OnPress += Start;
                Buttons.Add(startButton);
            }
            else
                _startTime = start;

            _startingGifUrl = Task.Run(async () => await TenorGifFetcher.GetRandomGifUrl("dance battle")).Result;
        }

        private MessageResponse? Start(IInteractionContext context)
        {
            Task.Run(async () => await context.Interaction.DeferAsync()).Wait();
            if (context != null)
            {
                while (!Expired)
                {
                    Task.Run(async () => {
                        await Next(context.Channel);
                        await UpdateAsync(context.Client);
                        await Task.Delay(TimeSpan.FromMinutes(1));
                    }).Wait();
                }
            }
            return null;
        }

        public async Task Next(IMessageChannel channel)
        {
            if(Status == GameStatus.Pending)
            {
                if (_startTime == null || DateTime.UtcNow > _startTime)
                {
                    if (Dancers.Count < 2)
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
                if (_survivors.Count == 1 && Dancers.Count == 0)
                    await GameOver(GameStatus.Won, channel);
                else
                    await PickOpponents(channel);
            }
        }

        private async Task PickOpponents(IMessageChannel channel)
        {
            var rng = new Random();
            if (Dancers.Count <= 1)
            {
                Dancers.AddRange(_survivors);
                _survivors.Clear();
            }

            Dancers = Dancers.OrderBy(_ => rng.Next()).ToList();

            if (Dancers.Count < 2) return;

            _player1 = Dancers[0];
            _player2 = Dancers[1];

            _survivors.Add(_player1);
            Dancers.RemoveAt(0);
            Dancers.RemoveAt(0);

            var randomGif = Task.Run(async () => await TenorGifFetcher.GetRandomGifUrl("party dance")).Result;
            var embedBuilder1 = new EmbedBuilder()
                .WithAuthor(new EmbedAuthorBuilder()
                    .WithUrl(_player1.ImageUrl)
                    .WithName($"{_player1.Name} won!")
                    .WithIconUrl(_player1.ImageUrl))
                .WithDescription($"<@{_player1.UserId}> {_winnerMessages.OrderBy(_ => rng.Next()).First()}")
                .WithImageUrl(randomGif);

            var randomGif2 = Task.Run(async () => await TenorGifFetcher.GetRandomGifUrl("bad dance")).Result;
            var embedBuilder2 = new EmbedBuilder()
                .WithAuthor(new EmbedAuthorBuilder()
                    .WithUrl(_player2.ImageUrl)
                    .WithName($"{_player2.Name} lost")
                    .WithIconUrl(_player2.ImageUrl))
                .WithDescription($"<@{_player2.UserId}> {_loseMessages.OrderBy(_ => rng.Next()).First()}")
                .WithImageUrl(randomGif2);

            await channel.SendMessageAsync(embeds: [embedBuilder1.Build(), embedBuilder2.Build()]);
            await channel.TriggerTypingAsync();
        }

        private async Task GameOver(GameStatus status, IMessageChannel channel)
        {
            if (status == GameStatus.Won)
            {
                _winner = _survivors.First();
                Awards.Add(new PlayerAward(_winner.UserId, 10_000, 10_000, 1, 0));
                var embedBuilder = new EmbedBuilder()
                    .WithTitle($"Dance Battle - Winner {_winner.Name}")
                    .WithImageUrl(_winner.ImageUrl)
                    .WithDescription($"<@{_winner.UserId}> was the last one standing!\n They won **10,000** stars and **10,000** XP!");

                await channel.SendMessageAsync(embed: embedBuilder.Build());
                await channel.SendMessageAsync($"<@{_winner.UserId}>");
            }
            Buttons.Clear();
            Expired = true;
            Status = status;
        }

        private MessageResponse? Join(IInteractionContext context)
        {
            if (!Dancers.Exists(d => d.UserId == context.User.Id))
            {
                Dancers.Add(new DanceBattleMember
                {
                    GuildId = context.Guild.Id,
                    UserId = context.User.Id,
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
                if (_startTime != null)
                {
                    var timeLeft = (TimeSpan)(_startTime - DateTime.UtcNow);
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
                            .WithValue($"{Dancers.Count}"));
                    return [embedBuilder.Build()];
                }
                else
                {
                    var embedBuilder = new EmbedBuilder()
                        .WithTitle("Dance Battle")
                        .WithDescription(sb.ToString())
                        .WithImageUrl(_startingGifUrl)
                        .WithFields(new EmbedFieldBuilder()
                            .WithName("Prize")
                            .WithValue($"**10,000** stars and **10,000** XP"),
                        new EmbedFieldBuilder()
                            .WithName("Dancers Joined")
                            .WithValue($"{Dancers.Count}"));
                    return [embedBuilder.Build()];
                }
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
