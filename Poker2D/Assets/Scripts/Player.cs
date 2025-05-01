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

    public void ReceiveCard(Card card, GameObject cardGO)
    {
        _hand.AddCard(card, cardGO);
    }

    public void ReceiveCards(List<Card> cards, List<GameObject> cardGOs)
    {
        _hand.AddCards(cards, cardGOs);
    }

    public void ClearPlayerHand()
    {
        _hand.Clear();
    }

    public void ShowPlayerHand()
    {
        _hand.ShowCards();
    }

    public Hand GetHand()
    {
        return _hand;
    }

    public List<Card> GetHandCards()
    {
        return _hand.Cards;
    }
}
