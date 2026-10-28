using UnityEngine;
using UnityEngine.UI;

public class MemoryCard : MonoBehaviour
{
    [Header("Card Images")]
    [SerializeField] private Image cardImage;
    [SerializeField] private Image symbolImage;

    private Sprite frontSprite;
    private Sprite backSprite;
    private Sprite assignedSymbol;

    public int SymbolID { get; private set; }
    public bool IsFaceUp { get; private set; }

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
}
