using System;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

namespace SVS.ChessMaze
{
    [System.Serializable]
    public class TileSet
    {
        public GameObject roadStraight;
        public GameObject roadTileCorner;
        public GameObject tileEmpty;
        public GameObject startTile;
        public GameObject exitTile;

        public GameObject[] environmentTiles;
    }

    public class MapVisualizer : MonoBehaviour
    {
        private Transform parent;
        public Color startColor, exitColor;

        public Vector3 mapOrigin = Vector3.zero;
        public float cellSize = 1f;

        public GameObject playerPrefab;
        public GameObject[] towerPrefabs;
        public float towerYOffset = 0.0f;

        [Header("Tile Sets")]
        public TileSet[] tileSets;

        [Header("Debug / Testing")]
        [SerializeField] private bool debugForceTileSet = false;
        [SerializeField] private int debugTileSetIndex = 0;

        private TileSet currentTileSet;

        Dictionary<Vector3, GameObject> dictionaryOfObstacles = new Dictionary<Vector3, GameObject>();
        private readonly List<GameObject> extraObjects = new List<GameObject>();
        private PlayerMover playerMover;

        public bool animate;

        private void Awake()
        {
            parent = this.transform;

            if (tileSets != null && tileSets.Length > 0)
            {
                int index;

                if (debugForceTileSet)
                {
                    index = Mathf.Clamp(debugTileSetIndex, 0, tileSets.Length - 1);
                }
                else
                {
                    index = Random.Range(0, tileSets.Length); // [0, length)
                }

                currentTileSet = tileSets[index];
                Debug.Log($"MapVisualizer: wybrany TileSet index = {index}");
            }
            else
            {
                Debug.LogWarning("MapVisualizer: Brak skonfigurowanych tileSets.");
            }
        }

        public void VisualizeMap(MapGrid grid, MapData data, bool visualizeUsingPrefabs)
        {
            if (visualizeUsingPrefabs)
            {
                VisualizeUsingPrefabs(grid, data);
            }
            else
            {
                VisualizeUsingPrimitives(grid, data);
            }
        }

