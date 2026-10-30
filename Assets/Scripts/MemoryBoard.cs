using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class MemoryBoard : MonoBehaviour
{
    [Header("Board Settings")]
    [SerializeField] private int columns = 5;
    [SerializeField] private int rows = 4;

    [Tooltip("Build the board on Start. GameManager can disable this and build each level itself.")]
    [SerializeField] private bool buildOnStart = true;

    [Header("References")]
    [SerializeField] private MemoryCard cardPrefab;
    [SerializeField] private Transform gridParent;
    [SerializeField] private Button startButton;

    [Header("Card Sprites")]
    [SerializeField] private Sprite frontSprite;
    [SerializeField] private Sprite backSprite;
    [SerializeField] private Sprite[] symbols;

    private List<MemoryCard> cards = new List<MemoryCard>();

    public IReadOnlyList<MemoryCard> Cards => cards;

    public int SymbolCount => symbols != null ? symbols.Length : 0;

    public bool BuildOnStart
    {
        get => buildOnStart;
        set => buildOnStart = value;
    }

    // Raised when the symbols are hidden.
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
            Debug.LogError("Board must have an even number of cards.");
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

        if (requiredPairs <= 0 ||
            symbols == null ||
            symbols.Length < requiredPairs)
        {
            Debug.LogError("Not enough symbols for this board!");
            return;
        }

        if (cardPrefab == null || gridParent == null ||
            frontSprite == null || backSprite == null)
        {
            Debug.LogError("MemoryBoard: Assign all required references.");
            return;
        }

        int totalCards = requiredPairs * 2;

        List<int> symbolIDs = new List<int>();

        // Check whether a study pattern is available.
        if (SharedCardPattern.SymbolIDs != null &&
            SharedCardPattern.SymbolIDs.Count == totalCards)
        {
            symbolIDs = new List<int>(SharedCardPattern.SymbolIDs);

            // Validate the saved pattern.
            foreach (int symbolID in symbolIDs)
            {
                if (symbolID < 0 || symbolID >= symbols.Length)
                {
                    Debug.LogError("Invalid symbol ID in shared card pattern.");
                    SharedCardPattern.SymbolIDs = null;
                    return;
                }
            }

            Debug.Log(
                "GameLevel: Reusing study pattern: " +
                string.Join(", ", symbolIDs)
            );

            SharedCardPattern.SymbolIDs = null;
        }
        else
        {
            // Generate a random pattern if no study pattern exists.
            Debug.LogWarning(
                "GameLevel: No study pattern found. Generating a new pattern."
            );

            List<int> available = new List<int>();

            for (int i = 0; i < symbols.Length; i++)
                available.Add(i);

            Shuffle(available);

            // Add two copies of each selected symbol.
            for (int i = 0; i < requiredPairs; i++)
            {
                symbolIDs.Add(available[i]);
                symbolIDs.Add(available[i]);
            }

            Shuffle(symbolIDs);
        }

        // Generate the cards.
        foreach (int symbolID in symbolIDs)
        {
            MemoryCard newCard = Instantiate(cardPrefab, gridParent);

            newCard.Initialize(
                symbolID,
                symbols[symbolID],
                frontSprite,
                backSprite
            );

            cards.Add(newCard);
        }

        FitGrid(totalCards);

        if (startButton != null)
            startButton.gameObject.SetActive(true);
    }

    private void FitGrid(int cardCount)
    {
        GridLayoutGroup grid = gridParent != null
            ? gridParent.GetComponent<GridLayoutGroup>()
            : null;

        if (grid == null ||
            grid.constraint != GridLayoutGroup.Constraint.FixedColumnCount)
            return;

        int bestColumns = columns;
        int start = Mathf.CeilToInt(Mathf.Sqrt(cardCount));

        for (int c = start; c <= columns; c++)
        {
            if (cardCount % c == 0)
            {
                bestColumns = c;
                break;
            }
        }

        grid.constraintCount = Mathf.Max(1, bestColumns);
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
            int randomIndex = UnityEngine.Random.Range(0, i + 1);

            int temp = list[i];
            list[i] = list[randomIndex];
            list[randomIndex] = temp;
        }
    }

    public void StartGame()
    {
        // Hide all symbols.
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
