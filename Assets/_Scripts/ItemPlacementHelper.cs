using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class ItemPlacementHelper
{
    Dictionary<PlacementType, HashSet<Vector2Int>>
        titleByType = new Dictionary<PlacementType, HashSet<Vector2Int>>();

    HashSet<Vector2Int> roomFloorNoCorridor;

    public ItemPlacementHelper(HashSet<Vector2Int> roomFloor,
        HashSet<Vector2Int> roomFloorNoCorridor) 
    {
        Graph graph = new Graph(roomFloor);
        this.roomFloorNoCorridor = roomFloorNoCorridor;

        foreach (var position in roomFloorNoCorridor)
        {
            int neighbourCount8Dir = graph.GetNeighbours8Directions(position).Count;
            PlacementType type = neighbourCount8Dir < 8 ? PlacementType.NearWall : PlacementType.OpenSpace; /// >> tez nwm czy dobrze

            if (titleByType.ContainsKey(type) == false)
                titleByType[type] = new HashSet<Vector2Int>();

            if (type == PlacementType.NearWall && graph.GetNeighbours4Directions(position).Count < 4) // ???? nwm czy to dobrze
                continue;
            titleByType[type].Add(position);
        }
    }

    public Vector2? GetItemPlacementPosition(PlacementType placementType, int iterationsMax, Vector2Int size, bool addOffset)
    {
        int itemArea = size.x * size.y;

        if (titleByType[placementType].Count < itemArea)
        {
            return null;
        }

        int iteration = 0;

        while (iteration < iterationsMax)
        {
            iteration++;

            int index = UnityEngine.Random.Range(0, titleByType[placementType].Count);
            Vector2Int position = titleByType[placementType].ElementAt(index);

            if (itemArea > 1)
            {
                var (result, placementPositions) = PlaceBigItem(position, size, addOffset);

                if (result == false)
                    continue;

                titleByType[placementType].ExceptWith(placementPositions);
                titleByType[PlacementType.NearWall].ExceptWith(placementPositions);

                
            }
            else
            {
                titleByType[placementType].Remove(position);
                
            }
            return position;
        }

        return null;
        
    }

    private (bool result, List<Vector2Int> placementPositions) PlaceBigItem(Vector2Int originPosition, Vector2Int size, bool addOffset)
    {
        List<Vector2Int> positions = new List<Vector2Int>() { originPosition };
        int maxX = addOffset ? size.x + 1 : size.x;
        int maxY = addOffset ? size.y + 1 : size.y;
        int minX = addOffset ? -1 : 0;
        int minY = addOffset ? -1 : 0;

        for (int row = minX; row <= maxX; row++)
        {
            for (int col = minY; col <= maxY; col++)
            {
                if (col == 0 && row == 0)
                    continue;
                Vector2Int newPosToCheck = new Vector2Int(originPosition.x + row, originPosition.y + col);
                if(roomFloorNoCorridor.Contains(newPosToCheck) == false)
                    return (false, positions);
                positions.Add(newPosToCheck);

            }
        }
        return (true, positions);
    }
}


public enum PlacementType
{
    OpenSpace,
    NearWall
}