using UnityEngine;

[System.Serializable]
public class EnemyPlacementData
{
    public int MinQuantity;
    public int MaxQuantity;

    public GameObject EnemyPrefab;

    public Vector2 EnemySize;
}