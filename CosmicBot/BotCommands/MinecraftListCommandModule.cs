using CosmicBot.DAL;
using CosmicBot.Service;
using Discord.Interactions;
using Microsoft.EntityFrameworkCore;
using System.Text;

namespace CosmicBot.BotCommands
{
    public class MinecraftListCommandModule : InteractionModuleBase<SocketInteractionContext>
    {
        private readonly DataContext _context;
        public MinecraftListCommandModule(DataContext context)
        {
            _context = context;
        }

        [SlashCommand("list", "Lists players on servers")]
        public async Task List()
        {
            var guildId = Context.Guild.Id;
            var servers = await _context.MinecraftServers.Where(s => s.GuildId == guildId).ToListAsync();
            if (servers.Count == 0)
            {
                await RespondAsync("No servers added", ephemeral: true);
                return;
            }

            var sb = new StringBuilder();
            foreach (var server in servers)
            {
                var service = new MinecraftServerService(server);
                var response = await service.SendCommand("list");
                if (!string.IsNullOrWhiteSpace(response))
                    sb.AppendLine($"*{server.Name}*\n{response}\n");
            }
            if (!string.IsNullOrWhiteSpace(sb.ToString()))
                await RespondAsync(sb.ToString());
        }
    }
}
