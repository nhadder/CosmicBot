namespace CosmicBot.Models.Enums
{
    public class Dice
    {
        public DiceNumber Value { get; set; }

        public override string ToString()
        {
            return Value switch
            {
                DiceNumber.One => "1",
                DiceNumber.Two => "2",
                DiceNumber.Three => "3",
                DiceNumber.Four => "4",
                DiceNumber.Five => "5",
                DiceNumber.Six => "6",
                _ => "",
            };
        }
    }
    public enum DiceNumber
    {
        One = 1,
        Two = 2,
        Three = 3,
        Four = 4,
        Five = 5,
        Six = 6
    }
}
