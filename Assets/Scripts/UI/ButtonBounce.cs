using UnityEngine;
using UnityEngine.EventSystems;

// Grows a button slightly on hover and shrinks it on press so it clearly reads as clickable.
// Reusable on any button in the game.
public class ButtonBounce : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler
{
    [SerializeField] float hoverScale = 1.05f;
    [SerializeField] float pressedScale = 0.95f;
    [SerializeField] float speed = 14f;

    Vector3 baseScale;
    float targetScale = 1f;
    bool hovered;

    void Awake() => baseScale = transform.localScale;

    void OnDisable()
    {
        hovered = false;
        targetScale = 1f;
        transform.localScale = baseScale;
    }

    void Update()
    {
        // Unscaled time so buttons still animate while the game is paused.
        var target = baseScale * targetScale;
        transform.localScale = Vector3.Lerp(transform.localScale, target, 1f - Mathf.Exp(-speed * Time.unscaledDeltaTime));
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        hovered = true;
        targetScale = hoverScale;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        hovered = false;
        targetScale = 1f;
    }

    public void OnPointerDown(PointerEventData eventData) => targetScale = pressedScale;

    public void OnPointerUp(PointerEventData eventData) => targetScale = hovered ? hoverScale : 1f;
}
