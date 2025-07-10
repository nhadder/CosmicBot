using CosmicBot.DiscordResponse;
using CosmicBot.Helpers;
using Discord;
using Discord.Interactions;

namespace CosmicBot.Commands
{
    public class GifCommandModule : CommandModule
    {
        public GifCommandModule() { }

        [SlashCommand("gif", "Send a gif")]
        public async Task Gif(string tags)
        {
            if (!HasChannelPermissions())
            {
                await Respond(new MessageResponse("I don't have valid permissions in this channel", ephemeral: true));
                return;
            }

            await Respond(await GifFetcher.GetGifMessage(tags));
        }

        [SlashCommand("slap", "Slap a person")]
        public async Task Slap(IUser user)
        {
            if (!HasChannelPermissions())
            {
                await Respond(new MessageResponse("I don't have valid permissions in this channel", ephemeral: true));
                return;
            }

            await Respond(await GifFetcher.GetGifEmoteMessage(
                $"<@{Context.User.Id}> slapped <@{user.Id}>!", 
                "Anime Slap"));
        }

        [SlashCommand("hug", "Hug a person")]
        public async Task Hug(IUser user)
        {
            if (!HasChannelPermissions())
            {
                await Respond(new MessageResponse("I don't have valid permissions in this channel", ephemeral: true));
                return;
            }

            await Respond(await GifFetcher.GetGifEmoteMessage(
                $"<@{Context.User.Id}> hugged <@{user.Id}>",
                "Anime Hug"));
        }

        [SlashCommand("kiss", "Kiss a person")]
        public async Task Kiss(IUser user)
        {
            if (!HasChannelPermissions())
            {
                await Respond(new MessageResponse("I don't have valid permissions in this channel", ephemeral: true));
                return;
            }

            await Respond(await GifFetcher.GetGifEmoteMessage(
                $"<@{Context.User.Id}> kissed <@{user.Id}> <3",
                "Anime Kiss"));
        }

        [SlashCommand("cuff", "Handcuff a person")]
        public async Task Cuff(IUser user)
        {
            if (!HasChannelPermissions())
            {
                await Respond(new MessageResponse("I don't have valid permissions in this channel", ephemeral: true));
                return;
            }

            await Respond(await GifFetcher.GetGifEmoteMessage(
                $"<@{Context.User.Id}> put <@{user.Id}> in handcuffs",
                "Anime Handcuff"));
        }

        [SlashCommand("flirt", "Handcuff a person")]
        public async Task Flirt(IUser user)
        {
            if (!HasChannelPermissions())
            {
                await Respond(new MessageResponse("I don't have valid permissions in this channel", ephemeral: true));
                return;
            }

            await Respond(await GifFetcher.GetGifEmoteMessage(
                $"<@{Context.User.Id}> flirted with <@{user.Id}> ",
                "Anime Flirt"));
        }
    }
}
