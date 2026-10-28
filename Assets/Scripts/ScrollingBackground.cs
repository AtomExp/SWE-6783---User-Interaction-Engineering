using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(RawImage))]
public class ScrollingBackground : MonoBehaviour
{
    [Header("Scrolling")]
    public Vector2 scrollSpeed = new Vector2(0.05f, 0.05f);

    [Header("Texture Repetition")]
    public Vector2 tiling = new Vector2(2f, 2f);

    private RawImage background;
    private Vector2 offset;

    private void Awake()
    {
        background = GetComponent<RawImage>();
    }

    private void Update()
    {
        offset += scrollSpeed * Time.deltaTime;

        // Keep offsets within 0-1.
        offset.x = Mathf.Repeat(offset.x, 1f);
        offset.y = Mathf.Repeat(offset.y, 1f);

        background.uvRect = new Rect(
            offset.x,
            offset.y,
            tiling.x,
            tiling.y
        );
    }
}