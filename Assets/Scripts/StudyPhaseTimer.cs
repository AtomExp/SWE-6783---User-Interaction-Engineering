using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class StudyPhaseTimer : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private TMP_Text timerText;
    [SerializeField] private GameObject timerClock;
    [SerializeField] private GameObject secondsLeftText;
    [SerializeField] private StudyMemoryBoard memoryBoard;
    [SerializeField] private ClockShake clockShake;

    [Header("Settings")]
    [SerializeField] private int countdownSeconds = 10;
    [SerializeField] private float transitionDelay = 0.5f;

    private bool transitionStarted;

    private void Start()
    {
        if (memoryBoard == null)
        {
            Debug.LogError("StudyPhaseTimer: MemoryBoard is not assigned.");
            return;
        }

        // Show all cards immediately.
        memoryBoard.ShowAllCards();

        if (GameSettings.SelectedDifficulty == Difficulty.Hard)
        {
            // HARD MODE: Show timer and clock.
            if (timerClock != null)
                timerClock.SetActive(true);

            if (timerText != null)
            {
                timerText.gameObject.SetActive(true);
                timerText.text = countdownSeconds.ToString();
            }

            if (secondsLeftText != null)
                secondsLeftText.SetActive(true);

            if (clockShake != null)
                clockShake.StartShaking();

            // Start countdown automatically.
            StartCoroutine(Countdown());
        }
        else
        {
            // EASY MODE: Hide timer and clock.
            if (timerClock != null)
                timerClock.SetActive(false);

            if (timerText != null)
                timerText.gameObject.SetActive(false);

            if (secondsLeftText != null)
                secondsLeftText.SetActive(false);

            if (clockShake != null)
                clockShake.StopShaking();
        }
    }

    private IEnumerator Countdown()
    {
        int remaining = countdownSeconds;

        while (remaining > 0 && !transitionStarted)
        {
            if (timerText != null)
                timerText.text = remaining.ToString();

            yield return new WaitForSeconds(1f);
            remaining--;

            // Increase clock shaking during the final 5 seconds.
            if (remaining == 5 && clockShake != null)
                clockShake.StartIntenseShaking();
        }

        if (!transitionStarted)
            GoToGameLevel();
    }

    // Called by the READY button in both difficulty modes.
    public void GoToGameLevel()
    {
        if (transitionStarted)
            return;

        transitionStarted = true;
        StartCoroutine(TransitionToGameplay());
    }

    private IEnumerator TransitionToGameplay()
    {
        // Hide cards before entering GameLevel.
        if (memoryBoard != null)
            memoryBoard.HideAllCards();

        if (clockShake != null)
            clockShake.StopShaking();

        yield return new WaitForSeconds(transitionDelay);

        SceneManager.LoadScene("GameLevel");
    }
}