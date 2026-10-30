using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenuController : MonoBehaviour
{
    // Opens the difficulty selection screen.
    public void OpenDifficultySelection()
    {
        SceneManager.LoadScene("DifficultySelection");
    }

    // Opens the instructions screen.
    public void OpenInstructions()
    {
        SceneLoader.Load(SceneNames.Instructions);
    }

    // Opens the score dashboard.
    public void OpenScoreDashboard()
    {
        SceneLoader.Load(SceneNames.ScoreDashboard);
    }

    // Opens the options screen.
    public void OpenOptions()
    {
        SceneLoader.Load(SceneNames.Options);
    }
}