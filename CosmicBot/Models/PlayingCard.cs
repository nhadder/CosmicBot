using CosmicBot.Models.Enums;

namespace CosmicBot.Models
{
    public class PlayingCard
    {
        public PlayingCardSuit Suit { get; set; }
        public PlayingCardNumber Number { get; set; }

        public override string ToString()
        {
            var str = string.Empty;
            switch(Number)
            {
                case PlayingCardNumber.Ace:
                    str = "A"; break;
                case PlayingCardNumber.Two:
                    str = "2"; break;
                case PlayingCardNumber.Three:
                    str = "3"; break;
                case PlayingCardNumber.Four:
                    str = "4"; break;
                case PlayingCardNumber.Five:
                    str = "5"; break;
                case PlayingCardNumber.Six:
                    str = "6"; break;
                case PlayingCardNumber.Seven:
                    str = "7"; break;
                case PlayingCardNumber.Eight:
                    str = "8"; break;
                case PlayingCardNumber.Nine:
                    str = "9"; break;
                case PlayingCardNumber.Ten:
                    str = "10"; break;
                case PlayingCardNumber.Jack:
                    str = "J"; break;
                case PlayingCardNumber.Queen:
                    str = "Q"; break;
                case PlayingCardNumber.King:
                    str = "K"; break;
            }

            switch(Suit)
            {
                case PlayingCardSuit.Hearts:
                    str += "♥"; break;
                case PlayingCardSuit.Clover:
                    str += "♣"; break;
                case PlayingCardSuit.Spade:
                    str += "♠"; break;
                case PlayingCardSuit.Diamonds:
                    str += "♦"; break;
            }

            return str;
        }

    }
}
