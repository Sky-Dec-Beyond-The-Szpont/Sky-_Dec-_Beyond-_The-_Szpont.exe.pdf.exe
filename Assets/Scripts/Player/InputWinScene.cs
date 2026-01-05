using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class InputWinScene : MonoBehaviour
{
    [SerializeField] private InputActionReference confirmAction;
    [SerializeField] private string menuScene = "MenuScene";

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
        LevelLoader.Instance.LoadLevelByName(menuScene);
    }
}
