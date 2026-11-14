using System.Collections;
using TMPro;
using UnityEngine;

public class FeedbackMessageController : MonoBehaviour
{
    [SerializeField] private TMP_Text feedbackText;
    [SerializeField] private float displayDuration = 0.5f;

    [Header("Message Colors")]
    [SerializeField] private Color successColor = Color.green;
    [SerializeField] private Color errorColor = Color.red;

    private Coroutine messageRoutine;

    public void ShowSuccess()
    {
        ShowMessage("In Sync!", successColor);
    }

    public void ShowError()
    {
        ShowMessage("Oh Nooooo!", errorColor);
    }

    private void ShowMessage(string message, Color color)
    {
        if (messageRoutine != null)
            StopCoroutine(messageRoutine);

        feedbackText.transform.localScale = Vector3.one;
        feedbackText.text = message;
        feedbackText.color = color;
        feedbackText.gameObject.SetActive(true);

        messageRoutine = StartCoroutine(HideMessage());
    }

    private IEnumerator HideMessage()
    {
        yield return new WaitForSecondsRealtime(displayDuration);

        feedbackText.gameObject.SetActive(false);
        messageRoutine = null;
    }
}