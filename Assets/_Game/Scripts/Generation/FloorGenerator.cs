using System;
using System.Collections.Generic;
using UnityEngine;

namespace MurinoHDR.Generation
{

public enum RoomCategory
{
    Start = 0,
    Corridor = 1,
    Hub = 2,
    Office = 3,
    Storage = 4,
    Utility = 5,
    ExitElevator = 6,
    ExitShaft = 7,
    ExitStairs = 8,
}

public sealed class RoomTemplate : ScriptableObject
{
    [SerializeField] private string _roomId = string.Empty;
    [SerializeField] private RoomCategory _category;
    [SerializeField] private Vector2Int _minSize = Vector2Int.one;
    [SerializeField] private Vector2Int _maxSize = Vector2Int.one;
    [SerializeField] private float _weight = 1f;
    [SerializeField] private bool _mandatory;

    public string RoomId => _roomId;
    public RoomCategory Category => _category;
    public Vector2Int MinSize => _minSize;
    public Vector2Int MaxSize => _maxSize;
    public float Weight => _weight;
    public bool Mandatory => _mandatory;

    public void Configure(string roomId, RoomCategory category, Vector2Int minSize, Vector2Int maxSize, float weight, bool mandatory)
    {
        _roomId = roomId;
        _category = category;
        _minSize = minSize;
        _maxSize = maxSize;
        _weight = weight;
        _mandatory = mandatory;
    }
}

public sealed class FloorGeneratorSettings : ScriptableObject
{
    [SerializeField] private float _cellSize = 4f;
    [SerializeField] private float _floorThickness = 0.24f;
    [SerializeField] private float _wallHeight = 3.2f;
    [SerializeField] private float _wallThickness = 0.28f;
    [SerializeField] private int _extraRoomsMin = 2;
    [SerializeField] private int _extraRoomsMax = 4;
    [SerializeField] private int _validationRuns = 24;
    [SerializeField] private RoomTemplate[] _templates = Array.Empty<RoomTemplate>();

    public float CellSize => _cellSize;
    public float FloorThickness => _floorThickness;
    public float WallHeight => _wallHeight;
    public float WallThickness => _wallThickness;
    public int ExtraRoomsMin => _extraRoomsMin;
    public int ExtraRoomsMax => _extraRoomsMax;
    public int ValidationRuns => _validationRuns;
    public RoomTemplate[] Templates => _templates;

    public void Configure(float cellSize, float floorThickness, float wallHeight, float wallThickness, int extraRoomsMin, int extraRoomsMax, int validationRuns, RoomTemplate[] templates)
    {
        _cellSize = cellSize;
        _floorThickness = floorThickness;
        _wallHeight = wallHeight;
        _wallThickness = wallThickness;
        _extraRoomsMin = extraRoomsMin;
        _extraRoomsMax = extraRoomsMax;
        _validationRuns = validationRuns;
        _templates = templates ?? Array.Empty<RoomTemplate>();
    }

    public RoomTemplate GetTemplate(RoomCategory category)
    {
        for (var i = 0; i < _templates.Length; i++)
        {
            if (_templates[i] != null && _templates[i].Category == category)
            {
                return _templates[i];
            }
        }

        return null;
    }
}

public sealed class GeneratedRoom
{
    public int Index;
    public string InstanceId;
    public RoomTemplate Template;
    public RectInt Rect;

    public RoomCategory Category => Template.Category;
    public Vector2Int CenterCell => new Vector2Int(Rect.xMin + Rect.width / 2, Rect.yMin + Rect.height / 2);
}

public sealed class FloorGenerationResult
{
    public int Seed;
    public Vector2 WorldOffset;
    public readonly List<GeneratedRoom> Rooms = new List<GeneratedRoom>();

    public GeneratedRoom GetRoom(RoomCategory category)
    {
        for (var i = 0; i < Rooms.Count; i++)
        {
            if (Rooms[i].Category == category)
            {
                return Rooms[i];
            }
        }

        return null;
    }
}

public sealed class FloorGenerationValidationReport
{
    public int Seed;
    public bool IsValid;
    public readonly List<string> Issues = new List<string>();

    public string BuildSummary()
    {
        return Issues.Count == 0 ? string.Format("Seed {0}: OK", Seed) : string.Format("Seed {0}: {1}", Seed, string.Join(" | ", Issues));
    }
}

public static class FloorGenerator
{
    private sealed class LayoutParameters
    {
        public int PlateHalfWidth;
        public int OpenHalfWidth;
        public int ReceptionHalfWidth;
        public int SouthOfficeDepth;
        public int SouthCorridorDepth;
        public int WorkDepth;
        public int CoreDepth;
        public int ServiceDepth;
        public int ExitDepth;

