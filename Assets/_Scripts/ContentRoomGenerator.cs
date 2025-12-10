using System.Collections.Generic;
using UnityEngine;

public class ContentRoomGenerator : MonoBehaviour
{
    [SerializeField] private GameObject[] enemyPrefabs;
    [SerializeField] private GameObject[] itemPrefabs;
    [SerializeField] private int maxEnemiesPerRoom = 3;
    [SerializeField] private int maxItemsPerRoom = 2;
    [SerializeField] private int placementIterations = 20;

    private HashSet<Vector2Int> roomFloor;
    private HashSet<Vector2Int> roomFloorNoCorridor;

    public void GenerateRoomContent(HashSet<Vector2Int> roomFloor,
                                    HashSet<Vector2Int> roomFloorNoCorridor,
                                    bool spawnEnemies = true,
                                    bool spawnTreasure = false)
    {
        this.roomFloor = roomFloor;
        this.roomFloorNoCorridor = roomFloorNoCorridor;

        ItemPlacementHelper placementHelper = new ItemPlacementHelper(roomFloor, roomFloorNoCorridor);

        if(spawnTreasure && itemPrefabs.Length > 0)
        {
            // Zak³adamy, ¿e ostatni prefab w itemPrefabs to skarb
            GameObject treasurePrefab = itemPrefabs[itemPrefabs.Length - 1];
            Vector2Int size = Vector2Int.one;
            Vector2? pos = placementHelper.GetItemPlacementPosition(PlacementType.OpenSpace, 20, size, false);
            if(pos.HasValue)
                Instantiate(treasurePrefab, new Vector3(pos.Value.x, pos.Value.y, 0), Quaternion.identity);
        }

        if(spawnEnemies)
            PlaceEnemies(placementHelper);

        PlaceItems(placementHelper); // pozosta³e przedmioty
    }

    private void PlaceItems(ItemPlacementHelper placementHelper)
    {
        for (int i = 0; i < maxItemsPerRoom; i++)
        {
            if (itemPrefabs.Length == 0)
                return;

            GameObject prefab = itemPrefabs[Random.Range(0, itemPrefabs.Length-1)];
            Vector2Int size = Vector2Int.one;

            Vector2? position = placementHelper.GetItemPlacementPosition(
                PlacementType.OpenSpace,
                placementIterations,
                size,
                false);

            if (position.HasValue)
            {
                Instantiate(prefab, new Vector3(position.Value.x, position.Value.y, 0), Quaternion.identity);
            }
        }
    }

    private void PlaceEnemies(ItemPlacementHelper placementHelper)
    {
        for (int i = 0; i < maxEnemiesPerRoom; i++)
        {
            if (enemyPrefabs.Length == 0)
                return;

            GameObject prefab = enemyPrefabs[Random.Range(0, enemyPrefabs.Length)];
            Vector2Int size = Vector2Int.one;

            Vector2? position = placementHelper.GetItemPlacementPosition(
                PlacementType.NearWall,
                placementIterations,
                size,
                false);

            if (position.HasValue)
            {
                Instantiate(prefab, new Vector3(position.Value.x, position.Value.y, 0), Quaternion.identity);
            }
        }
    }
}
