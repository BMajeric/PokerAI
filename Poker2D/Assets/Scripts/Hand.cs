using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UI;

public class Hand
{
    public List<Card> Cards { get; private set; }
    public HandRanking HandStrength { get; private set; }

    private List<GameObject> _cardGameObjects;

    public Hand()
    {
        Cards = new List<Card>();
        _cardGameObjects = new List<GameObject>();
    }

    public void AddCard(Card card, GameObject cardGO)
    {
        Cards.Add(card);
        _cardGameObjects.Add(cardGO);
    }

    public void AddCards(List<Card> cards, List<GameObject> cardGOs)
    {
        Cards.AddRange(cards);
        _cardGameObjects.AddRange(cardGOs);
    }

    public void Clear()
    {
        foreach (GameObject cardGO in _cardGameObjects)
        {
            GameObject.Destroy(cardGO);
        }
        _cardGameObjects.Clear();
        Cards.Clear();
    }

    public void ShowCards()
    {
        for (int i = 0; i < Cards.Count; i++)
        {
            _cardGameObjects[i].transform.GetChild(0).transform.GetChild(0).GetComponent<Image>().sprite = Cards[i].CardSprite;
        }
    }

    public int GetCardCount()
    {
        return Cards.Count;
    }
}
