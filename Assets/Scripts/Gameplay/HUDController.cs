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
    [SerializeField] private string observeMessage = "Look at the pattern carefully!";
    [SerializeField] private string observeTimedMessage = "Look at the pattern carefully!";
    [SerializeField] private string selectMessage = "Find the matching pairs!";
    [SerializeField] private string levelCompleteMessage = "Level clear! Next level...";
    [SerializeField] private string gameOverMessage = "Out of lives!";

    private GameState boundState;
    private float heartCenterX;
    private float heartSpacing;
    private bool heartLayoutInitialized;

    private void Start()
    {
        if (manager == null)
            manager = GameManager.Instance;

        if (manager == null)
        {
            Debug.LogWarning("HUDController: no GameManager found.", this);
            enabled = false;
            return;
        }

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
        if (lifeIcons == null || lifeIcons.Length < 2 ||
            lifeIcons[0] == null || lifeIcons[1] == null ||
            lifeIcons[lifeIcons.Length - 1] == null)
            return;

        float firstX = lifeIcons[0].rectTransform.anchoredPosition.x;
        float secondX = lifeIcons[1].rectTransform.anchoredPosition.x;
        float lastX = lifeIcons[lifeIcons.Length - 1].rectTransform.anchoredPosition.x;

        heartCenterX = (firstX + lastX) / 2f;
        heartSpacing = Mathf.Abs(secondX - firstX);
        heartLayoutInitialized = true;
    }

    private void Bind(GameState state)
    {
        Unbind();
        boundState = state;

        state.ScoreChanged += UpdateScore;
        state.LivesChanged += UpdateLives;
        state.LevelChanged += UpdateLevel;

        UpdateScore(state.Score);
        UpdateLives(state.Lives);
        UpdateLevel(state.Level);

        SetText(difficultyText, manager.Difficulty.ToString());
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
        Debug.Log("HUD received lives update: " + lives);

        if (lifeIcons == null)
            return;

        lives = Mathf.Clamp(lives, 0, lifeIcons.Length);

        for (int i = 0; i < lifeIcons.Length; i++)
        {
            if (lifeIcons[i] == null)
                continue;

            if (heartLayoutInitialized && i < lives)
            {
                RectTransform rect = lifeIcons[i].rectTransform;
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
        bool matching = phase == GamePhase.Selecting ||
                        phase == GamePhase.Checking;

        bool timed = observing && manager.IsObservationTimed;

        if (homeButton != null)
            homeButton.SetActive(observing);

        if (readyButton != null)
            readyButton.SetActive(observing);

        if (pauseButton != null)
            pauseButton.SetActive(matching);

        if (instructionBannerRoot != null)
            instructionBannerRoot.SetActive(observing);

        if (gameplayHudRoot != null)
            gameplayHudRoot.SetActive(!observing);

        if (timerRoot != null)
            timerRoot.SetActive(timed);

        if (timed)
            HandleTimerTick(manager.ObservationTimeRemaining);

        switch (phase)
        {
            case GamePhase.Observe:
                SetText(messageText, timed ? observeTimedMessage : observeMessage);
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

    private void HandleTimerTick(float secondsLeft)
    {
        SetText(timerText, Mathf.CeilToInt(secondsLeft).ToString());
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

        SetText(pairsText,
            round != null ? $"Pairs: {round.PairsFound}/{round.PairCount}" : "");
    }

    private static void SetText(TMP_Text label, string value)
    {
        if (label != null)
            label.text = value;
    }
}