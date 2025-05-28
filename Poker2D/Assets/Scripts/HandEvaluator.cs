using System;
using System.Collections.Generic;
using UnityEngine;

public class HandEvaluator
{
    public static void CalculateHandStrength(List<Card> cards)
    {
        ulong handBits = EncodeHandAsBits(cards);
        Debug.Log($"Hand bits: {Convert.ToString((long)handBits, 2)}");
        return;
    }

    public static ulong EncodeHandAsBits(List<Card> cards)
    {
        ulong handBits = 0;

        foreach(Card card in cards)
        {
            int rank = (int)card.Rank;
            int suit = (int)card.Suit;

            int bitPosition = (14 - rank) * 4 + suit;
            Debug.Log($"{card}: {bitPosition}");
            handBits |= 1UL << bitPosition;
        }

        return handBits;
    }
}
