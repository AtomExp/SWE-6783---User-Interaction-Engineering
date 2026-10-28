using UnityEngine;
using UnityEngine.SceneManagement;

// Scene names used across the game. Keep these in sync with the files in Assets/Scenes.
public static class SceneNames
{
    public const string MainMenu = "MainMenu";
    public const string DifficultySelection = "DifficultySelection";
    public const string Instructions = "Instructions";
    public const string StudyPhase = "StudyPhase";
}

public static class SceneLoader
{
    public static void Load(string sceneName)
    {
        // A scene that isn't in the build list (or doesn't exist yet) would throw, so warn instead.
        if (!Application.CanStreamedLevelBeLoaded(sceneName))
        {
            Debug.LogWarning($"Scene '{sceneName}' can't be loaded. Make sure it exists and is added to File > Build Profiles > Scene List.");
            return;
        }

        SceneManager.LoadScene(sceneName);
    }
}
