using UnityEngine;
using UnityEngine.SceneManagement;

public class DifficultySelectionController : MonoBehaviour
{
    public void GoBackToMainMenu()
    {
        SceneManager.LoadScene("MainMenu");
    }
}
