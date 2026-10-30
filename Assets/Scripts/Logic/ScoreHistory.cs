using System;
using System.Collections.Generic;
using System.Linq;

// The result of one finished game. Field names are the save format; only add new ones.
[Serializable]
public class ScoreRecord
{
    // (int)Difficulty
    public int difficulty;
    public int score;
    public int levelReached;
    public int levelsCleared;
    public int matches;
    public int mistakes;
    public int bestStreak;
    public long timestampUtcTicks;

    public DateTime TimestampUtc => new DateTime(timestampUtcTicks, DateTimeKind.Utc);

    // Matches / attempts as 0-1 (1 when nothing was attempted).
    public float Accuracy
    {
        get
        {
            int attempts = matches + mistakes;
            return attempts == 0 ? 1f : (float)matches / attempts;
        }
    }
}

// All saved results. Keeps the newest MaxRecordsPerDifficulty per difficulty but never drops a difficulty's best.
[Serializable]
public class ScoreHistory
{
    public const int MaxRecordsPerDifficulty = 50;

    public int version = 1;
    public List<ScoreRecord> records = new List<ScoreRecord>();

    // Returns true if the record is a new best for its difficulty.
    public bool Add(ScoreRecord record)
    {
        if (record == null) throw new ArgumentNullException(nameof(record));
        records ??= new List<ScoreRecord>();

        var previousBest = GetBest(record.difficulty);
        bool isNewBest = previousBest == null || IsBetter(record, previousBest);

        records.Add(record);
        Trim(record.difficulty);
        return isNewBest;
    }

    public ScoreRecord GetBest(int difficulty) => GetTop(difficulty, 1).FirstOrDefault();

    // Highest score first; ties go to the higher level, then the earlier game.
    public List<ScoreRecord> GetTop(int? difficulty, int count) =>
        Filter(difficulty)
            .OrderByDescending(r => r.score)
            .ThenByDescending(r => r.levelReached)
            .ThenBy(r => r.timestampUtcTicks)
            .Take(Math.Max(0, count))
            .ToList();

    // Newest first. Pass null for every difficulty.
    public List<ScoreRecord> GetRecent(int? difficulty, int count) =>
        Filter(difficulty)
            .OrderByDescending(r => r.timestampUtcTicks)
            .Take(Math.Max(0, count))
            .ToList();

    public int GamesPlayed(int difficulty) => Filter(difficulty).Count();

    public void Clear() => records?.Clear();

    public static bool IsBetter(ScoreRecord a, ScoreRecord b) =>
        a.score != b.score ? a.score > b.score : a.levelReached > b.levelReached;

    IEnumerable<ScoreRecord> Filter(int? difficulty)
    {
        IEnumerable<ScoreRecord> source = records ?? new List<ScoreRecord>();
        return difficulty.HasValue ? source.Where(r => r.difficulty == difficulty.Value) : source;
    }

    void Trim(int difficulty)
    {
        var mode = Filter(difficulty).ToList();
        if (mode.Count <= MaxRecordsPerDifficulty) return;

        var best = GetBest(difficulty);
        var oldest = mode
            .Where(r => r != best)
            .OrderBy(r => r.timestampUtcTicks)
            .Take(mode.Count - MaxRecordsPerDifficulty)
            .ToList();
        foreach (var r in oldest)
            records.Remove(r);
    }
}
