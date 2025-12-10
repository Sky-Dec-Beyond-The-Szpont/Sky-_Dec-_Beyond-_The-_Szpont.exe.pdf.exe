using UnityEngine;

[CreateAssetMenu(fileName = "Deck", menuName = "Cards/Deck")]
public class DeckSO : ScriptableObject
{
    [Tooltip("Lista referencji do CardSO (unikalne karty).")]
    public CardSO[] cardPool;

    [Tooltip("Domyœlny rozmiar decku generowanego z cardPool (opcjonalnie mo¿na ignorowaæ).")]
    public int defaultDeckSize = 20;
}