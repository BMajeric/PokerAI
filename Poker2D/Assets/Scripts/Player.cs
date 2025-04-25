using UnityEngine;
using System.Collections.Generic;

public class Player
{
    private Hand _hand;
    // TODO: Add chips

    public Player()
    {
        _hand = new Hand();
    }

    public void ReceiveCard(Card card)
    {
        _hand.AddCard(card);
    }

    public void ReceiveCards(List<Card> cards)
    {
        _hand.AddCards(cards);
    }

    public void ClearPlayerHand()
    {
        _hand.Clear();
    }

    public Hand GetHand()
    {
        return _hand;
    }

    public List<Card> GetHAndCards()
    {
        return _hand.GetCards();
    }
}
