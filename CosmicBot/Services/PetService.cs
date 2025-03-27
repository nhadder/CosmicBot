using CosmicBot.DAL;
using CosmicBot.DiscordResponse;
using CosmicBot.Models;

namespace CosmicBot.Services
{
    public class PetService
    {
        private readonly DataContext _context;

        public PetService(DataContext context)
        {
            _context = context;
        }

        public Pet? Get(ulong guildId, ulong userId)
        {
            return _context.Pets.FirstOrDefault(p => p.GuildId == guildId && p.UserId == userId);
        }

        public async Task<MessageResponse?> Create(ulong guildId, ulong userId, string name)
        {
            var existing = Get(guildId, userId);
            if (existing == null)
            {
                var rng = new Random();
                var pet = new Pet()
                {
                    GuildId = guildId,
                    UserId = userId,
                    Name = name,
                    Birthday = DateTime.UtcNow,
                    LastCleaned = DateTime.UtcNow,
                    LastPlayed = DateTime.UtcNow,
                    LastFed = DateTime.UtcNow,
                    Female = rng.Next(2) == 0,
                    Generation = 0
                };
                await _context.Pets.AddAsync(pet);
                await _context.SaveChangesAsync();

                return new MessageResponse("Successfully purchased your first pet!\nDo `/pet pet` to take care them\nDo `/pet rename {name}` to rename them ", ephemeral: true);
            }
            else if (existing.Sold || existing.Dead)
            {
                var rng = new Random();
                existing.Name = name;
                existing.Birthday = DateTime.UtcNow;
                existing.Female = rng.Next(2) == 0;
                existing.Generation = existing.Generation + 1;
                existing.Sold = false;
                existing.LastCleaned = DateTime.UtcNow;
                existing.LastPlayed = DateTime.UtcNow;
                existing.LastFed = DateTime.UtcNow;
                _context.Pets.Update(existing);
                await _context.SaveChangesAsync();

                return new MessageResponse("Successfully purchased another pet!\nDo `/pet pet` to take care them\nDo `/pet rename {name}` to rename them ", ephemeral: true);
            }
            return null;
        }

        public async Task<int> Sell(ulong guildId, ulong userId)
        {
            var pet = Get(guildId, userId);
            if (pet == null)
                return 0;

            if (pet.Sold || pet.Dead)
                return 0;

            pet.Sold = true;
            _context.Pets.Update(pet);
            await _context.SaveChangesAsync();

            return 250 + (pet.Age * pet.Age * pet.Age) + (pet.Age * (pet.Feeling == "Happy" ? 1 : 0));
        }

        public async Task Update(Pet pet)
        {
            _context.Pets.Update(pet);
            await _context.SaveChangesAsync();
        }
    }
}
