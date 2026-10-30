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

// Controls the matching game and preserves the game session
// while moving between StudyPhase and GameLevel.

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

    [Tooltip("Start a new game when the first GameLevel scene loads.")]
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

    // --------------------------------------------------
    // INITIALIZATION AND SCENE PERSISTENCE
    // --------------------------------------------------

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
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
        if (scene.name != SceneNames.GameLevel)
            return;

        FindAndAssignBoard();

        if (board == null)
        {
            Debug.LogError(
                "GameManager: Could not find MemoryBoard in GameLevel."
            );
            return;
        }

        if (State == null)
            return;

        if (Phase != GamePhase.GameOver)
            StartLevel();
    }

    private void FindAndAssignBoard()
    {
        UnsubscribeCards();

        if (board != null)
            board.Started -= HandleBoardStarted;

        board = FindFirstObjectByType<MemoryBoard>();

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

    // Prepares a completely new game without building
    // the gameplay board yet.
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

    // Starts a new game and builds the board when available.
    public void StartGame(Difficulty difficulty)
    {
        PrepareNewGame(difficulty);
        StartLevel();
    }

    public void RestartGame()
    {
        StartGame(Difficulty);
    }

    // Retained for compatibility with existing scripts.
    public void EndObservation()
    {
        if (Phase == GamePhase.Observe && board != null)
            board.StartGame();
    }

    public void GoToMainMenu()
    {
        SceneLoader.Load(SceneNames.MainMenu);
    }

    public void OpenScoreDashboard()
    {
        SceneLoader.Load(SceneNames.ScoreDashboard);
    }

    // --------------------------------------------------
    // LEVEL INITIALIZATION
    // --------------------------------------------------

    private void StartLevel()
    {
        if (State == null || board == null)
            return;

        StopPending();

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

        board.BuildBoard(pairs);

        SubscribeCards();

        List<int> keys = new List<int>();

        foreach (MemoryCard card in board.Cards)
            keys.Add(card.SymbolID);

        CurrentRound = new PairMatchRound(keys);

        ObservationTimeRemaining = 0f;

        board.SetStartButtonVisible(false);

        board.StartGame();

        SetPhase(GamePhase.Selecting);

        onSelectionPhaseStarted.Invoke();

        Debug.Log(
            $"GameLevel started: {Difficulty}, " +
            $"Level {State.Level}, {pairs} pairs. " +
            "Cards are hidden and ready for selection."
        );
    }

    private void HandleBoardStarted()
    {
        if (Phase != GamePhase.Observe)
            return;

        ObservationTimeRemaining = 0f;

        SetPhase(GamePhase.Selecting);

        onSelectionPhaseStarted.Invoke();
    }

    // --------------------------------------------------
    // CARD SELECTION AND MATCHING
    // --------------------------------------------------

    private void HandleCardClicked(MemoryCard card)
    {
        if (Phase != GamePhase.Selecting || CurrentRound == null)
            return;

        int index = IndexOf(card);

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

        onMismatch.Invoke();

        PairChecked?.Invoke(result);

        SetPhase(GamePhase.Checking);

        pendingRoutine = StartCoroutine(
            AfterDelay(mismatchRevealSeconds, () =>
            {
                card.ShowBack();
                other.ShowBack();

                if (State.IsGameOver)
                    EndGame();
                else
                    SetPhase(GamePhase.Selecting);
            })
        );
    }

    // --------------------------------------------------
    // LEVEL COMPLETION
    // --------------------------------------------------

    private void CompleteLevel()
    {
        State.CompleteLevel();

        SetPhase(GamePhase.LevelComplete);

        onLevelComplete.Invoke();

        pendingRoutine = StartCoroutine(
            AfterDelay(levelCompleteDelay, ReturnToStudyPhase)
        );
    }

    private void ReturnToStudyPhase()
    {
        if (State == null || Phase == GamePhase.GameOver)
            return;

        SharedCardPattern.SymbolIDs = null;

        Debug.Log(
            $"Level completed. Preparing StudyPhase for Level {State.Level}."
        );

        SceneLoader.Load(SceneNames.StudyPhase);
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

        SetPhase(GamePhase.GameOver);

        pendingRoutine = StartCoroutine(
            AfterDelay(gameOverDelay, () =>
            {
                onGameOver.Invoke();

                GameEnded?.Invoke(record, isNewBest);
            })
        );
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
        if (pendingRoutine != null)
        {
            StopCoroutine(pendingRoutine);

            pendingRoutine = null;
        }
    }
}