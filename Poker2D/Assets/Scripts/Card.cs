using UnityEngine;

public class Card
{
    public Suit Suit { get; private set; }
    public Rank Rank { get; private set; }
    public Sprite cardSprite { get; private set; }

    public Card(Rank rank, Suit suit)
    {
        Rank = rank;
        Suit = suit;
    }

    public override string ToString()
    {
        return $"{Rank} of {Suit}";
    }

}
