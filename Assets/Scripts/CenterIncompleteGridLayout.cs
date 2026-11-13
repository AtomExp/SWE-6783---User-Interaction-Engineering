using UnityEngine;
using UnityEngine.UI;

public class CenterIncompleteGridLayout : GridLayoutGroup
{
    public override void SetLayoutHorizontal()
    {
        base.SetLayoutHorizontal();
        CenterLastRow();
    }

    public override void SetLayoutVertical()
    {
        base.SetLayoutVertical();
        CenterLastRow();
    }

    private void CenterLastRow()
    {
        int columns = constraintCount;
        int cardCount = transform.childCount;

        if (columns <= 0 || cardCount == 0)
            return;

        int lastRowCount = cardCount % columns;

        if (lastRowCount == 0)
            return;

        float offset =
            (columns - lastRowCount) *
            (cellSize.x + spacing.x) / 2f;

        int firstCard = cardCount - lastRowCount;

        for (int i = firstCard; i < cardCount; i++)
        {
            RectTransform card =
                transform.GetChild(i) as RectTransform;

            if (card == null)
                continue;

            card.anchoredPosition +=
                new Vector2(offset, 0f);
        }
    }
}