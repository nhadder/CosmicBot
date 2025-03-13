using CosmicBot.Service;
using Discord;
using Discord.Interactions;

namespace CosmicBot.BotCommands
{
    [RequireUserPermission(GuildPermission.Administrator)]
    [Group("tasks", "Minecraft Server Tasks")]
    public class MinecraftTaskCommandModule : CommandModule
    {
        private readonly MinecraftServerService _service;
        public MinecraftTaskCommandModule(MinecraftServerService service)
        {
            _service = service;
        }

        [SlashCommand("add", "Add a scheduled command to run on your server. Start time: hh:mm:ss. Interval [d.]hh:mm:ss.")]
        public async Task AddTask(string serverId, string taskName, string commands, string startTime, string interval)
        {
            await Respond(await _service.AddTask(serverId, taskName, commands, startTime, interval));
        }

        [SlashCommand("update", "Update a scheduled command on your server. Start time: hh:mm:ss. Interval [d.]hh:mm:ss.")]
        public async Task UpdateTask(string taskId, string? taskName, string? commands, string? startTime, string? interval)
        {
            await Respond(await _service.UpdateTask(taskId, taskName, commands, startTime, interval));
        }

        [SlashCommand("remove", "Delete a scheduled command on your server.")]
        public async Task DeleteTask(string taskId)
        {
            await Respond(await _service.RemoveTask(taskId));
        }

        [SlashCommand("list", "List your current scheduled tasks for a server")]
        public async Task ListTasks(string serverId)
        {
            await Respond(await _service.ListTasks(serverId));
        }
    }
}
