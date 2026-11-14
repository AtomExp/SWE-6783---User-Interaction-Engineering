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

    private GameState boundState;

    private float heartCenterX;
    private float heartSpacing;
    private bool heartLayoutInitialized;

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

        manager.GameStarted += Bind;
        manager.PhaseChanged += HandlePhaseChanged;
        manager.ObservationTick += HandleTimerTick;
        manager.PairChecked += HandlePairChecked;

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
    }

    private void InitializeHeartLayout()
    {
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
        UpdateLives(state.Lives);
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

    private void UpdateLives(int lives)
    {
        Debug.Log(
            $"HUD LIVES | Received: {lives} | " +
            $"Icons assigned: " +
            $"{(lifeIcons == null ? 0 : lifeIcons.Length)}"
        );

        if (lifeIcons == null)
            return;

        lives = Mathf.Clamp(lives, 0, lifeIcons.Length);

        for (int i = 0; i < lifeIcons.Length; i++)
        {
            if (lifeIcons[i] == null)
            {
                Debug.LogWarning(
                    $"HUD: Life icon {i} is not assigned."
                );

                continue;
            }

            if (heartLayoutInitialized && i < lives)
            {
                RectTransform rect =
                    lifeIcons[i].rectTransform;

                Vector2 position = rect.anchoredPosition;

                position.x = heartCenterX +
                    (i - (lives - 1) / 2f) * heartSpacing;

                rect.anchoredPosition = position;
            }

            lifeIcons[i].gameObject.SetActive(i < lives);
        }
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

        if (gameplayHudRoot != null)
            gameplayHudRoot.SetActive(!observing && !gameOver);

        if (gameOverPanel != null)
            gameOverPanel.SetActive(gameOver);

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
                UpdateGameOverSummary();
                break;
        }

        UpdatePairs();
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