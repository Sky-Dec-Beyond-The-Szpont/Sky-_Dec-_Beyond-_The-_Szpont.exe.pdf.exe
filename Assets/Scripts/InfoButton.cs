using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class InfoButton : MonoBehaviour
{
    [SerializeField]
    private string InstructionSceneName;
    private void Awake()
    {
        Image image = GetComponent<Image>();

        // Ensure the Image can receive clicks
        image.raycastTarget = true;
    }

    public void LoadInstructionScene()
    {
        if (LevelLoader.Instance != null)
        {
            LevelLoader.Instance.LoadLevelByName(InstructionSceneName);
        }
        else
        {
            SceneManager.LoadScene(InstructionSceneName);
        }
    }
}
