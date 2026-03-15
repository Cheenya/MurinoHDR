using System.Collections.Generic;
using UnityEngine;

namespace MurinoHDR.Generation
{

public static class DoorValidator
{
    public static List<ValidationError> ValidateDoors(GridMap2D grid, bool[] walkable, int[] zoneId, IList<DoorInstance> doors, IList<PropInstance> props)
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
                var blockingPropId = FindBlockingPropId(door, props);
                errors.Add(new ValidationError
                {
                    Code = blockingPropId >= 0 ? ValidationErrorCode.DoorSealedByObstacle : ValidationErrorCode.DoorSideNotWalkable,
                    Severity = ValidationSeverity.Error,
                    Message = blockingPropId >= 0
                        ? string.Format("Door {0} запечатана пропом рядом с карманом двери", door.DebugName)
                        : string.Format("Door {0} имеет непроходимую сторону", door.DebugName),
                    Cell = centerCell,
                    WorldPos = door.WorldPos,
                    DoorId = door.DoorId,
                    RoomId = door.RoomAId,
                    PropId = blockingPropId,
                    SuggestedFix = blockingPropId >= 0 ? SuggestedFix.NudgeBlockingProp : SuggestedFix.OpenOrWidenDoor,
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

    private static int FindBlockingPropId(DoorInstance door, IList<PropInstance> props)
    {
        if (props == null)
        {
            return -1;
        }

        var expanded = door.Orientation == DoorOrientation.Horizontal
            ? Rect.MinMaxRect(door.WorldRectXZ.xMin - 0.35f, door.WorldRectXZ.yMin - 1.2f, door.WorldRectXZ.xMax + 0.35f, door.WorldRectXZ.yMax + 1.2f)
            : Rect.MinMaxRect(door.WorldRectXZ.xMin - 1.2f, door.WorldRectXZ.yMin - 0.35f, door.WorldRectXZ.xMax + 1.2f, door.WorldRectXZ.yMax + 0.35f);

        for (var i = 0; i < props.Count; i++)
        {
            if (!props[i].BlocksMovement)
            {
                continue;
            }

            var propRect = Rect.MinMaxRect(props[i].WorldBounds.min.x, props[i].WorldBounds.min.z, props[i].WorldBounds.max.x, props[i].WorldBounds.max.z);
            if (expanded.Overlaps(propRect))
            {
                return props[i].PropId;
            }
        }

        return -1;
    }
}
}

