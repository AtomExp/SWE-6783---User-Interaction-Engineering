using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

// Shown between levels: a star rating, the level's stats, lives left and
// what the next level holds. The player moves on with Continue (or
// Enter / Space), so there is a calm break instead of an instant new board.
//
// Put this on a full-screen RectTransform that is the LAST child of the
// gameplay canvas (so it draws on top). It builds its own UI at runtime from
// the sprites below; while it is in the scene, GameManager waits for it
// instead of auto-starting the next level.
[RequireComponent(typeof(RectTransform))]
public class LevelProgressScreen : MonoBehaviour
{
    [Header("Art")]
    [Tooltip("Optional header art (e.g. the 'Level Complete! Well Done!' sign). " +
             "When set it replaces the title ribbon and the stars sit inside it.")]
    [SerializeField] private Sprite headerSprite;
    [SerializeField] private Sprite panelSprite;
    [SerializeField] private Sprite titleSprite;
    [SerializeField] private Sprite statsSprite;
    [SerializeField] private Sprite starSprite;
    [SerializeField] private Sprite heartSprite;
    [SerializeField] private Sprite continueSprite;
    [SerializeField] private TMP_FontAsset font;

    [Header("Colors")]
    [SerializeField] private Color dimColor = new Color(0f, 0f, 0f, 0.8f);
    [SerializeField] private Color textColor = new Color(0.29f, 0.15f, 0.07f);
    [SerializeField] private Color titleColor = Color.white;
    [SerializeField] private Color titleOutlineColor = new Color(0.1f, 0.3f, 0.05f);
    [SerializeField] private Color emptyStarColor = new Color(0.3f, 0.22f, 0.15f, 0.35f);
    [SerializeField] private Color lostHeartColor = new Color(0.25f, 0.25f, 0.25f, 0.4f);

    [Header("Timing")]
    [SerializeField] private float introSeconds = 0.35f;
    [SerializeField] private float starPopSeconds = 0.3f;
    [SerializeField] private float outroSeconds = 0.15f;

    private const int MaxHearts = 5;

    private GameManager manager;

    private GameObject content;
    private CanvasGroup group;
    private RectTransform panel;

    private TMP_Text titleText;
    private TMP_Text pairsValue;
    private TMP_Text mistakesValue;
    private TMP_Text bonusValue;
    private TMP_Text scoreValue;
    private TMP_Text nextText;

    private readonly Image[] stars = new Image[3];
    private readonly Image[] hearts = new Image[MaxHearts];
    private Button continueButton;

    private Coroutine routine;
    private bool isOpen;
    private bool continuing;

    // False during the intro so a click carried over from the last match
    // can't skip the screen. (Not CanvasGroup.interactable, which would grey
    // out the Continue button.)
    private bool inputReady;

    // --------------------------------------------------
    // LIFECYCLE
    // --------------------------------------------------

    private void Awake()
    {
        BuildUI();
        HideImmediate();
    }

    private void Start()
    {
        manager = GameManager.Instance;

        if (manager == null)
        {
            Debug.LogWarning("LevelProgressScreen: GameManager not found.", this);
            return;
        }

        manager.WaitForLevelContinue = true;
        manager.LevelCompleted += Show;
        manager.PhaseChanged += HandlePhaseChanged;
        manager.GameStarted += HandleGameStarted;
    }

    private void OnDestroy()
    {
        if (manager == null)
            return;

        manager.WaitForLevelContinue = false;
        manager.LevelCompleted -= Show;
        manager.PhaseChanged -= HandlePhaseChanged;
        manager.GameStarted -= HandleGameStarted;
    }

    private void Update()
    {
        if (!isOpen || continuing || !inputReady)
            return;

        Keyboard keyboard = Keyboard.current;

        if (keyboard != null &&
            (keyboard.enterKey.wasPressedThisFrame ||
             keyboard.numpadEnterKey.wasPressedThisFrame ||
             keyboard.spaceKey.wasPressedThisFrame))
        {
            Continue();
        }
    }

    private void HandlePhaseChanged(GamePhase phase)
    {
        if (phase != GamePhase.LevelComplete && !continuing)
            HideImmediate();
    }

    private void HandleGameStarted(GameState state) => HideImmediate();

    // --------------------------------------------------
    // SHOW / HIDE
    // --------------------------------------------------

