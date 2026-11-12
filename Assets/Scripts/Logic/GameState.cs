using System;

// Stores score, lives and level information for one game session.
// Raises events so the HUD and other systems can respond to changes.
public class GameState
{
    public GameRules Rules { get; }

    // Difficulty is stored as an integer.
    public int DifficultyId { get; }

    public int Score { get; private set; }
    public int Lives { get; private set; }
    public int Level { get; private set; } = 1;
    public int Streak { get; private set; }
    public int BestStreak { get; private set; }
    public int LevelsCleared { get; private set; }
    public int TotalMatches { get; private set; }
    public int TotalMistakes { get; private set; }

    public bool IsGameOver => Lives <= 0;

    public int PairsThisLevel => Rules.PairsForLevel(Level);

    public event Action<int> ScoreChanged;
    public event Action<int> LivesChanged;
    public event Action<int> LevelChanged;
    public event Action GameOver;

    // --------------------------------------------------
    // CONSTRUCTOR
    // --------------------------------------------------

    public GameState(GameRules rules, int difficultyId)
    {
        Rules = rules ?? throw new ArgumentNullException(nameof(rules));

        DifficultyId = difficultyId;

        Lives = Math.Max(1, Rules.startingLives);
    }

    // --------------------------------------------------
    // MATCHING
    // --------------------------------------------------

    public int RegisterMatch()
    {
        if (IsGameOver)
            return 0;

        TotalMatches++;

        int points =
            Rules.pointsPerMatch +
            Streak * Rules.streakBonus;

        Streak++;

        if (Streak > BestStreak)
            BestStreak = Streak;

        AddScore(points);

        return points;
    }

    // --------------------------------------------------
    // MISTAKES AND LIVES
    // --------------------------------------------------

    public void RegisterMistake()
    {
        if (IsGameOver)
            return;

        TotalMistakes++;

        Streak = 0;

        if (Rules.wrongPenalty > 0)
            AddScore(-Rules.wrongPenalty);

        Lives = Math.Max(0, Lives - 1);

        LivesChanged?.Invoke(Lives);

        if (IsGameOver)
            GameOver?.Invoke();
    }

    // --------------------------------------------------
    // LEVEL COMPLETION
    // --------------------------------------------------

    public int CompleteLevel()
    {
        if (IsGameOver)
            return 0;

        LevelsCleared++;

        // Award the level completion bonus.
        int bonus = Rules.levelClearBonus * Level;

        AddScore(bonus);

        // Advance to the next level.
        Level++;

        // Restore starting lives for the new level.
        Lives = Math.Max(1, Rules.startingLives);

        LivesChanged?.Invoke(Lives);

        LevelChanged?.Invoke(Level);

        return bonus;
    }

    // --------------------------------------------------
    // SCORE RECORD
    // --------------------------------------------------

    public ScoreRecord ToRecord(DateTime finishedUtc)
    {
        return new ScoreRecord
        {
            difficulty = DifficultyId,
            score = Score,
            levelReached = Level,
            levelsCleared = LevelsCleared,
            matches = TotalMatches,
            mistakes = TotalMistakes,
            bestStreak = BestStreak,
            timestampUtcTicks =
                finishedUtc.ToUniversalTime().Ticks
        };
    }

    // --------------------------------------------------
    // SCORE MANAGEMENT
    // --------------------------------------------------

    private void AddScore(int delta)
    {
        int newScore = Math.Max(0, Score + delta);

        if (newScore == Score)
            return;

        Score = newScore;

        ScoreChanged?.Invoke(Score);
    }
}