        public int OpenMinX => -OpenHalfWidth;
        public int OpenWidth => OpenHalfWidth * 2;
        public int SideBandWidth => PlateHalfWidth - OpenHalfWidth;
        public int SouthCorridorY => SouthOfficeDepth;
        public int WorkY => SouthCorridorY + SouthCorridorDepth;
        public int CoreY => WorkY + WorkDepth;
        public int ServiceY => CoreY + CoreDepth;
        public int ExitY => ServiceY + ServiceDepth;
    }

    public static FloorGenerationResult Generate(int seed, FloorGeneratorSettings settings)
    {
        FloorGenerationResult lastCandidate = null;
        for (var attempt = 0; attempt < 8; attempt++)
        {
            var attemptSeed = seed + attempt * 97;
            var candidate = GenerateCandidate(attemptSeed, settings);
            var report = Validate(candidate, settings);
            if (report.IsValid)
            {
                return candidate;
            }

            lastCandidate = candidate;
        }

        return lastCandidate ?? GenerateCandidate(seed, settings);
    }

    public static FloorGenerationValidationReport Validate(FloorGenerationResult result, FloorGeneratorSettings settings)
    {
        var report = new FloorGenerationValidationReport();
        report.Seed = result.Seed;

        ValidateOverlaps(result, report);
        ValidateMandatoryRooms(result, report);
        ValidateConnectivity(result, report);
        ValidateExitCluster(result, report);
        ValidateOfficeMix(result, report);
        ValidatePerimeterZoning(result, report);
        ValidateCoreSupportZoning(result, report);

        report.IsValid = report.Issues.Count == 0;
        return report;
    }

    private static FloorGenerationResult GenerateCandidate(int seed, FloorGeneratorSettings settings)
    {
        var random = new System.Random(seed);
        var parameters = CreateLayoutParameters(random);
        var supportOrder = random.NextDouble() < 0.5d
            ? new[] { RoomCategory.Utility, RoomCategory.Storage }
            : new[] { RoomCategory.Storage, RoomCategory.Utility };

        var result = new FloorGenerationResult();
        result.Seed = seed;
        var occupancy = new HashSet<Vector2Int>();

        AddRoom(result, occupancy, settings.GetTemplate(RoomCategory.Start), new RectInt(-parameters.ReceptionHalfWidth, 0, parameters.ReceptionHalfWidth * 2, parameters.SouthOfficeDepth), "Reception");
        AddRoom(result, occupancy, settings.GetTemplate(RoomCategory.Office), new RectInt(-parameters.PlateHalfWidth, 0, parameters.PlateHalfWidth - parameters.ReceptionHalfWidth, parameters.SouthOfficeDepth), "SouthWestOffice");
        AddRoom(result, occupancy, settings.GetTemplate(RoomCategory.Office), new RectInt(parameters.ReceptionHalfWidth, 0, parameters.PlateHalfWidth - parameters.ReceptionHalfWidth, parameters.SouthOfficeDepth), "SouthEastOffice");

        AddRoom(result, occupancy, settings.GetTemplate(RoomCategory.Corridor), new RectInt(-parameters.PlateHalfWidth, parameters.SouthCorridorY, parameters.PlateHalfWidth * 2, parameters.SouthCorridorDepth), "SouthCrossCorridor");

        var mainCorridorDepth = Mathf.Max(2, parameters.WorkDepth - 2);
        var hubDepth = Mathf.Max(2, parameters.WorkDepth - mainCorridorDepth);
        AddRoom(result, occupancy, settings.GetTemplate(RoomCategory.Corridor), new RectInt(parameters.OpenMinX, parameters.WorkY, parameters.OpenWidth, mainCorridorDepth), "MainCorridor");
        AddRoom(result, occupancy, settings.GetTemplate(RoomCategory.Hub), new RectInt(parameters.OpenMinX, parameters.WorkY + mainCorridorDepth, parameters.OpenWidth, hubDepth), "Hub");

        AddRoom(result, occupancy, settings.GetTemplate(RoomCategory.Office), new RectInt(-parameters.PlateHalfWidth, parameters.WorkY, parameters.SideBandWidth, parameters.WorkDepth), "WestOfficeSouth");
        AddRoom(result, occupancy, settings.GetTemplate(RoomCategory.Office), new RectInt(parameters.OpenHalfWidth, parameters.WorkY, parameters.SideBandWidth, parameters.WorkDepth), "EastOfficeSouth");

        AddRoom(result, occupancy, settings.GetTemplate(RoomCategory.Office), new RectInt(-parameters.PlateHalfWidth, parameters.CoreY, parameters.SideBandWidth, parameters.CoreDepth), "WestOfficeNorth");
        AddRoom(result, occupancy, settings.GetTemplate(RoomCategory.Corridor), new RectInt(parameters.OpenMinX, parameters.CoreY, 2, parameters.CoreDepth), "WestCoreCorridor");
        AddRoom(result, occupancy, settings.GetTemplate(supportOrder[0]), new RectInt(-3, parameters.CoreY, 3, parameters.CoreDepth), "CoreSupportWest");
        AddRoom(result, occupancy, settings.GetTemplate(supportOrder[1]), new RectInt(0, parameters.CoreY, 3, parameters.CoreDepth), "CoreSupportEast");
        AddRoom(result, occupancy, settings.GetTemplate(RoomCategory.Corridor), new RectInt(3, parameters.CoreY, 2, parameters.CoreDepth), "EastCoreCorridor");
        AddRoom(result, occupancy, settings.GetTemplate(RoomCategory.Office), new RectInt(parameters.OpenHalfWidth, parameters.CoreY, parameters.SideBandWidth, parameters.CoreDepth), "EastOfficeNorth");

        AddRoom(result, occupancy, settings.GetTemplate(RoomCategory.Corridor), new RectInt(parameters.OpenMinX, parameters.ServiceY, parameters.OpenWidth, parameters.ServiceDepth), "ServiceCorridor");

        var sideExitWidth = (parameters.OpenWidth - 2) / 2;
        AddRoom(result, occupancy, settings.GetTemplate(RoomCategory.ExitElevator), new RectInt(parameters.OpenMinX, parameters.ExitY, sideExitWidth, parameters.ExitDepth), "ElevatorRoom");
        AddRoom(result, occupancy, settings.GetTemplate(RoomCategory.ExitShaft), new RectInt(parameters.OpenMinX + sideExitWidth, parameters.ExitY, 2, parameters.ExitDepth), "ShaftRoom");
        AddRoom(result, occupancy, settings.GetTemplate(RoomCategory.ExitStairs), new RectInt(parameters.OpenMinX + sideExitWidth + 2, parameters.ExitY, sideExitWidth, parameters.ExitDepth), "StairsRoom");

        RecalculateWorldOffset(result, settings);
        return result;
    }

