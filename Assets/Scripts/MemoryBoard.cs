using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class MemoryBoard : MonoBehaviour
{
    [Header("Board Settings")]
    [SerializeField] private int columns = 5;
    [SerializeField] private int rows = 4;

    [Tooltip("GameManager controls board creation.")]
    [SerializeField] private bool buildOnStart = true;

    [Header("References")]
    [SerializeField] private MemoryCard cardPrefab;
    [SerializeField] private Transform gridParent;
    [SerializeField] private Button startButton;

    [Header("Card Sprites")]
    [SerializeField] private Sprite frontSprite;
    [SerializeField] private Sprite backSprite;
    [SerializeField] private Sprite[] symbols;

    private readonly List<MemoryCard> cards =
        new List<MemoryCard>();

    public IReadOnlyList<MemoryCard> Cards => cards;

    public int SymbolCount =>
        symbols != null ? symbols.Length : 0;

    public bool BuildOnStart
    {
        get => buildOnStart;
        set => buildOnStart = value;
    }

    // Raised when observation ends.
    public event Action Started;

    private void Start()
    {
        if (buildOnStart)
            InitializeBoard();

        if (startButton != null)
            startButton.onClick.AddListener(StartGame);
    }

    public void InitializeBoard()
    {
        int totalCards = columns * rows;

        if (totalCards <= 0 || totalCards % 2 != 0)
        {
            Debug.LogError(
                "MemoryBoard: Board must have an even number of cards.",
                this);
            return;
        }

        BuildBoard(totalCards / 2);
    }

    public void BuildBoard(int requiredPairs)
    {
        // Remove previously generated cards.
        foreach (MemoryCard card in cards)
        {
            if (card != null)
            {
                card.transform.SetParent(null);
                Destroy(card.gameObject);
            }
        }

        cards.Clear();

        // Validate pair count.
        if (requiredPairs <= 0)
        {
            Debug.LogError(
                "MemoryBoard: Required pairs must be greater than zero.",
                this);
            return;
        }

        // Validate symbols.
        if (symbols == null || symbols.Length < requiredPairs)
        {
            Debug.LogError(
                $"MemoryBoard: Needs {requiredPairs} symbols, " +
                $"but only {SymbolCount} are assigned.",
                this);
            return;
        }

        // Validate references.
        if (cardPrefab == null ||
            gridParent == null ||
            frontSprite == null ||
            backSprite == null)
        {
            Debug.LogError(
                "MemoryBoard: Assign all required references.",
                this);
            return;
        }

        // Generate unique symbol IDs.
        List<int> availableSymbols = new List<int>();

        for (int i = 0; i < symbols.Length; i++)
            availableSymbols.Add(i);

        Shuffle(availableSymbols);

        // Generate two copies of each selected symbol.
        List<int> symbolIDs = new List<int>();

        for (int i = 0; i < requiredPairs; i++)
        {
            int symbolID = availableSymbols[i];

            symbolIDs.Add(symbolID);
            symbolIDs.Add(symbolID);
        }

        // Shuffle card positions.
        Shuffle(symbolIDs);

        // Create cards.
        foreach (int symbolID in symbolIDs)
        {
            MemoryCard newCard =
                Instantiate(cardPrefab, gridParent);

            newCard.Initialize(
                symbolID,
                symbols[symbolID],
                frontSprite,
                backSprite
            );

            cards.Add(newCard);
        }

        // Configure the grid.
        FitGrid(requiredPairs * 2);

        // Show Ready button during observation.
        if (startButton != null)
            startButton.gameObject.SetActive(true);

        Debug.Log(
            $"MemoryBoard: Created {requiredPairs} pairs " +
            $"({cards.Count} cards).",
            this
        );
    }

    private void FitGrid(int cardCount)
    {
        GridLayoutGroup grid =
            gridParent != null
                ? gridParent.GetComponent<GridLayoutGroup>()
                : null;

        if (grid == null)
            return;

        grid.constraint =
            GridLayoutGroup.Constraint.FixedColumnCount;

        int bestColumns = Mathf.Min(
            columns,
            Mathf.CeilToInt(Mathf.Sqrt(cardCount))
        );

        bestColumns = Mathf.Max(1, bestColumns);

        // Find a suitable column count.
        for (int c = bestColumns; c <= columns; c++)
        {
            if (cardCount % c == 0)
            {
                bestColumns = c;
                break;
            }
        }

        grid.constraintCount = bestColumns;
    }

    public void SetStartButtonVisible(bool visible)
    {
        if (startButton != null)
            startButton.gameObject.SetActive(visible);
    }

    private void Shuffle(List<int> list)
    {
        // Fisher-Yates shuffle.
        for (int i = list.Count - 1; i > 0; i--)
        {
            int randomIndex =
                UnityEngine.Random.Range(0, i + 1);

            int temp = list[i];

            list[i] = list[randomIndex];
            list[randomIndex] = temp;
        }
    }

    public void StartGame()
    {
        if (cards.Count == 0)
        {
            Debug.LogWarning(
                "MemoryBoard: Cannot start because no cards exist.",
                this);
            return;
        }

        // Hide all card symbols.
        foreach (MemoryCard card in cards)
        {
            if (card != null)
                card.ShowBack();
        }

        SetStartButtonVisible(false);

        Started?.Invoke();
    }

    private void OnDestroy()
    {
        if (startButton != null)
            startButton.onClick.RemoveListener(StartGame);
    }
}