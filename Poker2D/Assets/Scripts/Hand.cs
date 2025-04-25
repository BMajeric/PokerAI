using UnityEngine;
using System.Collections.Generic;

public class Hand
{
    private List<Card> _cards;

    public Hand()
    {
        _cards = new List<Card>();
    }

    public void AddCard(Card card)
    {
        _cards.Add(card);
    }

    public void AddCards(List<Card> cards)
    {
        _cards.AddRange(cards);
    }

    public void Clear()
    {
        _cards.Clear();
    }

    public List<Card> GetCards()
    {
        return _cards;
    }

    public int GetCardCount()
    {
        return _cards.Count;
    }
}