        private void VisualizeUsingPrefabs(MapGrid grid, MapData data)
        {
            if (data.path != null)
            {
                foreach (var pos in data.path)
                {
                    if (pos != data.exitPosition)
                    {
                        grid.SetCell((int)pos.x, (int)pos.z, CellObjectType.Road);
                    }
                }
            }

            if (data.altPath != null)
            {
                foreach (var pos in data.altPath)
                {
                    if (pos != data.exitPosition)
                    {
                        grid.SetCell((int)pos.x, (int)pos.z, CellObjectType.Road);
                    }
                }
            }
            for (int col = 0; col < grid.Width; col++)
            {
                for (int row = 0; row < grid.Length; row++)
                {
                    var cell = grid.GetCell(col, row);
                    var position = new Vector3(cell.X, 0, cell.Z);

                    var index = grid.CalculateIndexFromCoordinates(position.x, position.z);
                    if (data.obstacleArray[index] && cell.IsTaken == false)
                    {
                        cell.ObjectType = CellObjectType.Obstacle;
                    }
                    Direction previousDirection = Direction.None;
                    Direction nextDirection = Direction.None;
                    switch (cell.ObjectType)
                    {
                        case CellObjectType.Empty:
                            if (currentTileSet != null && currentTileSet.tileEmpty != null)
                            {
                                CreateIndicator(position, currentTileSet.tileEmpty);
                            }
                            break;
                        case CellObjectType.Road:
                            if (data.path.Count > 0)
                            {
                                previousDirection = GetDirectionOfPreviousCell(position, data);
                                nextDirection = GetDicrectionOfNextCell(position, data);
                            }

                            if (currentTileSet == null)
                                break;

                            var roadStraight = currentTileSet.roadStraight;
                            var roadTileCorner = currentTileSet.roadTileCorner;

                            if (previousDirection == Direction.Up && nextDirection == Direction.Right || previousDirection == Direction.Right && nextDirection == Direction.Up)
                            {
                                CreateIndicator(position, roadTileCorner, Quaternion.Euler(0, 90, 0));
                            }
                            else if (previousDirection == Direction.Right && nextDirection == Direction.Down || previousDirection == Direction.Down && nextDirection == Direction.Right)
                            {
                                CreateIndicator(position, roadTileCorner, Quaternion.Euler(0, 180, 0));
                            }
                            else if (previousDirection == Direction.Down && nextDirection == Direction.Left || previousDirection == Direction.Left && nextDirection == Direction.Down)
                            {
                                CreateIndicator(position, roadTileCorner, Quaternion.Euler(0, -90, 0));
                            }
                            else if (previousDirection == Direction.Left && nextDirection == Direction.Up || previousDirection == Direction.Up && nextDirection == Direction.Left)
                            {
                                CreateIndicator(position, roadTileCorner);
                            }
                            else if (previousDirection == Direction.Right && nextDirection == Direction.Left || previousDirection == Direction.Left && nextDirection == Direction.Right)
                            {
                                CreateIndicator(position, roadStraight, Quaternion.Euler(0, 90, 0));
                            }
                            else
                            {
                                CreateIndicator(position, roadStraight);
                            }

                            break;
                        case CellObjectType.Obstacle:
                            if (currentTileSet != null && currentTileSet.environmentTiles != null && currentTileSet.environmentTiles.Length > 0)
                            {
                                int randomIndex = Random.Range(0, currentTileSet.environmentTiles.Length);
                                CreateIndicator(position, currentTileSet.environmentTiles[randomIndex]);
                            }
                            break;
                        case CellObjectType.Start:
                            if (currentTileSet == null || currentTileSet.startTile == null)
                                break;

                            if (data.path.Count > 0)
                            {
                                nextDirection = GetDirectionFromVectors(data.path[0], position);
                            }
                            if (nextDirection == Direction.Right || nextDirection == Direction.Left)
                            {
                                CreateIndicator(position, currentTileSet.startTile, Quaternion.Euler(0, 90, 0));
                            }
                            else
                            {
                                CreateIndicator(position, currentTileSet.startTile);
                            }
                            break;
                        case CellObjectType.Exit:
                            if (currentTileSet == null || currentTileSet.exitTile == null)
                                break;
                            if (data.path.Count > 0)
                            {
                                previousDirection = GetDirectionOfPreviousCell(position, data);
                                switch (previousDirection)
                                {
                                    case Direction.Right:
                                        CreateIndicator(position, currentTileSet.exitTile, Quaternion.Euler(0, 90, 0));
                                        break;
                                    case Direction.Left:
                                        CreateIndicator(position, currentTileSet.exitTile, Quaternion.Euler(0, -90, 0));
                                        break;
                                    case Direction.Down:
                                        CreateIndicator(position, currentTileSet.exitTile, Quaternion.Euler(0, 180, 0));
                                        break;
                                    default:
                                        CreateIndicator(position, currentTileSet.exitTile);
                                        break;
                                }
                            }
                            break;
                        default:
                            break;
                    }
                }
            }
            SpawnPlayer(data);
            PlaceTowers(data);
            SetupExitClick(data);
        }

        private void SpawnPlayer(MapData data)
        {
            if (playerPrefab == null)
                return;

            Vector3 startGridPos = data.startPosition;
            Vector3 startWorldPos = GridToWorld(startGridPos);

            var playerObj = Instantiate(playerPrefab, startWorldPos, Quaternion.identity, parent);
            playerObj.transform.localScale *= cellSize;

            if (!playerObj.GetComponent<Collider>())
            {
                playerObj.AddComponent<CapsuleCollider>();
            }

            playerMover = playerObj.GetComponent<PlayerMover>();
            if (playerMover == null)
            {
                playerMover = playerObj.AddComponent<PlayerMover>();
            }

            extraObjects.Add(playerObj);

            if (animate)
            {
                playerObj.AddComponent<DropTween>();
                DropTween.IncreaseDropTime();
            }
        }


        private Direction GetDicrectionOfNextCell(Vector3 position, MapData data)
        {
            if (data.path != null)
            {
                int index = data.path.FindIndex(a => a == position);
                if (index >= 0 && index < data.path.Count - 1)
                {
                    var nextCellPosition = data.path[index + 1];
                    return GetDirectionFromVectors(nextCellPosition, position);
                }
            }

            if (data.altPath != null)
            {
                int index = data.altPath.FindIndex(a => a == position);
                if (index >= 0 && index < data.altPath.Count - 1)
                {
                    var nextCellPosition = data.altPath[index + 1];
                    return GetDirectionFromVectors(nextCellPosition, position);
                }
            }

            return Direction.None;
        }

