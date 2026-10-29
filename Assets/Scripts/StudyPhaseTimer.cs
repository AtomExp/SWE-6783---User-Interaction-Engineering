using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class StudyPhaseTimer : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private TMP_Text timerText;
    [SerializeField] private StudyMemoryBoard memoryBoard;
    [SerializeField] private ClockShake clockShake;

    [Header("Hard Mode Settings")]
    [SerializeField] private int countdownSeconds = 10;
    [SerializeField] private float transitionDelay = 0.5f;

    private bool studyStarted;
    private bool transitionStarted;

    private void Start()
    {
        // Wait for the player to press READY.
        if (timerText != null)
        {
            timerText.text = countdownSeconds.ToString();
        }
    }

    public void BeginStudyPhase()
    {
        if (studyStarted || transitionStarted)
            return;

        if (memoryBoard == null)
        {
            Debug.LogError(
                "StudyPhaseTimer: MemoryBoard is not assigned."
            );
            return;
        }

        studyStarted = true;

        // Reveal all cards.
        memoryBoard.ShowAllCards();

        // Start normal clock shaking.
        if (clockShake != null)
        {
            clockShake.StartShaking();
        }

        StartCoroutine(Countdown());
    }

    private IEnumerator Countdown()
    {
        int remaining = countdownSeconds;

        while (remaining > 0)
        {
            if (timerText != null)
            {
                timerText.text = remaining.ToString();
            }

            yield return new WaitForSeconds(1f);
            remaining--;

            // Increase shaking during the final 5 seconds.
            if (remaining == 5 && clockShake != null)
            {
                clockShake.StartIntenseShaking();
            }
        }

        if (timerText != null)
        {
            timerText.text = "0";
        }

        yield return StartCoroutine(TransitionToGameplay());
    }

    private IEnumerator TransitionToGameplay()
    {
        if (transitionStarted)
            yield break;

        transitionStarted = true;

        // Hide all cards.
        memoryBoard.HideAllCards();

        // Stop clock shaking.
        if (clockShake != null)
        {
            clockShake.StopShaking();
        }

        yield return new WaitForSeconds(transitionDelay);

        // Load the gameplay scene.
        SceneManager.LoadScene("GameLevel");
    }
}