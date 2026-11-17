using UnityEngine;

public class MenuAudioManager : MonoBehaviour
{
    public static MenuAudioManager Instance { get; private set; }

    private AudioSource musicSource;

    private const string MusicVolumeKey = "MusicVolume";
    private const string SFXVolumeKey = "SFXVolume";

    private float musicVolume = 0.4f;
    private float sfxVolume = 1f;

    public float GetMusicVolume() => musicVolume;
    public float GetSFXVolume() => sfxVolume;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        musicSource = GetComponent<AudioSource>();

        musicVolume = PlayerPrefs.GetFloat(MusicVolumeKey, 0.4f);
        sfxVolume = PlayerPrefs.GetFloat(SFXVolumeKey, 1f);

        if (musicSource != null)
        {
            musicSource.volume = musicVolume;
        }
    }

    public void SetMusicVolume(float volume)
    {
        musicVolume = Mathf.Clamp01(volume);

        if (musicSource != null)
            musicSource.volume = musicVolume;

        PlayerPrefs.SetFloat(MusicVolumeKey, musicVolume);
        PlayerPrefs.Save();
    }

    public void SetSFXVolume(float volume)
    {
        sfxVolume = Mathf.Clamp01(volume);

        PlayerPrefs.SetFloat(SFXVolumeKey, sfxVolume);
        PlayerPrefs.Save();
    }
}