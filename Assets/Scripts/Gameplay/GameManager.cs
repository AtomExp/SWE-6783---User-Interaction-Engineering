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
//
// StudyPhase handles observation:
// Easy = unlimited observation.
// Hard = 10-second observation.
//
// GameLevel starts directly in Selecting phase.
// Cards are face down and ready for matching.

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
        // Prevent a second GameManager from being created
        // when GameLevel is loaded again.
        if (Instance != null && Instance != this)
        {
            isDuplicate = true;
            Destroy(gameObject);
            return;
        }

        Instance = this;

        // Preserve this GameManager and its GameState
        // when StudyPhase and GameLevel are loaded.
        DontDestroyOnLoad(gameObject);

        // Listen for scene changes.
        SceneManager.sceneLoaded += HandleSceneLoaded;

        // Find the MemoryBoard in the current scene.
        FindAndAssignBoard();
    }

    private void Start()
    {
        // Start the initial game only once.
        // On subsequent GameLevel loads, the existing
        // GameState and level progress are preserved.
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

    // Called whenever Unity finishes loading a scene.
    private void HandleSceneLoaded(
        Scene scene,
        LoadSceneMode mode)
    {
        // We only need to reconnect the board when GameLevel loads.
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

        // First GameLevel visit: Start() will create Level 1.
        if (State == null)
            return;

        // Returning from StudyPhase:
        // Build the next level using the existing GameState.
        if (Phase != GamePhase.GameOver)
            StartLevel();
    }

    private void FindAndAssignBoard()
    {
        // Remove references to the previous scene's board.
        UnsubscribeCards();

        if (board != null)
            board.Started -= HandleBoardStarted;

        // Find the MemoryBoard belonging to the current scene.
        board = FindFirstObjectByType<MemoryBoard>();

        if (board != null)
        {
            // GameManager controls gameplay board generation.
            board.BuildOnStart = false;

            board.Started += HandleBoardStarted;
        }
    }

    // --------------------------------------------------
    // UPDATE
    // --------------------------------------------------

    private void Update()
    {
        // GameLevel does not run an observation countdown.
        // StudyPhaseTimer handles the observation period.
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

    public void StartGame(Difficulty difficulty)
    {
        StopPending();

        Difficulty = difficulty;

        // Select and copy the rules for this game session.
        GameRules rules = (
            difficulty == Difficulty.Hard
                ? hardRules
                : easyRules
        ).Clone();

        // Create the game state only once.
        State = new GameState(rules, (int)difficulty);

        GameStarted?.Invoke(State);

        StartLevel();
    }

    public void RestartGame()
    {
        // A restart creates a fresh game session.
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

        // Do not request more pairs than the available symbols.
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

        // Build the gameplay board.
        // MemoryBoard reuses SharedCardPattern when the
        // pattern contains the correct number of cards.
        board.BuildBoard(pairs);

        // Subscribe to card clicks.
        SubscribeCards();

        // Create the matching round.
        List<int> keys = new List<int>();

        foreach (MemoryCard card in board.Cards)
            keys.Add(card.SymbolID);

        CurrentRound = new PairMatchRound(keys);

        // StudyPhase has already completed observation.
        // No second timer is started in GameLevel.
        ObservationTimeRemaining = 0f;

        // Hide the gameplay Start button.
        board.SetStartButtonVisible(false);

        // Turn all gameplay cards face down.
        board.StartGame();

        // Start matching immediately.
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
        // Retained for compatibility with MemoryBoard.
        // In GameLevel, StartLevel already sets Selecting.
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

        // Reveal the selected card.
        card.ShowFront();

        onCardFlipped.Invoke();

        if (result == PickResult.FirstPick)
            return;

        MemoryCard other = board.Cards[otherIndex];

        if (result == PickResult.Match)
        {
            // Correct match.
            card.ShowMatched();
            other.ShowMatched();

            State.RegisterMatch();

            onMatch.Invoke();

            PairChecked?.Invoke(result);

            if (CurrentRound.IsComplete)
                CompleteLevel();

            return;
        }

        // Incorrect match.
        State.RegisterMistake();

        onMismatch.Invoke();

        PairChecked?.Invoke(result);

        SetPhase(GamePhase.Checking);

        // Briefly reveal both incorrect cards.
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
        // Award the level bonus and increment the level.
        State.CompleteLevel();

        SetPhase(GamePhase.LevelComplete);

        onLevelComplete.Invoke();

        // After completing a level, return to StudyPhase.
        // GameState remains alive because GameManager persists.
        pendingRoutine = StartCoroutine(
            AfterDelay(levelCompleteDelay, ReturnToStudyPhase)
        );
    }

    private void ReturnToStudyPhase()
    {
        if (State == null || Phase == GamePhase.GameOver)
            return;

        // Clear the previous pattern.
        // StudyMemoryBoard will generate a new pattern
        // for the next level when StudyPhase loads.
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

        // Reveal remaining unmatched cards.
        foreach (MemoryCard card in board.Cards)
        {
            if (!card.IsMatched)
                card.ShowFront();
        }

        // Save the final game record.
        ScoreRecord record = State.ToRecord(DateTime.UtcNow);

        bool isNewBest = ScoreRepository.Save(record);

        SetPhase(GamePhase.GameOver);

        // Preserve the existing Game Over flow.
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