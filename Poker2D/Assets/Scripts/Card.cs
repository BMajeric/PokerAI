using UnityEngine;

public class Card
{
    public Suit Suit { get; private set; }
    public Rank Rank { get; private set; }
    public Sprite CardSprite { get; private set; }

    public Card(Rank rank, Suit suit, Sprite sprite)
    {
        Rank = rank;
        Suit = suit;
        CardSprite = sprite;
    }

    public override string ToString()
    {
        return $"{Rank} of {Suit}";
    }

}
