using System.Collections.Generic;
using UnityEngine;

public class StudyMemoryBoard : MonoBehaviour
{
    [Header("Board Settings")]
    [SerializeField] private int columns = 5;
    [SerializeField] private int rows = 4;

    [Header("References")]
    [SerializeField] private MemoryCard cardPrefab;
    [SerializeField] private Transform gridParent;

    [Header("Card Sprites")]
    [SerializeField] private Sprite frontSprite;
    [SerializeField] private Sprite backSprite;
    [SerializeField] private Sprite[] symbols;

    private readonly List<MemoryCard> cards = new List<MemoryCard>();

    private void Awake()
    {
        InitializeBoard();
    }

    private void InitializeBoard()
    {
        int totalCards = columns * rows;

        if (totalCards <= 0 || totalCards % 2 != 0)
        {
            Debug.LogError("StudyMemoryBoard needs an even number of cards.");
            return;
        }

        if (symbols == null || symbols.Length < totalCards / 2)
        {
            Debug.LogError("StudyMemoryBoard needs 10 symbol sprites.");
            return;
        }

        if (cardPrefab == null || gridParent == null ||
            frontSprite == null || backSprite == null)
        {
            Debug.LogError("Assign all required StudyMemoryBoard references.");
            return;
        }

        List<int> symbolIDs = new List<int>();

        for (int i = 0; i < totalCards / 2; i++)
        {
            symbolIDs.Add(i);
            symbolIDs.Add(i);
        }

        // Shuffle the card positions.
        for (int i = symbolIDs.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            (symbolIDs[i], symbolIDs[j]) = (symbolIDs[j], symbolIDs[i]);
        }

        // Save the pattern for GameLevel.
        SharedCardPattern.SymbolIDs = new List<int>(symbolIDs);

        Debug.Log("StudyPhase pattern: " + string.Join(", ", symbolIDs));

        // Create cards face down.
        foreach (int symbolID in symbolIDs)
        {
            MemoryCard card = Instantiate(cardPrefab, gridParent);

            card.Initialize(
                symbolID,
                symbols[symbolID],
                frontSprite,
                backSprite
            );

            card.ShowBack();
            cards.Add(card);
        }
    }

    public void ShowAllCards()
    {
        foreach (MemoryCard card in cards)
        {
            if (card != null)
                card.ShowFront();
        }
    }

    public void HideAllCards()
    {
        foreach (MemoryCard card in cards)
        {
            if (card != null)
                card.ShowBack();
        }
    }
}