    private static LayoutParameters CreateLayoutParameters(System.Random random)
    {
        return new LayoutParameters
        {
            PlateHalfWidth = random.Next(9, 11),
            OpenHalfWidth = 5,
            ReceptionHalfWidth = random.Next(3, 5),
            SouthOfficeDepth = random.Next(4, 6),
            SouthCorridorDepth = 2,
            WorkDepth = random.Next(4, 6),
            CoreDepth = random.Next(4, 6),
            ServiceDepth = 2,
            ExitDepth = 3,
        };
    }

    private static void ValidateOverlaps(FloorGenerationResult result, FloorGenerationValidationReport report)
    {
        var occupancy = new HashSet<Vector2Int>();
        for (var i = 0; i < result.Rooms.Count; i++)
        {
            var room = result.Rooms[i];
            for (var x = room.Rect.xMin; x < room.Rect.xMax; x++)
            {
                for (var y = room.Rect.yMin; y < room.Rect.yMax; y++)
                {
                    var cell = new Vector2Int(x, y);
                    if (!occupancy.Add(cell))
                    {
                        report.Issues.Add(string.Format("Overlap in cell {0}", cell));
                    }
                }
            }
        }
    }

    private static void ValidateMandatoryRooms(FloorGenerationResult result, FloorGenerationValidationReport report)
    {
        if (result.GetRoom(RoomCategory.Start) == null)
        {
            report.Issues.Add("Missing start room");
        }

        if (result.GetRoom(RoomCategory.ExitElevator) == null)
        {
            report.Issues.Add("Missing elevator room");
        }

        if (result.GetRoom(RoomCategory.ExitShaft) == null)
        {
            report.Issues.Add("Missing shaft room");
        }

        if (result.GetRoom(RoomCategory.ExitStairs) == null)
        {
            report.Issues.Add("Missing stairs room");
        }
    }