    public void Show(LevelSummary summary)
    {
        if (routine != null)
            StopCoroutine(routine);

        if (titleText != null)
            titleText.text = $"Level {summary.CompletedLevel} Complete!";

        // With the header sign (no level number on it) the first row names
        // the level; otherwise the title does and the row shows pairs found.
        pairsValue.text = headerSprite != null
            ? summary.CompletedLevel.ToString()
            : summary.PairsFound.ToString();
        mistakesValue.text = summary.Mistakes.ToString();
        bonusValue.text = $"+{summary.LevelBonus:N0}";
        scoreValue.text = summary.Score.ToString("N0");
        nextText.text =
            $"Next: Level {summary.NextLevel}  ({summary.NextLevelPairs} pairs)";

        int maxLives = Mathf.Clamp(summary.MaxLives, 1, MaxHearts);

        for (int i = 0; i < hearts.Length; i++)
        {
            hearts[i].gameObject.SetActive(i < maxLives);
            hearts[i].color = i < summary.LivesLeft ? Color.white : lostHeartColor;

            // Keep the row centered for 3 (Hard) or 5 (Easy) lives.
            hearts[i].rectTransform.anchoredPosition =
                new Vector2((i - (maxLives - 1) / 2f) * 95f, 0f);
        }

        foreach (Image star in stars)
        {
            star.color = emptyStarColor;
            star.rectTransform.localScale = Vector3.one;
        }

        isOpen = true;
        continuing = false;
        inputReady = false;

        content.SetActive(true);
        transform.SetAsLastSibling();

        routine = StartCoroutine(Intro(summary.Stars));
    }

    private void Continue()
    {
        if (!isOpen || continuing || !inputReady)
            return;

        continuing = true;

        if (routine != null)
            StopCoroutine(routine);

        routine = StartCoroutine(Outro());
    }

    private void HideImmediate()
    {
        if (routine != null)
        {
            StopCoroutine(routine);
            routine = null;
        }

        isOpen = false;
        continuing = false;
        inputReady = false;

        if (group != null)
        {
            group.alpha = 0f;
            group.blocksRaycasts = false;
        }

        if (content != null)
            content.SetActive(false);
    }

    // --------------------------------------------------
    // ANIMATION (unscaled so it never stalls on timeScale)
    // --------------------------------------------------

    private IEnumerator Intro(int earnedStars)
    {
        group.alpha = 0f;
        group.blocksRaycasts = true;

        float duration = Mathf.Max(0.01f, introSeconds);

        for (float t = 0f; t < duration; t += Time.unscaledDeltaTime)
        {
            float p = t / duration;
            group.alpha = p;
            panel.localScale = Vector3.one * Mathf.LerpUnclamped(0.85f, 1f, EaseOutBack(p));
            yield return null;
        }

        group.alpha = 1f;
        panel.localScale = Vector3.one;
        inputReady = true;

        // Pop each earned star in turn.
        float popDuration = Mathf.Max(0.01f, starPopSeconds);

        for (int s = 0; s < Mathf.Clamp(earnedStars, 0, stars.Length); s++)
        {
            RectTransform star = stars[s].rectTransform;
            stars[s].color = Color.white;

            for (float t = 0f; t < popDuration; t += Time.unscaledDeltaTime)
            {
                star.localScale = Vector3.one * EaseOutBack(t / popDuration);
                yield return null;
            }

            star.localScale = Vector3.one;
        }

        routine = null;
    }

    private IEnumerator Outro()
    {
        float duration = Mathf.Max(0.01f, outroSeconds);

        for (float t = 0f; t < duration; t += Time.unscaledDeltaTime)
        {
            group.alpha = 1f - t / duration;
            yield return null;
        }

        routine = null;
        HideImmediate();

        if (manager != null)
            manager.ContinueToNextLevel();
    }

    private static float EaseOutBack(float p)
    {
        const float c1 = 1.70158f;
        const float c3 = c1 + 1f;
        float x = Mathf.Clamp01(p) - 1f;
        return 1f + c3 * x * x * x + c1 * x * x;
    }

    // --------------------------------------------------
    // UI CONSTRUCTION (layout in 1920x1080 canvas units)
    // --------------------------------------------------

