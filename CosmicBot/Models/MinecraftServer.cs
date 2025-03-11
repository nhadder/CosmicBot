using CosmicBot.DAL.Validation;
using System.ComponentModel.DataAnnotations;

namespace CosmicBot.Models
{
    public class MinecraftServer
    {
        [Key]
        public Guid ServerId { get; set; } = Guid.NewGuid();

        [Required]
        public ulong GuildId { get; set; }

        [Required]
        [MaxLength(50)]
        [NotEmpty]
        public string Name { get; set; } = string.Empty;

        public ServerType ServerType { get; set; } = ServerType.Vanilla;

        [Required]
        [MaxLength(50)]
        [NotEmpty]
        public string IpAddress { get; set; } = string.Empty;

        [Required]
        public ushort RconPort { get; set; } = 25575;

        [Required]
        public string RconPassword { get; set; } = string.Empty;
    }

    public enum ServerType
    {
        Vanilla,
        Modded
    }
}
