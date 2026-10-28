using UnityEngine;
using UnityEngine.UI;

// Difficulty Selection: the player picks Easy or Hard, then goes straight to the Study Phase.
public class DifficultySelectionScreen : MonoBehaviour
{
    [SerializeField] Button easyButton;
    [SerializeField] Button hardButton;
    [SerializeField] Button backButton;

    void Awake()
    {
        easyButton.onClick.AddListener(() => SelectDifficulty(Difficulty.Easy));
        hardButton.onClick.AddListener(() => SelectDifficulty(Difficulty.Hard));
        backButton.onClick.AddListener(() => SceneLoader.Load(SceneNames.MainMenu));
    }

    void SelectDifficulty(Difficulty difficulty)
    {
        GameSettings.SelectedDifficulty = difficulty;
        SceneLoader.Load(SceneNames.StudyPhase);
    }
}
