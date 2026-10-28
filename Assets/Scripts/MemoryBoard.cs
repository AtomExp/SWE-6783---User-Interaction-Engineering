
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class MemoryBoard : MonoBehaviour
{
    [Header("Board Settings")]
    [SerializeField] private int columns = 5;
    [SerializeField] private int rows = 4;

    [Header("References")]
    [SerializeField] private MemoryCard cardPrefab;
    [SerializeField] private Transform gridParent;
    [SerializeField] private Button startButton;

    [Header("Card Sprites")]
    [SerializeField] private Sprite frontSprite;
    [SerializeField] private Sprite backSprite;
    [SerializeField] private Sprite[] symbols;

    private List<MemoryCard> cards = new List<MemoryCard>();

    private void Start()
    {
        InitializeBoard();

        startButton.onClick.AddListener(StartGame);
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

        if (totalCards <= 0 || totalCards % 2 != 0)
        {
            Debug.LogError("Board must have an even number of cards.");
            return;
        }

        int requiredPairs = totalCards / 2;

        if (symbols == null || symbols.Length < requiredPairs)
        {
            Debug.LogError("Not enough symbols for this board!");
            return;
        }

        // Create two entries for every symbol.
        List<int> symbolIDs = new List<int>();

        for (int i = 0; i < requiredPairs; i++)
        {
            symbolIDs.Add(i);
            symbolIDs.Add(i);
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
    }

    private void Shuffle(List<int> list)
    {
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
        // Hide all the symbols.
        foreach (MemoryCard card in cards)
        {
            card.ShowBack();
        }

        // Hide the start button after pressing it.
        startButton.gameObject.SetActive(false);
    }

    private void OnDestroy()
    {
        if (startButton != null)
            startButton.onClick.RemoveListener(StartGame);
    }
}
