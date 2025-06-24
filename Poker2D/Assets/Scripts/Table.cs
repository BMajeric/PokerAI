using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Table
{
    public List<Card> CommunityCards { get; private set; }
    public List<GameObject> CommunityCardGameObjects { get; private set; }
    public int Pot { get; private set; }

    public Table()
    {
        CommunityCards = new List<Card>();
        CommunityCardGameObjects = new List<GameObject>();
    }

    public void AddCard(Card card, GameObject cardGO)
    {
        // Add card to data and game object to collection
        CommunityCards.Add(card);
        CommunityCardGameObjects.Add(cardGO);

        var img = cardGO.transform.GetChild(0).transform.GetChild(0).GetComponent<Image>();
        img.sprite = card.CardSprite;

        cardGO.GetComponentInChildren<Canvas>().worldCamera = Camera.main;
    }

    public void Clear()
    {
        // Destroy card game objects
        foreach (GameObject cardGO in CommunityCardGameObjects)
        {
            GameObject.Destroy(cardGO);
        }

        // Delete card data from collections
        CommunityCardGameObjects.Clear();
        CommunityCards.Clear();
    }

    public void AddChipsToPot(int chips)
    {
        Pot += chips;
    }
}
