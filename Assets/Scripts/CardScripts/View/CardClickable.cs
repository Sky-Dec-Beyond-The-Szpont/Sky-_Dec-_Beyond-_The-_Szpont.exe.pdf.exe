// CardClickable3D.cs
using UnityEngine;

[RequireComponent(typeof(Collider))]
public class CardClickable3D : MonoBehaviour, IClickable
{
    [HideInInspector] public CardView view;

    // -> explicit owner reference (przypisz przy spawn'ie karty)
    [HideInInspector] public PlayerHandVisualizer owner;

    void Start()
    {
        if (view == null) view = GetComponent<CardView>();

        // fallback: spróbuj znaleŸæ ownera w parentach (przydatne przy rêcznym dodawaniu)
        if (owner == null)
        {
            owner = GetComponentInParent<PlayerHandVisualizer>();
        }
    }

    public void OnClicked()
    {
        if (owner != null)
        {
            owner.OnCardClicked(gameObject);
        }
        else
        {
            Debug.LogWarning("CardClickable3D: brak przypisanego ownera (PlayerHandVisualizer).");
        }
    }
}
