using System.Collections.Generic;
using UnityEngine;

namespace MurinoHDR.Generation
{

public static class DoorValidator
{
    public static List<ValidationError> ValidateDoors(GridMap2D grid, bool[] walkable, int[] zoneId, IList<DoorInstance> doors)
    {
        var errors = new List<ValidationError>();
        if (grid == null || doors == null)
        {
            return errors;
        }

        for (var i = 0; i < doors.Count; i++)
        {
            var door = doors[i];
            var centerCell = grid.WorldToCell(door.WorldPos);
            var leftCell = centerCell;
            var rightCell = centerCell;
            if (door.Orientation == DoorOrientation.Horizontal)
            {
                leftCell += Vector2Int.down;
                rightCell += Vector2Int.up;
            }
            else
            {
                leftCell += Vector2Int.left;
                rightCell += Vector2Int.right;
            }

            var sideIssues = 0;
            if (!IsWalkable(grid, walkable, leftCell) || !IsWalkable(grid, walkable, rightCell))
            {
                errors.Add(new ValidationError
                {
                    Code = ValidationErrorCode.DoorSideNotWalkable,
                    Severity = ValidationSeverity.Error,
                    Message = string.Format("Door {0} имеет непроходимую сторону", door.DebugName),
                    Cell = centerCell,
                    WorldPos = door.WorldPos,
                    DoorId = door.DoorId,
                    RoomId = door.RoomAId,
                    SuggestedFix = SuggestedFix.OpenOrWidenDoor,
                });
                sideIssues++;
            }

            if (sideIssues > 0)
            {
                continue;
            }

            var zoneA = zoneId[grid.Index(leftCell.x, leftCell.y)];
            var zoneB = zoneId[grid.Index(rightCell.x, rightCell.y)];
            if (zoneA < 0 || zoneB < 0 || zoneA == zoneB)
            {
                errors.Add(new ValidationError
                {
                    Code = ValidationErrorCode.DoorNotConnectingTwoZones,
                    Severity = ValidationSeverity.Error,
                    Message = string.Format("Door {0} не соединяет две разные зоны", door.DebugName),
                    Cell = centerCell,
                    WorldPos = door.WorldPos,
                    DoorId = door.DoorId,
                    ZoneA = zoneA,
                    ZoneB = zoneB,
                    SuggestedFix = SuggestedFix.AddDoorConnection,
                });
            }

            var touchingZones = CollectTouchingZones(grid, zoneId, centerCell);
            if (touchingZones.Count > 2)
            {
                errors.Add(new ValidationError
                {
                    Code = ValidationErrorCode.DoorConnectsMoreThanTwoZones,
                    Severity = ValidationSeverity.Warning,
                    Message = string.Format("Door {0} касается больше чем двух зон", door.DebugName),
                    Cell = centerCell,
                    WorldPos = door.WorldPos,
                    DoorId = door.DoorId,
                    ZoneA = zoneA,
                    ZoneB = zoneB,
                });
            }
        }

        return errors;
    }

    private static bool IsWalkable(GridMap2D grid, bool[] walkable, Vector2Int cell)
    {
        return grid.InBounds(cell) && walkable[grid.Index(cell.x, cell.y)];
    }

    private static HashSet<int> CollectTouchingZones(GridMap2D grid, int[] zoneId, Vector2Int cell)
    {
        var result = new HashSet<int>();
        var neighbors = new[] { Vector2Int.up, Vector2Int.right, Vector2Int.down, Vector2Int.left };
        for (var i = 0; i < neighbors.Length; i++)
        {
            var next = cell + neighbors[i];
            if (!grid.InBounds(next))
            {
                continue;
            }

            var id = zoneId[grid.Index(next.x, next.y)];
            if (id >= 0)
            {
                result.Add(id);
            }
        }

        return result;
    }
}
}

