using UnityEngine;
using UnityEngine.SceneManagement;

public class DifficultySelectionController : MonoBehaviour
{
    public void GoBackToMainMenu()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene("MainMenu");
    }

    public void SelectEasyMode()
    {
        SelectDifficulty(Difficulty.Easy);
    }

    public void SelectHardMode()
    {
        SelectDifficulty(Difficulty.Hard);
    }

    private void SelectDifficulty(Difficulty difficulty)
    {
        Time.timeScale = 1f;

        GameSettings.SelectedDifficulty = difficulty;

        // GameManager prepares the new session after StudyPhase loads.
        SceneManager.LoadScene("StudyPhase");
    }
}