        private void PlaceTowers(MapData data)
        {
            if (towerPrefabs == null || towerPrefabs.Length == 0 || playerMover == null)
                return;

            int firstIdx = 0;
            int secondIdx = 0;
            if (towerPrefabs.Length == 1)
            {
                firstIdx = secondIdx = 0;
            }
            else
            {
                firstIdx = Random.Range(0, towerPrefabs.Length);
                do
                {
                    secondIdx = Random.Range(0, towerPrefabs.Length);
                } while (secondIdx == firstIdx);
            }

            // Wie¿a 1 - g³ówna œcie¿ka
            if (data.path != null && data.path.Count > 2)
            {
                int mid = data.path.Count / 2;
                var towerGridPos = data.path[mid];

                var toTower = BuildWorldPathSegment(data.path, 0, mid);
                var fromTower = BuildWorldPathSegment(data.path, mid, data.path.Count - 1);

                SpawnTower(towerGridPos, towerPrefabs[firstIdx], "Wie¿a 1", toTower, fromTower);
            }

            // Wie¿a 2 - alternatywna œcie¿ka
            if (data.altPath != null && data.altPath.Count > 2)
            {
                int mid = data.altPath.Count / 2;
                var towerGridPos = data.altPath[mid];

                // jeœli œrodek altPath wchodzi na g³ówn¹ œcie¿kê, próbujemy znaleŸæ inny punkt
                if (data.path != null && data.path.Contains(towerGridPos))
                {
                    for (int i = mid; i < data.altPath.Count; i++)
                    {
                        if (!data.path.Contains(data.altPath[i]))
                        {
                            towerGridPos = data.altPath[i];
                            mid = i;
                            break;
                        }
                    }
                }

                var toTower = BuildWorldPathSegment(data.altPath, 0, mid);
                var fromTower = BuildWorldPathSegment(data.altPath, mid, data.altPath.Count - 1);

                SpawnTower(towerGridPos, towerPrefabs[secondIdx], "Wie¿a 2", toTower, fromTower);
            }
        }

        private void SpawnTower(Vector3 gridPos,
                        GameObject prefab,
                        string label,
                        List<Vector3> toTower,
                        List<Vector3> fromTowerToExit)
        {
            if (prefab == null)
                return;

            var worldPos = GridToWorld(gridPos);
            worldPos.y += towerYOffset;

            var tower = Instantiate(prefab, worldPos, Quaternion.identity, parent);
            tower.transform.localScale *= cellSize;

            if (!tower.GetComponent<Collider>())
            {
                tower.AddComponent<BoxCollider>();
            }

            extraObjects.Add(tower);

            var click = tower.GetComponent<TowerClick>();
            if (click == null)
            {
                click = tower.AddComponent<TowerClick>();
            }

            click.label = label;
            click.SetupPlayerPaths(playerMover, toTower, fromTowerToExit);

            if (animate)
            {
                tower.AddComponent<DropTween>();
                DropTween.IncreaseDropTime();
            }
        }

        private Direction GetDirectionOfPreviousCell(Vector3 position, MapData data)
        {
            if (data.path != null)
            {
                int index = data.path.FindIndex(a => a == position);
                if (index >= 0)
                {
                    var previousCellPosition = index > 0 ? data.path[index - 1] : data.startPosition;
                    return GetDirectionFromVectors(previousCellPosition, position);
                }
            }

            if (data.altPath != null)
            {
                int index = data.altPath.FindIndex(a => a == position);
                if (index >= 0)
                {
                    var previousCellPosition = index > 0 ? data.altPath[index - 1] : data.startPosition;
                    return GetDirectionFromVectors(previousCellPosition, position);
                }
            }

            return Direction.None;
        }

        private Direction GetDirectionFromVectors(Vector3 positionToGoTo, Vector3 position)
        {
            if (positionToGoTo.x > position.x)
            {
                return Direction.Right;
            }
            else if (positionToGoTo.x < position.x)
            {
                return Direction.Left;
            }
            else if (positionToGoTo.z < position.z)
            {
                return Direction.Down;
            }
            return Direction.Up;
        }

        private List<Vector3> BuildWorldPathUntil(List<Vector3> gridPath, Vector3 targetGrid)
        {
            var list = new List<Vector3>();

            foreach (var p in gridPath)
            {
                list.Add(GridToWorld(p));
                if (p == targetGrid)
                    break;
            }

            return list;
        }


