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

    [Header("Mismatch")]
    [SerializeField] private Color mismatchTint = new Color(1f, 0.6f, 0.6f);
    [SerializeField] private float mismatchShakeAngle = 7f;
    [SerializeField] private float mismatchShakeSeconds = 0.35f;

    [Header("Flip Animation")]
    [Tooltip("Seconds for a full flip. 0 = swap instantly.")]
    [SerializeField] private float flipSeconds = 0.18f;

    private Sprite frontSprite;
    private Sprite backSprite;
    private Sprite assignedSymbol;

    private Vector3 baseScale = Vector3.one;
    private Quaternion baseRotation = Quaternion.identity;
    private bool hasBaseTransform;

    private Coroutine flipRoutine;
    private Coroutine shakeRoutine;

    public int SymbolID { get; private set; }
    public bool IsFaceUp { get; private set; }
    public bool IsMatched { get; private set; }

    // Raised when the card is clicked or tapped. GameManager decides what happens.
    public event Action<MemoryCard> Clicked;

    private void Awake()
    {
        CacheBaseTransform();
    }

    public void Initialize(
        int symbolID,
        Sprite symbol,
        Sprite front,
        Sprite back)
    {
        CacheBaseTransform();
        StopAnimations();

        SymbolID = symbolID;
        assignedSymbol = symbol;
        frontSprite = front;
        backSprite = back;

        IsMatched = false;
        cardImage.color = Color.white;

        // Cards are dealt face up for the observation phase; no flip needed.
        IsFaceUp = true;
        ApplyFace(true);
    }

    public void ShowFront()
    {
        SetFace(true);
    }

    public void ShowBack()
    {
        ShowBack(0f);
    }

    // `delay` staggers only the animation (e.g. the flip wave when matching
    // begins); the card counts as face down straight away.
    public void ShowBack(float delay)
    {
        // Clear any mismatch tint once the card is turned back over.
        if (!IsMatched)
            cardImage.color = Color.white;

        SetFace(false, delay);
    }

    // Keeps the card face up with a tint so found pairs stand out.
    public void ShowMatched()
    {
        IsMatched = true;
        cardImage.color = matchedTint;
        ShowFront();
    }

    // Tints the card red and gives it a short wobble so a wrong pick (and the
    // life it cost) is obvious. Uses rotation only, so it never fights the
    // GridLayoutGroup that positions the cards.
    public void ShowMismatch()
    {
        if (!IsMatched)
            cardImage.color = mismatchTint;

        if (shakeRoutine != null)
            StopCoroutine(shakeRoutine);

        shakeRoutine = isActiveAndEnabled && mismatchShakeSeconds > 0f
            ? StartCoroutine(Shake())
            : null;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        Clicked?.Invoke(this);
    }

    // --------------------------------------------------
    // ANIMATION
    // --------------------------------------------------

    private void SetFace(bool faceUp, float delay = 0f)
    {
        if (IsFaceUp == faceUp)
        {
            // Already showing (or already flipping to) this side.
            if (flipRoutine == null)
                ApplyFace(faceUp);

            return;
        }

        IsFaceUp = faceUp;

        if (flipRoutine != null)
        {
            StopCoroutine(flipRoutine);
            flipRoutine = null;
        }

        if (flipSeconds <= 0f || !isActiveAndEnabled)
        {
            ApplyFace(faceUp);
            SetWidth(1f);
            return;
        }

        flipRoutine = StartCoroutine(Flip(faceUp, delay));
    }

    private IEnumerator Flip(bool faceUp, float delay)
    {
        if (delay > 0f)
            yield return new WaitForSeconds(delay);

        float half = flipSeconds * 0.5f;

        // Squash to zero width (continuing from wherever an interrupted flip left off).
        float width = baseScale.x != 0f ? transform.localScale.x / baseScale.x : 1f;

        while (width > 0f)
        {
            width -= Time.deltaTime / half;
            SetWidth(Mathf.Max(0f, width));
            yield return null;
        }

        ApplyFace(faceUp);

        // Expand back out showing the new side.
        while (width < 1f)
        {
            width += Time.deltaTime / half;
            SetWidth(Mathf.Min(1f, width));
            yield return null;
        }

        flipRoutine = null;
    }

    private IEnumerator Shake()
    {
        float elapsed = 0f;

        while (elapsed < mismatchShakeSeconds)
        {
            float damping = 1f - elapsed / mismatchShakeSeconds;
            float angle = Mathf.Sin(elapsed * 55f) * mismatchShakeAngle * damping;

            transform.localRotation = baseRotation * Quaternion.Euler(0f, 0f, angle);

            elapsed += Time.deltaTime;
            yield return null;
        }

        transform.localRotation = baseRotation;
        shakeRoutine = null;
    }

    private void ApplyFace(bool faceUp)
    {
        if (faceUp)
        {
            cardImage.sprite = frontSprite;
            symbolImage.sprite = assignedSymbol;
            symbolImage.enabled = true;
        }
        else
        {
            cardImage.sprite = backSprite;
            symbolImage.enabled = false;
        }
    }

    private void SetWidth(float normalized)
    {
        transform.localScale = new Vector3(
            baseScale.x * normalized,
            baseScale.y,
            baseScale.z);
    }

    private void StopAnimations()
    {
        StopAllCoroutines();
        flipRoutine = null;
        shakeRoutine = null;

        if (hasBaseTransform)
        {
            transform.localScale = baseScale;
            transform.localRotation = baseRotation;
        }
    }

    private void CacheBaseTransform()
    {
        if (hasBaseTransform)
            return;

        baseScale = transform.localScale;
        baseRotation = transform.localRotation;
        hasBaseTransform = true;
    }
}
