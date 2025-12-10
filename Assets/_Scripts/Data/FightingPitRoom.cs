using System.Collections.Generic;
using UnityEngine;

public class FightingPitRoom : MonoBehaviour
{
    //public PrefabPlacer PrefabPlacer;

    [Header("Enemy Placement Data")]
    public List<EnemyPlacementData> EnemyPlacementData = new List<EnemyPlacementData>();

    [Header("Item Data")]
    public List<ItemPlacementData> ItemData = new List<ItemPlacementData>();
}
