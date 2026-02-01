using UnityEngine;
using System.Collections.Generic;

public class Deck
{
    private readonly List<Card> _cards = new List<Card>();
    private readonly Dictionary<string, Sprite> _cardSprites;

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
                // Debug.Log(spriteName);

                Sprite sprite = _cardSprites.ContainsKey(spriteName) ? _cardSprites[spriteName] : null;

                // Generate card of specified suit and rank
                Card card = new Card(rank, suit, sprite);
                _cards.Add(card);
            }
        }
    }

    public void Shuffle()
    {
        // Fisher-Yates shuffle
        System.Random rand = new();

        int n = _cards.Count;
        for (int i = 0; i < n; i++)
        {
            int j = i + rand.Next(n - i);

            // Swap the two selected cards
            (_cards[j], _cards[i]) = (_cards[i], _cards[j]);
        }
    }

    public Card DrawCard()
    {
        Card drawnCard = _cards[0];
        _cards.RemoveAt(0);

        // Debug.Log($"Drawn the {drawnCard}");
        return drawnCard;
    }

    public void ReshuffleIfNeeded()
    {
        // If deck would need to be reshuffled mid dealing, reshuffle it before to prevent duplicate cards
        if (_cards.Count < 9)
        {
            GenerateDeck();
            Shuffle();
        }
    }

    public int GetDeckSize()
    {
        return _cards.Count;
    }

}
