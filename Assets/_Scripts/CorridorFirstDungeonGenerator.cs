using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class CorridorFirstDungeonGenerator : SimpleRandomWalkMapGenerator
{
    [SerializeField]
    private int corridorLength = 14, corridorCount = 5;
    [SerializeField]
    [Range(0.1f, 1)]
    public float roomPercent = 0.8f;

    [SerializeField] private GameObject playerPrefab;
    private GameObject playerInstance;

    private Dictionary<Vector2Int, HashSet<Vector2Int>> roomsDictionary
        = new Dictionary<Vector2Int, HashSet<Vector2Int>>();

    private List<Color> roomColors = new List<Color>();

    private HashSet<Vector2Int> floorPositions, corridorPositions;

    protected override void RunProceduralGeneration()
    {
        CorridorFirstGeneration();
    }

    private void Start()
    {
        GenerateDungeon();
        SpawnPlayer();
    }

    private void CorridorFirstGeneration()
    {
        HashSet<Vector2Int> floorPositions = new HashSet<Vector2Int>();
        HashSet<Vector2Int> potentialRoomPositions = new HashSet<Vector2Int>();

        List<List<Vector2Int>> corridors =  CreateCorridors(floorPositions, potentialRoomPositions);

        HashSet<Vector2Int> roomPositions = CreateRooms(potentialRoomPositions);

        List<Vector2Int> deadEnds = FindAllDeadEnds(floorPositions);

        CreateRoomsAtDeadEnd(deadEnds, roomPositions);

        floorPositions.UnionWith(roomPositions);

        for(int i = 0; i < corridors.Count; i++)
        {
            corridors[i] = IncreaseCorridorSizeByOnee(corridors[i]);
            floorPositions.UnionWith(corridors[i]);
            // corridor = IncreaseCorridorBrush3by3(corridor);
        }

        tilemapVisualizer.PaintFloorTiles(floorPositions);
        WallGenerator.CreateWalls(floorPositions, tilemapVisualizer);

        // Dodaj treść do pokojów
        ContentRoomGenerator contentGenerator = UnityEngine.Object.FindFirstObjectByType<ContentRoomGenerator>();
        if (contentGenerator != null)
        {
            List<Vector2Int> roomCenters = new List<Vector2Int>(roomsDictionary.Keys);

            // 1️⃣ Pierwszy pokój (startowy) - bez wrogów
            Vector2Int firstRoomPos = roomCenters[0];
            HashSet<Vector2Int> firstRoom = roomsDictionary[firstRoomPos];
            HashSet<Vector2Int> firstRoomNoCorridor = new HashSet<Vector2Int>(firstRoom);
            firstRoomNoCorridor.ExceptWith(corridorPositions);

            contentGenerator.GenerateRoomContent(firstRoom, firstRoomNoCorridor, spawnEnemies: false, spawnTreasure: false);

            // 2️⃣ Znajdź ostatni pokój (najdalej od startu)
            Vector2Int lastRoomPos = GetFarthestRoom(roomCenters, startPos);
            HashSet<Vector2Int> lastRoom = roomsDictionary[lastRoomPos];
            HashSet<Vector2Int> lastRoomNoCorridor = new HashSet<Vector2Int>(lastRoom);
            lastRoomNoCorridor.ExceptWith(corridorPositions);

            contentGenerator.GenerateRoomContent(lastRoom, lastRoomNoCorridor, spawnEnemies: true, spawnTreasure: true);

            // 3️⃣ Pozostałe pokoje - normalni wrogowie, brak skarbu
            foreach (var roomPos in roomCenters)
            {
                if (roomPos == firstRoomPos || roomPos == lastRoomPos)
                    continue;

                HashSet<Vector2Int> room = roomsDictionary[roomPos];
                HashSet<Vector2Int> roomNoCorridor = new HashSet<Vector2Int>(room);
                roomNoCorridor.ExceptWith(corridorPositions);

                contentGenerator.GenerateRoomContent(room, roomNoCorridor, spawnEnemies: true, spawnTreasure: false);
            }
        }
    }

    private Vector2Int GetFarthestRoom(List<Vector2Int> rooms, Vector2Int start)
    {
        Vector2Int farthest = rooms[0];
        float maxDistance = Vector2Int.Distance(start, farthest);

        foreach (var room in rooms)
        {
            float distance = Vector2Int.Distance(start, room);
            if (distance > maxDistance)
            {
                maxDistance = distance;
                farthest = room;
            }
        }

        return farthest;
    }


    private List<Vector2Int> IncreaseCorridorSizeByOnee(List<Vector2Int> corridor)
    {
        List<Vector2Int> newCorridor = new List<Vector2Int>();
        Vector2Int previousDirection = Vector2Int.zero;
        for (int i = 1; i < corridor.Count; i++)
        {
            Vector2Int directionFromCell = corridor[i] - corridor[i - 1];
            if (previousDirection != Vector2Int.zero &&
                directionFromCell != previousDirection)
            {
                // handle corner
                for (int x = -1; x < 2; x++)
                {
                    for (int y = -1; y < 2; y++)
                    {
                        newCorridor.Add(corridor[i - 1] + new Vector2Int(x, y));
                    }
                }
                previousDirection = directionFromCell;

            }
            else
            {
                Vector2Int newCorridorTileOffset
                    = GetDirection90From(directionFromCell);
                newCorridor.Add(corridor[i - 1]);
                newCorridor.Add(corridor[i - 1] + newCorridorTileOffset);
            }
        }
        return newCorridor;
    }

    private Vector2Int GetDirection90From(Vector2Int direction)
    {
        if (direction == Vector2Int.up)
            return Vector2Int.right;
        if (direction == Vector2Int.right)
            return Vector2Int.down;
        if(direction == Vector2Int.down)
            return Vector2Int.left;
        if( direction == Vector2Int.left)
            return Vector2Int.up;
        return Vector2Int.zero;
    }

    private void CreateRoomsAtDeadEnd(List<Vector2Int> deadEnds, HashSet<Vector2Int> roomFloors)
    {
        foreach (var position in deadEnds)
        {
            if(roomFloors.Contains(position) == false)
            {
                var room = RunRandomWalk(randomWalkParameters, position);
                roomFloors.UnionWith(room);
            }

        }
    }

    private List<Vector2Int> FindAllDeadEnds(HashSet<Vector2Int> floorPositions)
    {
        List<Vector2Int> deadEnds = new List<Vector2Int>();
        foreach (var position in floorPositions)
        {
            int neighboursCount = 0;
            foreach (var direction in Direction2D.cardinalDirectionsList)
            {
                if (floorPositions.Contains(position + direction))
                    neighboursCount++;
                
            }
            if(neighboursCount == 1)
                deadEnds.Add(position);
        }

        return deadEnds;
    }

    private HashSet<Vector2Int> CreateRooms(HashSet<Vector2Int> potentialRoomPositions)
    {
        HashSet<Vector2Int> roomPositions = new HashSet<Vector2Int>();
        int roomsToCreateCount = Mathf.RoundToInt(potentialRoomPositions.Count * roomPercent);

        List<Vector2Int> roomsToCreate = potentialRoomPositions.OrderBy(x => Guid.NewGuid()).Take(roomsToCreateCount).ToList();

        foreach (var roomPosition in roomsToCreate)
        {
            var roomFloor = RunRandomWalk(randomWalkParameters, roomPosition);
            SaveRoomData(roomPosition, roomFloor);
            roomPositions.UnionWith(roomFloor);
        }

        return roomPositions;
    }

    private void SaveRoomData(Vector2Int roomPosition, HashSet<Vector2Int> roomFloor)
    {
        roomsDictionary[roomPosition] = roomFloor;
        roomColors.Add(UnityEngine.Random.ColorHSV());


    }

    private List<List<Vector2Int>> CreateCorridors(HashSet<Vector2Int> floorPositions, HashSet<Vector2Int> potentialRoomPositions)
    {
        var currentPos = startPos;
        potentialRoomPositions.Add(currentPos);
        List<List<Vector2Int>> corridors = new List<List<Vector2Int>>();

        for (int i = 0; i < corridorCount; i++)
        {
            var corridor = ProceduralGenerationAlgorithm.RandomWalkCorridor(currentPos, corridorLength);
            corridors.Add(corridor);
            currentPos = corridor[corridor.Count - 1];
            potentialRoomPositions.Add(currentPos);
            floorPositions.UnionWith(corridor);
        }

        corridorPositions = new HashSet<Vector2Int>(floorPositions);

        return corridors;

    }

    private void SpawnPlayer()
    {
        if (playerPrefab == null)
        {
            Debug.LogWarning("Player Prefab not assigned!");
            return;
        }

        // Pierwszy pokój (startowy)
        Vector2Int firstRoomPos = new List<Vector2Int>(roomsDictionary.Keys)[0];
        HashSet<Vector2Int> firstRoom = roomsDictionary[firstRoomPos];

        // Środek pokoju
        Vector2 sum = Vector2.zero;
        foreach (var pos in firstRoom)
            sum += new Vector2(pos.x, pos.y);

        Vector2 center = sum / firstRoom.Count;

        // Spawn gracza
        if (playerInstance == null)
            playerInstance = Instantiate(playerPrefab, new Vector3(center.x, center.y, 0), Quaternion.identity);
        else
            playerInstance.transform.position = new Vector3(center.x, center.y, 0);
    }
}
