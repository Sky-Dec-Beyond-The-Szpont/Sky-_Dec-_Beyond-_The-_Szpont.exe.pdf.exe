using NUnit.Framework;
using System;
using System.Collections.Generic;
using UnityEngine;

public class Graph
{
    private static List<Vector2Int> neighbours4directions = new List<Vector2Int>
    {
        new Vector2Int(0,1), // Up
        new Vector2Int(1,0), // Right
        new Vector2Int(0,-1), // Down
        new Vector2Int(-1,0), //Left
    };

    private static List<Vector2Int> neighbours8directions = new List<Vector2Int>
    {
        new Vector2Int(0,1), // Up
        new Vector2Int(1,0), // Right
        new Vector2Int(0,-1), // Down
        new Vector2Int(-1,0), // Left
        new Vector2Int(1,1), // Top-right
        new Vector2Int(1,-1), // Down-right
        new Vector2Int(-1,1), // Top-left
        new Vector2Int(-1,-1) // Down-left
    };

    List<Vector2Int> graph;

    public Graph(IEnumerable<Vector2Int> vertices)
    {
        graph = new List<Vector2Int>(vertices);
    }

    public List<Vector2Int> GetNeighbours4Directions(Vector2Int startPos)
    {
        return GetNeighbours(startPos, neighbours4directions);
    }

    public List<Vector2Int> GetNeighbours8Directions(Vector2Int startPos)
    {
        return GetNeighbours(startPos, neighbours8directions);
    }

    private List<Vector2Int> GetNeighbours(Vector2Int startPos, List<Vector2Int> neighboursOffsetList)
    {
        List<Vector2Int> neighbours = new List<Vector2Int>();
        foreach(var neighbourDirection in neighboursOffsetList)
        {
            Vector2Int potentialNeighbour = startPos + neighbourDirection;
            if(graph.Contains(potentialNeighbour))
                neighbours.Add(potentialNeighbour);
        }
        return neighbours;
    }
}
