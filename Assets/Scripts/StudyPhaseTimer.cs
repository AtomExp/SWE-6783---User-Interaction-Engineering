using TMPro;
using UnityEngine;

public class StudyPhaseTimer : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private TMP_Text timerText;
    [SerializeField] private GameObject timerClock;
    [SerializeField] private GameObject secondsLeftText;
    [SerializeField] private ClockShake clockShake;

    [Header("Game Controls")]
    [SerializeField] private GameObject readyButton;
    [SerializeField] private GameObject pauseButton;

    private GameManager gameManager;

    private void Start()
    {
        gameManager = GameManager.Instance;

        if (gameManager == null)
        {
            Debug.LogError(
                "StudyPhaseTimer: GameManager instance not found."
            );
            return;
        }

        gameManager.PhaseChanged += HandlePhaseChanged;
        gameManager.ObservationTick += HandleObservationTick;

        // Synchronise the UI with the current game phase.
        HandlePhaseChanged(gameManager.Phase);

        if (gameManager.IsObservationTimed)
        {
            HandleObservationTick(
                gameManager.ObservationTimeRemaining
            );
        }
    }

    private void OnDestroy()
    {
        if (gameManager != null)
        {
            gameManager.PhaseChanged -= HandlePhaseChanged;
            gameManager.ObservationTick -= HandleObservationTick;
        }
    }

    // Called whenever the game changes phase.
    private void HandlePhaseChanged(GamePhase phase)
    {
        bool observing = phase == GamePhase.Observe;

        bool matching =
            phase == GamePhase.Selecting ||
            phase == GamePhase.Checking;

        // Show the clock only during Hard-mode observation.
        bool showClock =
            observing && gameManager.IsObservationTimed;

        if (timerClock != null)
            timerClock.SetActive(showClock);

        if (secondsLeftText != null)
            secondsLeftText.SetActive(showClock);

        if (timerText != null)
            timerText.gameObject.SetActive(showClock);

        // READY is visible during observation.
        if (readyButton != null)
            readyButton.SetActive(observing);

        // PAUSE is visible during matching.
        if (pauseButton != null)
            pauseButton.SetActive(matching);

        // Control the clock animation.
        if (clockShake != null)
        {
            if (showClock)
                clockShake.StartShaking();
            else
                clockShake.StopShaking();
        }

        if (phase == GamePhase.LevelComplete ||
            phase == GamePhase.GameOver)
        {
            if (readyButton != null)
                readyButton.SetActive(false);

            if (pauseButton != null)
                pauseButton.SetActive(false);
        }
    }

    // Updates the countdown using GameManager's timer.
    private void HandleObservationTick(float remaining)
    {
        if (timerText == null)
            return;

        int seconds = Mathf.CeilToInt(remaining);

        timerText.text = seconds.ToString();

        if (seconds <= 5 && seconds > 0 && clockShake != null)
            clockShake.StartIntenseShaking();
    }

    // Connect this method to the READY button.
    public void GoToGameLevel()
    {
        if (gameManager == null)
            return;

        gameManager.EndObservation();
    }
}