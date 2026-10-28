
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class MemoryBoard : MonoBehaviour
{
    [Header("Board Settings")]
    [SerializeField] private int columns = 5;
    [SerializeField] private int rows = 4;

    [Tooltip("Build the columns x rows board on Start. GameManager turns this off and builds each level itself.")]
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

    // Raised when the symbols are hidden (Start pressed or StartGame called).
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

    // Builds a board with the given number of pairs, using a random set of symbols.
    public void BuildBoard(int requiredPairs)
    {
        // Remove any previously generated cards.
        foreach (MemoryCard card in cards)
        {
            if (card != null)
            {
                // Detach first so the grid doesn't lay out the old cards for one more frame.
                card.transform.SetParent(null);
                Destroy(card.gameObject);
            }
        }

        cards.Clear();

        if (requiredPairs <= 0 || symbols == null || symbols.Length < requiredPairs)
        {
            Debug.LogError("Not enough symbols for this board!");
            return;
        }

        // Pick which symbols to use this time.
        List<int> available = new List<int>();

        for (int i = 0; i < symbols.Length; i++)
            available.Add(i);

        Shuffle(available);

        // Create two entries for every chosen symbol.
        List<int> symbolIDs = new List<int>();

        for (int i = 0; i < requiredPairs; i++)
        {
            symbolIDs.Add(available[i]);
            symbolIDs.Add(available[i]);
        }

        // Shuffle the card positions.
        Shuffle(symbolIDs);

        // Generate the board.
        for (int i = 0; i < symbolIDs.Count; i++)
        {
            int symbolID = symbolIDs[i];

            MemoryCard newCard = Instantiate(
                cardPrefab,
                gridParent
            );

            newCard.Initialize(
                symbolID,
                symbols[symbolID],
                frontSprite,
                backSprite
            );

            cards.Add(newCard);
        }

        FitGrid(symbolIDs.Count);

        if (startButton != null)
            startButton.gameObject.SetActive(true);
    }

    // Picks a column count that fills rows evenly (6 cards = 3x2, 8 = 4x2, 12 = 4x3), up to the columns setting.
    private void FitGrid(int cardCount)
    {
        GridLayoutGroup grid = gridParent != null ? gridParent.GetComponent<GridLayoutGroup>() : null;
        if (grid == null || grid.constraint != GridLayoutGroup.Constraint.FixedColumnCount)
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
        // Hide all the symbols.
        foreach (MemoryCard card in cards)
        {
            card.ShowBack();
        }

        // Hide the start button after pressing it.
        SetStartButtonVisible(false);

        Started?.Invoke();
    }

    private void OnDestroy()
    {
        if (startButton != null)
            startButton.onClick.RemoveListener(StartGame);
    }
}
