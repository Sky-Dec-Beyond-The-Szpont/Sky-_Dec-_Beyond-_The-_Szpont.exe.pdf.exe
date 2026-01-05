using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class InputMenu : MonoBehaviour
{
    [SerializeField] private InputActionReference confirmAction;
    [SerializeField] private string mapSceneName = "MapScene";
    [SerializeField] private GameObject elementsToTurnOff;

    private void OnEnable()
    {
        if (confirmAction != null)
        {
            confirmAction.action.performed += OnConfirmPressed;
            confirmAction.action.Enable();
        }
    }

    private void OnDisable()
    {
        if (confirmAction != null)
        {
            confirmAction.action.performed -= OnConfirmPressed;
            confirmAction.action.Disable();
        }
    }

    private void OnConfirmPressed(InputAction.CallbackContext ctx)
    {
        elementsToTurnOff.SetActive(false);

        LevelLoader.Instance.LoadLevelByName(mapSceneName);
    }
}
