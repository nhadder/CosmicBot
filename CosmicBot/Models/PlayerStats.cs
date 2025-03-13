using System.ComponentModel.DataAnnotations;

namespace CosmicBot.Models
{
    public class PlayerStats
    {
        [Key]
        public Guid PlayerId { get; set; } = Guid.NewGuid();
        public ulong GuildId { get; set; }
        public ulong UserId { get; set; }
        public long Points { get; set; } = 0;
        public int Level { get; set; } = 1;
        public long Experience { get; set; } = 0;
        public DateTime? LastDaily { get; set; } = null;
        public int GamesWon { get; set; } = 0;
        public int GamesLost { get; set; } = 0;
    }
}
