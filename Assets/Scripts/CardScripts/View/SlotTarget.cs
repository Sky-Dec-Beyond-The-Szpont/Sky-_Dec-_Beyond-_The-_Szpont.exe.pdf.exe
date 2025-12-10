// SlotTarget.cs
using UnityEngine;

public class SlotTarget : MonoBehaviour, IClickable
{
    public int slotIndex = 0;
    public MeshRenderer highlightRenderer; // opcjonalnie
    Color originalColor;

    // explicit owner reference (przypisz w inspektorze lub runtime)
    [HideInInspector] public PlayerHandVisualizer owner;

    void Start()
    {
        if (highlightRenderer != null) originalColor = highlightRenderer.material.color;

        if (owner == null)
        {
            // fallback: spróbuj znaleŸæ w rodzicach
            owner = GetComponentInParent<PlayerHandVisualizer>();
        }
    }

    public void OnClicked()
    {
        if (owner != null)
        {
            owner.OnSlotClicked(slotIndex, this);
        }
        else
        {
            Debug.LogWarning("SlotTarget: brak przypisanego ownera (PlayerHandVisualizer).");
        }
    }

    public void SetHighlight(bool on)
    {
        if (highlightRenderer == null) return;
        highlightRenderer.material.color = on ? Color.yellow : originalColor;
    }
}
