using UnityEngine;

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

    [Header("SFX Volume")]
    [Range(0f, 1f)]
    [SerializeField] private float volume = 0.5f;

    private AudioSource audioSource;

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
        if (clip != null)
        {
            audioSource.PlayOneShot(clip, volume);
        }
    }
}