using System;
using UnityEngine;

namespace MurinoHDR.Generation
{

[Flags]
public enum GridLayerFlags
{
    None = 0,
    BlockedRaw = 1 << 0,
    Floor = 1 << 1,
    Door = 1 << 2,
    Window = 1 << 3,
}

public sealed class GridMap2D
{
    public readonly int Width;
    public readonly int Height;
    public readonly float CellSize;
    public readonly Vector3 OriginWorld;

    public readonly bool[] BlockedRaw;
    public readonly bool[] Floor;
    public readonly bool[] Door;
    public readonly bool[] Window;
    public readonly int[] RoomId;
    public readonly int[] DoorId;
    public readonly int[] WindowId;
    public readonly GridCellTags[] Tags;

    public GridMap2D(int width, int height, float cellSize, Vector3 originWorld)
    {
        Width = Mathf.Max(1, width);
        Height = Mathf.Max(1, height);
        CellSize = Mathf.Max(0.01f, cellSize);
        OriginWorld = originWorld;

        var count = Width * Height;
        BlockedRaw = new bool[count];
        Floor = new bool[count];
        Door = new bool[count];
        Window = new bool[count];
        RoomId = new int[count];
        DoorId = new int[count];
        WindowId = new int[count];
        Tags = new GridCellTags[count];

        for (var i = 0; i < count; i++)
        {
            RoomId[i] = -1;
            DoorId[i] = -1;
            WindowId[i] = -1;
        }
    }

    public int Index(int x, int y)
    {
        return x + y * Width;
    }

    public bool InBounds(int x, int y)
    {
        return x >= 0 && x < Width && y >= 0 && y < Height;
    }

    public bool InBounds(Vector2Int cell)
    {
        return InBounds(cell.x, cell.y);
    }

    public Vector2Int WorldToCell(Vector3 worldPos)
    {
        var x = Mathf.FloorToInt((worldPos.x - OriginWorld.x) / CellSize);
        var y = Mathf.FloorToInt((worldPos.z - OriginWorld.z) / CellSize);
        return new Vector2Int(x, y);
    }

    public Vector3 CellToWorldCenter(Vector2Int cell)
    {
        return new Vector3(OriginWorld.x + (cell.x + 0.5f) * CellSize, 0f, OriginWorld.z + (cell.y + 0.5f) * CellSize);
    }

    public RectInt WorldBoundsToCellRect(Bounds bounds)
    {
        var minX = Mathf.FloorToInt((bounds.min.x - OriginWorld.x) / CellSize);
        var minY = Mathf.FloorToInt((bounds.min.z - OriginWorld.z) / CellSize);
        var maxX = Mathf.CeilToInt((bounds.max.x - OriginWorld.x) / CellSize);
        var maxY = Mathf.CeilToInt((bounds.max.z - OriginWorld.z) / CellSize);
        return ClampRect(new RectInt(minX, minY, Mathf.Max(1, maxX - minX), Mathf.Max(1, maxY - minY)));
    }

    public RectInt WorldRectToCellRect(Rect worldRect)
    {
        var minX = Mathf.FloorToInt((worldRect.xMin - OriginWorld.x) / CellSize);
        var minY = Mathf.FloorToInt((worldRect.yMin - OriginWorld.z) / CellSize);
        var maxX = Mathf.CeilToInt((worldRect.xMax - OriginWorld.x) / CellSize);
        var maxY = Mathf.CeilToInt((worldRect.yMax - OriginWorld.z) / CellSize);
        return ClampRect(new RectInt(minX, minY, Mathf.Max(1, maxX - minX), Mathf.Max(1, maxY - minY)));
    }

    public RectInt ClampRect(RectInt rect)
    {
        var xMin = Mathf.Clamp(rect.xMin, 0, Width);
        var yMin = Mathf.Clamp(rect.yMin, 0, Height);
        var xMax = Mathf.Clamp(rect.xMax, 0, Width);
        var yMax = Mathf.Clamp(rect.yMax, 0, Height);
        return new RectInt(xMin, yMin, Mathf.Max(0, xMax - xMin), Mathf.Max(0, yMax - yMin));
    }

    public void FillRect(RectInt rect, GridLayerFlags flags)
    {
        var clamped = ClampRect(rect);
        for (var x = clamped.xMin; x < clamped.xMax; x++)
        {
            for (var y = clamped.yMin; y < clamped.yMax; y++)
            {
                SetFlags(Index(x, y), flags, true);
            }
        }
    }

    public void ClearRect(RectInt rect, GridLayerFlags flags)
    {
        var clamped = ClampRect(rect);
        for (var x = clamped.xMin; x < clamped.xMax; x++)
        {
            for (var y = clamped.yMin; y < clamped.yMax; y++)
            {
                SetFlags(Index(x, y), flags, false);
            }
        }
    }

