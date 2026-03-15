using System.Collections.Generic;
using UnityEngine;

namespace MurinoHDR.Generation
{

public static class FloorGenValidator
{
    private const float FacadeRatioMin = 0.3f;
    private const float FacadeRatioMax = 0.6f;

    public static ValidationReport Validate(FloorResult result, ValidationConfig config)
    {
        var report = new ValidationReport();
        if (result == null || config == null)
        {
            report.Add(
                ValidationErrorCode.InvalidFootprintOrGrid,
                ValidationSeverity.Fatal,
                "FloorResult или ValidationConfig отсутствует",
                new Vector2Int(-1, -1),
                Vector3.zero,
                -1,
                -1,
                -1,
                SuggestedFix.RegenerateFloor);
            report.Success = false;
            return report;
        }

        report.Seed = result.Seed;
        report.AttemptIndex = result.AttemptIndex;
        report.CellSize = config.CellSize;

        var grid = GridFromFloor(result, config, report);
        report.Grid = grid;
        report.GridWidth = grid.Width;
        report.GridHeight = grid.Height;
        if (report.FatalCount > 0)
        {
            report.Success = false;
            return report;
        }

        var walkable = WalkMaskBuilder.BuildWalkable(grid, config.InflateRadiusCells);
        var spawnCell = grid.WorldToCell(result.SpawnWorld);
        var reachable = FloodFill.FloodFillFrom(grid, spawnCell, walkable);
        var distances = FloodFill.BuildDistanceMap(grid, spawnCell, walkable);

        var walkableWithoutDoors = new bool[walkable.Length];
        for (var i = 0; i < walkable.Length; i++)
        {
            walkableWithoutDoors[i] = walkable[i] && !grid.Door[i];
        }

        int zoneCount;
        var zoneId = FloodFill.LabelConnectedZones(grid, walkableWithoutDoors, out zoneCount);

        report.Walkable = walkable;
        report.Reachable = reachable;
        report.DistanceFromSpawn = distances;
        report.ZoneId = zoneId;

        Append(report, ReachabilityValidator.ValidateReachability(grid, reachable, distances, result, config));
        Append(report, DoorValidator.ValidateDoors(grid, walkable, zoneId, result.Doors, result.Props));
        Append(report, CorridorWidthValidator.ValidateMinWidth(grid, walkable, config.MainCorridorMinWidthCells, config.SecondaryCorridorMinWidthCells, config.CorridorSampleStride, config.AllowSingleCellTechChokepoints));

        ValidateDoorWidths(result, config, report);
        ValidateRoomsReachCorridor(grid, reachable, result, report);
        ValidateWindows(result, report);
        ValidateFacadeRatio(result, report);
        ValidateAlternativeLoop(grid, reachable, report);

        report.Success = report.FatalCount == 0 && report.ErrorCount == 0;
        return report;
    }

    public static GridMap2D GridFromFloor(FloorResult result, ValidationConfig config, ValidationReport report)
    {
        var footprint = result.FootprintWorld;
        if (footprint.size.x <= 0.01f || footprint.size.z <= 0.01f)
        {
            report.Add(
                ValidationErrorCode.InvalidFootprintOrGrid,
                ValidationSeverity.Fatal,
                "Footprint world bounds пустой",
                new Vector2Int(-1, -1),
                footprint.center,
                -1,
                -1,
                -1,
                SuggestedFix.RegenerateFloor);
            return new GridMap2D(1, 1, config.CellSize, Vector3.zero);
        }

        var paddingMeters = config.FootprintPaddingCells * config.CellSize;
        var origin = new Vector3(footprint.min.x - paddingMeters, 0f, footprint.min.z - paddingMeters);
        var width = Mathf.CeilToInt((footprint.size.x + paddingMeters * 2f) / config.CellSize);
        var height = Mathf.CeilToInt((footprint.size.z + paddingMeters * 2f) / config.CellSize);
        if (width <= 0 || height <= 0)
        {
            report.Add(
                ValidationErrorCode.InvalidFootprintOrGrid,
                ValidationSeverity.Fatal,
                "Grid size получился невалидным",
                new Vector2Int(-1, -1),
                footprint.center,
                -1,
                -1,
                -1,
                SuggestedFix.RegenerateFloor);
            return new GridMap2D(1, 1, config.CellSize, Vector3.zero);
        }

        var grid = new GridMap2D(width, height, config.CellSize, origin);

        for (var i = 0; i < result.Rooms.Count; i++)
        {
            var room = result.Rooms[i];
            var rect = grid.WorldBoundsToCellRect(room.WorldBounds);
            grid.FillRect(rect, GridLayerFlags.Floor);
            grid.SetRoomId(rect, room.RoomId);
            grid.AddTags(rect, GetGridTags(room));
        }

        for (var i = 0; i < result.Walls.Count; i++)
        {
            var wallRect = GetWallRect(result.Walls[i]);
            grid.FillRect(grid.WorldRectToCellRect(wallRect), GridLayerFlags.BlockedRaw);
        }

        for (var i = 0; i < result.Props.Count; i++)
        {
            if (!result.Props[i].BlocksMovement)
            {
                continue;
            }

            grid.FillRect(grid.WorldBoundsToCellRect(result.Props[i].WorldBounds), GridLayerFlags.BlockedRaw);
        }

        for (var i = 0; i < result.Doors.Count; i++)
        {
            var door = result.Doors[i];
            var rect = grid.WorldRectToCellRect(door.WorldRectXZ);
            grid.ClearRect(rect, GridLayerFlags.BlockedRaw);
            grid.FillRect(rect, GridLayerFlags.Floor);
            grid.MarkDoor(rect, door.DoorId);
        }

        for (var i = 0; i < result.Windows.Count; i++)
        {
            var window = result.Windows[i];
            var rect = BuildWindowRect(window, config.CellSize);
            var cellRect = grid.WorldRectToCellRect(rect);
            grid.MarkWindow(cellRect, window.WindowId);
            grid.AddTags(cellRect, GridCellTags.Facade | GridCellTags.Window);
        }

        return grid;
    }

