using System;
using System.Collections.Generic;

public enum PickResult
{
    // First card of a pair turned over; waiting for the second.
    FirstPick,
    // Second card matches the first. Both stay face up.
    Match,
    // Second card doesn't match. Both should be turned back over.
    Mismatch,
    // Card already matched, already picked, out of range, or the round is over. Nothing changes.
    Ignored
}

// Matching rules for one board: every card has a pair key (e.g. MemoryCard.SymbolID) and two cards
// match when their keys are equal. Plain C# with no Unity types, so it can be unit tested.
public class PairMatchRound
{
    readonly int[] keys;
    readonly bool[] matched;
    int firstPick = -1;

    public int CardCount => keys.Length;
    public int PairCount => keys.Length / 2;
    public int PairsFound { get; private set; }
    public int Mistakes { get; private set; }
    public bool IsComplete => PairsFound >= PairCount;

    // Index of the card waiting for its partner, or -1.
    public int PendingPick => firstPick;

    public PairMatchRound(IList<int> pairKeys)
    {
        if (pairKeys == null) throw new ArgumentNullException(nameof(pairKeys));

        var counts = new Dictionary<int, int>();
        foreach (int key in pairKeys)
            counts[key] = counts.TryGetValue(key, out int c) ? c + 1 : 1;

        foreach (var entry in counts)
        {
            if (entry.Value != 2)
                throw new ArgumentException($"Key {entry.Key} appears {entry.Value} times; every key must appear exactly twice.", nameof(pairKeys));
        }

        keys = new int[pairKeys.Count];
        pairKeys.CopyTo(keys, 0);
        matched = new bool[keys.Length];
    }

    public bool IsMatched(int index) => IsValid(index) && matched[index];

    public int KeyAt(int index) => keys[index];

    // Picks a card. On Match/Mismatch, otherIndex is the first card of the pair; otherwise -1.
    public PickResult Pick(int index, out int otherIndex)
    {
        otherIndex = -1;
        if (!IsValid(index) || IsComplete || matched[index] || index == firstPick)
            return PickResult.Ignored;

        if (firstPick < 0)
        {
            firstPick = index;
            return PickResult.FirstPick;
        }

        otherIndex = firstPick;
        firstPick = -1;

        if (keys[index] == keys[otherIndex])
        {
            matched[index] = true;
            matched[otherIndex] = true;
            PairsFound++;
            return PickResult.Match;
        }

        Mistakes++;
        return PickResult.Mismatch;
    }

    // Shuffled list of keys 0..pairCount-1, each twice. Handy for tests and boards without their own layout.
    public static List<int> CreateShuffledKeys(int pairCount, Random random)
    {
        var result = new List<int>(pairCount * 2);
        for (int i = 0; i < pairCount; i++)
        {
            result.Add(i);
            result.Add(i);
        }

        for (int i = result.Count - 1; i > 0; i--)
        {
            int j = random.Next(i + 1);
            (result[i], result[j]) = (result[j], result[i]);
        }
        return result;
    }

    bool IsValid(int index) => index >= 0 && index < keys.Length;
}
