using TMPro;
using UnityEngine;
using UnityEngine.UI;

// One row in the Score Dashboard history. Assign the separate columns for a table layout,
// or only summaryText for one formatted line. With nothing assigned, a summary text is created.
public class ScoreRowView : MonoBehaviour
{
    [SerializeField] private TMP_Text rankText;
    [SerializeField] private TMP_Text dateText;
    [SerializeField] private TMP_Text modeText;
    [SerializeField] private TMP_Text scoreText;
    [SerializeField] private TMP_Text levelText;
    [SerializeField] private TMP_Text accuracyText;
    [SerializeField] private TMP_Text summaryText;

    [SerializeField] private Image background;
    [SerializeField] private Color normalColor = new Color(1f, 1f, 1f, 0.1f);
    [SerializeField] private Color highlightColor = new Color(1f, 0.83f, 0.23f, 0.45f);

    public void Bind(ScoreRecord record, int rank, bool highlight)
    {
        EnsureBuilt();

        string date = record.TimestampUtc.ToLocalTime().ToString("MMM d, h:mm tt");
        string mode = ScoreRepository.DifficultyOf(record).ToString();

        SetText(rankText, $"#{rank}");
        SetText(dateText, date);
        SetText(modeText, mode);
        SetText(scoreText, record.score.ToString("N0"));
        SetText(levelText, $"Lv {record.levelReached}");
        SetText(accuracyText, record.Accuracy.ToString("P0"));
        SetText(summaryText, $"#{rank}   {record.score:N0} pts   {mode}   Lv {record.levelReached}   {record.Accuracy:P0}   {date}");

        if (background != null)
            background.color = highlight ? highlightColor : normalColor;
    }

    private void EnsureBuilt()
    {
        if (background == null)
            background = GetComponent<Image>();

        bool hasColumns = rankText || dateText || modeText || scoreText || levelText || accuracyText;
        if (hasColumns || summaryText != null)
            return;

        if (background == null)
            background = gameObject.AddComponent<Image>();

        // Explicit null check: "??" skips Unity's null handling for missing components.
        var layout = GetComponent<LayoutElement>();
        if (layout == null)
            layout = gameObject.AddComponent<LayoutElement>();
        layout.minHeight = 56f;
        layout.preferredHeight = 56f;

        var textObject = new GameObject("Summary", typeof(RectTransform));
        textObject.transform.SetParent(transform, false);
        var rect = (RectTransform)textObject.transform;
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = new Vector2(16f, 4f);
        rect.offsetMax = new Vector2(-16f, -4f);

        summaryText = textObject.AddComponent<TextMeshProUGUI>();
        summaryText.fontSize = 28f;
        summaryText.alignment = TextAlignmentOptions.MidlineLeft;
        summaryText.raycastTarget = false;
    }

    private static void SetText(TMP_Text label, string value)
    {
        if (label != null)
            label.text = value;
    }
}
