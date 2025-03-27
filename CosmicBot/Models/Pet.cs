using System.ComponentModel.DataAnnotations;

namespace CosmicBot.Models
{
    public class Pet
    {
        [Key]
        public Guid Id { get; set; }
        public ulong GuildId { get; set; }
        public ulong UserId { get; set; }
        public string Name { get; set; } = string.Empty;
        public int Generation { get; set; } = 0;
        public bool Female { get; set; }
        public DateTime Birthday { get; set; } = DateTime.UtcNow;
        public DateTime LastFed { get; set; } = DateTime.UtcNow;
        public DateTime LastPlayed { get; set; } = DateTime.UtcNow;
        public DateTime LastCleaned { get; set; } = DateTime.UtcNow;
        public bool Sold { get; set; } = false;

        public int Age => Convert.ToInt32((DateTime.UtcNow - Birthday).TotalDays);
        private TimeSpan TimeSinceLastPlayed => DateTime.UtcNow - LastPlayed;
        private TimeSpan TimeSinceLastFed => DateTime.UtcNow - LastFed;
        private TimeSpan TimeSinceLastCleaned => DateTime.UtcNow - LastCleaned;

        public string Feeling => Dead ? "Dead" : Dirty ? "Dirty" : Hungry ? "Hungry" : Happy ? "Happy" : "Sad";

        public bool Tired => TimeSinceLastPlayed.TotalMinutes <= 5;
        public bool Full => TimeSinceLastFed.TotalHours <= 1;
        private bool Hungry => TimeSinceLastFed.TotalHours >= 6;
        public bool Dirty => TimeSinceLastCleaned.TotalHours >= 3;
        private bool Happy => TimeSinceLastPlayed.TotalHours < (24 - (Hungry ? 12 : 0) - (Dirty ? 6 : 0));
        public bool Dead => (TimeSinceLastFed.TotalDays > 2 || TimeSinceLastCleaned.TotalDays > 3 || TimeSinceLastPlayed.TotalDays > 5 || Age > 99);

        private PetAgeGroup AgeGroup
        {
            get
            {
                if (Age <= 1)
                    return PetAgeGroup.Infant;
                if (Age <= 3)
                    return PetAgeGroup.Baby;
                if (Age <= 7)
                    return PetAgeGroup.Todler;
                if (Age <= 12)
                    return PetAgeGroup.Child;
                if (Age <= 17)
                    return PetAgeGroup.Teen;
                if (Age <= 59)
                    return PetAgeGroup.Adult;
                return PetAgeGroup.Old;
            }
        }

        public List<string> GetFrame()
        {
            switch (AgeGroup)
            {
                case PetAgeGroup.Old:
                    return new List<string>
                    {
                        @$"   _____            ",
                        @$" <({i}``)>____      ",
                        @$" (~~(~~~~___( )`~,  ",
                        @$"C_C___)_C_____)`~`  ",
                        @$"{dirt}              {dirt}"
                    };
                case PetAgeGroup.Adult:
                    return new List<string>
                    {
                        @$"   /\_/\       ~,,, ",
                        @$"  ({i}  )_____ ~~ ~,",
                        @$" (~~(    ___( )`  ` ",
                        @$"C_C___)_C_____)     ",
                        @$"{dirt}              {dirt}"
                    };
                case PetAgeGroup.Teen:
                    return new List<string>
                    {
                        @$"   /\_/\       ~,,  ",
                        @$"  ({i}  )_____ ~ ~, ",
                        @$"  (~(    ___( )` `  ",
                        @$"c_c___)_c_____)     ",
                        @$"{dirt}              {dirt}"
                    };
                case PetAgeGroup.Child:
                    return new List<string>
                    {
                        @$"  /\_/\             ",
                        @$" ( {i} )____   ,~~  ",
                        @$"  (_________)``     ",
                        @$"  \/ \/ \/\/        ",
                        @$"{dirt}              {dirt}"
                    };
                case PetAgeGroup.Todler:
                    return new List<string>
                    {
                        @$"     ^_^            ",
                        @$"    ({i})___  ,~    ",
                        @$"     (______)``     ",
                        @$"     v v  v v       ",
                        @$"{dirt}              {dirt}"
                    };
                case PetAgeGroup.Baby:
                    return new List<string>
                    {
                        @$"        ^-^         ",
                        @$"       ({i})        ",
                        @$"      `( . )`       ",
                        @$"        v v`~       ",
                        @$"{dirt}              {dirt}"
                    };
                case PetAgeGroup.Infant:
                default:
                    return new List<string>
                    {
                        @$"        ,,,         ",
                        @$"      <({i})>       ",
                        @$"       (`.`)~       ",
                        @$"        ` `         ",
                        @$"{dirt}              {dirt}"
                    };
            }
        }

        private string i
        {
            get
            {
                if (Dead)
                    return "x.x";
                if (Hungry)
                    return ">w<";
                if (!Happy)
                    return ",_,";

                if (AgeGroup == PetAgeGroup.Infant || AgeGroup == PetAgeGroup.Old)
                    return "-.-";

                return "^w^";
            }
        }

        private string dirt
        {
            get
            {
                return Dirty ? "~" : " ";
            }
        }
    }

    public enum PetAgeGroup
    {
        Infant,
        Baby,
        Todler,
        Child,
        Teen,
        Adult,
        Old
    }
}
