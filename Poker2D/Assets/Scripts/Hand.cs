using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UI;
using DG.Tweening;
using System.Collections;

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

    public void AddCard(Card card, GameObject cardGO, bool isFaceUp)
    {
        // Add card to data and game object to collection
        Cards.Add(card);
        CardGameObjects.Add(cardGO);

        // Apply the correct sprite (whether face up or face down)
        var img = cardGO.transform.GetChild(0).transform.GetChild(0).GetComponent<Image>();
        img.sprite = isFaceUp ? card.CardSprite : Resources.Load<Sprite>("CardBacks/Card_Back");

        cardGO.GetComponentInChildren<Canvas>().worldCamera = Camera.main;
    }

    public void AddCards(List<Card> cards, List<GameObject> cardGOs, bool areFaceUp)
    {
        // Add card to data and game object to collection
        Cards.AddRange(cards);
        CardGameObjects.AddRange(cardGOs);

        // Apply the correct sprites (whether face up or face down)
        for (int i = 0; i < Cards.Count; i++)
        {
            var img = CardGameObjects[i].transform.GetChild(0).transform.GetChild(0).GetComponent<Image>();
            img.sprite = areFaceUp ? Cards[i].CardSprite : Resources.Load<Sprite>("CardBacks/Card_Back");

            CardGameObjects[i].GetComponentInChildren<Canvas>().worldCamera = Camera.main;
        }
    }

    public void Clear()
    {
        // Destroy card game objects
        foreach (GameObject cardGO in CardGameObjects)
        {
            GameObject.Destroy(cardGO);
        }

        // Delete card data from collections
        CardGameObjects.Clear();
        Cards.Clear();
    }

    public HandRanking CalculateHandStrength(List<Card> communityCards)
    {
        // Create new list that combines 
        List<Card> playerCards = new List<Card>();
        playerCards.AddRange(communityCards);
        playerCards.AddRange(Cards);

        //communityCards.AddRange(Cards);

        HandEvaluator.CalculateHandStrength(playerCards);

        return HandRanking.HIGH_CARD;
    }

    public IEnumerator RevealHandAnimated()
    {
        // Make the cards shrink to nothing
        foreach (GameObject cardGO in CardGameObjects)
        {
            cardGO.transform.DOScaleX(0, 0.125f);
        }

        yield return new WaitForSeconds(0.125f);

        // Change card sprite
        for (int i = 0; i < Cards.Count; i++)
        {
            CardGameObjects[i].transform.GetChild(0).transform.GetChild(0).GetComponent<Image>().sprite = Cards[i].CardSprite;
        }

        // Make the cards expant do original size
        foreach (GameObject cardGO in CardGameObjects)
        {
            cardGO.transform.DOScaleX(1, 0.125f);
        }
    }

    public int GetCardCount()
    {
        return Cards.Count;
    }
}
