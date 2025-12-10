using SVS.AI;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Random = UnityEngine.Random;

namespace SVS.ChessMaze
{
    public class CandidateMap
    {
        private MapGrid grid;
        private int numberOfPieces = 0;
        private bool[] obstaclesArray = null;
        private Vector3 startPoint, exitPoint;
        private List<KnightPiece> knightPiecesList;
        private List<Vector3> path = new List<Vector3>();
        List<Vector3> altPath = new List<Vector3>();


        public MapGrid Grid { get => grid; }
        public bool[] ObstaclesArray { get => obstaclesArray; }

        public CandidateMap(MapGrid grid, int numberOfPieces)
        {
            this.numberOfPieces = numberOfPieces;

            this.grid = grid;
        }

        public void CreateMap(Vector3 startPosition, Vector3 exitPosition, bool autoRepair = false)
        {
            this.startPoint = startPosition;
            this.exitPoint = exitPosition;

            int attemptsLeft = 50;         // zabezpieczenie przed nieskoñczon¹ pêtl¹
            bool success = false;

            while (!success && attemptsLeft-- > 0)
            {
                obstaclesArray = new bool[grid.Width * grid.Length];
                knightPiecesList = new List<KnightPiece>();
                path = new List<Vector3>();
                altPath = new List<Vector3>();

                RandomlyPlaceKnightPieces(this.numberOfPieces);
                PlaceObstacles();
                FindPath();

                if (autoRepair && (path == null || path.Count == 0))
                {
                    Repair();
                }

                if (path == null || path.Count == 0)
                    continue;

                FindAltPath();

                success = altPath != null && altPath.Count > 0;
            }

            if (altPath == null || altPath.Count == 0)
            {
                ForceAltPathByRemovingObstacles();
            }
        }


        private void FindPath()
        {
            this.path = Astar.GetPath(startPoint, exitPoint, obstaclesArray, grid);

            altPath = new List<Vector3>();

            if (this.path != null && this.path.Count > 0)
            {
                var altObstacles = (bool[])obstaclesArray.Clone();

                foreach (var p in this.path)
                {
                    if (p == startPoint || p == exitPoint)
                        continue;

                    int idx = grid.CalculateIndexFromCoordinates(p.x, p.z);
                    altObstacles[idx] = true;
                }

                var alt = Astar.GetPath(startPoint, exitPoint, altObstacles, grid);

                if (alt != null && alt.Count > 0)
                {
                    altPath = alt;
                }
            }
        }

        private void FindAltPath()
        {
            altPath = new List<Vector3>();

            if (path == null || path.Count == 0)
                return;

            var altObstacles = (bool[])obstaclesArray.Clone();

            foreach (var p in path)
            {
                if (p == startPoint || p == exitPoint)
                    continue;

                int idx = grid.CalculateIndexFromCoordinates(p.x, p.z);
                altObstacles[idx] = true;
            }

            var candidate = Astar.GetPath(startPoint, exitPoint, altObstacles, grid);

            if (candidate != null && candidate.Count > 0)
            {
                altPath = candidate;
            }
        }

        private void ForceAltPathByRemovingObstacles()
        {
            if (path == null || path.Count == 0)
                return;


            var altObstacles = (bool[])obstaclesArray.Clone();
            System.Random rnd = new System.Random();

            int safety = altObstacles.Length;

            while ((altPath == null || altPath.Count == 0) && safety-- > 0)
            {
                int idx = rnd.Next(0, altObstacles.Length);

                var coord = grid.CalculateCoordinatesFromIndex(idx);
                if (coord == startPoint || coord == exitPoint)
                    continue;

                if (path.Contains(coord))
                    continue;

                altObstacles[idx] = false;

                var candidate = Astar.GetPath(startPoint, exitPoint, altObstacles, grid);
                if (candidate != null && candidate.Count > 0)
                {
                    altPath = candidate;
                    break;
                }
            }

            if (altPath != null && altPath.Count > 0)
            {
                obstaclesArray = altObstacles;
            }
        }


        private bool CheckIfPositionCanBeObstacle(Vector3 position)
        {
            if (position == startPoint || position == exitPoint)
            {
                return false;
            }
            int index = grid.CalculateIndexFromCoordinates(position.x, position.z);

            return obstaclesArray[index] == false;
        }

        private void RandomlyPlaceKnightPieces(int numbeOfPieces)
        {
            var count = numberOfPieces;
            var knighPlacementTryLimit = 100;
            while (count > 0 && knighPlacementTryLimit > 0)
            {
                var randomIndex = Random.Range(0, obstaclesArray.Length);
                if (obstaclesArray[randomIndex] == false)
                {
                    var coordinates = grid.CalculateCoordinatesFromIndex(randomIndex);
                    if (coordinates == startPoint || coordinates == exitPoint)
                    {
                        continue;
                    }
                    obstaclesArray[randomIndex] = true;
                    knightPiecesList.Add(new KnightPiece(coordinates));
                    count--;

                }
                knighPlacementTryLimit--;
            }
        }

        private void PlaceObstaclesForThisKnight(KnightPiece knight)
        {
            foreach (var position in KnightPiece.listOfPossibleMoves)
            {
                var newPosition = knight.Position + position;
                if (grid.IsCellValid(newPosition.x, newPosition.z) && CheckIfPositionCanBeObstacle(newPosition))
                {
                    obstaclesArray[grid.CalculateIndexFromCoordinates(newPosition.x, newPosition.z)] = true;
                }
            }
        }

        private void PlaceObstacles()
        {
            foreach (var knight in knightPiecesList)
            {
                PlaceObstaclesForThisKnight(knight);
            }
        }

        public MapData ReturnMapData()
        {
            return new MapData
            {
                obstacleArray = this.obstaclesArray,
                knightPiecesList = knightPiecesList,
                startPosition = startPoint,
                exitPosition = exitPoint,
                path = this.path,
                altPath = this.altPath
            };
        }

        public List<Vector3> Repair()
        {
            int numberOfObstacles = obstaclesArray.Where(obstacle => obstacle).Count();
            List<Vector3> listOfObstaclesToRemove = new List<Vector3>();
            if (path.Count <= 0)
            {
                do
                {
                    int obstacleIndexToRemove = Random.Range(0, numberOfObstacles);
                    for (int i = 0; i < obstaclesArray.Length; i++)
                    {
                        if (obstaclesArray[i])
                        {
                            if (obstacleIndexToRemove == 0)
                            {
                                obstaclesArray[i] = false;
                                listOfObstaclesToRemove.Add(grid.CalculateCoordinatesFromIndex(i));
                                break;
                            }
                            obstacleIndexToRemove--;
                        }
                    }

                    FindPath();
                } while (this.path.Count <= 0);
            }
            foreach (var obstaclePosition in listOfObstaclesToRemove)
            {
                if (path.Contains(obstaclePosition) == false)
                {
                    int index = grid.CalculateIndexFromCoordinates(obstaclePosition.x, obstaclePosition.z);
                    obstaclesArray[index] = true;
                }
            }

            return listOfObstaclesToRemove;
        }
    }
}