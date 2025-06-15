using System;
using System.Numerics;
using System.Collections.Generic;
using UnityEngine;

public class HandEvaluator
{
    public static (HandRanking ranking, ulong encodedValue) CalculateHandStrength(List<Card> cards)
    {
        // ulong handBits = EncodeHandAsBits(cards);
        ulong handBits = 0b1000100010001001100000000000000000001000000000000000;
        Debug.Log($"Hand bits: {Convert.ToString((long)handBits, 2)}");
        Debug.Log($"Hand bits shifted by 4: {Convert.ToString((long)handBits>>4, 2)}");
        // 1000100010001000100110000000000000001000000000000000
        // 0000100010001000100110000000000000000000100000000000
        // 0000000010001000100010001001100000000000000010000000
        // 0000000000001000100010001000100110000000000000001000
        // 0000000000000000100010001000100010011000000000000000
        // 0000000000000000100010000000000000000000000000000000
        CheckFlush(handBits);

        // What should hand checkers return?
        // Hand checkers should return the HandRanking enum value of the strength of the hand if found,
        // otherwise return HandRanking.HIGH_CARD
        //
        // Hands that have kickers: Four of a kind, three of a kind, two pair, one pair, high card
        // Hands that don't have kickers: Royal flush, straight flush, full house, flush, straight
        // 
        // Hands can be represented by a 5 digit hex number where all the digits represent cards from the strongest to the weakest
        // The cards in larger clusters have priority since they represent the main hand strength value
        // (eg. a hand with three 2s an Ace and a 10 would be represented as 0x222EA or something like that, up for discussion)
        // The value calcualted like this includes all the important cards and kickers
        // so that comparing two hands of the same rank gives which one is stronger immediately
        //
        // So in conclusion, every method should return a touple that looks something like this:
        // (HandRank, value)

        CheckStraightFlush(handBits);

        return (HandRanking.HIGH_CARD, 0);
    }

    public static ulong EncodeHandAsBits(List<Card> cards)
    {
        ulong handBits = 0;

        // Put the higher ranked cards as more valuable digits
        foreach(Card card in cards)
        {
            int rank = (int)card.Rank;
            int suit = (int)card.Suit;

            int bitPosition = (rank - 2) * 4 + suit;
            Debug.Log($"{card}: {bitPosition}");
            handBits |= 1UL << bitPosition;
        }

        return handBits;
    }

    public static void CheckStraightFlush(ulong handBits)
    {
        // Check for straight flush using bitshifting formula
        ulong result = handBits & (handBits >> 4) & (handBits >> 8) & (handBits >> 12) & (handBits >> 16);
        Debug.Log($"The result of straight flush check is: {result}");

        // If is straight flush up to the ace, return royal flush

    }

    public static void CheckMultipleSameRankCards(ulong handBits)
    {
        // Initialize masks for "Pairs", "Trips" and "Quads"

        // Go through all ranks

        // Split handBits into nibbles

        // Popcount the nibble to find out if there is none, one, two, three or four cards of that rank

        // Based on popcounts return "Four of a Kind", "Full House", "Three of a Kind", "Two Pair" or "Pair"
    }

    public static void CheckFlush(ulong handBits)
    {
        // Check for flush and straight flush
        // Iterate through all the suits, then all the ranks for each suit
        // If that card exists, write its rank in the suit mask for that suit
        // After all the ranks of a suit are checked, check if there are 5 or more cards of the same suit
        // If there are, we have a flush
        ulong[] suitMasks = new ulong[4];
        int flushSuit = -1;

        for (int suit = 0; suit < 4; suit++)
        {
            ulong suitMask = 0;
            for (int rank = 0; rank < 13; rank++)
            {
                int bitPos = rank * 4 + suit;
                if (((handBits >> bitPos) & 1UL) != 0)
                    suitMask |= 1UL << rank;
            }
            suitMasks[suit] = suitMask;
            if (PopCount(suitMask) >= 5)
            {
                flushSuit = suit;
                break;
            }
        }

        // If no flush detected -> return
        if (flushSuit == -1)
            return; // HandRanking.HIGH_CARD

        // Check for straight flush
        ulong flushRanks = suitMasks[flushSuit];

        // Normal straight check
        for (int i = 12; i >= 4; i--)
        {
            if ((flushRanks & (0b11111UL << (i - 4))) == (0b11111UL << (i - 4)))
            {
                // Normal straight detected
                if (i == 12)
                    return; // HandRanking.ROYAL_FLUSH
                return; // HandRanking.STRAIGHT_FLUSH, value?
            }
        }

        // Ace-low straight check
        if ((flushRanks & 0b1000000001111UL) == 0b1000000001111UL)
            return; // HandRanking.STRAIGHT_FLUSH, value?

        return; // HandRanking.FLUSH, value?
    }

    public static void CheckStraight(ulong handBits)
    {
        // Check for straight using bitshifting formula
    }

    private static int PopCount(ulong value)
    {
        // Implementation of popcount
        // https://github.com/dotnet/runtime/blob/main/src/libraries/System.Private.CoreLib/src/System/Numerics/BitOperations.cs#L434

        const ulong c1 = 0x5555555555555555UL; 
        const ulong c2 = 0x3333333333333333UL; 
        const ulong c3 = 0x0F0F0F0F0F0F0F0FUL; 
        const ulong c4 = 0x0101010101010101UL; 

        value -= (value >> 1) & c1;
        value = (value & c2) + ((value >> 2) & c2);
        value = (((value + (value >> 4)) & c3) * c4) >> 56;

        return (int)value;
    }


}