    private static void ValidateConnectivity(FloorGenerationResult result, FloorGenerationValidationReport report)
    {
        var start = result.GetRoom(RoomCategory.Start);
        if (start == null)
        {
            return;
        }

        var visited = new HashSet<int>();
        var queue = new Queue<GeneratedRoom>();
        visited.Add(start.Index);
        queue.Enqueue(start);

        while (queue.Count > 0)
        {
            var room = queue.Dequeue();
            for (var i = 0; i < result.Rooms.Count; i++)
            {
                var other = result.Rooms[i];
                if (visited.Contains(other.Index) || !HasTraversableConnection(room, other))
                {
                    continue;
                }

                visited.Add(other.Index);
                queue.Enqueue(other);
            }
        }

        for (var i = 0; i < result.Rooms.Count; i++)
        {
            if (!visited.Contains(result.Rooms[i].Index))
            {
                report.Issues.Add(string.Format("Unreachable room: {0}", result.Rooms[i].InstanceId));
            }
        }
    }

    private static void ValidateExitCluster(FloorGenerationResult result, FloorGenerationValidationReport report)
    {
        var service = FindRoom(result, "ServiceCorridor");
        var elevator = result.GetRoom(RoomCategory.ExitElevator);
        var shaft = result.GetRoom(RoomCategory.ExitShaft);
        var stairs = result.GetRoom(RoomCategory.ExitStairs);
        if (service == null || elevator == null || shaft == null || stairs == null)
        {
            return;
        }

        if (!AreAdjacent(service.Rect, elevator.Rect))
        {
            report.Issues.Add("Elevator is not attached to service corridor");
        }

        if (!AreAdjacent(service.Rect, shaft.Rect))
        {
            report.Issues.Add("Shaft is not attached to service corridor");
        }

        if (!AreAdjacent(service.Rect, stairs.Rect))
        {
            report.Issues.Add("Stairs are not attached to service corridor");
        }

        if (!AreAdjacent(elevator.Rect, shaft.Rect) || !AreAdjacent(shaft.Rect, stairs.Rect))
        {
            report.Issues.Add("Exit rooms must form a single service cluster");
        }

        var bounds = GetLayoutBounds(result);
        if (!TouchesPerimeter(elevator.Rect, bounds) || !TouchesPerimeter(shaft.Rect, bounds) || !TouchesPerimeter(stairs.Rect, bounds))
        {
            report.Issues.Add("Exit rooms must sit on the building perimeter");
        }
    }

    private static void ValidateOfficeMix(FloorGenerationResult result, FloorGenerationValidationReport report)
    {
        if (!HasCategory(result, RoomCategory.Office))
        {
            report.Issues.Add("No office room generated");
        }

        if (!HasCategory(result, RoomCategory.Storage))
        {
            report.Issues.Add("No storage room generated");
        }

        if (!HasCategory(result, RoomCategory.Utility))
        {
            report.Issues.Add("No utility room generated");
        }
    }

    private static void ValidatePerimeterZoning(FloorGenerationResult result, FloorGenerationValidationReport report)
    {
        var bounds = GetLayoutBounds(result);
        for (var i = 0; i < result.Rooms.Count; i++)
        {
            var room = result.Rooms[i];
            if ((room.Category == RoomCategory.Start || room.Category == RoomCategory.Office) && !TouchesPerimeter(room.Rect, bounds))
            {
                report.Issues.Add(string.Format("{0} should be on the perimeter/daylight band", room.InstanceId));
            }

            if ((room.Category == RoomCategory.Storage || room.Category == RoomCategory.Utility) && TouchesPerimeter(room.Rect, bounds))
            {
                report.Issues.Add(string.Format("{0} should stay near the core, not on the perimeter", room.InstanceId));
            }
        }
    }

    private static void ValidateCoreSupportZoning(FloorGenerationResult result, FloorGenerationValidationReport report)
    {
        for (var i = 0; i < result.Rooms.Count; i++)
        {
            var room = result.Rooms[i];
            if (room.Category != RoomCategory.Storage && room.Category != RoomCategory.Utility)
            {
                continue;
            }

            if (!HasAdjacentCirculationRoom(result, room))
            {
                report.Issues.Add(string.Format("Core support room {0} is isolated from circulation", room.InstanceId));
            }
        }
    }

    private static bool HasAdjacentCirculationRoom(FloorGenerationResult result, GeneratedRoom targetRoom)
    {
        for (var i = 0; i < result.Rooms.Count; i++)
        {
            var other = result.Rooms[i];
            if (other.Index == targetRoom.Index)
            {
                continue;
            }

            if (IsCirculationRoom(other.Category) && AreAdjacent(targetRoom.Rect, other.Rect))
            {
                return true;
            }
        }

        return false;
    }

