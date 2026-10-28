using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenuController : MonoBehaviour
{
    public void OpenDifficultySelection()
    {
        SceneManager.LoadScene("DifficultySelection");
    }
}