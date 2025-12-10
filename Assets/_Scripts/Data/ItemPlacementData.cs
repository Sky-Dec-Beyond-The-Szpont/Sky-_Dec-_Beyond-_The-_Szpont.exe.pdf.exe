using UnityEngine;

[System.Serializable]
public class ItemPlacementData
{
    public int MinQuantity;
    public int MaxQuantity;

    public ScriptableObject ItemData;
    // lub jeœli masz w³asn¹ klasê np. ItemData : ScriptableObject,
    // podmieñ typ powy¿ej na ItemData
}
