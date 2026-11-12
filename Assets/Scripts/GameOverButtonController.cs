using UnityEngine;

public class GameOverButtonController : MonoBehaviour
{
    public void ReplayGame()
    {
        GameManager manager = GameManager.Instance;

        if (manager == null)
        {
            Debug.LogError(
                "GameOver: GameManager instance not found."
            );
            return;
        }

        Debug.Log("GameOver: Replay clicked.");

        Time.timeScale = 1f;
        manager.RestartGame();
    }

    public void ReturnToHome()
    {
        GameManager manager = GameManager.Instance;

        if (manager == null)
        {
            Debug.LogError(
                "GameOver: GameManager instance not found."
            );
            return;
        }

        Debug.Log("GameOver: Home clicked.");

        Time.timeScale = 1f;
        manager.GoToMainMenu();
    }
}