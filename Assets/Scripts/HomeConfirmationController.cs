using UnityEngine;

public class HomeConfirmationController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GameObject confirmationPanel;
    [SerializeField] private GameManager gameManager;

    private float previousTimeScale = 1f;
    private bool confirmationOpen = false;

    private void Awake()
    {
        // Get the persistent GameManager.
        if (gameManager == null)
            gameManager = GameManager.Instance;

        if (confirmationPanel == null)
        {
            Debug.LogError(
                "HomeConfirmationController: Confirmation Panel is missing!",
                this
            );
        }
        else
        {
            confirmationPanel.SetActive(false);
        }

        Debug.Log(
            "HomeConfirmationController initialized.",
            this
        );
    }

    // HOME BUTTON - Observation screen
    public void ShowFromObservation()
    {
        Debug.Log("HOME CLICKED: Observation screen");

        ShowConfirmation();
    }

    // HOME BUTTON - Pause screen
    public void ShowFromPause()
    {
        Debug.Log("HOME CLICKED: Pause screen");

        ShowConfirmation();
    }

    // Display the shared confirmation panel.
    private void ShowConfirmation()
    {
        Debug.Log("SHOWING HOME CONFIRMATION");

        if (confirmationPanel == null)
        {
            Debug.LogError(
                "Cannot show confirmation: Panel reference is missing."
            );
            return;
        }

        if (confirmationOpen)
        {
            Debug.Log("Confirmation is already open.");
            return;
        }

        previousTimeScale = Time.timeScale;
        confirmationOpen = true;

        // Pause observation or keep an already-paused game paused.
        Time.timeScale = 0f;

        confirmationPanel.SetActive(true);

        Debug.Log(
            "Confirmation panel active: " +
            confirmationPanel.activeSelf
        );
    }

    // YES BUTTON - Exit to Main Menu.
    public void ConfirmExit()
    {
        Debug.Log("CONFIRM EXIT: YES clicked");

        if (gameManager == null)
            gameManager = GameManager.Instance;

        if (gameManager == null)
        {
            Debug.LogError(
                "HomeConfirmationController: GameManager not found."
            );
            return;
        }

        confirmationOpen = false;

        Time.timeScale = 1f;

        confirmationPanel.SetActive(false);

        gameManager.GoToMainMenu();
    }

    // NO BUTTON - Return to the previous game state.
    public void CancelExit()
    {
        Debug.Log("CANCEL EXIT: NO clicked");

        if (!confirmationOpen)
        {
            Debug.LogWarning(
                "CancelExit called, but confirmation is not open."
            );
            return;
        }

        confirmationOpen = false;

        confirmationPanel.SetActive(false);

        // Restore the exact previous state:
        // 1 = observation/gameplay running
        // 0 = pause menu already open
        Time.timeScale = previousTimeScale;

        Debug.Log(
            "Confirmation closed. Restored TimeScale: " +
            Time.timeScale
        );
    }
}