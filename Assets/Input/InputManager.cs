using UnityEngine;
using UnityEngine.InputSystem;

public class InputManager : MonoBehaviour
{
    public static Vector2 Movement;
    public static bool DashPressed;
    public static bool AttackPressed;

    private PlayerInput _playerInput;
    private InputAction _moveAction;
    private InputAction _dashAction;
    private InputAction _attackAction;

    private void Awake()
    {
        _playerInput = GetComponent<PlayerInput>();

        _moveAction = _playerInput.actions["Move"];
        _dashAction = _playerInput.actions["Dash"];
        _attackAction = _playerInput.actions["Attack"];
    }

    private void Update()
    {
        Movement = _moveAction.ReadValue<Vector2>();
        DashPressed = _dashAction.WasPressedThisFrame();
        AttackPressed = _attackAction.WasPressedThisFrame();
    }
}
