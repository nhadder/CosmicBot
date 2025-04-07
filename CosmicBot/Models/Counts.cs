using System.ComponentModel.DataAnnotations;

namespace CosmicBot.Models
{
    public class Counts
    {
        [Key]
        public Guid CountId { get; set; }
        public ulong GuildId { get; set; }
        public ulong? LastUserId { get; set; }
        public ulong Count { get; set; } = 0;
        public ulong Record { get; set; } = 0;
    }
}
