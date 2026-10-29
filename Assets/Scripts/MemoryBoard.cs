using System.Collections.Generic;
using UnityEngine;
// using UnityEngine.UI;

public class MemoryBoard : MonoBehaviour
{
    [Header("Board Settings")]
    [SerializeField] private int columns = 5;
    [SerializeField] private int rows = 4;

    [Header("References")]
    [SerializeField] private MemoryCard cardPrefab;
    [SerializeField] private Transform gridParent;
    // [SerializeField] private Button startButton;

    [Header("Card Sprites")]
    [SerializeField] private Sprite frontSprite;
    [SerializeField] private Sprite backSprite;
    [SerializeField] private Sprite[] symbols;

    private List<MemoryCard> cards = new List<MemoryCard>();

    private void Start()
    {
        // Create the gameplay board when GameLevel loads.
        InitializeBoard();

        // startButton.onClick.AddListener(StartGame);
    }

    public void InitializeBoard()
    {
        // Remove any previously generated cards.
        foreach (MemoryCard card in cards)
        {
            if (card != null)
                Destroy(card.gameObject);
        }

        cards.Clear();

        int totalCards = columns * rows;

        // A matching-pairs board must have an even number of cards.
        if (totalCards <= 0 || totalCards % 2 != 0)
        {
            Debug.LogError("Board must have an even number of cards.");
            return;
        }

        int requiredPairs = totalCards / 2;

        // Ensure enough symbols are assigned in the Inspector.
        if (symbols == null || symbols.Length < requiredPairs)
        {
            Debug.LogError("Not enough symbols for this board!");
            return;
        }

        // Create two entries for every symbol to form matching pairs.
        List<int> symbolIDs = new List<int>();

        for (int i = 0; i < requiredPairs; i++)
        {
            symbolIDs.Add(i);
            symbolIDs.Add(i);
        }

        // Reuse the study pattern so gameplay shows the same card positions.
        if (SharedCardPattern.SymbolIDs != null &&
            SharedCardPattern.SymbolIDs.Count == totalCards)
        {
            // Copy the saved pattern from StudyPhase.
            symbolIDs = new List<int>(SharedCardPattern.SymbolIDs);

            // Log the pattern so it can be compared with the study pattern.
            Debug.Log("GameLevel: Reusing study pattern: " +
                      string.Join(", ", symbolIDs));

            // Clear the saved pattern after using it.
            SharedCardPattern.SymbolIDs = null;
        }
        else
        {
            // If GameLevel is opened independently, create a random pattern.
            Debug.LogWarning(
                "GameLevel: No study pattern found. Generating a new pattern."
            );

            Shuffle(symbolIDs);
        }

        // Generate cards using either the saved or newly shuffled pattern.
        // This loop must remain OUTSIDE the if/else block.
        for (int i = 0; i < symbolIDs.Count; i++)
        {
            int symbolID = symbolIDs[i];

            MemoryCard newCard = Instantiate(cardPrefab, gridParent);

            newCard.Initialize(
                symbolID,
                symbols[symbolID],
                frontSprite,
                backSprite
            );

            cards.Add(newCard);
        }
    }

    private void Shuffle(List<int> list)
    {
        // Randomize card positions using the Fisher-Yates shuffle.
        for (int i = list.Count - 1; i > 0; i--)
        {
            int randomIndex = Random.Range(0, i + 1);

            int temp = list[i];
            list[i] = list[randomIndex];
            list[randomIndex] = temp;
        }
    }

    public void StartGame()
    {
        // Hide all symbols when the game starts.
        foreach (MemoryCard card in cards)
        {
            if (card != null)
                card.ShowBack();
        }

        // startButton.gameObject.SetActive(false);
    }

    /*
    private void OnDestroy()
    {
        if (startButton != null)
            startButton.onClick.RemoveListener(StartGame);
    }
    */
}