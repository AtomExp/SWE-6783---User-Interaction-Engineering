using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Score / Lives / Level / observation timer display. Every reference is optional.
public class HUDController : MonoBehaviour
{
    [Tooltip("Defaults to GameManager.Instance.")]
    [SerializeField] private GameManager manager;

    [Header("Text")]
    [SerializeField] private TMP_Text scoreText;
    [SerializeField] private TMP_Text livesText;
    [SerializeField] private TMP_Text levelText;
    [SerializeField] private TMP_Text difficultyText;
    [SerializeField] private TMP_Text messageText;
    [SerializeField] private TMP_Text timerText;
    [SerializeField] private TMP_Text pairsText;

    [Header("Optional")]
    [Tooltip("Shown while the Hard-mode countdown runs. Defaults to the timer text's object.")]
    [SerializeField] private GameObject timerRoot;
    [Tooltip("Heart icons, one per life. Lost lives are faded.")]
    [SerializeField] private Image[] lifeIcons;
    [SerializeField] private Color lifeLostColor = new Color(1f, 1f, 1f, 0.25f);

    [Header("Messages")]
    [SerializeField] private string observeMessage = "Memorize the cards!";
    [SerializeField] private string observeTimedMessage = "Memorize the cards before time runs out!";
    [SerializeField] private string selectMessage = "Find the matching pairs!";
    [SerializeField] private string levelCompleteMessage = "Level clear! Next level...";
    [SerializeField] private string gameOverMessage = "Out of lives!";

    private GameState boundState;
    private Color lifeColor = Color.white;

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
        if (lifeIcons != null && lifeIcons.Length > 0 && lifeIcons[0] != null)
            lifeColor = lifeIcons[0].color;

        manager.GameStarted += Bind;
        manager.PhaseChanged += HandlePhaseChanged;
        manager.ObservationTick += HandleTimerTick;
        manager.PairChecked += HandlePairChecked;

        // The game may have started before this Start ran.
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

    private void UpdateScore(int score) => SetText(scoreText, $"Score: {score:N0}");
    private void UpdateLevel(int level) => SetText(levelText, $"Level {level}");

    private void UpdateLives(int lives)
    {
        SetText(livesText, $"Lives: {lives}");
        if (lifeIcons == null)
            return;
        for (int i = 0; i < lifeIcons.Length; i++)
        {
            if (lifeIcons[i] != null)
                lifeIcons[i].color = i < lives ? lifeColor : lifeLostColor;
        }
    }

    private void HandlePhaseChanged(GamePhase phase)
    {
        bool timed = phase == GamePhase.Observe && manager.IsObservationTimed;
        if (timerRoot != null)
            timerRoot.SetActive(timed);
        if (timed)
            HandleTimerTick(manager.ObservationTimeRemaining);

        switch (phase)
        {
            case GamePhase.Observe: SetText(messageText, timed ? observeTimedMessage : observeMessage); break;
            case GamePhase.Selecting: SetText(messageText, selectMessage); break;
            case GamePhase.LevelComplete: SetText(messageText, levelCompleteMessage); break;
            case GamePhase.GameOver: SetText(messageText, gameOverMessage); break;
        }

        UpdatePairs();
    }

    private void HandleTimerTick(float secondsLeft) => SetText(timerText, Mathf.CeilToInt(secondsLeft).ToString());

    private void HandlePairChecked(PickResult result) => UpdatePairs();

    private void UpdatePairs()
    {
        var round = manager.CurrentRound;
        SetText(pairsText, round != null ? $"Pairs: {round.PairsFound}/{round.PairCount}" : string.Empty);
    }

    private static void SetText(TMP_Text label, string value)
    {
        if (label != null)
            label.text = value;
    }
}
