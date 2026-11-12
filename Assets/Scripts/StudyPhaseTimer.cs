using TMPro;
using UnityEngine;

public class StudyPhaseTimer : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private TMP_Text timerText;
    [SerializeField] private GameObject timerClock;
    [SerializeField] private GameObject secondsLeftText;
    [SerializeField] private ClockShake clockShake;

    private GameManager gameManager;

    private void Start()
    {
        gameManager = GameManager.Instance;

        if (gameManager == null)
        {
            Debug.LogError(
                "StudyPhaseTimer: GameManager instance not found.");
            enabled = false;
            return;
        }

        gameManager.PhaseChanged += HandlePhaseChanged;
        gameManager.ObservationTick += HandleObservationTick;

        HandlePhaseChanged(gameManager.Phase);

        if (gameManager.IsObservationTimed)
            HandleObservationTick(
                gameManager.ObservationTimeRemaining);
    }

    private void OnDestroy()
    {
        if (gameManager == null)
            return;

        gameManager.PhaseChanged -= HandlePhaseChanged;
        gameManager.ObservationTick -= HandleObservationTick;
    }

    private void HandlePhaseChanged(GamePhase phase)
    {
        bool showClock =
            phase == GamePhase.Observe &&
            gameManager.IsObservationTimed;

        if (timerClock != null)
            timerClock.SetActive(showClock);

        if (secondsLeftText != null)
            secondsLeftText.SetActive(showClock);

        if (timerText != null)
            timerText.gameObject.SetActive(showClock);

        if (clockShake != null)
        {
            if (showClock)
                clockShake.StartShaking();
            else
                clockShake.StopShaking();
        }
    }

    private void HandleObservationTick(float remaining)
    {
        if (timerText == null)
            return;

        int seconds = Mathf.CeilToInt(remaining);
        timerText.text = seconds.ToString();

        if (clockShake != null && seconds > 0 && seconds <= 5)
            clockShake.StartIntenseShaking();
    }

    public void GoToGameLevel()
    {
        if (gameManager != null)
            gameManager.EndObservation();
    }
}