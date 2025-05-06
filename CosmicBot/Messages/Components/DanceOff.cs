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

            _startingGifUrl = Task.Run(async () => await GifFetcher.GetRandomGifUrl("dance battle")).Result;
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

            var randomGif = Task.Run(async () => await GifFetcher.GetRandomGifUrl("dance party")).Result;
            var embedBuilder1 = new EmbedBuilder()
                .WithAuthor(new EmbedAuthorBuilder()
                    .WithUrl(_player1.ImageUrl)
                    .WithName($"{_player1.Name} survived this round!")
                    .WithIconUrl(_player1.ImageUrl))
                .WithDescription($"<@{_player1.UserId}> {_winnerMessages.OrderBy(_ => rng.Next()).First()}")
                .WithImageUrl(randomGif);

            var randomGif2 = Task.Run(async () => await GifFetcher.GetRandomGifUrl("bad dance")).Result;
            var embedBuilder2 = new EmbedBuilder()
                .WithAuthor(new EmbedAuthorBuilder()
                    .WithUrl(_player2.ImageUrl)
                    .WithName($"{_player2.Name} lost...")
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
                Awards.Add(new PlayerAward(_winner.UserId, 100_000, 100_000, 1, 0));
                var embedBuilder = new EmbedBuilder()
                    .WithTitle($"Dance Battle - Winner {_winner.Name}")
                    .WithImageUrl(_winner.ImageUrl)
                    .WithDescription($"<@{_winner.UserId}> was the last one standing!\n They won **100,000** stars and **100,000** XP!");

                var winnerMessage = await channel.SendMessageAsync(embed: embedBuilder.Build());
                await channel.SendMessageAsync($"<@{_winner.UserId}>");

                if (channel != null)
                {
                    var pins = await channel.GetPinnedMessagesAsync();
                    if (pins != null)
                    {
                        var pinsByBot = pins.Where(p => p.Author.IsBot);
                        foreach (var pin in pinsByBot)
                        {
                            if (pin is IUserMessage lastPinnedRecord)
                                await lastPinnedRecord.UnpinAsync();
                        }
                    }
                }

                await winnerMessage.PinAsync();
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
                    var expired = Expired ? "\nThis message has expired" : string.Empty;
                    var embedBuilder = new EmbedBuilder()
                        .WithTitle("Dance Battle")
                        .WithDescription(sb.ToString() + expired)
                        .WithImageUrl(_startingGifUrl)
                        .WithFields(new EmbedFieldBuilder()
                            .WithName("Prize")
                            .WithValue($"**100,000** stars and **100,000** XP"),
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
                            .WithValue($"**100,000** stars and **100,000** XP"),
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
            "literaly **BROKE** their leg...",
            "tripped over their own feet.",
            "couldn't keep up to the beat.",
            "danced like a grandma.",
            "was no match against the master.",
            "lost but had some...*interesting* moves at least.",
            "at least tried their best...",
            "was booed off the dance floor",
            "needs to work on that move some more...",
            "made the crowd disappear and now the bar in the back is too crowded",
            "made everyone question the definition of \"dancing\"",
            "forgot what rhythm even is.",
            "*accidentally* did the worm... backwards.",
            "got tangled in their own shoelaces.",
            "thought it was a yoga competition instead.",
            "hit themselves with their own elbow somehow.",
            "moved like a broken robot with low battery.",
            "*panicked* and started flossing.",
            "just stood there... blinking.",
            "looked like they were being chased by bees.",
            "didn't dance — they just *vibrated*.",
            "tried to twerk. The floor cracked.",
            "turned the vibe into a funeral.",
            "caused the DJ to *turn off the music*.",
            "got caught in a spin and never recovered.",
            "performed the *invisible dance* — no one saw it.",
            "moonwalked off the stage... into the wall.",
            "forgot which way was up.",
            "*screamed* their way through the routine.",
            "made someone drop their drink out of secondhand embarrassment.",
            "looked like a confused penguin.",
            "might have summoned a demon with those moves.",
            "turned the dance battle into a cringe compilation.",
            "got eliminated by *popular vote*. Harsh."
        };

        private List<string> _winnerMessages = new List<string>()
        {
            "**KILLED** it on the dance floor!",
            "showed off some gnarly moves!",
            "invented a new dance move. It's **SUPER** effective!",
            "shuffled all the way to the top!",
            "set the dance floor on fire!",
            "had some fresh moves. Nobody had seen any dancing this cool before.",
            "*REALLY* knows what they're doing!",
            "wiped out the competition with some funky fresh moves!",
            "received a round of applause",
            "has definetely been practicing at home",
            "inspired the crowd to join in their awesome dance move",
            "made everyone jealous with their epic dance moves",
            "**OWNED** the floor like it was their living room.",
            "got the crowd chanting their name!",
            "turned the dance battle into a victory lap.",
            "unleashed the *forbidden move*... and it WORKED.",
            "summoned a flash mob mid-routine.",
            "spun so fast they reversed time (almost).",
            "*levitated* with the power of the beat.",
            "outdanced gravity itself.",
            "redefined what rhythm means.",
            "stole the spotlight — and everyone's hearts.",
            "had moves so smooth, butter took notes.",
            "left their opponent stunned and spinning.",
            "wowed the judges with a surprise backflip.",
            "snapped so hard, the floor clapped back.",
            "didn't just dance — they *ascended*.",
            "did *THE* move. You know the one.",
            "was born for this moment.",
            "took a bow before the applause even started.",
            "landed a triple spin with style and sass.",
            "executed the cleanest moonwalk since MJ.",
            "left the floor smoking — someone call the fire marshal!"
        };
    }   
}
