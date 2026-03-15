using UnityEngine;

namespace MurinoHDR.Generation
{

public static class FloorGenAutoFixer
{
    public static bool ApplyFixes(FloorResult result, ValidationReport report, ValidationConfig config)
    {
        if (result == null || report == null || report.Grid == null)
        {
            return false;
        }

        var changed = false;
        var fixBudget = Mathf.Max(1, config.MaxAutoFixIterations);
        for (var i = 0; i < report.Errors.Count && fixBudget > 0; i++)
        {
            var error = report.Errors[i];
            switch (error.SuggestedFix)
            {
                case SuggestedFix.RelocatePickup:
                    changed |= TryRelocatePickup(result, report, error);
                    break;
                case SuggestedFix.OpenOrWidenDoor:
                case SuggestedFix.ConvertDoorToAlwaysOpen:
                    changed |= TryFixDoor(result, config, error);
                    break;
                case SuggestedFix.RemoveBlockingProp:
                case SuggestedFix.NudgeBlockingProp:
                    changed |= TryFixBlockingProp(result, report, error);
                    break;
            }

            if (changed)
            {
                fixBudget--;
            }
        }

        return changed;
    }

    private static bool TryRelocatePickup(FloorResult result, ValidationReport report, ValidationError error)
    {
        for (var i = 0; i < result.Pickups.Count; i++)
        {
            var pickup = result.Pickups[i];
            if (pickup.RoomId != error.RoomId && error.RoomId >= 0)
            {
                continue;
            }

            var cell = FindNearestReachableCellInRoom(report.Grid, report.Reachable, error.RoomId, pickup.WorldPos);
            if (cell.x < 0)
            {
                cell = FindNearestReachableDoorCorridorCell(result, report.Grid, report.Reachable, error.RoomId);
            }

            if (cell.x < 0)
            {
                continue;
            }

            pickup.WorldPos = report.Grid.CellToWorldCenter(cell) + Vector3.up * 0.2f;
            return true;
        }

        return false;
    }

    private static Vector2Int FindNearestReachableCellInRoom(GridMap2D grid, bool[] reachable, int roomId, Vector3 worldPos)
    {
        var bestCell = new Vector2Int(-1, -1);
        var bestDistance = float.MaxValue;
        for (var y = 0; y < grid.Height; y++)
        {
            for (var x = 0; x < grid.Width; x++)
            {
                var index = grid.Index(x, y);
                if (!reachable[index])
                {
                    continue;
                }

                if (roomId >= 0 && grid.RoomId[index] != roomId)
                {
                    continue;
                }

                var distance = Vector3.SqrMagnitude(grid.CellToWorldCenter(new Vector2Int(x, y)) - worldPos);
                if (distance >= bestDistance)
                {
                    continue;
                }

                bestDistance = distance;
                bestCell = new Vector2Int(x, y);
            }
        }

        return bestCell;
    }

    private static Vector2Int FindNearestReachableDoorCorridorCell(FloorResult result, GridMap2D grid, bool[] reachable, int roomId)
    {
        var bestCell = new Vector2Int(-1, -1);
        var bestDistance = float.MaxValue;
        for (var i = 0; i < result.Doors.Count; i++)
        {
            var door = result.Doors[i];
            if (door.RoomAId != roomId && door.RoomBId != roomId)
            {
                continue;
            }

            var center = grid.WorldToCell(door.WorldPos);
            var candidates = door.Orientation == DoorOrientation.Horizontal
                ? new[] { center + Vector2Int.up, center + Vector2Int.down }
                : new[] { center + Vector2Int.left, center + Vector2Int.right };
            for (var c = 0; c < candidates.Length; c++)
            {
                var cell = candidates[c];
                if (!grid.InBounds(cell) || !reachable[grid.Index(cell.x, cell.y)])
                {
                    continue;
                }

                var distance = Vector2Int.Distance(center, cell);
                if (distance >= bestDistance)
                {
                    continue;
                }

                bestDistance = distance;
                bestCell = cell;
            }
        }

        return bestCell;
    }

