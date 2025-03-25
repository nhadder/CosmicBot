namespace CosmicBot.Models
{
    public class DanceBattleMember
    {
        public ulong GuildId { get; set; }
        public ulong UserId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string ImageUrl { get; set; } = string.Empty;
    }
}
