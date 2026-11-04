using UnityEngine;

public class PauseController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GameObject pausePanel;

    private GameManager gameManager;
    private bool isPaused;

    private void Start()
    {
        gameManager = GameManager.Instance;

        if (gameManager == null)
        {
            Debug.LogError("PauseController: GameManager not found.");
            return;
        }

        if (pausePanel != null)
            pausePanel.SetActive(false);
    }

    // Connect to the PAUSE button.
    public void PauseGame()
    {
        if (gameManager == null || isPaused)
            return;

        if (gameManager.Phase != GamePhase.Selecting &&
            gameManager.Phase != GamePhase.Checking)
            return;

        isPaused = true;
        Time.timeScale = 0f;

        if (pausePanel != null)
            pausePanel.SetActive(true);
    }

    // Connect to the RESUME button.
    public void ResumeGame()
    {
        if (!isPaused)
            return;

        Time.timeScale = 1f;
        isPaused = false;

        if (pausePanel != null)
            pausePanel.SetActive(false);
    }

    // Connect to the RESTART button.
    public void RestartGame()
    {
        if (gameManager == null)
            return;

        Time.timeScale = 1f;
        isPaused = false;

        if (pausePanel != null)
            pausePanel.SetActive(false);

        gameManager.RestartGame();
    }

    private void OnDestroy()
    {
        Time.timeScale = 1f;
    }
}