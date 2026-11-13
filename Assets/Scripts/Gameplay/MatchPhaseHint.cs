using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// A short label shown when the cards turn over for matching, telling the
// player the cards kept their places (the arrangement they memorized is the
// one they now match in). Fades out by itself so it never clutters the board.
//
// Place on a RectTransform anchored to the top edge of the card tray. Builds
// its own pill + text at runtime.
[RequireComponent(typeof(RectTransform))]
public class MatchPhaseHint : MonoBehaviour
{
    [SerializeField] private Sprite pillSprite;
    [SerializeField] private TMP_FontAsset font;

    [SerializeField] private string message =
        "Same cards, same spots. Find the matching pairs!";

    [SerializeField] private Color textColor = new Color(0.29f, 0.15f, 0.07f);

    [Tooltip("Show on every level instead of only the first level of a game.")]
    [SerializeField] private bool showEveryLevel;

    [SerializeField] private float holdSeconds = 2.6f;
    [SerializeField] private float fadeSeconds = 0.3f;

    private GameManager manager;
    private CanvasGroup group;
    private GamePhase lastPhase = GamePhase.NotStarted;
    private Coroutine routine;

    private void Awake()
    {
        BuildUI();
        group.alpha = 0f;
    }

    private void Start()
    {
        manager = GameManager.Instance;

        if (manager == null)
            return;

        lastPhase = manager.Phase;
        manager.PhaseChanged += HandlePhaseChanged;
    }

    private void OnDestroy()
    {
        if (manager != null)
            manager.PhaseChanged -= HandlePhaseChanged;
    }

    private void HandlePhaseChanged(GamePhase phase)
    {
        bool matchingStarted =
            lastPhase == GamePhase.Observe && phase == GamePhase.Selecting;

        lastPhase = phase;

        if (matchingStarted &&
            (showEveryLevel || manager.State == null || manager.State.Level == 1))
        {
            if (routine != null)
                StopCoroutine(routine);

            routine = StartCoroutine(Play());
        }
        else if (phase != GamePhase.Selecting && phase != GamePhase.Checking)
        {
            // Level ended, game over, restart: get out of the way at once.
            if (routine != null)
                StopCoroutine(routine);

            routine = null;
            group.alpha = 0f;
        }
    }

    private IEnumerator Play()
    {
        float fade = Mathf.Max(0.01f, fadeSeconds);

        for (float t = 0f; t < fade; t += Time.unscaledDeltaTime)
        {
            group.alpha = t / fade;
            yield return null;
        }

        group.alpha = 1f;

        // Scaled time: the hint waits while the game is paused.
        yield return new WaitForSeconds(holdSeconds);

        for (float t = 0f; t < fade; t += Time.unscaledDeltaTime)
        {
            group.alpha = 1f - t / fade;
            yield return null;
        }

        group.alpha = 0f;
        routine = null;
    }

    private void BuildUI()
    {
        group = gameObject.AddComponent<CanvasGroup>();
        group.blocksRaycasts = false;
        group.interactable = false;

        RectTransform root = (RectTransform)transform;

        Image pill = new GameObject("Pill", typeof(RectTransform), typeof(Image))
            .GetComponent<Image>();
        pill.gameObject.layer = gameObject.layer;
        pill.rectTransform.SetParent(root, false);
        Stretch(pill.rectTransform);
        pill.sprite = pillSprite;
        pill.raycastTarget = false;

        if (pillSprite != null && pillSprite.border != Vector4.zero)
        {
            pill.type = Image.Type.Sliced;
            // Keep the rounded ends no taller than the pill itself.
            pill.pixelsPerUnitMultiplier =
                Mathf.Max(1f, 2f * pillSprite.border.y / Mathf.Max(1f, root.rect.height));
        }

        TextMeshProUGUI label = new GameObject("Text", typeof(RectTransform))
            .AddComponent<TextMeshProUGUI>();
        label.gameObject.layer = gameObject.layer;
        label.rectTransform.SetParent(root, false);
        Stretch(label.rectTransform);

        if (font != null)
            label.font = font;

        label.text = message;
        label.fontSize = 36f;
        label.color = textColor;
        label.alignment = TextAlignmentOptions.Center;
        label.textWrappingMode = TextWrappingModes.NoWrap;
        label.raycastTarget = false;
    }

    private static void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }
}
