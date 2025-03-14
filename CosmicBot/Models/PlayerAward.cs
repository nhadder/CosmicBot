using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CosmicBot.Models
{
    public class PlayerAward
    {
        public ulong UserId { get; set; }
        public long Points { get; set; }
        public long Experience { get; set; }
        public int GamesWon { get; set; }
        public int GamesLost { get; set; }

        public PlayerAward(ulong userId, long points, long experience, int gamesWon, int gamesLost)
        {
            UserId = userId;
            Points = points;
            Experience = experience;
            GamesWon = gamesWon;
            GamesLost = gamesLost;
        }
    }
}
