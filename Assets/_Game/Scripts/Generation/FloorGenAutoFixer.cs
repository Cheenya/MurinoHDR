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
                    changed |= TryDisableProp(result, error);
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

    private static bool TryDisableProp(FloorResult result, ValidationError error)
    {
        for (var i = 0; i < result.Props.Count; i++)
        {
            if (result.Props[i].PropId != error.PropId)
            {
                continue;
            }

            result.Props[i].BlocksMovement = false;
            return true;
        }

        return false;
    }
}
}

