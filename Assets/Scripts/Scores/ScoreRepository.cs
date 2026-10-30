using System;
using System.Collections.Generic;
using UnityEngine;

// Saves and loads finished games as JSON in PlayerPrefs (works on desktop and WebGL).
// Read from anywhere, e.g. ScoreRepository.GetBest(Difficulty.Easy).
public static class ScoreRepository
{
    const string PrefsKey = "ThinkNSync.ScoreHistory.v1";

    static ScoreHistory cached;

    // Raised after a save or clear.
    public static event Action Changed;

    public static ScoreHistory History => cached ??= Load();

    // Saves a finished game. Returns true if it is a new best for its difficulty.
    public static bool Save(ScoreRecord record)
    {
        bool isNewBest = History.Add(record);
        Persist();
        return isNewBest;
    }

    public static ScoreRecord GetBest(Difficulty difficulty) => History.GetBest((int)difficulty);

    public static List<ScoreRecord> GetTop(Difficulty? difficulty, int count) =>
        History.GetTop(difficulty.HasValue ? (int)difficulty.Value : (int?)null, count);

    public static List<ScoreRecord> GetRecent(Difficulty? difficulty, int count) =>
        History.GetRecent(difficulty.HasValue ? (int)difficulty.Value : (int?)null, count);

    public static int GamesPlayed(Difficulty difficulty) => History.GamesPlayed((int)difficulty);

    public static Difficulty DifficultyOf(ScoreRecord record) => (Difficulty)record.difficulty;

    public static void ClearAll()
    {
        History.Clear();
        Persist();
    }

    static ScoreHistory Load()
    {
        string json = PlayerPrefs.GetString(PrefsKey, string.Empty);
        if (string.IsNullOrEmpty(json))
            return new ScoreHistory();

        try
        {
            return JsonUtility.FromJson<ScoreHistory>(json) ?? new ScoreHistory();
        }
        catch (Exception e)
        {
            Debug.LogWarning($"Couldn't read saved scores, starting fresh. {e.Message}");
            return new ScoreHistory();
        }
    }

    static void Persist()
    {
        PlayerPrefs.SetString(PrefsKey, JsonUtility.ToJson(History));
        PlayerPrefs.Save();
        Changed?.Invoke();
    }

    // Statics survive between Play sessions when Domain Reload is off; reset them.
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics()
    {
        cached = null;
        Changed = null;
    }
}
