using UnityEngine;
using UnityEngine.SceneManagement;

public class OptionsController : MonoBehaviour
{
    public void GoBackToMainMenu()
    {
        SceneManager.LoadScene("MainMenu");
    }
}