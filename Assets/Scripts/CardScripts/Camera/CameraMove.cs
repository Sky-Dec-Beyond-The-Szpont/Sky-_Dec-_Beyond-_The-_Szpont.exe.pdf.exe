using UnityEngine;
using UnityEngine.InputSystem;

public class CameraMoveToggle : MonoBehaviour
{
    [Header("Camera Point")]
    public Transform tablePoint;   // punkt nad sto³em (widok na karty)

    [Header("Settings")]
    public float moveSpeed = 3f;   // prêdkoœæ interpolacji
    public float tablePitch = 55f;
    public float stopThreshold = 0.01f;

    private Vector3 startPosition;
    private Vector3 startEuler;

    private bool movingToTable = false;
    private bool movingToStart = false;

    private void Start()
    {
        startPosition = transform.position;
        startEuler = transform.eulerAngles;
    }

    private void OnEnable()
    {
        if (CardClickInputManager.Instance != null)
        {
            CardClickInputManager.Instance.OnToggleToTable += HandleToggleToTable;
            CardClickInputManager.Instance.OnToggleToStart += HandleToggleToStart;
        }
        else
        {
            Debug.LogWarning("CameraMoveToggle: CardClickInputManager.Instance not found — subscribe failed. Przywróæ manager lub przypisz akcje do managera.");
        }
    }

    private void OnDisable()
    {
        if (CardClickInputManager.Instance != null)
        {
            CardClickInputManager.Instance.OnToggleToTable -= HandleToggleToTable;
            CardClickInputManager.Instance.OnToggleToStart -= HandleToggleToStart;
        }
    }

    private void HandleToggleToTable()
    {
        movingToTable = true;
        movingToStart = false;
    }

    private void HandleToggleToStart()
    {
        movingToStart = true;
        movingToTable = false;
    }

    private void Update()
    {
        if (movingToTable && tablePoint != null)
        {
            MoveTowards(tablePoint.position, tablePoint.eulerAngles, tablePitch, ref movingToTable);
        }
        else if (movingToStart)
        {
            MoveTowards(startPosition, startEuler, startEuler.x, ref movingToStart);
        }
    }

    private void MoveTowards(Vector3 targetPos, Vector3 targetEuler, float pitch, ref bool movingFlag)
    {
        // pozycja – p³ynne przejœcie
        transform.position = Vector3.Lerp(transform.position, targetPos, Time.deltaTime * moveSpeed);

        // rotacja – p³ynne przejœcie z korekt¹ pitcha
        Vector3 targetRotEuler = targetEuler;
        targetRotEuler.x = pitch;
        Quaternion targetRot = Quaternion.Euler(targetRotEuler);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, Time.deltaTime * moveSpeed);

        // sprawdŸ czy wystarczaj¹co blisko – ale bez nag³ego przeskoku
        if (Vector3.Distance(transform.position, targetPos) < stopThreshold &&
            Quaternion.Angle(transform.rotation, targetRot) < 0.1f)
        {
            movingFlag = false;
        }
    }
}
