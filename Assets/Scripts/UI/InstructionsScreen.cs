using UnityEngine;
using UnityEngine.UI;

// Instructions: explains the matching rules and controls. Opened from the Main Menu.
public class InstructionsScreen : MonoBehaviour
{
    [SerializeField] Button backButton;

    void Awake()
    {
        backButton.onClick.AddListener(() => SceneLoader.Load(SceneNames.MainMenu));
    }
}
