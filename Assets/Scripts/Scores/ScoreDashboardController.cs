using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

// Score Dashboard: best score per mode, score history (filter by mode, sort by recent or top) and Back.
// Every reference except historyContent is optional.
public class ScoreDashboardController : MonoBehaviour
{
    private enum Filter { All, Easy, Hard }
    private enum Sort { Recent, Top }

    [Header("Best Scores")]
    [SerializeField] private TMP_Text easyBestText;
    [SerializeField] private TMP_Text hardBestText;
    [SerializeField] private TMP_Text easyGamesText;
    [SerializeField] private TMP_Text hardGamesText;

    [Header("History")]
    [Tooltip("Content object of a ScrollRect, ideally with a VerticalLayoutGroup.")]
    [SerializeField] private RectTransform historyContent;
    [Tooltip("Optional row prefab. If empty, simple text rows are generated.")]
    [SerializeField] private ScoreRowView rowPrefab;
    [SerializeField] private TMP_Text emptyText;
    [SerializeField] private int maxRows = 20;

    [Header("Filters (optional)")]
    [SerializeField] private Button allButton;
    [SerializeField] private Button easyButton;
    [SerializeField] private Button hardButton;
    [SerializeField] private Button recentButton;
    [SerializeField] private Button topButton;

    [Header("Navigation")]
    [SerializeField] private Button backButton;
    [Tooltip("Optional. Needs a second tap within 3 seconds to confirm.")]
    [SerializeField] private Button clearHistoryButton;

    private readonly List<ScoreRowView> rows = new List<ScoreRowView>();
    private Filter filter = Filter.All;
    private Sort sort = Sort.Recent;
    private float clearConfirmUntil = -1f;
    private string clearButtonLabel;
    private ScoreRecord lastGame;

    private void Start()
    {
        // Open on the mode just played and highlight that game.
        var recent = ScoreRepository.GetRecent(null, 1);
        lastGame = recent.Count > 0 ? recent[0] : null;
        if (lastGame != null)
            filter = ScoreRepository.DifficultyOf(lastGame) == Difficulty.Hard ? Filter.Hard : Filter.Easy;

        AddListener(allButton, () => SetFilter(Filter.All));
        AddListener(easyButton, () => SetFilter(Filter.Easy));
        AddListener(hardButton, () => SetFilter(Filter.Hard));
        AddListener(recentButton, () => SetSort(Sort.Recent));
        AddListener(topButton, () => SetSort(Sort.Top));
        AddListener(backButton, GoBack);
        AddListener(clearHistoryButton, HandleClearClicked);

        if (clearHistoryButton != null)
        {
            var label = clearHistoryButton.GetComponentInChildren<TMP_Text>();
            clearButtonLabel = label != null ? label.text : null;
        }

        ScoreRepository.Changed += Refresh;
        Refresh();
    }

    private void OnDestroy()
    {
        ScoreRepository.Changed -= Refresh;
    }

    private void Update()
    {
        if (clearConfirmUntil > 0f && Time.unscaledTime > clearConfirmUntil)
        {
            clearConfirmUntil = -1f;
            SetClearLabel(clearButtonLabel);
        }
    }

    public void GoBack() => SceneLoader.Load(SceneNames.MainMenu);

    public void Refresh()
    {
        SetBest(easyBestText, easyGamesText, Difficulty.Easy);
        SetBest(hardBestText, hardGamesText, Difficulty.Hard);

        var records = sort == Sort.Top
            ? ScoreRepository.GetTop(SelectedDifficulty(), maxRows)
            : ScoreRepository.GetRecent(SelectedDifficulty(), maxRows);

        for (int i = 0; i < records.Count; i++)
        {
            var row = GetRow(i);
            if (row == null)
                break;
            row.gameObject.SetActive(true);
            bool isLastGame = lastGame != null && records[i].timestampUtcTicks == lastGame.timestampUtcTicks;
            row.Bind(records[i], i + 1, isLastGame);
        }

        for (int i = records.Count; i < rows.Count; i++)
            rows[i].gameObject.SetActive(false);

        if (emptyText != null)
            emptyText.gameObject.SetActive(records.Count == 0);

        SetSelected(allButton, filter == Filter.All);
        SetSelected(easyButton, filter == Filter.Easy);
        SetSelected(hardButton, filter == Filter.Hard);
        SetSelected(recentButton, sort == Sort.Recent);
        SetSelected(topButton, sort == Sort.Top);
    }

    private Difficulty? SelectedDifficulty()
    {
        switch (filter)
        {
            case Filter.Easy: return Difficulty.Easy;
            case Filter.Hard: return Difficulty.Hard;
            default: return null;
        }
    }

    private ScoreRowView GetRow(int index)
    {
        if (index < rows.Count)
            return rows[index];

        if (historyContent == null)
        {
            Debug.LogWarning("ScoreDashboardController: History Content is not assigned.", this);
            return null;
        }

        ScoreRowView row;
        if (rowPrefab != null)
        {
            row = Instantiate(rowPrefab, historyContent, false);
        }
        else
        {
            var go = new GameObject($"Score Row {index + 1}", typeof(RectTransform));
            go.transform.SetParent(historyContent, false);
            row = go.AddComponent<ScoreRowView>();
        }

        rows.Add(row);
        return row;
    }

    private void SetFilter(Filter value)
    {
        filter = value;
        Refresh();
    }

    private void SetSort(Sort value)
    {
        sort = value;
        Refresh();
    }

    private void HandleClearClicked()
    {
        if (clearConfirmUntil > 0f && Time.unscaledTime <= clearConfirmUntil)
        {
            clearConfirmUntil = -1f;
            SetClearLabel(clearButtonLabel);
            lastGame = null;
            ScoreRepository.ClearAll();
            return;
        }

        clearConfirmUntil = Time.unscaledTime + 3f;
        SetClearLabel("Tap again to confirm");
    }

    private void SetClearLabel(string text)
    {
        if (clearHistoryButton == null || text == null)
            return;
        var label = clearHistoryButton.GetComponentInChildren<TMP_Text>();
        if (label != null)
            label.text = text;
    }

    private static void SetBest(TMP_Text bestText, TMP_Text gamesText, Difficulty difficulty)
    {
        var best = ScoreRepository.GetBest(difficulty);
        if (bestText != null)
            bestText.text = best != null ? $"{best.score:N0}  (Lv {best.levelReached})" : "--";
        if (gamesText != null)
            gamesText.text = $"{ScoreRepository.GamesPlayed(difficulty)} games";
    }

    // The active filter/sort shows as a pressed (non-interactable) button.
    private static void SetSelected(Button button, bool selected)
    {
        if (button != null)
            button.interactable = !selected;
    }

    private static void AddListener(Button button, UnityAction action)
    {
        if (button != null)
            button.onClick.AddListener(action);
    }
}
