using CosmicBot.DAL.Validation;
using System.ComponentModel.DataAnnotations;

namespace CosmicBot.Models
{
    public class MinecraftScheduledTask
    {
        [Key]
        public Guid ScheduledTaskId { get; set; } = Guid.NewGuid();

        [Required]
        public Guid ServerId { get; set; }

        [Required]
        [MaxLength(50)]
        [NotEmpty]
        public string Name { get; set; } = string.Empty;

        [Required]
        public TimeOnly StartTime { get; set; }

        [Required]
        public TimeSpan Interval { get; set; }

        public DateTime? LastRan { get; set; }
    }
}
