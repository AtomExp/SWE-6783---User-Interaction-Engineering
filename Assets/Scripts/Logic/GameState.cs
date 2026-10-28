using System;

// Score, lives and level for one play session. Raises events so the HUD, audio or anything
// else can react without GameManager knowing about them.
public class GameState
{
    public GameRules Rules { get; }

    // (int)Difficulty from GameSettings. Stored as int because this assembly can't see that enum.
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

    public GameState(GameRules rules, int difficultyId)
    {
        Rules = rules ?? throw new ArgumentNullException(nameof(rules));
        DifficultyId = difficultyId;
        Lives = Math.Max(1, rules.startingLives);
    }

    // Returns the points awarded.
    public int RegisterMatch()
    {
        if (IsGameOver) return 0;

        TotalMatches++;
        int points = Rules.pointsPerMatch + Streak * Rules.streakBonus;
        Streak++;
        if (Streak > BestStreak) BestStreak = Streak;
        AddScore(points);
        return points;
    }

    public void RegisterMistake()
    {
        if (IsGameOver) return;

        TotalMistakes++;
        Streak = 0;
        if (Rules.wrongPenalty > 0)
            AddScore(-Rules.wrongPenalty);

        Lives = Math.Max(0, Lives - 1);
        LivesChanged?.Invoke(Lives);
        if (IsGameOver)
            GameOver?.Invoke();
    }

    // Board cleared: award the bonus and move to the next level. Returns the bonus.
    public int CompleteLevel()
    {
        if (IsGameOver) return 0;

        LevelsCleared++;
        int bonus = Rules.levelClearBonus * Level;
        AddScore(bonus);
        Level++;
        LevelChanged?.Invoke(Level);
        return bonus;
    }

    public ScoreRecord ToRecord(DateTime finishedUtc) => new ScoreRecord
    {
        difficulty = DifficultyId,
        score = Score,
        levelReached = Level,
        levelsCleared = LevelsCleared,
        matches = TotalMatches,
        mistakes = TotalMistakes,
        bestStreak = BestStreak,
        timestampUtcTicks = finishedUtc.ToUniversalTime().Ticks
    };

    void AddScore(int delta)
    {
        int newScore = Math.Max(0, Score + delta);
        if (newScore == Score) return;
        Score = newScore;
        ScoreChanged?.Invoke(Score);
    }
}
