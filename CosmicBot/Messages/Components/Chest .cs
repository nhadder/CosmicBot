using CosmicBot.DiscordResponse;
using Discord;

namespace CosmicBot.Messages.Components
{
    public class Chest : EmbedMessage
    {
        private int _amount;
        private string _user = string.Empty;
        public Chest() : base(null, true)
        {
            _amount = CalculatePrize();
            var grabButton = new MessageButton("Open", ButtonStyle.Success);
            grabButton.OnPress = Grab;
            Buttons.Add(grabButton);
        }

        private int CalculatePrize()
        {
            var rng = new Random();
            if (rng.Next(1_000) == 1)
                return 10_000;
            if (rng.Next(500) == 1)
                return 5_000;
            if (rng.Next(100) == 1)
                return 1_000;
            if (rng.Next(50) == 1)
                return 500;
            if (rng.Next(10) == 1)
                return 100;
            if (rng.Next(5) == 1)
                return 50;
            if (rng.Next(2) == 1)
                return 20;
            else
                return 10;
        }

        public MessageResponse? Grab(IInteractionContext context)
        {
            Buttons.Clear();
            _user = context.User.GlobalName;
            Expired = true;
            Awards.Add(new Models.PlayerAward(context.User.Id, _amount, 0, 0, 0));
            return new MessageResponse($"You found {_amount} stars!", ephemeral: true);
        }

        public override Embed[] GetEmbeds()
        {
            EmbedBuilder chest;
            if (!Expired)
                chest = new EmbedBuilder()
                .WithDescription(":gift:").WithColor(Color.Gold);
            else if (!string.IsNullOrWhiteSpace(_user))
                chest = new EmbedBuilder()
                .WithDescription(string.Join(" ", Enumerable.Range(0, _amount).Select(s => ":star:")))
                .WithFooter($"{_user} found a gift of {_amount} star(s)!");
            else
                chest = new EmbedBuilder()
                .WithDescription(":gift:\nNobody grabbed the gift in time....");
            return [chest.Build()];
        }
    }
}
