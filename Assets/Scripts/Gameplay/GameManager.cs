using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;

public enum GamePhase
{
    NotStarted,
    Observe,
    Selecting,
    Checking,
    LevelComplete,
    GameOver
}

// Controls the complete game session inside StudyPhase.
public class GameManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private MemoryBoard board;

    [Header("Rules")]
    [SerializeField] private GameRules easyRules = GameRules.CreateEasy();
    [SerializeField] private GameRules hardRules = GameRules.CreateHard();

    [Header("Timing")]
    [SerializeField] private float mismatchRevealSeconds = 0.8f;
    [SerializeField] private float gameOverDelay = 3f;

    [Tooltip("Start a new game when StudyPhase loads.")]
    [SerializeField] private bool startOnLoad = true;

    [Header("Events (audio, animations)")]
    public UnityEvent onObservePhaseStarted = new UnityEvent();
    public UnityEvent onSelectionPhaseStarted = new UnityEvent();
    public UnityEvent onCardFlipped = new UnityEvent();
    public UnityEvent onMatch = new UnityEvent();
    public UnityEvent onMismatch = new UnityEvent();
    public UnityEvent onLevelComplete = new UnityEvent();
    public UnityEvent onGameOver = new UnityEvent();

    private readonly List<MemoryCard> subscribedCards =
        new List<MemoryCard>();

    private Coroutine pendingRoutine;
    private bool isDuplicate;
    private bool initialGameStarted;

    // The wrong pair currently on show, waiting to be flipped back.
    private MemoryCard mismatchFirst;
    private MemoryCard mismatchSecond;
    private float mismatchShownAt;

    private int mistakesAtLevelStart;

    public static GameManager Instance { get; private set; }

    public GameState State { get; private set; }
    public PairMatchRound CurrentRound { get; private set; }
    public GamePhase Phase { get; private set; } = GamePhase.NotStarted;

    public Difficulty Difficulty { get; private set; }

    public bool IsObservationTimed =>
        State != null && State.Rules.HasObservationLimit;

    public float ObservationTimeRemaining { get; private set; }

    // Events for HUD, UI and other game systems.
    public event Action<GameState> GameStarted;
    public event Action<GamePhase> PhaseChanged;
    public event Action<float> ObservationTick;
    public event Action<PickResult> PairChecked;
    public event Action<ScoreRecord, bool> GameEnded;
    public event Action<LevelSummary> LevelCompleted;

    // Set by a level progression screen while it is in the scene. When true,
    // the next level waits for ContinueToNextLevel() instead of auto-starting.
    public bool WaitForLevelContinue { get; set; }

    // --------------------------------------------------
    // INITIALIZATION AND SCENE PERSISTENCE
    // --------------------------------------------------

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            // This copy came with a freshly loaded scene, so its Inspector
            // event wiring (e.g. MatchAudioController sounds) points at that
            // scene's objects. Hand it to the persistent instance; otherwise
            // it keeps calling objects destroyed with the previous scene and
            // sounds go silent after the first game.
            Instance.AdoptSceneEvents(this);

            isDuplicate = true;
            Destroy(gameObject);
            return;
        }

        Instance = this;

        DontDestroyOnLoad(gameObject);

        SceneManager.sceneLoaded += HandleSceneLoaded;

        FindAndAssignBoard();
    }

    private void Start()
    {
        if (!isDuplicate &&
            startOnLoad &&
            State == null &&
            board != null &&
            !initialGameStarted)
        {
            initialGameStarted = true;
            StartGame(GameSettings.SelectedDifficulty);
        }
    }

    private void OnDestroy()
    {
        if (isDuplicate)
            return;

        SceneManager.sceneLoaded -= HandleSceneLoaded;

        UnsubscribeCards();

        if (board != null)
            board.Started -= HandleBoardStarted;

        if (Instance == this)
            Instance = null;
    }

    private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name != SceneNames.StudyPhase)
            return;

        FindAndAssignBoard();

        if (board == null)
        {
            Debug.LogError(
                "GameManager: Could not find MemoryBoard in StudyPhase."
            );
            return;
        }

        if (State != null && Phase != GamePhase.GameOver)
        {
            StartLevel();
        }
    }

    private void AdoptSceneEvents(GameManager sceneCopy)
    {
        onObservePhaseStarted = sceneCopy.onObservePhaseStarted;
        onSelectionPhaseStarted = sceneCopy.onSelectionPhaseStarted;
        onCardFlipped = sceneCopy.onCardFlipped;
        onMatch = sceneCopy.onMatch;
        onMismatch = sceneCopy.onMismatch;
        onLevelComplete = sceneCopy.onLevelComplete;
        onGameOver = sceneCopy.onGameOver;
    }

    private void FindAndAssignBoard()
    {
        UnsubscribeCards();

        if (board != null)
            board.Started -= HandleBoardStarted;

        board = FindAnyObjectByType<MemoryBoard>();

        if (board != null)
        {
            board.BuildOnStart = false;
            board.Started += HandleBoardStarted;
        }
    }

    // --------------------------------------------------
    // UPDATE
    // --------------------------------------------------

    private void Update()
    {
        if (Phase != GamePhase.Observe || !IsObservationTimed)
            return;

        ObservationTimeRemaining = Mathf.Max(
            0f,
            ObservationTimeRemaining - Time.deltaTime
        );

        ObservationTick?.Invoke(ObservationTimeRemaining);

        if (ObservationTimeRemaining <= 0f)
            EndObservation();
    }

    // --------------------------------------------------
    // PUBLIC API
    // --------------------------------------------------

    public void PrepareNewGame(Difficulty difficulty)
    {
        StopPending();

        UnsubscribeCards();

        SharedCardPattern.SymbolIDs = null;

        Difficulty = difficulty;

        GameRules rules = (
            difficulty == Difficulty.Hard
                ? hardRules
                : easyRules
        ).Clone();

        State = new GameState(rules, (int)difficulty);

        CurrentRound = null;
        ObservationTimeRemaining = 0f;

        SetPhase(GamePhase.NotStarted);

        GameStarted?.Invoke(State);

        Debug.Log("New game prepared: " + difficulty);
    }

    public void StartGame(Difficulty difficulty)
    {
        PrepareNewGame(difficulty);
        StartLevel();
    }

    public void RestartGame()
    {
        StartGame(Difficulty);
    }

    // Called when READY is pressed or the Hard timer expires.
    public void EndObservation()
    {
        if (Phase == GamePhase.Observe && board != null)
        {
            board.StartGame();
        }
    }

    public void GoToMainMenu()
    {
        SceneLoader.Load(SceneNames.MainMenu);
    }

    public void OpenScoreDashboard()
    {
        SceneLoader.Load(SceneNames.ScoreDashboard);
    }

    // Called by the Continue button on LevelCompletePanel.
    public void ContinueToNextLevel()
    {
        if (Phase != GamePhase.LevelComplete)
            return;

        StartNextLevel();
    }

    // --------------------------------------------------
    // LEVEL INITIALIZATION
    // --------------------------------------------------

    private void StartLevel()
    {
        if (State == null || board == null)
            return;

        StopPending();

        UnsubscribeCards();

        SharedCardPattern.SymbolIDs = null;

        int pairs = State.PairsThisLevel;

        if (pairs > board.SymbolCount)
        {
            Debug.LogWarning(
                $"Level {State.Level} wants {pairs} pairs, " +
                $"but MemoryBoard only has {board.SymbolCount} symbols.",
                this
            );

            pairs = board.SymbolCount;
        }

        if (pairs <= 0)
        {
            Debug.LogError(
                "MemoryBoard has no symbol sprites.",
                this
            );
            return;
        }

        // Generate the cards for this level.
        board.BuildBoard(pairs);

        SubscribeCards();

        // Prepare the matching engine using generated cards.
        List<int> keys = new List<int>();

        foreach (MemoryCard card in board.Cards)
            keys.Add(card.SymbolID);

        CurrentRound = new PairMatchRound(keys);

        mistakesAtLevelStart = State.TotalMistakes;

        board.SetStartButtonVisible(false);

        // Start the observation phase.
        ObservationTimeRemaining =
            IsObservationTimed
                ? State.Rules.observationSeconds
                : 0f;

        SetPhase(GamePhase.Observe);

        onObservePhaseStarted.Invoke();

        if (IsObservationTimed)
            ObservationTick?.Invoke(ObservationTimeRemaining);

        Debug.Log(
            $"StudyPhase started: {Difficulty}, " +
            $"Level {State.Level}, {pairs} pairs. " +
            "Cards are visible for observation."
        );
    }

    private void HandleBoardStarted()
    {
        if (Phase != GamePhase.Observe)
            return;

        ObservationTimeRemaining = 0f;

        SetPhase(GamePhase.Selecting);

        onSelectionPhaseStarted.Invoke();

        Debug.Log("Observation ended. Matching started.");
    }

    // --------------------------------------------------
    // CARD SELECTION AND MATCHING
    // --------------------------------------------------

    private void HandleCardClicked(MemoryCard card)
    {
        // Let the player keep going without waiting out the full mismatch
        // reveal: the next click flips the wrong pair back and counts as a pick.
        if (Phase == GamePhase.Checking &&
            mismatchFirst != null &&
            Time.time - mismatchShownAt >= minMismatchRevealSeconds)
        {
            ResolveMismatch();
        }

        if (Phase != GamePhase.Selecting || CurrentRound == null)
            return;

        int index = IndexOf(card);

        if (index < 0)
            return;

        PickResult result = CurrentRound.Pick(
            index,
            out int otherIndex
        );

        if (result == PickResult.Ignored)
            return;

        card.ShowFront();

        onCardFlipped.Invoke();

        if (result == PickResult.FirstPick)
            return;

        MemoryCard other = board.Cards[otherIndex];

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

        card.PlayIncorrectFeedback();
        other.PlayIncorrectFeedback();

        onMismatch.Invoke();

        PairChecked?.Invoke(result);

        if (State.IsGameOver)
        {
            // Leave the losing pair face up and go straight into the
            // game-over sequence (no flip-back-then-reveal flicker).
            EndGame();
            return;
        }

        mismatchFirst = other;
        mismatchSecond = card;
        mismatchShownAt = Time.time;

        SetPhase(GamePhase.Checking);

        pendingRoutine = StartCoroutine(
            AfterDelay(mismatchRevealSeconds, ResolveMismatch)
        );
    }

    // Turns the shown wrong pair back over and returns to selecting.
    private void ResolveMismatch()
    {
        MemoryCard first = mismatchFirst;
        MemoryCard second = mismatchSecond;

        StopPending();

        if (first != null)
            first.ShowBack();

        if (second != null)
            second.ShowBack();

        if (Phase == GamePhase.Checking)
            SetPhase(GamePhase.Selecting);
    }

    // --------------------------------------------------
    // LEVEL COMPLETION
    // --------------------------------------------------

    private void CompleteLevel()
    {
        int completedLevel = State.Level;
        int livesLeft = State.Lives;
        int mistakes = State.TotalMistakes - mistakesAtLevelStart;
        int pairsFound = CurrentRound != null ? CurrentRound.PairsFound : 0;

        int bonus = State.CompleteLevel();

        LevelSummary summary = new LevelSummary
        {
            CompletedLevel = completedLevel,
            NextLevel = State.Level,
            NextLevelPairs = board != null
                ? Mathf.Min(State.PairsThisLevel, board.SymbolCount)
                : State.PairsThisLevel,
            PairsFound = pairsFound,
            Mistakes = mistakes,
            LivesLeft = livesLeft,
            MaxLives = Mathf.Max(1, State.Rules.startingLives),
            LevelBonus = bonus,
            Score = State.Score,
            Difficulty = Difficulty
        };

        SetPhase(GamePhase.LevelComplete);

        // Play level-up audio and display the Level Complete panel.
        // The next level waits for the Continue button.
        onLevelComplete.Invoke();
    }

    // Called by the level progression screen's Continue button.
    public void ContinueToNextLevel()
    {
        if (Phase != GamePhase.LevelComplete || State == null)
            return;

        StopPending();
        StartNextLevel();
    }

    private void StartNextLevel()
    {
        if (State == null || Phase == GamePhase.GameOver)
            return;

        Debug.Log(
            $"Level completed. Preparing Level {State.Level} " +
            "in the same StudyPhase scene."
        );

        StartLevel();
    }

    // --------------------------------------------------
    // GAME OVER
    // --------------------------------------------------

    private void EndGame()
    {
        if (Phase == GamePhase.GameOver)
            return;

        foreach (MemoryCard card in board.Cards)
        {
            if (!card.IsMatched)
                card.ShowFront();
        }

        ScoreRecord record = State.ToRecord(DateTime.UtcNow);

        bool isNewBest = ScoreRepository.Save(record);

        // Locks input immediately; the HUD keeps the board and hearts on
        // screen until GameEnded fires and the summary panel appears.
        SetPhase(GamePhase.GameOver);

        pendingRoutine = StartCoroutine(
            GameOverSequence(record, isNewBest)
        );
    }

    private IEnumerator GameOverSequence(ScoreRecord record, bool isNewBest)
    {
        // 1. Let the losing pair's shake play so the player sees what happened.
        if (mismatchRevealSeconds > 0f)
            yield return new WaitForSeconds(mismatchRevealSeconds);

        // 2. Reveal the rest of the board one card at a time.
        if (board != null)
        {
            foreach (MemoryCard card in board.Cards)
            {
                if (card == null || card.IsMatched || card.IsFaceUp)
                    continue;

                card.ShowFront();

                if (gameOverRevealStagger > 0f)
                    yield return new WaitForSeconds(gameOverRevealStagger);
            }
        }

        // 3. Give the revealed board a moment before the summary covers it.
        if (gameOverDelay > 0f)
            yield return new WaitForSeconds(gameOverDelay);

        pendingRoutine = null;

        onGameOver.Invoke();

        GameEnded?.Invoke(record, isNewBest);
    }

    // --------------------------------------------------
    // HELPERS
    // --------------------------------------------------

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

        foreach (MemoryCard card in board.Cards)
        {
            card.Clicked += HandleCardClicked;

            subscribedCards.Add(card);
        }
    }

    private void UnsubscribeCards()
    {
        foreach (MemoryCard card in subscribedCards)
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
        mismatchFirst = null;
        mismatchSecond = null;

        if (pendingRoutine != null)
        {
            StopCoroutine(pendingRoutine);

            pendingRoutine = null;
        }
    }
}