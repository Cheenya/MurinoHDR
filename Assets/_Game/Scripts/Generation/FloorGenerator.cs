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
    private sealed class CandidateRoom
    {
        public string Name;
        public RectInt Rect;
        public RoomCategory[] Variants = Array.Empty<RoomCategory>();
    }

    public static FloorGenerationResult Generate(int seed, FloorGeneratorSettings settings)
    {
        var result = new FloorGenerationResult();
        result.Seed = seed;

        var occupancy = new HashSet<Vector2Int>();
        var random = new System.Random(seed);

        AddRoom(result, occupancy, settings.GetTemplate(RoomCategory.Start), new RectInt(-3, 0, 6, 4), "Reception");
        AddRoom(result, occupancy, settings.GetTemplate(RoomCategory.Corridor), new RectInt(-1, 4, 2, 5), "MainCorridor");
        AddRoom(result, occupancy, settings.GetTemplate(RoomCategory.Hub), new RectInt(-3, 9, 6, 4), "Hub");
        AddRoom(result, occupancy, settings.GetTemplate(RoomCategory.Corridor), new RectInt(-3, 13, 6, 2), "ServiceCorridor");

        AddRoom(result, occupancy, settings.GetTemplate(RoomCategory.ExitElevator), new RectInt(-3, 15, 2, 3), "ElevatorRoom");
        AddRoom(result, occupancy, settings.GetTemplate(RoomCategory.ExitShaft), new RectInt(-1, 15, 2, 3), "ShaftRoom");
        AddRoom(result, occupancy, settings.GetTemplate(RoomCategory.ExitStairs), new RectInt(1, 15, 2, 3), "StairsRoom");

        var candidates = new List<CandidateRoom>
        {
            new CandidateRoom { Name = "WestOfficeSouth", Rect = new RectInt(-6, 4, 5, 3), Variants = new[] { RoomCategory.Office, RoomCategory.Storage } },
            new CandidateRoom { Name = "WestOfficeNorth", Rect = new RectInt(-6, 7, 5, 4), Variants = new[] { RoomCategory.Office, RoomCategory.Utility } },
            new CandidateRoom { Name = "WestSupport", Rect = new RectInt(-6, 11, 3, 4), Variants = new[] { RoomCategory.Storage, RoomCategory.Utility } },
            new CandidateRoom { Name = "EastOfficeSouth", Rect = new RectInt(1, 4, 5, 3), Variants = new[] { RoomCategory.Office, RoomCategory.Utility } },
            new CandidateRoom { Name = "EastOfficeNorth", Rect = new RectInt(3, 9, 4, 4), Variants = new[] { RoomCategory.Office, RoomCategory.Storage } },
            new CandidateRoom { Name = "EastSupport", Rect = new RectInt(3, 13, 4, 2), Variants = new[] { RoomCategory.Utility, RoomCategory.Office } },
        };

        var extraTarget = Mathf.Clamp(random.Next(settings.ExtraRoomsMin + 1, settings.ExtraRoomsMax + 3), 0, candidates.Count);
        Shuffle(candidates, random);
        for (var i = 0; i < extraTarget; i++)
        {
            var candidate = candidates[i];
            var category = candidate.Variants[random.Next(0, candidate.Variants.Length)];
            AddRoom(result, occupancy, settings.GetTemplate(category), candidate.Rect, candidate.Name);
        }

        EnsureMinimumSupportRooms(result, occupancy, settings, random);
        RecalculateWorldOffset(result, settings);
        return result;
    }

    public static FloorGenerationValidationReport Validate(FloorGenerationResult result, FloorGeneratorSettings settings)
    {
        var report = new FloorGenerationValidationReport();
        report.Seed = result.Seed;

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

        ValidateMandatoryRooms(result, report);
        ValidateConnectivity(result, report);
        ValidateExitCluster(result, report);
        ValidateOfficeMix(result, report);

        report.IsValid = report.Issues.Count == 0;
        return report;
    }

    private static void EnsureMinimumSupportRooms(FloorGenerationResult result, HashSet<Vector2Int> occupancy, FloorGeneratorSettings settings, System.Random random)
    {
        if (HasCategory(result, RoomCategory.Office) && HasCategory(result, RoomCategory.Storage) && HasCategory(result, RoomCategory.Utility))
        {
            return;
        }

        var fallbackCandidates = new List<CandidateRoom>
        {
            new CandidateRoom { Name = "FallbackOffice", Rect = new RectInt(1, 7, 5, 2), Variants = new[] { RoomCategory.Office } },
            new CandidateRoom { Name = "FallbackStorage", Rect = new RectInt(-6, 11, 3, 4), Variants = new[] { RoomCategory.Storage } },
            new CandidateRoom { Name = "FallbackUtility", Rect = new RectInt(3, 13, 4, 2), Variants = new[] { RoomCategory.Utility } },
        };

        for (var i = 0; i < fallbackCandidates.Count; i++)
        {
            var fallback = fallbackCandidates[i];
            var targetCategory = fallback.Variants[0];
            if (HasCategory(result, targetCategory))
            {
                continue;
            }

            if (TryReplaceOrAddRoom(result, occupancy, settings.GetTemplate(targetCategory), fallback.Rect, fallback.Name))
            {
                continue;
            }

            var randomName = string.Format("{0}_{1}", fallback.Name, random.Next(100, 999));
            TryReplaceOrAddRoom(result, occupancy, settings.GetTemplate(targetCategory), fallback.Rect, randomName);
        }
    }

    private static bool TryReplaceOrAddRoom(FloorGenerationResult result, HashSet<Vector2Int> occupancy, RoomTemplate template, RectInt rect, string instanceId)
    {
        for (var i = 0; i < result.Rooms.Count; i++)
        {
            var existing = result.Rooms[i];
            if (existing.Rect.Equals(rect))
            {
                existing.Template = template;
                existing.InstanceId = instanceId;
                return true;
            }
        }

        if (CanOccupy(rect, occupancy))
        {
            AddRoom(result, occupancy, template, rect, instanceId);
            return true;
        }

        return false;
    }

    private static bool CanOccupy(RectInt rect, HashSet<Vector2Int> occupancy)
    {
        for (var x = rect.xMin; x < rect.xMax; x++)
        {
            for (var y = rect.yMin; y < rect.yMax; y++)
            {
                if (occupancy.Contains(new Vector2Int(x, y)))
                {
                    return false;
                }
            }
        }

        return true;
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
                if (visited.Contains(other.Index) || !AreAdjacent(room.Rect, other.Rect))
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

    private static void Shuffle<T>(IList<T> items, System.Random random)
    {
        for (var i = items.Count - 1; i > 0; i--)
        {
            var j = random.Next(0, i + 1);
            var temp = items[i];
            items[i] = items[j];
            items[j] = temp;
        }
    }
}
}