    private static bool HasTraversableConnection(GeneratedRoom a, GeneratedRoom b)
    {
        if (!AreAdjacent(a.Rect, b.Rect))
        {
            return false;
        }

        var aCirculation = IsCirculationRoom(a.Category);
        var bCirculation = IsCirculationRoom(b.Category);
        if (aCirculation && bCirculation)
        {
            return true;
        }

        return (aCirculation && IsEnclosedProgram(b.Category)) || (bCirculation && IsEnclosedProgram(a.Category));
    }

    private static bool IsCirculationRoom(RoomCategory category)
    {
        return category == RoomCategory.Start || category == RoomCategory.Corridor || category == RoomCategory.Hub;
    }

    private static bool IsEnclosedProgram(RoomCategory category)
    {
        return category == RoomCategory.Office || category == RoomCategory.Storage || category == RoomCategory.Utility || category == RoomCategory.ExitElevator || category == RoomCategory.ExitShaft || category == RoomCategory.ExitStairs;
    }

    private static GeneratedRoom FindRoom(FloorGenerationResult result, string instanceId)
    {
        for (var i = 0; i < result.Rooms.Count; i++)
        {
            if (result.Rooms[i].InstanceId == instanceId)
            {
                return result.Rooms[i];
            }
        }

        return null;
    }

    private static bool HasCategory(FloorGenerationResult result, RoomCategory category)
    {
        return result.GetRoom(category) != null;
    }

    private static void RecalculateWorldOffset(FloorGenerationResult result, FloorGeneratorSettings settings)
    {
        var min = new Vector2Int(int.MaxValue, int.MaxValue);
        var max = new Vector2Int(int.MinValue, int.MinValue);
        for (var i = 0; i < result.Rooms.Count; i++)
        {
            var rect = result.Rooms[i].Rect;
            min.x = Mathf.Min(min.x, rect.xMin);
            min.y = Mathf.Min(min.y, rect.yMin);
            max.x = Mathf.Max(max.x, rect.xMax);
            max.y = Mathf.Max(max.y, rect.yMax);
        }

        var size = new Vector2(max.x - min.x, max.y - min.y);
        result.WorldOffset = new Vector2(-(min.x + size.x * 0.5f) * settings.CellSize, -(min.y + size.y * 0.5f) * settings.CellSize);
    }

    private static void AddRoom(FloorGenerationResult result, HashSet<Vector2Int> occupancy, RoomTemplate template, RectInt rect, string instanceId)
    {
        if (template == null)
        {
            throw new InvalidOperationException(string.Format("Template missing for room {0}", instanceId));
        }

        for (var x = rect.xMin; x < rect.xMax; x++)
        {
            for (var y = rect.yMin; y < rect.yMax; y++)
            {
                var cell = new Vector2Int(x, y);
                if (!occupancy.Add(cell))
                {
                    throw new InvalidOperationException(string.Format("Generation overlap in {0} at {1}", instanceId, cell));
                }
            }
        }

        result.Rooms.Add(new GeneratedRoom
        {
            Index = result.Rooms.Count,
            InstanceId = instanceId,
            Template = template,
            Rect = rect,
        });
    }

    private static bool AreAdjacent(RectInt a, RectInt b)
    {
        var xOverlap = Mathf.Min(a.xMax, b.xMax) - Mathf.Max(a.xMin, b.xMin);
        var yOverlap = Mathf.Min(a.yMax, b.yMax) - Mathf.Max(a.yMin, b.yMin);
        if (xOverlap > 0 && (a.yMax == b.yMin || b.yMax == a.yMin))
        {
            return true;
        }

        if (yOverlap > 0 && (a.xMax == b.xMin || b.xMax == a.xMin))
        {
            return true;
        }

        return false;
    }

    private static RectInt GetLayoutBounds(FloorGenerationResult result)
    {
        var minX = int.MaxValue;
        var minY = int.MaxValue;
        var maxX = int.MinValue;
        var maxY = int.MinValue;
        for (var i = 0; i < result.Rooms.Count; i++)
        {
            var rect = result.Rooms[i].Rect;
            minX = Mathf.Min(minX, rect.xMin);
            minY = Mathf.Min(minY, rect.yMin);
            maxX = Mathf.Max(maxX, rect.xMax);
            maxY = Mathf.Max(maxY, rect.yMax);
        }

        return new RectInt(minX, minY, maxX - minX, maxY - minY);
    }

    private static bool TouchesPerimeter(RectInt rect, RectInt bounds)
    {
        return rect.xMin == bounds.xMin || rect.xMax == bounds.xMax || rect.yMin == bounds.yMin || rect.yMax == bounds.yMax;
    }
}
}
