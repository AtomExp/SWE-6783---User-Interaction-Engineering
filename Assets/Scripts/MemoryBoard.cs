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

    [Header("Card Sizing")]
    [Tooltip("Cards are sized to fill this area (e.g. the wooden tray). " +
             "If empty, the grid's GridLayoutGroup cell size is used instead.")]
    [SerializeField] private RectTransform fitArea;

    [Tooltip("Inset from the fit area's edges, as a fraction of its size " +
             "(keeps cards off the tray's wooden frame).")]
    [SerializeField] private Vector2 fitAreaInset = new Vector2(0.08f, 0.1f);

    [SerializeField] private float cardSpacing = 6f;
    [SerializeField] private float minCardSize = 90f;
    [SerializeField] private float maxCardSize = 250f;
    [SerializeField] private int maxColumns = 8;

    [Header("Card Sprites")]
    [SerializeField] private Sprite frontSprite;
    [SerializeField] private Sprite backSprite;
    [SerializeField] private Sprite[] symbols;

    private readonly List<MemoryCard> cards =
        new List<MemoryCard>();

    private Vector2 lastFitSize;

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

        // Size and place the cards.
        if (fitArea != null)
            LayoutCardsToFit();
        else
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

    private void LateUpdate()
    {
        // Re-fit if the tray changes size (e.g. a stretch-anchored fit area
        // while the window is resized).
        if (fitArea != null && cards.Count > 0 &&
            fitArea.rect.size != lastFitSize)
        {
            LayoutCardsToFit();
        }
    }

    // Picks the column count that gives the largest square cards inside the
    // fit area, then positions the cards manually so a partial last row is
    // centered (GridLayoutGroup would left-align it).
    private void LayoutCardsToFit()
    {
        int count = cards.Count;

        if (count == 0 || fitArea == null || gridParent == null)
            return;

        lastFitSize = fitArea.rect.size;

        GridLayoutGroup grid = gridParent.GetComponent<GridLayoutGroup>();

        if (grid != null)
            grid.enabled = false;

        Vector2 area = new Vector2(
            lastFitSize.x * (1f - 2f * fitAreaInset.x),
            lastFitSize.y * (1f - 2f * fitAreaInset.y));

        int bestColumns = 1;
        float bestSize = -1f;

        for (int c = 1; c <= Mathf.Min(count, Mathf.Max(1, maxColumns)); c++)
        {
            int r = Mathf.CeilToInt(count / (float)c);

            float size = Mathf.Min(
                (area.x - (c - 1) * cardSpacing) / c,
                (area.y - (r - 1) * cardSpacing) / r);

            // Prefer full rows when the size is (nearly) as good.
            if (count % c != 0)
                size *= 0.97f;

            if (size > bestSize)
            {
                bestSize = size;
                bestColumns = c;
            }
        }

        float cardSize = Mathf.Clamp(bestSize, minCardSize, maxCardSize);
        int columns = bestColumns;
        int rowCount = Mathf.CeilToInt(count / (float)columns);
        float pitch = cardSize + cardSpacing;

        // Convert the fit area's center into the grid parent's space.
        RectTransform parentRect = (RectTransform)gridParent;
        Vector3 worldCenter = fitArea.TransformPoint(fitArea.rect.center);
        Vector2 center = parentRect.InverseTransformPoint(worldCenter);

        for (int i = 0; i < count; i++)
        {
            int row = i / columns;
            int column = i % columns;

            int inThisRow = row == rowCount - 1
                ? count - row * columns
                : columns;

            RectTransform rect = (RectTransform)cards[i].transform;

            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(cardSize, cardSize);
            rect.anchoredPosition = center + new Vector2(
                (column - (inThisRow - 1) / 2f) * pitch,
                ((rowCount - 1) / 2f - row) * pitch);
        }
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