    public void StrokeRect(RectInt rect, GridLayerFlags flags, int thickness)
    {
        var clamped = ClampRect(rect);
        for (var i = 0; i < Mathf.Max(1, thickness); i++)
        {
            FillRect(new RectInt(clamped.xMin, clamped.yMin + i, clamped.width, 1), flags);
            FillRect(new RectInt(clamped.xMin, clamped.yMax - 1 - i, clamped.width, 1), flags);
            FillRect(new RectInt(clamped.xMin + i, clamped.yMin, 1, clamped.height), flags);
            FillRect(new RectInt(clamped.xMax - 1 - i, clamped.yMin, 1, clamped.height), flags);
        }
    }

    public void DrawLine(Vector2Int a, Vector2Int b, GridLayerFlags flags, int thickness)
    {
        var x0 = a.x;
        var y0 = a.y;
        var x1 = b.x;
        var y1 = b.y;
        var dx = Mathf.Abs(x1 - x0);
        var dy = Mathf.Abs(y1 - y0);
        var sx = x0 < x1 ? 1 : -1;
        var sy = y0 < y1 ? 1 : -1;
        var err = dx - dy;

        while (true)
        {
            FillRect(new RectInt(x0 - thickness / 2, y0 - thickness / 2, Mathf.Max(1, thickness), Mathf.Max(1, thickness)), flags);
            if (x0 == x1 && y0 == y1)
            {
                break;
            }

            var e2 = err * 2;
            if (e2 > -dy)
            {
                err -= dy;
                x0 += sx;
            }

            if (e2 < dx)
            {
                err += dx;
                y0 += sy;
            }
        }
    }

    public void SetRoomId(RectInt rect, int roomId)
    {
        var clamped = ClampRect(rect);
        for (var x = clamped.xMin; x < clamped.xMax; x++)
        {
            for (var y = clamped.yMin; y < clamped.yMax; y++)
            {
                RoomId[Index(x, y)] = roomId;
            }
        }
    }

    public void AddTags(RectInt rect, GridCellTags tags)
    {
        var clamped = ClampRect(rect);
        for (var x = clamped.xMin; x < clamped.xMax; x++)
        {
            for (var y = clamped.yMin; y < clamped.yMax; y++)
            {
                Tags[Index(x, y)] |= tags;
            }
        }
    }

    public void MarkDoor(RectInt rect, int doorId)
    {
        var clamped = ClampRect(rect);
        for (var x = clamped.xMin; x < clamped.xMax; x++)
        {
            for (var y = clamped.yMin; y < clamped.yMax; y++)
            {
                var index = Index(x, y);
                Door[index] = true;
                DoorId[index] = doorId;
                Tags[index] |= GridCellTags.Door;
            }
        }
    }

    public void MarkWindow(RectInt rect, int windowId)
    {
        var clamped = ClampRect(rect);
        for (var x = clamped.xMin; x < clamped.xMax; x++)
        {
            for (var y = clamped.yMin; y < clamped.yMax; y++)
            {
                var index = Index(x, y);
                Window[index] = true;
                WindowId[index] = windowId;
                Tags[index] |= GridCellTags.Window;
            }
        }
    }

    public bool[] InflateBlocked(int radiusCells)
    {
        if (radiusCells <= 0)
        {
            var copy = new bool[BlockedRaw.Length];
            Array.Copy(BlockedRaw, copy, BlockedRaw.Length);
            return copy;
        }

        var inflated = new bool[BlockedRaw.Length];
        for (var x = 0; x < Width; x++)
        {
            for (var y = 0; y < Height; y++)
            {
                var centerIndex = Index(x, y);
                if (!BlockedRaw[centerIndex])
                {
                    continue;
                }

                for (var dx = -radiusCells; dx <= radiusCells; dx++)
                {
                    for (var dy = -radiusCells; dy <= radiusCells; dy++)
                    {
                        if (dx * dx + dy * dy > radiusCells * radiusCells)
                        {
                            continue;
                        }

                        var nx = x + dx;
                        var ny = y + dy;
                        if (!InBounds(nx, ny))
                        {
                            continue;
                        }

                        inflated[Index(nx, ny)] = true;
                    }
                }
            }
        }

        return inflated;
    }

    private void SetFlags(int index, GridLayerFlags flags, bool value)
    {
        if ((flags & GridLayerFlags.BlockedRaw) != 0) BlockedRaw[index] = value;
        if ((flags & GridLayerFlags.Floor) != 0) Floor[index] = value;
        if ((flags & GridLayerFlags.Door) != 0) Door[index] = value;
        if ((flags & GridLayerFlags.Window) != 0) Window[index] = value;
    }
}
}

