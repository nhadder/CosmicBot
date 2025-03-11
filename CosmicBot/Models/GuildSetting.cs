using CosmicBot.DAL.Validation;
using System.ComponentModel.DataAnnotations;

namespace CosmicBot.Models
{
    public class GuildSetting
    {
        [Key]
        public Guid GuildSettingId { get; set; }

        [Required]
        public ulong GuildId { get; set; }

        [Required]
        [NotEmpty]
        public string SettingKey { get; set; } = string.Empty;

        public string SettingValue { get; set; } = string.Empty;
    }
}
