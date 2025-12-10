using UnityEngine;

public class EndTurnTrigger : MonoBehaviour, IClickable
{
    public TurnManager turnManager;

    private bool? lastIsPlayerTurn = null;
    private Quaternion targetLocalRotation;
    public float rotateSpeedDegPerSec = 360f;
    private Quaternion baseWorldRotation;

    private void Start()
    {
        if (turnManager == null)
            turnManager = FindFirstObjectByType<TurnManager>();

        // store the original local rotation (X/Z tilt included)
        baseWorldRotation = transform.rotation; // store world rotation

        if (turnManager != null)
        {
            float angleY = turnManager.isPlayerTurn ? 0f : 180f;
            targetLocalRotation = Quaternion.Euler(0f, angleY, 0f) * baseWorldRotation;
            transform.rotation = targetLocalRotation;
            lastIsPlayerTurn = turnManager.isPlayerTurn;
        }
    }

    private void Update()
    {
        if (turnManager == null)
            return;

        bool currentTurn = turnManager.isPlayerTurn;

        if (lastIsPlayerTurn != currentTurn)
        {
            float angleY = currentTurn ? 0f : 180f;
            targetLocalRotation = Quaternion.Euler(0f, angleY, 0f) * baseWorldRotation;
            lastIsPlayerTurn = currentTurn;
        }

        transform.rotation = Quaternion.RotateTowards(
            transform.rotation,
            targetLocalRotation,
            rotateSpeedDegPerSec * Time.deltaTime
        );
    }

    public void OnClicked()
    {
        if (turnManager != null && turnManager.isPlayerTurn)
        {
            turnManager.EndPlayerTurn();
        }
    }
}
