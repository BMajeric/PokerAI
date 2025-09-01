using System;
using System.Collections.Generic;
using UnityEngine;

public class HandEvaluator
{
    public static (HandRanking ranking, uint encodedValue) CalculateHandStrength(List<Card> cards)
    {
        // What should hand checkers return?
        // Hand checkers should return the HandRanking enum value of the strength of the hand if found,
        // otherwise return HandRanking.HIGH_CARD
        //
        // Hands that have kickers: Four of a kind, three of a kind, two pair, one pair, high card
        // Hands that don't have kickers: Royal flush, straight flush, full house, flush, straight
        // 
        // Hands can be represented by a 5 digit hex number where all the digits represent cards from the strongest to the weakest
        // The cards in larger clusters have priority since they represent the main hand strength value
        // (eg. a hand with three 2s an Ace and a 10 would be represented as 0x222EA)
        // The value calcualted like this includes all the important cards and kickers
        // so that comparing two hands of the same rank gives which one is stronger immediately
        //
        // So in conclusion, every method should return a touple that looks something like this:
        // (HandRank rank, uint value)

        // Testing
        // ulong handBits = 0b1000100010000000000000000000000000000001010000100001;

        // Encode the cards as bits
        ulong handBits = EncodeHandAsBits(cards);
        Debug.Log($"Hand bits: {Convert.ToString((long)handBits, 2)}");

        // Set the error value (encodedValue cannot be 0)
        (HandRanking ranking, uint encodedValue) res = (HandRanking.HIGH_CARD, 0);

        // Check for Flush, Straight Flush and Royal Flush
        (HandRanking ranking, uint encodedValue) flushHandStrength = CheckFlush(handBits);
        if (flushHandStrength.ranking > res.ranking)
            res = flushHandStrength;

        // Check for Straight
        (HandRanking ranking, uint encodedValue) straightHandStrength = CheckStraight(handBits);
        if (straightHandStrength.ranking > res.ranking)
            res = straightHandStrength;

        // Check for Four of a Kind, Full House, Three of a Kind, Two Pair, Pair, High Card
        (HandRanking ranking, uint encodedValue) multiplesHandStrength = CheckMultipleSameRankCards(handBits);
        if ((multiplesHandStrength.ranking > res.ranking) || (multiplesHandStrength.ranking == res.ranking && multiplesHandStrength.encodedValue > res.encodedValue))
            res = multiplesHandStrength;

        // Debug.Log($"Flush? ({flushHandStrength.ranking}, 0x{flushHandStrength.encodedValue:X})\nStraight? ({straightHandStrength.ranking}, 0x{straightHandStrength.encodedValue:X})\nMultiples? ({multiplesHandStrength.ranking}, 0x{multiplesHandStrength.encodedValue:X})");
        Debug.Log($"Result: ({res.ranking}, 0x{res.encodedValue:X})");

        return res;
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

    public static (HandRanking ranking, uint encodedValue) CheckMultipleSameRankCards(ulong handBits)
    {
        // Initialize rank masks for "Pairs", "Trips" and "Quads"
        // A rank mask represents all the card ranks and has 1 ticked where there is a pair, trip or quad respectively
        ushort pairs = 0;
        ushort trips = 0;
        ushort quads = 0;

        // Initialize a rank mask for kickers
        ushort kickers = 0;

        // Fist fill the masks with info on present pairs, trips and quads
        for (int rank = 0; rank < 13; rank++)
        {
            // Split handBits into nibbles and popcount the nibble to find out if there is none, one, two, three or four cards of that rank
            int count = PopCount(handBits & (0b1111UL << (rank * 4)));
            // Debug.Log($"Iteration {rank}: count -> {count}");

            if (count == 4)     // Found a four of a kind
            {
                quads |= (ushort)(1 << rank);
            } 
            else if (count == 3)    // Found a three of a kind
            {
                trips |= (ushort)(1 << rank);
            }
            else if (count == 2)    // Found a pair
            {
                pairs |= (ushort)(1 << rank);
            }

            // Fill kicker mask if that number exists in the hand
            if (count > 0)
            {
                kickers |= (ushort)(1 << rank);
            }
        }

        // Second check if there are any multiples
        int quadCount = PopCount(quads);
        int tripCount = PopCount(trips);
        int pairCount = PopCount(pairs);

        // Debug.Log($"Quads: {Convert.ToString((long)quads, 2)}; {quadCount}");
        // Debug.Log($"Trips: {Convert.ToString((long)trips, 2)}; {tripCount}");
        // Debug.Log($"Pairs: {Convert.ToString((long)pairs, 2)}; {pairCount}");

        // Four of a kind check
        if (quadCount > 0)
        {
            // Debug.Log("HERE QUADS!");
            int quadRank = 0;
            int kickerRank = 0;
            for (int rank = 14; rank >= 2; rank--)
            {
                // Find the quad rank
                if (quadRank == 0 && (quads & (1 << (rank - 2))) != 0)
                    quadRank = rank;

                // Find the kicker rank (largest kicker that isn't the quad)
                if (kickerRank == 0 && (kickers & (1 << (rank - 2))) != 0 && quadRank != rank)
                    kickerRank = rank;
            }

            // Encode and return
            return (HandRanking.FOUR_OF_A_KIND, EncodeFourOfAKind(quadRank, kickerRank));
        }

        // Full house check
        if (tripCount > 0 && pairCount > 0)
        {
            // Debug.Log("HERE FULL HOUSE!");
            int tripRank = 0;
            int pairRank = 0;
            for (int rank = 14; rank >= 2; rank--)
            {
                // Find the trip rank
                if (tripRank == 0 && (trips & (1 << (rank - 2))) != 0)
                    tripRank = rank;

                // Find the pair rank
                if (pairRank == 0 && (pairs & (1 << (rank - 2))) != 0)
                    pairRank = rank;
            }

            // Encode and return
            return (HandRanking.FULL_HOUSE, EncodeFullHouse(tripRank, pairRank));
        }

        // Three of a kind check
        if (tripCount > 0)
        {
            // Debug.Log("HERE TRIPS!");
            int tripRank = 0;
            int kicker1 = 0;
            int kicker2 = 0;
            int kickerCounter = 2;
            for (int rank = 14; rank >= 2; rank--)
            {
                // Find the trip rank
                if (tripRank == 0 && (trips & (1 << (rank - 2))) != 0)
                    tripRank = rank;

                // Find the kickers
                if (kickerCounter > 0 && (kickers & (1 << (rank - 2))) != 0 && tripRank != rank)
                {
                    if (kickerCounter == 2)
                        kicker1 = rank;
                    else
                        kicker2 = rank;

                    kickerCounter--;
                }
            }

            // Encode and return
            return (HandRanking.THREE_OF_A_KIND, EncodeThreeOfAKind(tripRank, kicker1, kicker2));
        }

        // Pair and two pair check
        if (PopCount(pairs) > 0)
        {
            // Debug.Log("HERE PAIRS!");
            int firstPairRank = 0;
            int secondPairRank = 0;
            int pairCounter = 2;
            int kickerRank = 0;
            for (int rank = 14; rank >= 2; rank--)
            {
                // Find the pairs
                if (pairCounter > 0 && (pairs & (1 << (rank - 2))) != 0)
                {
                    // Debug.Log("PAIRS!");
                    if (pairCounter == 2)
                        firstPairRank = rank;
                    else
                        secondPairRank = rank;

                    pairCounter--;
                }
                // Find the kicker for two pair
                if (kickerRank == 0 && (kickers & (1 << (rank - 2))) != 0 && firstPairRank != rank && secondPairRank != rank)
                    kickerRank = rank;
            }

            if (secondPairRank != 0)
            {
                // Two pairs found -> encode and return two pair
                return (HandRanking.TWO_PAIR, EncodeTwoPair(firstPairRank, secondPairRank, kickerRank));
            }
            else
            {
                // One pair found -> encode and return pair
                return (HandRanking.PAIR, EncodePair(firstPairRank, kickers));
            }
        }

        // If no multiples found -> return high card value
        return (HandRanking.HIGH_CARD, EncodeHighCard(kickers));
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

        // Debug.Log($"Straight checker bits: {Convert.ToString((long)straightRanks, 2)}");

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

        // If no straight found -> return
        return (HandRanking.HIGH_CARD, 0);
    }

    private static uint EncodeFourOfAKind(int quadRank, int kicker)
    {
        uint res = 0;

        // Put the quad rank in the front of the encoded value
        for (int i = 4; i > 0; i--)
        {
            res |= (uint)(quadRank << (i * 4));
        }

        // Add the kicker
        res |= (uint)kicker;

        return res;
    }

    private static uint EncodeFullHouse(int tripRank, int pairRank)
    {
        uint res = 0;
        // Debug.Log($"Trips: {tripRank}; Pair: {pairRank}");
        // Put the trips in the front and the pair in the back of the encoded value
        for (int i = 4; i >= 0; i--)
        {
            res |= (uint)(((i > 1) ? tripRank : pairRank) << (i * 4));
        }

        return res;
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

        // Debug.Log($"Rank: {highestRank}; Hex value: 0x{res:X}");

        return res;
    }

    private static uint EncodeThreeOfAKind(int tripRank, int kicker1, int kicker2)
    {
        uint res = 0;

        // Put the trip rank in the front of the encoded value
        for (int i = 4; i > 1; i--)
        {
            res |= (uint)(tripRank << (i * 4));
        }

        // Add the kickers
        res |= (uint)(kicker1 << 4);
        res |= (uint)kicker2;

        return res;
    }

    private static uint EncodeTwoPair(int pairRank1, int pairRank2, int kicker)
    {
        uint res = 0;

        // Put the larger pair in the front and the smaller pair bahind it in the encoded value
        for (int i = 4; i >= 1; i--)
        {
            res |= (uint)(((i > 2) ? pairRank1 : pairRank2) << (i * 4));
        }

        // Add the kicker
        res |= (uint)kicker;

        return res;
    }

    private static uint EncodePair(int pairRank, ushort kickers)
    {
        uint res = 0;

        // Put the pair rank in the front of the encoded value
        for (int i = 4; i > 2; i--)
        {
            res |= (uint)(pairRank << (i * 4));
        }

        // Handle kicker parsing and adding to the back of the encoded value
        int counter = 2;

        for (int rank = 14; rank >= 2; rank--)
        {
            if ((kickers & (1 << (rank - 2))) != 0 && pairRank != rank)
            {
                res |= (uint)(rank << (counter * 4));
                if (counter == 0)
                    break;
                counter--;
            }
        }

        return res;
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

        // Debug.Log($"Cards: {Convert.ToString((int)cards, 2)}; Hex value for high card or flush: 0x{res:X}");

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
