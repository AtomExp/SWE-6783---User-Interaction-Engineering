using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class OptionsController : MonoBehaviour
{
    [SerializeField] private AudioMixer audioMixer;

    [SerializeField] private Slider musicSlider;
    [SerializeField] private Slider sfxSlider;

    private void Start()
    {
        float musicVolume = PlayerPrefs.GetFloat("MusicVolume", 1f);
        float sfxVolume = PlayerPrefs.GetFloat("SFXVolume", 1f);

        musicSlider.minValue = 0f;
        musicSlider.maxValue = 1f;

        sfxSlider.minValue = 0f;
        sfxSlider.maxValue = 1f;

        musicSlider.SetValueWithoutNotify(musicVolume);
        sfxSlider.SetValueWithoutNotify(sfxVolume);

        SetMusicVolume(musicVolume);
        SetSFXVolume(sfxVolume);

        musicSlider.onValueChanged.AddListener(SetMusicVolume);
        sfxSlider.onValueChanged.AddListener(SetSFXVolume);
    }

    public void SetMusicVolume(float volume)
    {
        SetMixerVolume("MusicVolume", volume);
        PlayerPrefs.SetFloat("MusicVolume", volume);
    }

    public void SetSFXVolume(float volume)
    {
        SetMixerVolume("SoundVolume", volume);
        PlayerPrefs.SetFloat("SoundVolume", volume);
    }

    private void SetMixerVolume(string parameter, float volume)
    {
        float decibels = volume <= 0f
            ? -80f
            : Mathf.Log10(volume) * 20f;

        audioMixer.SetFloat(parameter, decibels);
    }

    public void GoBackToMainMenu()
    {
        SceneManager.LoadScene("MainMenu");
    }
}