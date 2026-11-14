using System;
using System.Collections;
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

    [Header("Incorrect Feedback")]
    [SerializeField] private Color incorrectFlash = Color.red;
    [SerializeField] private float flashDuration = 3f;

    private Sprite frontSprite;
    private Sprite backSprite;
    private Sprite assignedSymbol;

    private Coroutine flashCoroutine;

    public int SymbolID { get; private set; }
    public bool IsFaceUp { get; private set; }
    public bool IsMatched { get; private set; }

    // Raised when the card is clicked or tapped.
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

        if (flashCoroutine != null)
        {
            StopCoroutine(flashCoroutine);
            flashCoroutine = null;
        }

        cardImage.color = Color.white;

        ShowFront();
    }

    public void ShowFront()
    {
        cardImage.sprite = frontSprite;
        cardImage.color = Color.white;

        symbolImage.sprite = assignedSymbol;
        symbolImage.enabled = true;

        IsFaceUp = true;
    }

    public void ShowBack()
    {
        if (flashCoroutine != null)
        {
            StopCoroutine(flashCoroutine);
            flashCoroutine = null;
        }

        cardImage.color = Color.white;
        cardImage.sprite = backSprite;
        symbolImage.enabled = false;

        IsFaceUp = false;
    }

    // Keeps matched cards visible with a green tint.
    public void ShowMatched()
    {
        ShowFront();

        IsMatched = true;
        cardImage.color = matchedTint;
    }

    // Brief red flash for an incorrect match.
    public void PlayIncorrectFeedback()
    {
        if (flashCoroutine != null)
            StopCoroutine(flashCoroutine);

        flashCoroutine = StartCoroutine(FlashRed());
    }

    private IEnumerator FlashRed()
    {
        cardImage.color = incorrectFlash;

        yield return new WaitForSeconds(flashDuration);

        cardImage.color = Color.white;
        flashCoroutine = null;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        Clicked?.Invoke(this);
    }
}