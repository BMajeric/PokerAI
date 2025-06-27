using UnityEngine;
using System.Collections.Generic;

public class Player
{
    private readonly Hand _hand;
    // TODO: Add chips
    private int _chips;

    public Player()
    {
        _hand = new Hand();
        _chips = 2500;
    }

    public void ReceiveCard(Card card, GameObject cardGO, bool isFaceUp)
    {
        _hand.AddCard(card, cardGO, isFaceUp);
    }

    public void ReceiveCards(List<Card> cards, List<GameObject> cardGOs, bool areFaceUp)
    {
        _hand.AddCards(cards, cardGOs, areFaceUp);
    }

    public void ClearPlayerHand()
    {
        _hand.Clear();
    }

    public Hand GetHand()
    {
        return _hand;
    }

}
