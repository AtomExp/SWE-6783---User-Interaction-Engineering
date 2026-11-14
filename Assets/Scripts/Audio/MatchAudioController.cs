
using UnityEngine;
using System.Collections;

[RequireComponent(typeof(AudioSource))]
public class MatchAudioController : MonoBehaviour
{
    [Header("Match Sound Effects")]
    [SerializeField] private AudioClip successSound;
    [SerializeField] private AudioClip errorSound;

    [Header("Level Complete Sound")]
    [SerializeField] private AudioClip levelCompleteSound;

    [Header("Game Over Sound")]
    [SerializeField] private AudioClip gameOverSound;

    [Header("Music Audio Source")]
    [SerializeField] private AudioSource musicSource;

    [Header("SFX Volume")]
    [Range(0f, 1f)]
    [SerializeField] private float volume = 0.5f;

    private AudioSource audioSource;
    private Coroutine resumeCoroutine;

    private void Awake()
    {
        audioSource = GetComponent<AudioSource>();

        audioSource.playOnAwake = false;
        audioSource.loop = false;
        audioSource.spatialBlend = 0f;
    }

    public void PlaySuccessSound()
    {
        PlaySound(successSound);
    }

    public void PlayErrorSound()
    {
        PlaySound(errorSound);
    }

    public void PlayLevelCompleteSound()
    {
        PlaySound(levelCompleteSound);
    }

    public void PlayGameOverSound()
    {
        PlaySound(gameOverSound);
    }

    private void PlaySound(AudioClip clip)
    {
        if (clip == null)
            return;

        // Cancel previous resume timer
        if (resumeCoroutine != null)
        {
            StopCoroutine(resumeCoroutine);
            resumeCoroutine = null;
        }

        // Pause background music
        if (musicSource != null && musicSource.isPlaying)
        {
            musicSource.Pause();
        }

        // Play sound effect
        audioSource.PlayOneShot(clip, volume);

        // Resume music after sound effects finish
        resumeCoroutine = StartCoroutine(ResumeMusic());
    }

    private IEnumerator ResumeMusic()
    {
        while (audioSource.isPlaying)
        {
            yield return null;
        }

        if (musicSource != null)
        {
            musicSource.UnPause();
        }

        resumeCoroutine = null;
    }
}
