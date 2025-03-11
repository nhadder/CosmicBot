using CoreRCON;
using CosmicBot.DAL;
using CosmicBot.Models;
using System.Net;
using System.Text.RegularExpressions;

namespace CosmicBot.Service
{
    public class MinecraftServerService
    {
        private readonly MinecraftServer _server;

        public MinecraftServerService(MinecraftServer server)
        {
            _server = server;
        }

        public async Task SendCommands(List<MinecraftCommand> commands)
        {
            commands = commands.OrderBy(c => c.Order).ToList();
            foreach (var command in commands)
            {
                var response = await SendCommand(command.Command);
                if (!string.IsNullOrWhiteSpace(response))
                    Console.WriteLine($"Response: {response}");
            }
        }

        public async Task<string> SendCommand(string command)
        {
            try
            {
                using (var rcon = new RCON(IPAddress.Parse(_server.IpAddress), _server.RconPort, _server.RconPassword))
                {
                    await rcon.ConnectAsync();
                    string response = await rcon.SendCommandAsync(command);
                    return RemoveColorCodes(response);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to send command: `{command}` to {_server.IpAddress}:{_server.RconPort}");
                Console.WriteLine(ex.Message);
                return string.Empty;
            }
        }

        private static string RemoveColorCodes(string input)
        {
            string pattern = "§[0-9A-Za-z]";
            return Regex.Replace(input, pattern, string.Empty);
        }
    }
}