    private static void ValidateDoorWidths(FloorResult result, ValidationConfig config, ValidationReport report)
    {
        for (var i = 0; i < result.Doors.Count; i++)
        {
            var door = result.Doors[i];
            if (door.ClearWidthMeters >= config.MinDoorClearWidthMeters)
            {
                continue;
            }

            report.Add(new ValidationError
            {
                Code = ValidationErrorCode.DoorTooNarrow,
                Severity = ValidationSeverity.Error,
                Message = string.Format("Door {0} already than {1:0.00}m", door.DebugName, config.MinDoorClearWidthMeters),
                WorldPos = door.WorldPos,
                DoorId = door.DoorId,
                SuggestedFix = SuggestedFix.OpenOrWidenDoor,
            });
        }
    }

    private static void ValidateRoomsReachCorridor(GridMap2D grid, bool[] reachable, FloorResult result, ValidationReport report)
    {
        for (var i = 0; i < result.Rooms.Count; i++)
        {
            var room = result.Rooms[i];
            if (room.RoomType == RoomCategory.Corridor || room.RoomType == RoomCategory.Hub || room.RoomType == RoomCategory.Start)
            {
                continue;
            }

            var cell = grid.WorldToCell(room.WorldBounds.center);
            if (grid.InBounds(cell) && reachable[grid.Index(cell.x, cell.y)])
            {
                continue;
            }

            report.Add(new ValidationError
            {
                Code = ValidationErrorCode.RoomDisconnectedFromCorridor,
                Severity = ValidationSeverity.Error,
                Message = string.Format("Комната {0} не подключена к проходимому коридору", room.Name),
                Cell = cell,
                WorldPos = room.WorldBounds.center,
                RoomId = room.RoomId,
                SuggestedFix = SuggestedFix.AddDoorConnection,
            });
        }
    }

    private static void ValidateWindows(FloorResult result, ValidationReport report)
    {
        for (var i = 0; i < result.Windows.Count; i++)
        {
            var window = result.Windows[i];
            if (IsWindowOnFacade(result.FootprintWorld, window))
            {
                continue;
            }

            report.Add(new ValidationError
            {
                Code = ValidationErrorCode.WindowNotOnFacade,
                Severity = ValidationSeverity.Error,
                Message = "Окно поставлено не на фасадной стене",
                WorldPos = window.WorldPos,
                RoomId = window.RoomId,
                SuggestedFix = SuggestedFix.RegenerateFloor,
            });
        }

        for (var i = 0; i < result.Rooms.Count; i++)
        {
            var room = result.Rooms[i];
            if ((room.Tags & RoomTags.FacadeRoom) == 0)
            {
                continue;
            }

            var hasWindow = false;
            for (var j = 0; j < result.Windows.Count; j++)
            {
                if (result.Windows[j].RoomId == room.RoomId)
                {
                    hasWindow = true;
                    break;
                }
            }

            if (hasWindow)
            {
                continue;
            }

            report.Add(new ValidationError
            {
                Code = ValidationErrorCode.FacadeRoomMissingWindow,
                Severity = ValidationSeverity.Error,
                Message = string.Format("Фасадная комната {0} без окна", room.Name),
                WorldPos = room.WorldBounds.center,
                RoomId = room.RoomId,
                SuggestedFix = SuggestedFix.RegenerateFloor,
            });
        }
    }

