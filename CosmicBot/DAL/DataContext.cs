using CosmicBot.Models;
using Microsoft.EntityFrameworkCore;

namespace CosmicBot.DAL
{
    public class DataContext : DbContext
    {
        public DbSet<MinecraftServer> MinecraftServers { get; set; }
        public DbSet<MinecraftScheduledTask> MinecraftScheduledTasks { get; set; }
        public DbSet<MinecraftCommand> MinecraftCommands { get; set; }
        public DbSet<RedditAutoPost> RedditAutoPosts { get; set; }
        public DbSet<GuildSetting> GuildSettings { get; set; }
        public DbSet<PlayerStats> PlayerStats { get; set; }
        public DbSet<DanceBattleMember> DanceBattleMembers { get; set; }

        public DataContext(DbContextOptions<DataContext> options) : base(options) 
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<MinecraftServer>()
                .Property(m => m.ServerId).ValueGeneratedOnAdd();


            modelBuilder.Entity<MinecraftScheduledTask>(e =>
            {
                e.Property(m => m.ScheduledTaskId).ValueGeneratedOnAdd();
                e.Property(m => m.Interval).HasColumnType("NVARCHAR(20)").HasConversion(
                    v => v.ToString(),
                    v => TimeSpan.Parse(v));
            });

            modelBuilder.Entity<RedditAutoPost>(e =>
            {
                e.Property(m => m.Id).ValueGeneratedOnAdd();
                e.Property(m => m.Interval).HasColumnType("NVARCHAR(20)").HasConversion(
                    v => v.ToString(),
                    v => TimeSpan.Parse(v));
            });

            modelBuilder.Entity<GuildSetting>()
                .Property(m => m.GuildSettingId).ValueGeneratedOnAdd();

            modelBuilder.Entity<MinecraftCommand>()
                .Property(m => m.CommandId).ValueGeneratedOnAdd();

            modelBuilder.Entity<PlayerStats>(e =>
            {
                e.Property(m => m.PlayerId).ValueGeneratedOnAdd();
                e.Property(m => m.UserId).IsRequired();
            });

            modelBuilder.Entity<DanceBattleMember>(e =>
            {
                e.HasKey(p => new { p.GuildId, p.UserId });
            });
        }
    }
}
