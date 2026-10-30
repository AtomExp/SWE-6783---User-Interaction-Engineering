using UnityEngine;
using UnityEngine.SceneManagement;

public class DifficultySelectionController : MonoBehaviour
{
    public void GoBackToMainMenu()
    {
        SceneManager.LoadScene("MainMenu");
    }

    public void SelectEasyMode()
    {
        GameSettings.SelectedDifficulty = Difficulty.Easy;
        SceneManager.LoadScene("StudyPhase");
    }

    public void SelectHardMode()
    {
        GameSettings.SelectedDifficulty = Difficulty.Hard;
        SceneManager.LoadScene("StudyPhase");
    }
}