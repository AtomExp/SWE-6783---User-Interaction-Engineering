using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
public class StudyMemoryBoard : MonoBehaviour
{
    [Header("Board Settings")]
    [SerializeField] private int easyColumns = 3;
    [SerializeField] private int hardColumns = 5;

    [Header("References")]
    [SerializeField] private MemoryCard cardPrefab;
    [SerializeField] private Transform gridParent;

    [Header("Card Sprites")]
    [SerializeField] private Sprite frontSprite;
    [SerializeField] private Sprite backSprite;
    [SerializeField] private Sprite[] symbols;

    private readonly List<MemoryCard> cards =
        new List<MemoryCard>();

    private void Awake()
    {
        InitializeBoard();
    }

    private void InitializeBoard()
    {
        // Determine how many pairs are required for this level.
        int requiredPairs;

        if (GameManager.Instance != null &&
            GameManager.Instance.State != null)
        {
            // Later levels: read the pair count from the saved game state.
            requiredPairs =
                GameManager.Instance.State.PairsThisLevel;
        }
        else
        {
            // First visit to StudyPhase:
            // GameManager does not exist yet because it is created
            // when GameLevel loads for the first time.
            requiredPairs =
                GameSettings.SelectedDifficulty == Difficulty.Easy
                    ? 3
                    : 10;
        }

        int totalCards = requiredPairs * 2;

        // Choose the maximum number of columns for this difficulty.
        int maxColumns =
            GameSettings.SelectedDifficulty == Difficulty.Easy
                ? easyColumns
                : hardColumns;

        // Calculate a suitable number of columns for the board.
        int columns = Mathf.Min(
            maxColumns,
            Mathf.CeilToInt(Mathf.Sqrt(totalCards))
        );

        columns = Mathf.Max(1, columns);

        // Validate the required references.
        if (cardPrefab == null || gridParent == null ||
            frontSprite == null || backSprite == null)
        {
            Debug.LogError(
                "StudyMemoryBoard: Assign all required references."
            );
            return;
        }

        // Check whether enough symbol sprites are available.
        if (symbols == null || symbols.Length < requiredPairs)
        {
            Debug.LogError(
                "StudyMemoryBoard needs at least " +
                requiredPairs + " symbol sprites, but only " +
                (symbols == null ? 0 : symbols.Length) +
                " are assigned."
            );
            return;
        }

        // Configure the grid layout.
        GridLayoutGroup grid =
            gridParent.GetComponent<GridLayoutGroup>();

        if (grid != null)
        {
            grid.constraint =
                GridLayoutGroup.Constraint.FixedColumnCount;

            grid.constraintCount = columns;
        }
        else
        {
            Debug.LogWarning(
                "StudyMemoryBoard: GridLayoutGroup is missing."
            );
        }

        // Generate two cards for each symbol.
        List<int> symbolIDs = new List<int>();

        for (int i = 0; i < requiredPairs; i++)
        {
            symbolIDs.Add(i);
            symbolIDs.Add(i);
        }

        // Shuffle the cards so their positions are random.
        for (int i = symbolIDs.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);

            (symbolIDs[i], symbolIDs[j]) =
                (symbolIDs[j], symbolIDs[i]);
        }

        // Save this pattern so GameLevel uses the exact same cards.
        SharedCardPattern.SymbolIDs =
            new List<int>(symbolIDs);

        Debug.Log(
            "StudyPhase: " +
            GameSettings.SelectedDifficulty +
            " | Level: " +
            (GameManager.Instance != null &&
             GameManager.Instance.State != null
                ? GameManager.Instance.State.Level
                : 1) +
            " | Pairs: " + requiredPairs +
            " | Cards: " + totalCards
        );

        // Create all cards face down.
        foreach (int symbolID in symbolIDs)
        {
            MemoryCard card =
                Instantiate(cardPrefab, gridParent);

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

    // Called by StudyPhaseTimer when observation begins.
    public void ShowAllCards()
    {
        foreach (MemoryCard card in cards)
        {
            if (card != null)
                card.ShowFront();
        }
    }

    // Called when READY is pressed or the timer expires.
    public void HideAllCards()
    {
        foreach (MemoryCard card in cards)
        {
            if (card != null)
                card.ShowBack();
        }
    }
}