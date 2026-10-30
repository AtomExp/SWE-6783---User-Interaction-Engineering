using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class WelcomeScreenController : MonoBehaviour
{
    [Header("Welcome Screen References")]
    [SerializeField] private RectTransform gameLogo;
    [SerializeField] private CanvasGroup tagLineGroup;
    [SerializeField] private CanvasGroup loadingBarGroup;
    [SerializeField] private Image loadingFill;
    [SerializeField] private CanvasGroup screenGroup;
    [SerializeField] private AudioSource welcomeMusic;

    [Header("Animation Settings")]
    [SerializeField] private float logoDropDuration = 1.5f;
    [SerializeField] private float taglineFadeDuration = 0.5f;
    [SerializeField] private float loadingDuration = 3f;
    [SerializeField] private float transitionDuration = 0.7f;

    private Vector2 logoFinalPosition;

    private void Start()
    {
        logoFinalPosition = gameLogo.anchoredPosition;

        tagLineGroup.alpha = 0;
        loadingBarGroup.alpha = 0;

        gameLogo.anchoredPosition =
            logoFinalPosition + new Vector2(0, 700);

        loadingFill.fillAmount = 0;

        // Start welcome screen sound.
        if (welcomeMusic != null)
        {
            welcomeMusic.Play();
        }

        StartCoroutine(PlayWelcomeAnimation());
    }

    private IEnumerator PlayWelcomeAnimation()
    {
        yield return StartCoroutine(AnimateLogo());

        yield return StartCoroutine(FadeIn(tagLineGroup));

        yield return StartCoroutine(FadeIn(loadingBarGroup));

        yield return StartCoroutine(LoadProgress());

        yield return StartCoroutine(FadeOut());

        // Stop sound before opening Main Menu.
        if (welcomeMusic != null)
        {
            welcomeMusic.Stop();
        }

        SceneManager.LoadScene("MainMenu");
    }

    private IEnumerator AnimateLogo()
    {
        float elapsed = 0f;
        Vector2 startPosition =
            logoFinalPosition + new Vector2(0, 700);

        while (elapsed < logoDropDuration)
        {
            elapsed += Time.deltaTime;

            float progress =
                Mathf.Clamp01(elapsed / logoDropDuration);

            float bounce =
                1f - Mathf.Pow(1f - progress, 3f);

            float bounceHeight =
                Mathf.Sin(progress * Mathf.PI * 3f) *
                (1f - progress) * 45f;

            gameLogo.anchoredPosition =
                Vector2.Lerp(startPosition, logoFinalPosition, bounce)
                + new Vector2(0, bounceHeight);

            yield return null;
        }

        gameLogo.anchoredPosition = logoFinalPosition;
    }

    private IEnumerator FadeIn(CanvasGroup group)
    {
        float elapsed = 0f;

        while (elapsed < taglineFadeDuration)
        {
            elapsed += Time.deltaTime;
            group.alpha = Mathf.Clamp01(
                elapsed / taglineFadeDuration);

            yield return null;
        }

        group.alpha = 1f;
    }

    private IEnumerator LoadProgress()
    {
        float elapsed = 0f;

        while (elapsed < loadingDuration)
        {
            elapsed += Time.deltaTime;

            loadingFill.fillAmount =
                Mathf.Clamp01(elapsed / loadingDuration);

            yield return null;
        }

        loadingFill.fillAmount = 1f;
    }

    private IEnumerator FadeOut()
    {
        float elapsed = 0f;

        while (elapsed < transitionDuration)
        {
            elapsed += Time.deltaTime;

            screenGroup.alpha =
                1f - Mathf.Clamp01(elapsed / transitionDuration);

            yield return null;
        }

        screenGroup.alpha = 0f;
    }
}