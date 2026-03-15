using System.Collections.Generic;
using UnityEngine;

namespace MurinoHDR.Generation
{

public static class FloodFill
{
    private static readonly Vector2Int[] Neighbors =
    {
        Vector2Int.up,
        Vector2Int.right,
        Vector2Int.down,
        Vector2Int.left,
    };

    public static bool[] FloodFillFrom(GridMap2D grid, Vector2Int startCell, bool[] walkable)
    {
        var reachable = new bool[walkable.Length];
        if (grid == null || !grid.InBounds(startCell) || !walkable[grid.Index(startCell.x, startCell.y)])
        {
            return reachable;
        }

        var queue = new Queue<Vector2Int>();
        queue.Enqueue(startCell);
        reachable[grid.Index(startCell.x, startCell.y)] = true;

        while (queue.Count > 0)
        {
            var cell = queue.Dequeue();
            for (var i = 0; i < Neighbors.Length; i++)
            {
                var next = cell + Neighbors[i];
                if (!grid.InBounds(next))
                {
                    continue;
                }

                var nextIndex = grid.Index(next.x, next.y);
                if (!walkable[nextIndex] || reachable[nextIndex])
                {
                    continue;
                }

                reachable[nextIndex] = true;
                queue.Enqueue(next);
            }
        }

        return reachable;
    }

    public static int[] BuildDistanceMap(GridMap2D grid, Vector2Int startCell, bool[] walkable)
    {
        var distance = new int[walkable.Length];
        for (var i = 0; i < distance.Length; i++)
        {
            distance[i] = -1;
        }

        if (grid == null || !grid.InBounds(startCell) || !walkable[grid.Index(startCell.x, startCell.y)])
        {
            return distance;
        }

        var queue = new Queue<Vector2Int>();
        queue.Enqueue(startCell);
        distance[grid.Index(startCell.x, startCell.y)] = 0;

        while (queue.Count > 0)
        {
            var cell = queue.Dequeue();
            var baseIndex = grid.Index(cell.x, cell.y);
            for (var i = 0; i < Neighbors.Length; i++)
            {
                var next = cell + Neighbors[i];
                if (!grid.InBounds(next))
                {
                    continue;
                }

                var nextIndex = grid.Index(next.x, next.y);
                if (!walkable[nextIndex] || distance[nextIndex] >= 0)
                {
                    continue;
                }

                distance[nextIndex] = distance[baseIndex] + 1;
                queue.Enqueue(next);
            }
        }

        return distance;
    }

    public static int[] LabelConnectedZones(GridMap2D grid, bool[] walkable, out int zoneCount)
    {
        zoneCount = 0;
        var zones = new int[walkable.Length];
        for (var i = 0; i < zones.Length; i++)
        {
            zones[i] = -1;
        }

        var queue = new Queue<Vector2Int>();
        for (var y = 0; y < grid.Height; y++)
        {
            for (var x = 0; x < grid.Width; x++)
            {
                var index = grid.Index(x, y);
                if (!walkable[index] || zones[index] >= 0)
                {
                    continue;
                }

                zones[index] = zoneCount;
                queue.Enqueue(new Vector2Int(x, y));
                while (queue.Count > 0)
                {
                    var cell = queue.Dequeue();
                    for (var i = 0; i < Neighbors.Length; i++)
                    {
                        var next = cell + Neighbors[i];
                        if (!grid.InBounds(next))
                        {
                            continue;
                        }

                        var nextIndex = grid.Index(next.x, next.y);
                        if (!walkable[nextIndex] || zones[nextIndex] >= 0)
                        {
                            continue;
                        }

                        zones[nextIndex] = zoneCount;
                        queue.Enqueue(next);
                    }
                }

                zoneCount++;
            }
        }

        return zones;
    }
}
}

