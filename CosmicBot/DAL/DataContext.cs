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

        public DataContext(DbContextOptions<DataContext> options) : base(options) 
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<MinecraftServer>()
                .Property(m => m.ServerId)
                .ValueGeneratedOnAdd();

            modelBuilder.Entity<MinecraftScheduledTask>()
                .Property(m => m.ScheduledTaskId)
                .ValueGeneratedOnAdd();

            modelBuilder.Entity<MinecraftScheduledTask>()
                .Property(m => m.Interval)
                .HasColumnType("NVARCHAR(20)")
                .HasConversion(
                    v => v.ToString(),
                    v => TimeSpan.Parse(v)
                );

            modelBuilder.Entity<RedditAutoPost>()
                .Property(m => m.Id)
                .ValueGeneratedOnAdd();

            modelBuilder.Entity<RedditAutoPost>()
                .Property(m => m.Interval)
                .HasColumnType("NVARCHAR(20)")
                .HasConversion(
                    v => v.ToString(),
                    v => TimeSpan.Parse(v)
                );

            modelBuilder.Entity<GuildSetting>()
                .Property(m => m.GuildSettingId)
                .ValueGeneratedOnAdd();

            modelBuilder.Entity<MinecraftCommand>()
                .Property(m => m.CommandId)
                .ValueGeneratedOnAdd();
        }
    }
}
