using System;
using UnityEngine;
using UnityEngine.InputSystem;

[DefaultExecutionOrder(-100)] // early
public class CardClickInputManager : MonoBehaviour
{
    public static CardClickInputManager Instance { get; private set; }

    [Header("Input Actions (assign in Inspector)")]
    public InputActionReference clickAction;          // typ Button, binding: <Mouse>/leftButton (lub touch tap)
    public InputActionReference pointerPositionAction; // typ Value(Vector2), binding: <Mouse>/position and <Touchscreen>/position

    [Header("Global Toggle Actions (optional)")]
    public InputActionReference toggleToTableAction;    // typ Button, binding: <Keyboard>/w
    public InputActionReference toggleToStartAction;

    [Header("Raycast settings")]
    public Camera raycastCamera; // zostaw null -> Camera.main u¿yta
    public float maxDistance = 100f;
    public LayerMask raycastMask = ~0; // wszystko domyœlnie

    public event Action OnToggleToTable;
    public event Action OnToggleToStart;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    private void OnEnable()
    {
        if (clickAction != null && clickAction.action != null)
            clickAction.action.performed += OnClickPerformed;

        if (toggleToTableAction != null && toggleToTableAction.action != null)
            toggleToTableAction.action.performed += OnToggleToTablePerformed;

        if (toggleToStartAction != null && toggleToStartAction.action != null)
            toggleToStartAction.action.performed += OnToggleToStartPerformed;

        // upewnij siê, ¿e akcje s¹ w³¹czone
        clickAction?.action?.Enable();
        pointerPositionAction?.action?.Enable();
        toggleToTableAction?.action?.Enable();
        toggleToStartAction?.action?.Enable();
    }

    private void OnDisable()
    {
        if (clickAction != null && clickAction.action != null)
            clickAction.action.performed -= OnClickPerformed;

        if (toggleToTableAction != null && toggleToTableAction.action != null)
            toggleToTableAction.action.performed -= OnToggleToTablePerformed;

        if (toggleToStartAction != null && toggleToStartAction.action != null)
            toggleToStartAction.action.performed -= OnToggleToStartPerformed;

        clickAction?.action?.Disable();
        pointerPositionAction?.action?.Disable();
        toggleToTableAction?.action?.Disable();
        toggleToStartAction?.action?.Disable();
    }

    private void OnClickPerformed(InputAction.CallbackContext ctx)
    {
        Vector2 screenPos = GetPointerScreenPosition(ctx);
        DoRaycast(screenPos);
    }

    private void OnToggleToTablePerformed(InputAction.CallbackContext ctx)
    {
        OnToggleToTable?.Invoke();
    }

    private void OnToggleToStartPerformed(InputAction.CallbackContext ctx)
    {
        OnToggleToStart?.Invoke();
    }

    /// <summary>
    /// Pobiera pozycjê kursora / dotyku — próbuje InputAction, potem Mouse/Ttouch jako fallback.
    /// </summary>
    private Vector2 GetPointerScreenPosition(InputAction.CallbackContext ctx)
    {
        // 1) jeœli mamy osobn¹ akcjê pointerPositionAction, u¿yj jej
        if (pointerPositionAction != null && pointerPositionAction.action != null)
        {
            return pointerPositionAction.action.ReadValue<Vector2>();
        }

        // 2) spróbuj odczytaæ z kontekstu (jeœli binding przekazywa³ Vector2)
        // (najczêœciej nie bêdzie - pozostaw jako fallback)
        if (ctx.control != null && ctx.control is InputControl)
        {
            // nie zawsze ma sens, ale próbujemy:
            try
            {
                var v2 = ctx.ReadValue<Vector2>();
                if (v2 != Vector2.zero) return v2;
            }
            catch { }
        }

        // 3) fallback na Mouse / Touchscreen
        if (Mouse.current != null)
        {
            return Mouse.current.position.ReadValue();
        }
        if (Touchscreen.current != null)
        {
            var touch = Touchscreen.current.primaryTouch;
            if (touch != null) return touch.position.ReadValue();
        }

        // default
        return new Vector2(Screen.width / 2f, Screen.height / 2f);
    }

    private void DoRaycast(Vector2 screenPos)
    {
        Camera cam = raycastCamera != null ? raycastCamera : Camera.main;
        if (cam == null)
        {
            Debug.LogWarning("CardClickInputManager: brak kamery do raycastów (Camera.main == null).");
            return;
        }

        Ray ray = cam.ScreenPointToRay(screenPos);
        if (Physics.Raycast(ray, out RaycastHit hit, maxDistance, raycastMask))
        {
            var clickable = hit.collider.GetComponent<IClickable>();
            if (clickable != null)
            {
                clickable.OnClicked();
                return;
            }

            // nastêpnie spróbuj w rodzicu (np. collider na childzie)
            var clickableParent = hit.collider.GetComponentInParent<IClickable>();
            if (clickableParent != null)
            {
                clickableParent.OnClicked();
                return;
            }
        }
    }
}
