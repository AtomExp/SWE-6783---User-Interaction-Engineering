using UnityEngine;

public class QuitConfirmationController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GameObject confirmationPanel;

    private void Awake()
    {
        if (confirmationPanel == null)
        {
            Debug.LogError(
                "QuitConfirmationController: Confirmation Panel is not assigned!",
                this
            );
            return;
        }

        // Hide the confirmation panel when Main Menu loads.
        confirmationPanel.SetActive(false);

        Debug.Log("QuitConfirmationController initialized.");
    }

    // EXIT BUTTON - Show quit confirmation.
    public void ShowQuitConfirmation()
    {
        Debug.Log("1. QUIT BUTTON CLICKED");

        if (confirmationPanel == null)
        {
            Debug.LogError("2. Confirmation Panel is NULL!");
            return;
        }

        Debug.Log(
            "3. Panel before activation: " +
            confirmationPanel.activeSelf
        );

        confirmationPanel.SetActive(true);

        Debug.Log(
            "4. Panel after activation: " +
            confirmationPanel.activeSelf
        );
    }

    // YES BUTTON - Quit the application.
    public void ConfirmQuit()
    {
        Debug.Log("QUIT CONFIRMED");

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    // NO BUTTON - Close popup and stay on Main Menu.
    public void CancelQuit()
    {
        Debug.Log("QUIT CANCELLED");

        if (confirmationPanel != null)
        {
            confirmationPanel.SetActive(false);
        }
    }
}