        private void CreateIndicator(Vector3 position, GameObject prefab, Quaternion rotation = new Quaternion())
        {
            var worldPos = GridToWorld(position);
            var element = Instantiate(prefab, worldPos, rotation, parent);
            element.transform.localScale *= cellSize;
            dictionaryOfObstacles.Add(position, element);

            if (animate)
            {
                element.AddComponent<DropTween>();
                DropTween.IncreaseDropTime();
            }
        }

        private void VisualizeUsingPrimitives(MapGrid grid, MapData data)
        {
            PlaceStartAndExitPoints(data);
            for (int i = 0; i < data.obstacleArray.Length; i++)
            {
                if (data.obstacleArray[i])
                {
                    var positionOnGrid = grid.CalculateCoordinatesFromIndex(i);
                    if (positionOnGrid == data.startPosition || positionOnGrid == data.exitPosition)
                    {
                        continue;
                    }
                    grid.SetCell(positionOnGrid.x, positionOnGrid.z, CellObjectType.Obstacle);
                    if (PlaceKnightObstacle(data, positionOnGrid))
                    {
                        continue;
                    }
                    if (dictionaryOfObstacles.ContainsKey(positionOnGrid) == false)
                    {
                        CreateIndicator(positionOnGrid, Color.white, PrimitiveType.Cube);
                    }

                }
            }
        }

        private bool PlaceKnightObstacle(MapData data, Vector3 positionOnGrid)
        {
            foreach (var knight in data.knightPiecesList)
            {
                if (knight.Position == positionOnGrid)
                {
                    CreateIndicator(positionOnGrid, Color.red, PrimitiveType.Cube);
                    return true;
                }
            }
            return false;
        }

        private void PlaceStartAndExitPoints(MapData data)
        {
            CreateIndicator(data.startPosition, startColor, PrimitiveType.Sphere);
            CreateIndicator(data.exitPosition, exitColor, PrimitiveType.Sphere);
        }

        private void CreateIndicator(Vector3 position, Color color, PrimitiveType primitiveType)
        {
            var element = GameObject.CreatePrimitive(primitiveType);
            var worldPos = GridToWorld(position);

            element.transform.position = worldPos;
            element.transform.parent = parent;
            element.transform.localScale = Vector3.one * cellSize;

            dictionaryOfObstacles.Add(position, element);

            var renderer = element.GetComponent<Renderer>();
            renderer.material.SetColor("_Color", color);

            if (animate)
            {
                element.AddComponent<DropTween>();
                DropTween.IncreaseDropTime();
            }
        }
        private void SetupExitClick(MapData data)
        {
            if (dictionaryOfObstacles.TryGetValue(data.exitPosition, out var exitObj))
            {
                if (!exitObj.GetComponent<Collider>())
                {
                    exitObj.AddComponent<BoxCollider>();
                }

                if (!exitObj.GetComponent<ExitClick>())
                {
                    exitObj.AddComponent<ExitClick>();
                }
            }
        }

        private Vector3 GridToWorld(Vector3 gridPos)
        {
            return new Vector3(
                mapOrigin.x + (gridPos.x + 0.5f) * cellSize,
                mapOrigin.y + 0.5f * cellSize,
                mapOrigin.z + (gridPos.z + 0.5f) * cellSize
            );
        }

        private List<Vector3> BuildWorldPathSegment(List<Vector3> gridPath, int startIndex, int endIndexInclusive)
        {
            var list = new List<Vector3>();
            if (gridPath == null || gridPath.Count == 0)
                return list;

            startIndex = Mathf.Clamp(startIndex, 0, gridPath.Count - 1);
            endIndexInclusive = Mathf.Clamp(endIndexInclusive, 0, gridPath.Count - 1);
            if (endIndexInclusive < startIndex)
                return list;

            for (int i = startIndex; i <= endIndexInclusive; i++)
            {
                list.Add(GridToWorld(gridPath[i]));
            }

            return list;
        }

        public void ClearMap()
        {
            foreach (var obstacle in dictionaryOfObstacles.Values)
            {
                if (obstacle)
                    Destroy(obstacle);
            }
            dictionaryOfObstacles.Clear();

            foreach (var obj in extraObjects)
            {
                if (obj)
                    Destroy(obj);
            }
            extraObjects.Clear();

            playerMover = null;

            // reset logiki wie¿ i exita
            TowerClick.chosenTower = null;

            if (animate)
                DropTween.ResetTime();
        }
    }
}