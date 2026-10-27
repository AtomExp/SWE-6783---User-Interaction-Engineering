using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections;

public class ButtonHoverEffect : MonoBehaviour,
    IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler,
    IPointerUpHandler
{
    [SerializeField] private float hoverScale = 1.05f;
    [SerializeField] private float clickScale = 0.95f;
    [SerializeField] private float speed = 12f;

    private Vector3 originalScale;
    private Vector3 targetScale;
    private Coroutine animationRoutine;

    private void Awake()
    {
        originalScale = transform.localScale;
        targetScale = originalScale;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        AnimateTo(originalScale * hoverScale);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        AnimateTo(originalScale);
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        AnimateTo(originalScale * clickScale);
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        AnimateTo(originalScale * hoverScale);
    }

    private void AnimateTo(Vector3 scale)
    {
        targetScale = scale;

        if (animationRoutine != null)
            StopCoroutine(animationRoutine);

        animationRoutine = StartCoroutine(ScaleSmoothly());
    }

    private IEnumerator ScaleSmoothly()
    {
        while (Vector3.Distance(transform.localScale, targetScale) > 0.001f)
        {
            transform.localScale = Vector3.Lerp(
                transform.localScale, targetScale, Time.unscaledDeltaTime * speed);

            yield return null;
        }

        transform.localScale = targetScale;
        animationRoutine = null;
    }

    private void OnDisable()
    {
        if (animationRoutine != null)
            StopCoroutine(animationRoutine);

        transform.localScale = originalScale;
        targetScale = originalScale;
        animationRoutine = null;
    }
}