using System;
using System.Numerics;
using System.Collections.Generic;
using UnityEngine;

public class HandEvaluator
{
    public static (HandRanking ranking, uint encodedValue) CalculateHandStrength(List<Card> cards)
    {
        // ulong handBits = EncodeHandAsBits(cards);
        ulong handBits = 0b1000000010001000000000001000000000000000100010000001;
        Debug.Log($"Hand bits: {Convert.ToString((long)handBits, 2)}");

        CheckFlush(handBits);
        CheckStraight(handBits);

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
        // (HandRank, uint value)

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

    public static void CheckMultipleSameRankCards(ulong handBits)
    {
        // Initialize masks for "Pairs", "Trips" and "Quads"

        // Go through all ranks

        // Split handBits into nibbles

        // Popcount the nibble to find out if there is none, one, two, three or four cards of that rank

        // Based on popcounts return "Four of a Kind", "Full House", "Three of a Kind", "Two Pair" or "Pair"
    }

    public static (HandRanking ranking, uint encodedValue) CheckFlush(ulong handBits)
    {
        // Check for flush and straight flush
        // Iterate through all the suits, then all the ranks for each suit
        // If that card exists, write its rank in the suit mask for that suit
        // After all the ranks of a suit are checked, check if there are 5 or more cards of the same suit
        // If there are, we have a flush
        ushort[] suitMasks = new ushort[4];
        int flushSuit = -1;

        for (int suit = 0; suit < 4; suit++)
        {
            // Create rank mask for specific suit
            ushort suitMask = 0;
            for (int rank = 0; rank < 13; rank++)
            {
                int bitPos = rank * 4 + suit;
                if (((handBits >> bitPos) & 1UL) != 0)
                    suitMask |= (ushort)(1 << rank);
            }

            // Check if created rank mask contains a flush
            suitMasks[suit] = suitMask;
            if (PopCount(suitMask) >= 5)
            {
                flushSuit = suit;
                break;
            }
        }

        // If no flush detected -> return
        if (flushSuit == -1)
            return (HandRanking.HIGH_CARD, 0);

        // Check for straight flush
        ushort flushRanks = suitMasks[flushSuit];

        (HandRanking ranking, uint encodedValue) straightCheckedResult = CheckStraightFromBitMask(flushRanks, true);

        // Return the straight flush if detected
        if (straightCheckedResult != (HandRanking.HIGH_CARD, 0))
            return straightCheckedResult;

        // Flush is detected and straight is not, return that the hand is a flush
        return (HandRanking.FLUSH, EncodeHighCard(flushRanks));
    }

    public static (HandRanking ranking, uint encodedValue) CheckStraight(ulong handBits)
    {
        // Create a 13 bit mask of rank presence
        ushort straightRanks = 0;
        for (int rank = 0; rank < 13; rank++)
        {
            if ((handBits & (0b1111UL << (rank * 4))) != 0)
            {
                straightRanks |= (ushort)(1 << rank);
            }
        }

        Debug.Log($"Straight checker bits: {Convert.ToString((long)straightRanks, 2)}");

        // Check for straight in the bit mask and return the value
        return CheckStraightFromBitMask(straightRanks, false);
    }

    public static (HandRanking ranking, uint encodedValue) CheckStraightFromBitMask(ushort bitMask, bool isFromFlush)
    {
        // Normal straight check
        // - Slide bit window from highest to lowst
        // - Stop at the first pattern match (largest straight)
        for (int i = 14; i >= 6; i--)
        {
            if ((bitMask & (ushort)(0b11111 << (i - 6))) == (ushort)(0b11111 << (i - 6)))
            {
                HandRanking ranking;
                if (isFromFlush)
                {
                    ranking = (i == 14) ? HandRanking.ROYAL_FLUSH : HandRanking.STRAIGHT_FLUSH;
                }
                else
                {
                    ranking = HandRanking.STRAIGHT;
                }
                return (ranking, EncodeStraight(i));
            }
        }

        // Ace-low straight check
        if ((bitMask & 0b1000000001111) == 0b1000000001111)
            return (isFromFlush ? HandRanking.STRAIGHT_FLUSH : HandRanking.STRAIGHT, EncodeStraight(5));

        return (HandRanking.HIGH_CARD, 0);
    }

    private static void EncodeFourOfAKind(int quadRank, int kicker)
    {

    }

    private static void EncodeFullHouse(int tripRank, int pairRank)
    {

    }

    private static uint EncodeStraight(int highestRank)
    {
        // Encoding for Royal Flush, Straight Flush and Straight
        uint res = 0;

        for (int i = 0; i < 5; i++)
        {
            if (highestRank - i != 1)
                res |= (uint)((highestRank - i) << ((4 - i) * 4));
            else
                res |= 0xE;   // Low Ace
        }

        Debug.Log($"Rank: {highestRank}; Hex value: 0x{res:X}");

        return res;
    }

    private static void EncodeThreeOfAKind(int tripRank, int kicker1, int kicker2)
    {

    }

    private static void EncodeTwoPair(int pairRank1, int pairRank2, int kicker)
    {

    }

    private static void EncodePair(int pairRank, ushort kickers)
    {

    }

    private static uint EncodeHighCard(ushort cards)
    {
        // Encoding for Flush and High Card
        // Go through all the cards from highest to lowest
        // If there is a one in that place, detect which card it is and encode it
        // Repeat for first 5 cards detected
        
        uint res = 0;
        int counter = 4;

        for (int i = 14; i >= 2; i--)
        {
            if ((cards & (1 << (i - 2))) != 0)
            {
                res |= (uint)(i << (counter * 4));
                if (counter == 0)
                    break;
                counter--;
            }
        }

        Debug.Log($"Cards: {Convert.ToString((int)cards, 2)}; Hex value for high card or flush: 0x{res:X}");

        return res;
    }

    private static int PopCount(ulong value)
    {
        // Implementation of popcount (Hacker's Delight)
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
