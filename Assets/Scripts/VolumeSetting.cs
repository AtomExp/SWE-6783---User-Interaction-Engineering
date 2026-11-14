using UnityEngine;
using UnityEngine.Audio;

public class VolumeSetting : MonoBehaviour
{
    [SerializeField] private AudioMixer audioMixer;

    private static VolumeSetting instance;

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);

        ApplyVolume("MusicVolume");
        ApplyVolume("SoundVolume");
    }

    private void ApplyVolume(string parameter)
    {
        float volume = PlayerPrefs.GetFloat(parameter, 1f);

        float decibels = volume <= 0f
            ? -80f
            : Mathf.Log10(volume) * 20f;

        audioMixer.SetFloat(parameter, decibels);
    }
}
