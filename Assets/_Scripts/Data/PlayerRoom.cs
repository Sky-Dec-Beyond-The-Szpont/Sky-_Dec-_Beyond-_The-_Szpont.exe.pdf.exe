using System.Collections.Generic;
using UnityEngine;

public class PlayerRoom : MonoBehaviour
{
    [Header("Player Room")]
    public GameObject Player;

    [Header("Item Data")]
    public List<ItemPlacementData> ItemData = new List<ItemPlacementData>();

}