    private void BuildUI()
    {
        RectTransform root = (RectTransform)transform;
        Stretch(root);

        content = new GameObject("Content", typeof(RectTransform), typeof(CanvasGroup));
        content.layer = gameObject.layer;
        RectTransform contentRect = (RectTransform)content.transform;
        contentRect.SetParent(root, false);
        Stretch(contentRect);
        group = content.GetComponent<CanvasGroup>();

        // Dim the board and swallow clicks behind the panel.
        Image dimmer = CreateImage("Dimmer", contentRect, null, Vector2.zero, Vector2.zero);
        Stretch(dimmer.rectTransform);
        dimmer.color = dimColor;

        bool hasHeader = headerSprite != null;

        panel = CreateImage("Panel", contentRect, panelSprite,
            new Vector2(0f, hasHeader ? -40f : -10f),
            new Vector2(hasHeader ? 920f : 940f, hasHeader ? 860f : 820f)).rectTransform;

        if (hasHeader)
        {
            // Header sign across the top edge of the panel; the stars sit in
            // the empty band under its "Well Done!" text.
            CreateImage("HeaderSign", panel, headerSprite,
                new Vector2(0f, 365f), new Vector2(600f, 337f));

            for (int i = 0; i < stars.Length; i++)
            {
                float y = i == 1 ? 266f : 258f;
                stars[i] = CreateImage($"Star{i + 1}", panel, starSprite,
                    new Vector2((i - 1) * 84f, y), new Vector2(66f, 66f));
            }
        }
        else
        {
            Image title = CreateImage("TitleRibbon", panel, titleSprite,
                new Vector2(0f, 370f), new Vector2(700f, 140f), sliced: true);
            titleText = CreateText("TitleText", title.rectTransform, "",
                Vector2.zero, new Vector2(660f, 120f), 62f, titleColor,
                TextAlignmentOptions.Center);
            titleText.outlineWidth = 0.25f;
            titleText.outlineColor = titleOutlineColor;

            for (int i = 0; i < stars.Length; i++)
            {
                float y = i == 1 ? 245f : 225f; // middle star sits a little higher
                stars[i] = CreateImage($"Star{i + 1}", panel, starSprite,
                    new Vector2((i - 1) * 150f, y), new Vector2(130f, 130f));
            }
        }

        Image stats = CreateImage("Stats", panel, statsSprite,
            new Vector2(0f, hasHeader ? 30f : 5f), new Vector2(680f, 300f), sliced: true);

        pairsValue = CreateStatRow(stats.rectTransform,
            hasHeader ? "Level cleared" : "Pairs found", 105f);
        mistakesValue = CreateStatRow(stats.rectTransform, "Mistakes", 35f);
        bonusValue = CreateStatRow(stats.rectTransform, "Level bonus", -35f);
        scoreValue = CreateStatRow(stats.rectTransform, "Total score", -105f);

        RectTransform heartRow = CreateRect("Hearts", panel,
            new Vector2(0f, hasHeader ? -170f : -185f), new Vector2(420f, 70f));

        for (int i = 0; i < hearts.Length; i++)
            hearts[i] = CreateImage($"Heart{i + 1}", heartRow, heartSprite,
                Vector2.zero, new Vector2(150f, 130f));

        nextText = CreateText("NextText", panel, "",
            new Vector2(0f, hasHeader ? -248f : -255f), new Vector2(760f, 60f), 42f, textColor,
            TextAlignmentOptions.Center);

        Image continueImage = CreateImage("ContinueButton", panel, continueSprite,
            new Vector2(0f, hasHeader ? -375f : -360f), new Vector2(460f, 120f));
        continueButton = continueImage.gameObject.AddComponent<Button>();
        continueButton.targetGraphic = continueImage;
        continueButton.onClick.AddListener(Continue);
        continueImage.gameObject.AddComponent<ButtonBounce>();
    }

    private TMP_Text CreateStatRow(RectTransform parent, string label, float y)
    {
        CreateText(label, parent, label,
            new Vector2(-95f, y), new Vector2(400f, 64f), 44f, textColor,
            TextAlignmentOptions.Left);

        return CreateText(label + " Value", parent, "",
            new Vector2(170f, y), new Vector2(260f, 64f), 48f, textColor,
            TextAlignmentOptions.Right);
    }

    private RectTransform CreateRect(string name, Transform parent, Vector2 position, Vector2 size)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.layer = gameObject.layer;

        RectTransform rect = (RectTransform)go.transform;
        rect.SetParent(parent, false);
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
        return rect;
    }

    private Image CreateImage(string name, Transform parent, Sprite sprite,
        Vector2 position, Vector2 size, bool sliced = false)
    {
        Image image = CreateRect(name, parent, position, size).gameObject.AddComponent<Image>();
        image.sprite = sprite;

        if (sprite != null)
        {
            image.type = sliced && sprite.border != Vector4.zero
                ? Image.Type.Sliced
                : Image.Type.Simple;
            image.preserveAspect = !sliced;
        }

        return image;
    }

    private TMP_Text CreateText(string name, Transform parent, string text,
        Vector2 position, Vector2 size, float fontSize, Color color,
        TextAlignmentOptions alignment)
    {
        TextMeshProUGUI label =
            CreateRect(name, parent, position, size).gameObject.AddComponent<TextMeshProUGUI>();

        if (font != null)
            label.font = font;

        label.text = text;
        label.fontSize = fontSize;
        label.color = color;
        label.alignment = alignment;
        label.textWrappingMode = TextWrappingModes.NoWrap;
        label.overflowMode = TextOverflowModes.Overflow;
        label.raycastTarget = false;
        return label;
    }

    private static void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = Vector2.zero;
    }
}