    private static void ValidateFacadeRatio(FloorResult result, ValidationReport report)
    {
        if (result.Rooms.Count == 0)
        {
            return;
        }

        var facadeRooms = 0;
        for (var i = 0; i < result.Rooms.Count; i++)
        {
            if ((result.Rooms[i].Tags & RoomTags.FacadeRoom) != 0)
            {
                facadeRooms++;
            }
        }

        var ratio = facadeRooms / (float)result.Rooms.Count;
        if (ratio < FacadeRatioMin || ratio > FacadeRatioMax)
        {
            report.Add(new ValidationError
            {
                Code = ValidationErrorCode.FacadeRoomMissingWindow,
                Severity = ValidationSeverity.Warning,
                Message = string.Format("Facade room ratio вне диапазона: {0:P0}", ratio),
            });
        }
    }

    private static void ValidateAlternativeLoop(GridMap2D grid, bool[] reachable, ValidationReport report)
    {
        var vertexCount = 0;
        var edgeCount = 0;
        for (var y = 0; y < grid.Height; y++)
        {
            for (var x = 0; x < grid.Width; x++)
            {
                var index = grid.Index(x, y);
                if (!reachable[index])
                {
                    continue;
                }

                vertexCount++;
                if (x + 1 < grid.Width && reachable[grid.Index(x + 1, y)])
                {
                    edgeCount++;
                }

                if (y + 1 < grid.Height && reachable[grid.Index(x, y + 1)])
                {
                    edgeCount++;
                }
            }
        }

        if (vertexCount > 0 && edgeCount < vertexCount)
        {
            report.Add(new ValidationError
            {
                Code = ValidationErrorCode.NoAlternativeLoop,
                Severity = ValidationSeverity.Warning,
                Message = "В планировке не найден альтернативный цикл маршрута",
                SuggestedFix = SuggestedFix.RebuildSubgraph,
            });
        }
    }

    private static void Append(ValidationReport report, IList<ValidationError> errors)
    {
        for (var i = 0; i < errors.Count; i++)
        {
            report.Add(errors[i]);
        }
    }

    private static GridCellTags GetGridTags(RoomInstance room)
    {
        var tags = GridCellTags.None;
        if (room.RoomType == RoomCategory.Corridor || room.RoomType == RoomCategory.Hub)
        {
            tags |= GridCellTags.Corridor;
            tags |= (room.Tags & RoomTags.MainPath) != 0 ? GridCellTags.MainCorridor : GridCellTags.SecondaryCorridor;
        }
        else
        {
            tags |= GridCellTags.Room;
        }

        if ((room.Tags & RoomTags.FacadeRoom) != 0)
        {
            tags |= GridCellTags.Facade;
        }

        if ((room.Tags & RoomTags.Tech) != 0)
        {
            tags |= GridCellTags.Tech;
        }

        return tags;
    }

    private static Rect GetWallRect(WallSegment wall)
    {
        var minX = Mathf.Min(wall.AWorld.x, wall.BWorld.x);
        var maxX = Mathf.Max(wall.AWorld.x, wall.BWorld.x);
        var minZ = Mathf.Min(wall.AWorld.z, wall.BWorld.z);
        var maxZ = Mathf.Max(wall.AWorld.z, wall.BWorld.z);
        if (Mathf.Abs(maxX - minX) < 0.001f)
        {
            minX -= wall.ThicknessMeters * 0.5f;
            maxX += wall.ThicknessMeters * 0.5f;
        }
        else
        {
            minZ -= wall.ThicknessMeters * 0.5f;
            maxZ += wall.ThicknessMeters * 0.5f;
        }

        return Rect.MinMaxRect(minX, minZ, maxX, maxZ);
    }

    private static Rect BuildWindowRect(WindowMarker window, float cellSize)
    {
        var thickness = Mathf.Max(0.08f, cellSize * 0.5f);
        if (window.Orientation == DoorOrientation.Horizontal)
        {
            return new Rect(window.WorldPos.x - window.WidthMeters * 0.5f, window.WorldPos.z - thickness * 0.5f, window.WidthMeters, thickness);
        }

        return new Rect(window.WorldPos.x - thickness * 0.5f, window.WorldPos.z - window.WidthMeters * 0.5f, thickness, window.WidthMeters);
    }

    private static bool IsWindowOnFacade(Bounds footprint, WindowMarker window)
    {
        var epsilon = 0.35f;
        if (window.Orientation == DoorOrientation.Horizontal)
        {
            return Mathf.Abs(window.WorldPos.z - footprint.min.z) <= epsilon || Mathf.Abs(window.WorldPos.z - footprint.max.z) <= epsilon;
        }

        return Mathf.Abs(window.WorldPos.x - footprint.min.x) <= epsilon || Mathf.Abs(window.WorldPos.x - footprint.max.x) <= epsilon;
    }
}
}

