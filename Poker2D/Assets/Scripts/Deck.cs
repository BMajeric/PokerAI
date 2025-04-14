using UnityEngine;
using System.Collections.Generic;

public class Deck
{
    private List<Card> cards = new List<Card>();
    private Dictionary<string, Sprite> _cardSprites;

    public Deck (Dictionary<string, Sprite> cardSprites)
    {
        _cardSprites = cardSprites;
        GenerateDeck();
        Shuffle();
    }

    private void GenerateDeck()
    {
        // Iterate over all suits and ranks
        foreach (Suit suit in System.Enum.GetValues(typeof(Suit)))
        {
            foreach (Rank rank in System.Enum.GetValues(typeof(Rank)))
            {
                string spriteName = $"{rank}_of_{suit}";
                Debug.Log(spriteName);

                Sprite sprite = _cardSprites.ContainsKey(spriteName) ? _cardSprites[spriteName] : null;

                // Generate card of specified suit and rank
                Card card = new Card(rank, suit, sprite);
                cards.Add(card);
            }
        }
    }

    public void Shuffle()
    {
        // Fisher-Yates shuffle
        System.Random rand = new();

        int n = cards.Count;
        for (int i = 0; i < n; i++)
        {
            int j = i + rand.Next(n - i);

            // Swap the two selected cards
            (cards[j], cards[i]) = (cards[i], cards[j]);
        }
    }

    public Card DrawCard()
    {
        // TODO: see if we should recreate the deck or just return null
        if (cards.Count == 0) return null;

        Card drawnCard = cards[0];
        cards.RemoveAt(0);

        Debug.Log($"Drawn the {drawnCard}");
        return drawnCard;
    }

    public int GetDeckSize()
    {
        return cards.Count;
    }

}