    private static bool TryFixDoor(FloorResult result, ValidationConfig config, ValidationError error)
    {
        for (var i = 0; i < result.Doors.Count; i++)
        {
            var door = result.Doors[i];
            if (door.DoorId != error.DoorId)
            {
                continue;
            }

            door.StartsClosed = false;
            if (door.ClearWidthMeters < config.MinDoorClearWidthMeters)
            {
                door.ClearWidthMeters = config.MinDoorClearWidthMeters;
                door.WorldRectXZ = door.Orientation == DoorOrientation.Horizontal
                    ? new Rect(door.WorldPos.x - door.ClearWidthMeters * 0.5f, door.WorldPos.z - 0.14f, door.ClearWidthMeters, 0.28f)
                    : new Rect(door.WorldPos.x - 0.14f, door.WorldPos.z - door.ClearWidthMeters * 0.5f, 0.28f, door.ClearWidthMeters);
            }

            return true;
        }

        return false;
    }

    private static bool TryFixBlockingProp(FloorResult result, ValidationReport report, ValidationError error)
    {
        for (var i = 0; i < result.Props.Count; i++)
        {
            if (result.Props[i].PropId != error.PropId)
            {
                continue;
            }

            if (TryNudgeProp(result, report, result.Props[i], error))
            {
                return true;
            }

            result.Props[i].BlocksMovement = false;
            result.Props[i].SealsDoorZone = false;
            return true;
        }

        return false;
    }

    private static bool TryNudgeProp(FloorResult result, ValidationReport report, PropInstance prop, ValidationError error)
    {
        if (report.Grid == null)
        {
            return false;
        }

        var step = report.Grid.CellSize;
        var seed = StableHash(report.Seed, report.AttemptIndex, prop.PropId);
        var directions = new[]
        {
            Vector3.right,
            Vector3.left,
            Vector3.forward,
            Vector3.back,
        };

        for (var distance = 1; distance <= 2; distance++)
        {
            for (var i = 0; i < directions.Length; i++)
            {
                var direction = directions[(seed + i) % directions.Length];
                var offset = direction * (step * distance);
                var moved = new Bounds(prop.WorldBounds.center + offset, prop.WorldBounds.size);
                if (!FitsInRoom(result, prop.RoomId, moved) || OverlapsBlockingProp(result, prop.PropId, moved))
                {
                    continue;
                }

                prop.WorldBounds = moved;
                return true;
            }
        }

        return false;
    }

    private static bool FitsInRoom(FloorResult result, int roomId, Bounds bounds)
    {
        var room = result.GetRoom(roomId);
        if (room == null)
        {
            return false;
        }

        return room.WorldBounds.Contains(new Vector3(bounds.min.x, room.WorldBounds.center.y, bounds.min.z))
            && room.WorldBounds.Contains(new Vector3(bounds.max.x, room.WorldBounds.center.y, bounds.max.z));
    }

    private static bool OverlapsBlockingProp(FloorResult result, int ignoredPropId, Bounds bounds)
    {
        var rect = Rect.MinMaxRect(bounds.min.x, bounds.min.z, bounds.max.x, bounds.max.z);
        for (var i = 0; i < result.Props.Count; i++)
        {
            var prop = result.Props[i];
            if (!prop.BlocksMovement || prop.PropId == ignoredPropId)
            {
                continue;
            }

            var other = Rect.MinMaxRect(prop.WorldBounds.min.x, prop.WorldBounds.min.z, prop.WorldBounds.max.x, prop.WorldBounds.max.z);
            if (rect.Overlaps(other))
            {
                return true;
            }
        }

        return false;
    }

    private static int StableHash(int a, int b, int c)
    {
        unchecked
        {
            var value = a;
            value = (value * 397) ^ b;
            value = (value * 397) ^ c;
            return Mathf.Abs(value);
        }
    }
}
}

