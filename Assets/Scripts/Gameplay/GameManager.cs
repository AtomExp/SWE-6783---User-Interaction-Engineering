using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public enum GamePhase
{
    NotStarted,
    // Cards are face up. Easy: until Start is pressed. Hard: 10-second countdown.
    Observe,
    // Cards are face down; the player turns over two at a time.
    Selecting,
    // Two non-matching cards are showing briefly before turning back over.
    Checking,
    // Every pair found; short pause before the next, bigger board.
    LevelComplete,
    GameOver
}

// Runs the matching game on top of MemoryBoard:
// Observe -> Selecting (pick two cards: match = points, mismatch = lose a life) -> LevelComplete -> next level ... -> GameOver.
// Difficulty comes from GameSettings.SelectedDifficulty. Other features hook in through the C# events
// (HUD, Study Phase UI) or the UnityEvents in the Inspector (audio, animations).
public class GameManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private MemoryBoard board;

    [Header("Rules")]
    [SerializeField] private GameRules easyRules = GameRules.CreateEasy();
    [SerializeField] private GameRules hardRules = GameRules.CreateHard();

    [Header("Timing")]
    [SerializeField] private float mismatchRevealSeconds = 0.8f;
    [SerializeField] private float levelCompleteDelay = 1.25f;
    [SerializeField] private float gameOverDelay = 1f;

    [Tooltip("Start a game in Start(). Turn off to call StartGame yourself.")]
    [SerializeField] private bool startOnLoad = true;

    [Header("Events (audio, animations)")]
    public UnityEvent onObservePhaseStarted = new UnityEvent();
    public UnityEvent onSelectionPhaseStarted = new UnityEvent();
    public UnityEvent onCardFlipped = new UnityEvent();
    public UnityEvent onMatch = new UnityEvent();
    public UnityEvent onMismatch = new UnityEvent();
    public UnityEvent onLevelComplete = new UnityEvent();
    public UnityEvent onGameOver = new UnityEvent();

    private readonly List<MemoryCard> subscribedCards = new List<MemoryCard>();
    private Coroutine pendingRoutine;

    public static GameManager Instance { get; private set; }

    public GameState State { get; private set; }
    public PairMatchRound CurrentRound { get; private set; }
    public GamePhase Phase { get; private set; } = GamePhase.NotStarted;
    public Difficulty Difficulty { get; private set; }

    public bool IsObservationTimed => State != null && State.Rules.HasObservationLimit;
    public float ObservationTimeRemaining { get; private set; }

    // A new game began. Subscribe to State's ScoreChanged / LivesChanged / LevelChanged here.
    public event Action<GameState> GameStarted;
    public event Action<GamePhase> PhaseChanged;
    // Seconds left in a timed observation phase, every frame.
    public event Action<float> ObservationTick;
    // After each second card: the result (Match or Mismatch).
    public event Action<PickResult> PairChecked;
    // Final record and whether it is a new best for this difficulty.
    public event Action<ScoreRecord, bool> GameEnded;

    private void Awake()
    {
        if (Instance != null && Instance != this)
            Debug.LogWarning("More than one GameManager in the scene.", this);
        Instance = this;

        if (board == null)
            board = FindFirstObjectByType<MemoryBoard>();

        if (board != null)
        {
            // GameManager builds every level itself; stop the board building its default layout too.
            board.BuildOnStart = false;
            board.Started += HandleBoardStarted;
        }
        else
            Debug.LogError("GameManager needs a MemoryBoard.", this);
    }

    private void Start()
    {
        if (startOnLoad && board != null)
            StartGame(GameSettings.SelectedDifficulty);
    }

    private void OnDestroy()
    {
        UnsubscribeCards();
        if (board != null)
            board.Started -= HandleBoardStarted;
        if (Instance == this)
            Instance = null;
    }

    private void Update()
    {
        if (Phase != GamePhase.Observe || !IsObservationTimed)
            return;

        ObservationTimeRemaining = Mathf.Max(0f, ObservationTimeRemaining - Time.deltaTime);
        ObservationTick?.Invoke(ObservationTimeRemaining);

        if (ObservationTimeRemaining <= 0f)
            EndObservation();
    }

    // ---------- Public API (buttons and other scripts) ----------

    public void StartGame(Difficulty difficulty)
    {
        StopPending();

        Difficulty = difficulty;
        var rules = (difficulty == Difficulty.Hard ? hardRules : easyRules).Clone();
        State = new GameState(rules, (int)difficulty);
        GameStarted?.Invoke(State);

        StartLevel();
    }

    public void RestartGame() => StartGame(Difficulty);

    // Hides the cards and starts the selection phase. Called by the board's Start button,
    // the Hard-mode timer, or a Study Phase script.
    public void EndObservation()
    {
        if (Phase == GamePhase.Observe)
            board.StartGame(); // hides the symbols, then raises Started -> HandleBoardStarted
    }

    public void GoToMainMenu() => SceneLoader.Load(SceneNames.MainMenu);
    public void OpenScoreDashboard() => SceneLoader.Load(SceneNames.ScoreDashboard);

    // ---------- Flow ----------

    private void StartLevel()
    {
        int pairs = State.PairsThisLevel;
        if (pairs > board.SymbolCount)
        {
            Debug.LogWarning($"Level {State.Level} wants {pairs} pairs but MemoryBoard only has {board.SymbolCount} symbols.", this);
            pairs = board.SymbolCount;
        }

        if (pairs <= 0)
        {
            Debug.LogError("MemoryBoard has no symbol sprites, so no board can be dealt.", this);
            return;
        }

        board.BuildBoard(pairs);
        SubscribeCards();

        var keys = new List<int>();
        foreach (var card in board.Cards)
            keys.Add(card.SymbolID);
        CurrentRound = new PairMatchRound(keys);

        // Hard mode counts down on its own, so the Start button is only for Easy.
        ObservationTimeRemaining = State.Rules.observationSeconds;
        board.SetStartButtonVisible(!IsObservationTimed);

        SetPhase(GamePhase.Observe);
        onObservePhaseStarted.Invoke();
    }

    private void HandleBoardStarted()
    {
        if (Phase != GamePhase.Observe)
            return;

        ObservationTimeRemaining = 0f;
        SetPhase(GamePhase.Selecting);
        onSelectionPhaseStarted.Invoke();
    }

    private void HandleCardClicked(MemoryCard card)
    {
        if (Phase != GamePhase.Selecting || CurrentRound == null)
            return;

        int index = IndexOf(card);
        var result = CurrentRound.Pick(index, out int otherIndex);
        if (result == PickResult.Ignored)
            return;

        card.ShowFront();
        onCardFlipped.Invoke();

        if (result == PickResult.FirstPick)
            return;

        var other = board.Cards[otherIndex];

        if (result == PickResult.Match)
        {
            card.ShowMatched();
            other.ShowMatched();
            State.RegisterMatch();
            onMatch.Invoke();
            PairChecked?.Invoke(result);

            if (CurrentRound.IsComplete)
                CompleteLevel();
            return;
        }

        State.RegisterMistake();
        onMismatch.Invoke();
        PairChecked?.Invoke(result);

        SetPhase(GamePhase.Checking);
        pendingRoutine = StartCoroutine(AfterDelay(mismatchRevealSeconds, () =>
        {
            card.ShowBack();
            other.ShowBack();

            if (State.IsGameOver)
                EndGame();
            else
                SetPhase(GamePhase.Selecting);
        }));
    }

    private void CompleteLevel()
    {
        State.CompleteLevel();
        SetPhase(GamePhase.LevelComplete);
        onLevelComplete.Invoke();
        pendingRoutine = StartCoroutine(AfterDelay(levelCompleteDelay, StartLevel));
    }

    private void EndGame()
    {
        if (Phase == GamePhase.GameOver)
            return;

        // Show where the remaining pairs were.
        foreach (var card in board.Cards)
        {
            if (!card.IsMatched)
                card.ShowFront();
        }

        var record = State.ToRecord(DateTime.UtcNow);
        bool isNewBest = ScoreRepository.Save(record);

        SetPhase(GamePhase.GameOver);
        pendingRoutine = StartCoroutine(AfterDelay(gameOverDelay, () =>
        {
            onGameOver.Invoke();
            GameEnded?.Invoke(record, isNewBest);
        }));
    }

    // ---------- Helpers ----------

    private void SetPhase(GamePhase phase)
    {
        Phase = phase;
        PhaseChanged?.Invoke(phase);
    }

    private int IndexOf(MemoryCard card)
    {
        for (int i = 0; i < board.Cards.Count; i++)
        {
            if (board.Cards[i] == card)
                return i;
        }
        return -1;
    }

    private void SubscribeCards()
    {
        UnsubscribeCards();
        foreach (var card in board.Cards)
        {
            card.Clicked += HandleCardClicked;
            subscribedCards.Add(card);
        }
    }

    private void UnsubscribeCards()
    {
        foreach (var card in subscribedCards)
        {
            if (card != null)
                card.Clicked -= HandleCardClicked;
        }
        subscribedCards.Clear();
    }

    private IEnumerator AfterDelay(float seconds, Action action)
    {
        if (seconds > 0f)
            yield return new WaitForSeconds(seconds);
        pendingRoutine = null;
        action();
    }

    private void StopPending()
    {
        if (pendingRoutine != null)
        {
            StopCoroutine(pendingRoutine);
            pendingRoutine = null;
        }
    }
}
