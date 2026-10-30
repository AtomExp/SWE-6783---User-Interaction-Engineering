
using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenuController : MonoBehaviour
{
    public void OpenDifficultySelection()
    {
        SceneManager.LoadScene("DifficultySelection");
    }

    public void OpenInstructions()
    {
        SceneLoader.Load(SceneNames.Instructions);
    }

    public void OpenScoreDashboard()
    {
        SceneLoader.Load(SceneNames.ScoreDashboard);
    }
}