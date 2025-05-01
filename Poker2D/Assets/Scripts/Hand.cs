using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UI;
using DG.Tweening;

public class Hand
{
    public List<Card> Cards { get; private set; }
    public List<GameObject> CardGameObjects { get; private set; }
    public HandRanking HandStrength { get; private set; }

    public Hand()
    {
        Cards = new List<Card>();
        CardGameObjects = new List<GameObject>();
    }

    public void AddCard(Card card, GameObject cardGO)
    {
        Cards.Add(card);
        CardGameObjects.Add(cardGO);
    }

    public void AddCards(List<Card> cards, List<GameObject> cardGOs)
    {
        Cards.AddRange(cards);
        CardGameObjects.AddRange(cardGOs);
    }

    public void Clear()
    {
        foreach (GameObject cardGO in CardGameObjects)
        {
            GameObject.Destroy(cardGO);
        }
        CardGameObjects.Clear();
        Cards.Clear();
    }

    public void ShowCards()
    {
        for (int i = 0; i < Cards.Count; i++)
        {
            CardGameObjects[i].transform.GetChild(0).transform.GetChild(0).GetComponent<Image>().sprite = Cards[i].CardSprite;
        }
    }

    public int GetCardCount()
    {
        return Cards.Count;
    }
}
