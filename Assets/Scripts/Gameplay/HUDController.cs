using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class HUDController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GameManager manager;

    [Header("Buttons")]
    [SerializeField] private GameObject homeButton;
    [SerializeField] private GameObject readyButton;
    [SerializeField] private GameObject pauseButton;

    [Header("Phase Visibility")]
    [SerializeField] private GameObject instructionBannerRoot;
    [SerializeField] private GameObject gameplayHudRoot;

    [Header("Game Over")]
    [SerializeField] private GameObject gameOverPanel;
    [SerializeField] private TMP_Text finalScoreValue;
    [SerializeField] private TMP_Text levelReachedValue;

    [Header("Text")]
    [SerializeField] private TMP_Text scoreText;
    [SerializeField] private TMP_Text livesText;
    [SerializeField] private TMP_Text levelText;
    [SerializeField] private TMP_Text difficultyText;
    [SerializeField] private TMP_Text messageText;
    [SerializeField] private TMP_Text timerText;
    [SerializeField] private TMP_Text pairsText;

    [Header("Optional")]
    [SerializeField] private GameObject timerRoot;
    [SerializeField] private Image[] lifeIcons;

    [Header("Messages")]
    [SerializeField] private string observeMessage =
        "Look at the pattern carefully!";

    [SerializeField] private string observeTimedMessage =
        "Look at the pattern carefully!";

    [SerializeField] private string selectMessage =
        "Find the matching pairs!";

    [SerializeField] private string levelCompleteMessage =
        "Level clear! Next level...";

    [SerializeField] private string gameOverMessage =
        "Out of lives!";

    [Header("Animation")]
    [Tooltip("Seconds for a heart to pop out (life lost) or pop in (lives restored).")]
    [SerializeField] private float heartPopSeconds = 0.35f;

    [Tooltip("How quickly the remaining hearts slide to re-center.")]
    [SerializeField] private float heartSlideSpeed = 12f;

    [SerializeField] private float gameOverPanelFadeSeconds = 0.3f;

    private GameState boundState;

    private float heartCenterX;
    private float heartSpacing;
    private bool heartLayoutInitialized;

    private float[] heartTargetX;
    private Vector3[] heartBaseScales;
    private Color[] heartBaseColors;
    private Coroutine[] heartRoutines;
    private int shownLives = -1;

    private Vector3 gameOverPanelBaseScale = Vector3.one;
    private Coroutine gameOverPanelRoutine;

    private void Start()
    {
        // Always retrieve the persistent GameManager.
        // Do not rely on a potentially stale Inspector reference.
        manager = GameManager.Instance;

        if (manager == null)
        {
            Debug.LogError(
                "HUDController: Persistent GameManager instance not found.",
                this
            );

            enabled = false;
            return;
        }

        Debug.Log(
            $"HUD START | Difficulty: {manager.Difficulty} | " +
            $"Phase: {manager.Phase} | " +
            $"State exists: {manager.State != null}"
        );

        if (timerRoot == null && timerText != null)
            timerRoot = timerText.gameObject;

        InitializeHeartLayout();

        if (gameOverPanel != null)
            gameOverPanelBaseScale = gameOverPanel.transform.localScale;

        manager.GameStarted += Bind;
        manager.PhaseChanged += HandlePhaseChanged;
        manager.ObservationTick += HandleTimerTick;
        manager.PairChecked += HandlePairChecked;
        manager.GameEnded += HandleGameEnded;

        if (manager.State != null)
            Bind(manager.State);

        HandlePhaseChanged(manager.Phase);
    }

    private void OnDestroy()
    {
        Unbind();

        if (manager == null)
            return;

        manager.GameStarted -= Bind;
        manager.PhaseChanged -= HandlePhaseChanged;
        manager.ObservationTick -= HandleTimerTick;
        manager.PairChecked -= HandlePairChecked;
        manager.GameEnded -= HandleGameEnded;
    }

    private void Update()
    {
        // Slide visible hearts toward their centered slots.
        if (!heartLayoutInitialized || lifeIcons == null)
            return;

        float t = 1f - Mathf.Exp(-heartSlideSpeed * Time.unscaledDeltaTime);

        for (int i = 0; i < lifeIcons.Length; i++)
        {
            if (lifeIcons[i] == null || i >= shownLives)
                continue;

            RectTransform rect = lifeIcons[i].rectTransform;
            Vector2 position = rect.anchoredPosition;

            position.x = Mathf.Lerp(position.x, heartTargetX[i], t);
            rect.anchoredPosition = position;
        }
    }

    private void InitializeHeartLayout()
    {
        if (lifeIcons != null)
        {
            int count = lifeIcons.Length;

            heartTargetX = new float[count];
            heartBaseScales = new Vector3[count];
            heartBaseColors = new Color[count];
            heartRoutines = new Coroutine[count];

            for (int i = 0; i < count; i++)
            {
                if (lifeIcons[i] == null)
                    continue;

                heartTargetX[i] = lifeIcons[i].rectTransform.anchoredPosition.x;
                heartBaseScales[i] = lifeIcons[i].rectTransform.localScale;
                heartBaseColors[i] = lifeIcons[i].color;
            }
        }

        if (lifeIcons == null ||
            lifeIcons.Length < 2 ||
            lifeIcons[0] == null ||
            lifeIcons[1] == null ||
            lifeIcons[lifeIcons.Length - 1] == null)
        {
            Debug.LogWarning(
                "HUD: Heart layout could not be initialized. " +
                "Check lifeIcons references.",
                this
            );

            return;
        }

        float firstX =
            lifeIcons[0].rectTransform.anchoredPosition.x;

        float secondX =
            lifeIcons[1].rectTransform.anchoredPosition.x;

        float lastX =
            lifeIcons[lifeIcons.Length - 1]
                .rectTransform.anchoredPosition.x;

        heartCenterX = (firstX + lastX) / 2f;
        heartSpacing = Mathf.Abs(secondX - firstX);

        heartLayoutInitialized = true;
    }

    private void Bind(GameState state)
    {
        Unbind();

        if (state == null)
        {
            Debug.LogError(
                "HUD: Bind received a null GameState."
            );

            return;
        }

        boundState = state;

        state.ScoreChanged += UpdateScore;
        state.LivesChanged += UpdateLives;
        state.LevelChanged += UpdateLevel;

        Debug.Log(
            $"HUD BIND | Score: {state.Score} | " +
            $"Lives: {state.Lives} | Level: {state.Level}"
        );

        UpdateScore(state.Score);
        ApplyLives(state.Lives, animate: false);
        UpdateLevel(state.Level);

        SetText(
            difficultyText,
            manager.Difficulty.ToString()
        );
    }

    private void Unbind()
    {
        if (boundState == null)
            return;

        boundState.ScoreChanged -= UpdateScore;
        boundState.LivesChanged -= UpdateLives;
        boundState.LevelChanged -= UpdateLevel;

        boundState = null;
    }

    private void UpdateScore(int score)
    {
        SetText(scoreText, $"Score: {score:N0}");
    }

    private void UpdateLevel(int level)
    {
        SetText(levelText, $"Level {level}");
    }

    // Raised by GameState whenever a life is lost or lives are restored.
    private void UpdateLives(int lives)
    {
        ApplyLives(lives, animate: true);
    }

    // Shows `lives` hearts centered in the heart badge. When animating,
    // lost hearts pop out, restored hearts pop in, and the rest slide
    // smoothly to their new centered slots instead of jumping.
    private void ApplyLives(int lives, bool animate)
    {
        Debug.Log(
            $"HUD LIVES | Received: {lives} | Animate: {animate} | " +
            $"Icons assigned: " +
            $"{(lifeIcons == null ? 0 : lifeIcons.Length)}"
        );

        if (lifeIcons == null || heartRoutines == null)
            return;

        lives = Mathf.Clamp(lives, 0, lifeIcons.Length);

        int previousLives = shownLives;
        shownLives = lives;

        bool canAnimate =
            animate && previousLives >= 0 && isActiveAndEnabled;

        for (int i = 0; i < lifeIcons.Length; i++)
        {
            Image heart = lifeIcons[i];

            if (heart == null)
            {
                Debug.LogWarning(
                    $"HUD: Life icon {i} is not assigned."
                );

                continue;
            }

            bool visible = i < lives;
            bool wasVisible = previousLives >= 0
                ? i < previousLives
                : heart.gameObject.activeSelf;

            if (heartLayoutInitialized && visible)
                heartTargetX[i] = heartCenterX +
                    (i - (lives - 1) / 2f) * heartSpacing;

            if (!canAnimate)
            {
                StopHeartRoutine(i);
                ResetHeart(i);
                SnapHeartToTarget(i);
                heart.gameObject.SetActive(visible);
                continue;
            }

            if (visible && !wasVisible)
            {
                StopHeartRoutine(i);
                SnapHeartToTarget(i);
                heart.gameObject.SetActive(true);
                heartRoutines[i] = StartCoroutine(PopHeart(i, appearing: true));
            }
            else if (!visible && wasVisible)
            {
                StopHeartRoutine(i);
                heartRoutines[i] = StartCoroutine(PopHeart(i, appearing: false));
            }
        }
    }

    private IEnumerator PopHeart(int index, bool appearing)
    {
        Image heart = lifeIcons[index];
        RectTransform rect = heart.rectTransform;
        Vector3 baseScale = heartBaseScales[index];
        Color baseColor = heartBaseColors[index];

        float elapsed = 0f;
        float duration = Mathf.Max(0.01f, heartPopSeconds);

        while (elapsed < duration)
        {
            float p = elapsed / duration;

            // Out: quick swell then shrink away. In: grow with a small overshoot.
            float scale = appearing
                ? EaseOutBack(p)
                : (p < 0.3f
                    ? Mathf.Lerp(1f, 1.35f, p / 0.3f)
                    : Mathf.Lerp(1.35f, 0f, (p - 0.3f) / 0.7f));

            float alpha = appearing ? Mathf.Clamp01(p * 2f) : 1f - p * p;

            rect.localScale = baseScale * scale;

            Color color = baseColor;
            color.a = baseColor.a * alpha;
            heart.color = color;

            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }

        ResetHeart(index);

        if (!appearing)
            heart.gameObject.SetActive(false);

        heartRoutines[index] = null;
    }

    private void StopHeartRoutine(int index)
    {
        if (heartRoutines[index] == null)
            return;

        StopCoroutine(heartRoutines[index]);
        heartRoutines[index] = null;
    }

    private void ResetHeart(int index)
    {
        lifeIcons[index].rectTransform.localScale = heartBaseScales[index];
        lifeIcons[index].color = heartBaseColors[index];
    }

    private void SnapHeartToTarget(int index)
    {
        if (!heartLayoutInitialized)
            return;

        RectTransform rect = lifeIcons[index].rectTransform;
        Vector2 position = rect.anchoredPosition;
        position.x = heartTargetX[index];
        rect.anchoredPosition = position;
    }

    private static float EaseOutBack(float p)
    {
        const float c1 = 1.70158f;
        const float c3 = c1 + 1f;
        float x = p - 1f;
        return 1f + c3 * x * x * x + c1 * x * x;
    }

    private void HandlePhaseChanged(GamePhase phase)
    {
        bool observing = phase == GamePhase.Observe;

        bool matching =
            phase == GamePhase.Selecting ||
            phase == GamePhase.Checking;

        bool timed =
            observing && manager.IsObservationTimed;

        bool gameOver = phase == GamePhase.GameOver;

        Debug.Log(
            $"HUD PHASE | {phase} | " +
            $"Difficulty: {manager.Difficulty} | " +
            $"Timed: {manager.IsObservationTimed} | " +
            $"Ready assigned: {readyButton != null} | " +
            $"Ready parent active: " +
            $"{(readyButton != null && readyButton.transform.parent.gameObject.activeInHierarchy)}"
        );

        if (homeButton != null)
            homeButton.SetActive(observing);

        if (readyButton != null)
            readyButton.SetActive(observing);

        if (pauseButton != null)
            pauseButton.SetActive(matching);

        if (instructionBannerRoot != null)
            instructionBannerRoot.SetActive(observing);

        // During GameOver the HUD stays up (hearts at zero, board revealing)
        // until GameManager raises GameEnded; then the summary panel fades in.
        if (gameplayHudRoot != null)
            gameplayHudRoot.SetActive(!observing);

        if (!gameOver)
            HideGameOverPanel();

        if (timerRoot != null)
            timerRoot.SetActive(timed);

        if (timed)
            HandleTimerTick(
                manager.ObservationTimeRemaining
            );

        switch (phase)
        {
            case GamePhase.Observe:
                SetText(
                    messageText,
                    timed ? observeTimedMessage : observeMessage
                );
                break;

            case GamePhase.Selecting:
            case GamePhase.Checking:
                SetText(messageText, selectMessage);
                break;

            case GamePhase.LevelComplete:
                SetText(messageText, levelCompleteMessage);
                break;

            case GamePhase.GameOver:
                SetText(messageText, gameOverMessage);
                break;
        }

        UpdatePairs();
    }

    private void HandleGameEnded(ScoreRecord record, bool isNewBest)
    {
        UpdateGameOverSummary();

        if (gameplayHudRoot != null)
            gameplayHudRoot.SetActive(false);

        if (gameOverPanel == null)
            return;

        if (gameOverPanelRoutine != null)
            StopCoroutine(gameOverPanelRoutine);

        gameOverPanel.SetActive(true);
        gameOverPanelRoutine = StartCoroutine(FadeInGameOverPanel());
    }

    private IEnumerator FadeInGameOverPanel()
    {
        CanvasGroup group = gameOverPanel.GetComponent<CanvasGroup>();

        if (group == null)
            group = gameOverPanel.AddComponent<CanvasGroup>();

        Transform panel = gameOverPanel.transform;
        float duration = Mathf.Max(0.01f, gameOverPanelFadeSeconds);
        float elapsed = 0f;

        // Block clicks on Replay/Home until the panel has settled.
        group.interactable = false;

        while (elapsed < duration)
        {
            float p = elapsed / duration;

            group.alpha = p;
            panel.localScale = gameOverPanelBaseScale *
                Mathf.LerpUnclamped(0.85f, 1f, EaseOutBack(p));

            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }

        group.alpha = 1f;
        group.interactable = true;
        panel.localScale = gameOverPanelBaseScale;

        gameOverPanelRoutine = null;
    }

    private void HideGameOverPanel()
    {
        if (gameOverPanel == null)
            return;

        if (gameOverPanelRoutine != null)
        {
            StopCoroutine(gameOverPanelRoutine);
            gameOverPanelRoutine = null;
        }

        CanvasGroup group = gameOverPanel.GetComponent<CanvasGroup>();

        if (group != null)
        {
            group.alpha = 1f;
            group.interactable = true;
        }

        gameOverPanel.transform.localScale = gameOverPanelBaseScale;
        gameOverPanel.SetActive(false);
    }

    private void UpdateGameOverSummary()
    {
        if (manager == null || manager.State == null)
            return;

        SetText(
            finalScoreValue,
            manager.State.Score.ToString("N0")
        );

        SetText(
            levelReachedValue,
            manager.State.Level.ToString()
        );
    }

    private void HandleTimerTick(float secondsLeft)
    {
        SetText(
            timerText,
            Mathf.CeilToInt(secondsLeft).ToString()
        );
    }

    private void HandlePairChecked(PickResult result)
    {
        UpdatePairs();
    }

    private void UpdatePairs()
    {
        if (manager == null)
            return;

        var round = manager.CurrentRound;

        Debug.Log(
            $"HUD PAIRS | CurrentRound null: {round == null}" +
            (round != null
                ? $" | Found: {round.PairsFound}/{round.PairCount}"
                : "")
        );

        SetText(
            pairsText,
            round != null
                ? $"Pairs: {round.PairsFound}/{round.PairCount}"
                : ""
        );
    }

    private static void SetText(
        TMP_Text label,
        string value)
    {
        if (label != null)
            label.text = value;
    }
}