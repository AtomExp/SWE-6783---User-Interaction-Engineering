using UnityEngine;

public enum Difficulty
{
    Easy,
    Hard
}

// Settings chosen on the menus that later scenes (Study Phase, Game Board) read.
public static class GameSettings
{
    const string DifficultyKey = "ThinkNSync.Difficulty";

    // Hard mode hides the objects after this many seconds. Easy mode has no limit.
    public const float HardObservationSeconds = 10f;

    // Saved in PlayerPrefs so it survives scene loads and pressing Play on a later scene directly.
    public static Difficulty SelectedDifficulty
    {
        get => (Difficulty)PlayerPrefs.GetInt(DifficultyKey, (int)Difficulty.Easy);
        set
        {
            PlayerPrefs.SetInt(DifficultyKey, (int)value);
            PlayerPrefs.Save();
        }
    }

    public static bool HasObservationTimer => SelectedDifficulty == Difficulty.Hard;
}
