using UnityEngine;

// Lets a stretch-anchored play area (the card tray) grow into extra screen
// space on non-16:9 windows, but keeps its width/height ratio within limits
// so it never turns into a long plank (ultrawide) or a tall box (portrait).
// Any space beyond the limit is split evenly, keeping the tray centered in
// the region its anchors and offsets give it.
//
// The anchors/offsets set in the editor define the largest region the tray
// may use; this only ever shrinks it.
[RequireComponent(typeof(RectTransform))]
public class PlayAreaAspectLimit : MonoBehaviour
{
    [Tooltip("Narrowest allowed shape (width / height).")]
    [SerializeField] private float minAspect = 1.25f;

    [Tooltip("Widest allowed shape (width / height).")]
    [SerializeField] private float maxAspect = 2.3f;

    private RectTransform rect;
    private RectTransform parent;
    private Vector2 baseOffsetMin;
    private Vector2 baseOffsetMax;
    private Vector2 lastParentSize;

    private void Awake()
    {
        rect = (RectTransform)transform;
        parent = rect.parent as RectTransform;
        baseOffsetMin = rect.offsetMin;
        baseOffsetMax = rect.offsetMax;
        Apply();
    }

    private void LateUpdate()
    {
        if (parent != null && parent.rect.size != lastParentSize)
            Apply();
    }

    private void Apply()
    {
        if (parent == null)
            return;

        lastParentSize = parent.rect.size;

        Vector2 anchorSpan = rect.anchorMax - rect.anchorMin;

        // Size the region would have from its anchors and offsets alone.
        float width = lastParentSize.x * anchorSpan.x + baseOffsetMax.x - baseOffsetMin.x;
        float height = lastParentSize.y * anchorSpan.y + baseOffsetMax.y - baseOffsetMin.y;

        if (width <= 0f || height <= 0f)
            return;

        float clampedWidth = Mathf.Min(width, height * maxAspect);
        float clampedHeight = Mathf.Min(height, width / minAspect);

        Vector2 trim = new Vector2(
            (width - clampedWidth) * 0.5f,
            (height - clampedHeight) * 0.5f);

        rect.offsetMin = baseOffsetMin + trim;
        rect.offsetMax = baseOffsetMax - trim;
    }
}
