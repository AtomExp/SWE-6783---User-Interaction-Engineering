using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class OptionsController : MonoBehaviour
{
    [Header("Volume Sliders")]
    [SerializeField] private Slider musicVolumeSlider;
    [SerializeField] private Slider soundEffectsSlider;

    private void Start()
    {
        if (MenuAudioManager.Instance == null)
        {
            Debug.LogWarning("MenuAudioManager was not found.");
            return;
        }

        musicVolumeSlider.value =
            MenuAudioManager.Instance.GetMusicVolume();

        soundEffectsSlider.value =
            MenuAudioManager.Instance.GetSFXVolume();

        musicVolumeSlider.onValueChanged.AddListener(SetMusicVolume);
        soundEffectsSlider.onValueChanged.AddListener(SetSFXVolume);
    }

    private void SetMusicVolume(float volume)
    {
        if (MenuAudioManager.Instance != null)
            MenuAudioManager.Instance.SetMusicVolume(volume);
    }

    private void SetSFXVolume(float volume)
    {
        if (MenuAudioManager.Instance != null)
            MenuAudioManager.Instance.SetSFXVolume(volume);
    }

    public void GoBackToMainMenu()
    {
        SceneManager.LoadScene("MainMenu");
    }

    private void OnDestroy()
    {
        if (musicVolumeSlider != null)
            musicVolumeSlider.onValueChanged.RemoveListener(SetMusicVolume);

        if (soundEffectsSlider != null)
            soundEffectsSlider.onValueChanged.RemoveListener(SetSFXVolume);
    }
}