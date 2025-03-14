using CosmicBot.Helpers;
using Discord.Interactions;
using Discord.WebSocket;

namespace CosmicBot.Commands
{
    public class PagedListInteractionModule : InteractionModuleBase<SocketInteractionContext>
    {
        [ComponentInteraction("page_*")]
        public async Task HandlePageButtons()
        {
            var interactionContext = Context.Interaction as SocketMessageComponent;
            if (interactionContext != null)
                if (PagedMessageStore.TryGetMessage(interactionContext.Message.Id, out var pagedList))
                {
                    if (pagedList == null)
                        return;

                    await pagedList.HandleButtonAsync(interactionContext);
                }
                else
                {
                    await RespondAsync("That message component has expired", ephemeral: true);
                }
        }
    }
}
