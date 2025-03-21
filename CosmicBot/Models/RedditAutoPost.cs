using CosmicBot.DAL.Validation;
using CosmicBot.Models.Enums;
using System.ComponentModel.DataAnnotations;

namespace CosmicBot.Models
{
    public class RedditAutoPost
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required]
        [NotEmpty]
        public string Subreddit { get; set; } = string.Empty;

        [Required]
        public ulong GuildId { get; set; }

        [Required]
        public ulong ChannelId { get; set; }

        [Required]
        public TimeOnly StartTime { get; set; }

        [Required]
        public TimeSpan Interval { get; set; }

        public DateTime? LastRan { get; set; }

        public RedditCategory Category { get; set; } = RedditCategory.Hot;
    }
}
