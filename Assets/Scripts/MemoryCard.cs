using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class MemoryCard : MonoBehaviour, IPointerClickHandler
{
    [Header("Card Images")]
    [SerializeField] private Image cardImage;
    [SerializeField] private Image symbolImage;

    [Header("Matched")]
    [SerializeField] private Color matchedTint = new Color(0.7f, 1f, 0.7f);

    private Sprite frontSprite;
    private Sprite backSprite;
    private Sprite assignedSymbol;

    public int SymbolID { get; private set; }
    public bool IsFaceUp { get; private set; }
    public bool IsMatched { get; private set; }

    // Raised when the card is clicked or tapped. GameManager decides what happens.
    public event Action<MemoryCard> Clicked;

    public void Initialize(
        int symbolID,
        Sprite symbol,
        Sprite front,
        Sprite back)
    {
        SymbolID = symbolID;
        assignedSymbol = symbol;
        frontSprite = front;
        backSprite = back;

        IsMatched = false;
        cardImage.color = Color.white;

        ShowFront();
    }

    public void ShowFront()
    {
        cardImage.sprite = frontSprite;
        symbolImage.sprite = assignedSymbol;
        symbolImage.enabled = true;

        IsFaceUp = true;
    }

    public void ShowBack()
    {
        cardImage.sprite = backSprite;
        symbolImage.enabled = false;

        IsFaceUp = false;
    }

    // Keeps the card face up with a tint so found pairs stand out.
    public void ShowMatched()
    {
        ShowFront();
        IsMatched = true;
        cardImage.color = matchedTint;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        Clicked?.Invoke(this);
    }
}
