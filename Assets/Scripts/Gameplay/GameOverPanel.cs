using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Shows the final result when the game ends, with Play Again / Score Dashboard / Main Menu buttons.
// Put this on an always-active object and point panelRoot at the panel (hidden until game over).
public class GameOverPanel : MonoBehaviour
{
    [Tooltip("Defaults to GameManager.Instance.")]
    [SerializeField] private GameManager manager;
    [SerializeField] private GameObject panelRoot;

    [SerializeField] private TMP_Text finalScoreText;
    [SerializeField] private TMP_Text bestScoreText;
    [SerializeField] private TMP_Text detailsText;
    [SerializeField] private GameObject newBestBadge;

    [SerializeField] private Button playAgainButton;
    [SerializeField] private Button scoreDashboardButton;
    [SerializeField] private Button mainMenuButton;

    private void Start()
    {
        if (manager == null)
            manager = GameManager.Instance;
        if (manager == null)
        {
            Debug.LogWarning("GameOverPanel: no GameManager found.", this);
            return;
        }

        manager.GameEnded += Show;
        manager.GameStarted += HandleGameStarted;

        if (playAgainButton != null) playAgainButton.onClick.AddListener(manager.RestartGame);
        if (scoreDashboardButton != null) scoreDashboardButton.onClick.AddListener(manager.OpenScoreDashboard);
        if (mainMenuButton != null) mainMenuButton.onClick.AddListener(manager.GoToMainMenu);

        Hide();
    }

    private void OnDestroy()
    {
        if (manager == null)
            return;
        manager.GameEnded -= Show;
        manager.GameStarted -= HandleGameStarted;
    }

    public void Show(ScoreRecord record, bool isNewBest)
    {
        var difficulty = ScoreRepository.DifficultyOf(record);
        var best = ScoreRepository.GetBest(difficulty);

        if (finalScoreText != null)
            finalScoreText.text = $"Score: {record.score:N0}";
        if (bestScoreText != null)
            bestScoreText.text = best != null ? $"Best ({difficulty}): {best.score:N0}" : string.Empty;
        if (detailsText != null)
            detailsText.text = $"Level reached: {record.levelReached}\nAccuracy: {record.Accuracy:P0}\nBest streak: {record.bestStreak}";
        if (newBestBadge != null)
            newBestBadge.SetActive(isNewBest);
        if (panelRoot != null)
            panelRoot.SetActive(true);
    }

    public void Hide()
    {
        if (panelRoot != null && panelRoot != gameObject)
            panelRoot.SetActive(false);
    }

    private void HandleGameStarted(GameState state) => Hide();
}
