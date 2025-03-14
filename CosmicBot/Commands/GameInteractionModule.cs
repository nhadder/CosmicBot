using CosmicBot.Helpers;
using CosmicBot.Service;
using Discord.Interactions;
using Discord.WebSocket;

namespace CosmicBot.Commands
{
    public class GameInteractionModule : InteractionModuleBase<SocketInteractionContext>
    {
        private readonly PlayerService _service;

        public GameInteractionModule(PlayerService service)
        {
            _service = service;
        }

        [ComponentInteraction("game_bj_*")]
        public async Task HandleButtons()
        {
            var interactionContext = Context.Interaction as SocketMessageComponent;
            if (interactionContext != null)
                if (GameMessageStore.TryGetMessage(interactionContext.Message.Id, out var game))
                {
                    if (game == null)
                        return;

                    if (Context.User.Id != game.UserId)
                    {
                        await RespondAsync("You are not a participant of that game.", ephemeral: true);
                        return;
                    }

                    await game.HandleButtonAsync(interactionContext, Context.Interaction.User);

                    if (game.Status != Models.Enums.GameStatus.InProgress)
                    {
                        if (game.Status == Models.Enums.GameStatus.Won)
                            await _service.Award(Context.Guild.Id, game.UserId, game.Bet, 10, 1);

                        if (game.Status == Models.Enums.GameStatus.Lost)
                            await _service.Award(Context.Guild.Id, game.UserId, -game.Bet, 2, 0, 1);

                        GameMessageStore.RemoveMessage(interactionContext.Message.Id);
                    }

                }
                else
                {
                    await RespondAsync("That game has expired", ephemeral: true);
                }
        }
    }
}
