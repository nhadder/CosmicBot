using CosmicBot.DiscordResponse;
using CosmicBot.Models;
using Discord;
using System.Text;

namespace CosmicBot.Messages.Components
{
    public class PetGame : EmbedMessage
    {
        public Pet Pet { get; set; }
        private string _lastAction = string.Empty;
        private IUser _user;
        public PetGame(IUser user, Pet pet) : base([user.Id])
        {
            Pet = pet;
            _user = user;
            _lastAction = $"You pet {pet.Name}.\n" + (pet.Feeling.Equals("Happy") ? "They love you" : "They are still upset...");
            RefreshButtons();
        }

        private void RefreshButtons()
        {
            Buttons.Clear();
            if (!Pet.Dead)
            {
                var feedButton = new MessageButton("Feed", ButtonStyle.Primary, emote: new Emoji("🥕"));
                feedButton.OnPress = Feed;
                Buttons.Add(feedButton);

                var playButton = new MessageButton("Play", ButtonStyle.Primary, emote: new Emoji("🪀"));
                playButton.OnPress = Play;
                Buttons.Add(playButton);

                var cleanButton = new MessageButton("Clean", ButtonStyle.Primary, emote: new Emoji("🧹"));
                cleanButton.OnPress = Clean;
                Buttons.Add(cleanButton);
            }
        }

        public MessageResponse? Feed(IInteractionContext context)
        {
            if (Pet.Full)
            {
                Pet.LastCleaned = (Pet.LastCleaned) - TimeSpan.FromHours(4);
                _lastAction = $"You over fed {Pet.Name}!\n They threw up...";
            }
            else
            {
                _lastAction = $"You fed {Pet.Name}!";
            }
            Pet.LastFed = DateTime.UtcNow;
            RefreshButtons();
            return null;
        }

        public MessageResponse? Play(IInteractionContext context)
        {
            if (Pet.Tired)
            {
                _lastAction = $"You threw a ball to {Pet.Name}.\nThey grew hungrier...";
                Pet.LastFed = (Pet.LastFed) - TimeSpan.FromHours(7);
            }
            else
            {
                if (Pet.TimeSinceLastPlayed.TotalHours > 1)
                {
                    var rng = new Random();
                    if(rng.Next(3) == 0)
                    {
                        var stars = rng.Next(101);
                        Awards.Add(new PlayerAward(_user.Id, stars, 0, 0, 0));
                        _lastAction = $"You threw a ball to {Pet.Name}.\nThey brought back some stars!\nYou received **{stars}** stars!";
                    }
                    else
                        _lastAction = $"You threw a ball to {Pet.Name}.\nThey brought it back.";
                }
                else
                    _lastAction = $"You threw a ball to {Pet.Name}.\nThey brought it back.";
            }
            Pet.LastPlayed = DateTime.UtcNow;
            RefreshButtons();
            return null;
        }

        public MessageResponse? Clean(IInteractionContext context)
        {
            if (!Pet.Dirty)
            {
                if (Pet.TimeSinceLastCleaned.TotalDays > 1)
                {
                    var rng = new Random();
                    if (rng.Next(2) == 0)
                    {
                        var stars = rng.Next(501);
                        Awards.Add(new PlayerAward(_user.Id, stars, 0, 0, 0));
                        _lastAction = $"You cleaned out {Pet.Name}'s room.\nYou found some stars!\nYou received **{stars}** stars!";
                    }
                    else
                        _lastAction = $"You cleaned out {Pet.Name}'s room.\nNothing changed...";
                }
                else
                    _lastAction = $"You cleaned out {Pet.Name}'s room.\nNothing changed...";
            }
            else
            {
                _lastAction = $"You cleaned out {Pet.Name}'s room.";
            }
            Pet.LastCleaned = DateTime.UtcNow;
            RefreshButtons();
            return null;
        }

        public override Embed[] GetEmbeds()
        {
            var builder = new EmbedBuilder()
                .WithAuthor(new EmbedAuthorBuilder()
                    .WithIconUrl(_user.GetAvatarUrl())
                    .WithName(Pet.Name))
                .WithDescription(GetGameWindow())
                .WithFields(new EmbedFieldBuilder()
                    .WithName("Age")
                    .WithIsInline(true)
                    .WithValue(Pet.Age),
                new EmbedFieldBuilder()
                    .WithName("Sex")
                    .WithIsInline(true)
                    .WithValue(Pet.Female ? "Girl" : "Boy"),
                new EmbedFieldBuilder()
                    .WithName("Feeling")
                    .WithIsInline(true)
                    .WithValue(Pet.Feeling))
                .WithFooter($"{_user.GlobalName}'s pet");

            return [builder.Build()];
        }

        private string GetGameWindow()
        {
            var sb = new StringBuilder();
            sb.Append("```");
            if (Pet.Generation > 0)
                sb.AppendLine($"[Generation {Pet.Generation}]");

            var frame = Pet.GetFrame();
            foreach (var line in frame)
                sb.AppendLine($"{line}");
            sb.Append("```");

            if (Pet.Dead)
                sb.AppendLine($"{Pet.Name} has died. Do `/pet buy` to buy a new one.");
            else if (!string.IsNullOrEmpty(_lastAction))
                sb.AppendLine(_lastAction);

            if (Expired)
                sb.AppendLine("\nThis game has expired!");

            return sb.ToString();
        }
    }
}
