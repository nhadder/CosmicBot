using CosmicBot.DAL.Validation;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CosmicBot.Models
{
    public class MinecraftCommand
    {
        [Key]
        public Guid CommandId { get; set; } = Guid.NewGuid();

        [ForeignKey("ScheduledTaskId")]
        public Guid ScheduledTaskId { get; set; }

        [Required]
        [NotEmpty]
        public string Command { get; set; } = string.Empty;

        public int Order { get; set; }
    }
}
