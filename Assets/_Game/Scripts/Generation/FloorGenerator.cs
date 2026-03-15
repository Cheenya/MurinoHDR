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
    [SerializeField] private ValidationConfig _validationConfig;
    [SerializeField] private OutsideThemeProfile _outsideThemeProfile;

    public float CellSize => _cellSize;
    public float FloorThickness => _floorThickness;
    public float WallHeight => _wallHeight;
    public float WallThickness => _wallThickness;
    public int ExtraRoomsMin => _extraRoomsMin;
    public int ExtraRoomsMax => _extraRoomsMax;
    public int ValidationRuns => _validationRuns;
    public RoomTemplate[] Templates => _templates;
    public ValidationConfig ValidationConfig => _validationConfig;
    public OutsideThemeProfile OutsideThemeProfile => _outsideThemeProfile;

    public void Configure(
        float cellSize,
        float floorThickness,
        float wallHeight,
        float wallThickness,
        int extraRoomsMin,
        int extraRoomsMax,
        int validationRuns,
        RoomTemplate[] templates,
        ValidationConfig validationConfig,
        OutsideThemeProfile outsideThemeProfile)
    {
        _cellSize = cellSize;
        _floorThickness = floorThickness;
        _wallHeight = wallHeight;
        _wallThickness = wallThickness;
        _extraRoomsMin = extraRoomsMin;
        _extraRoomsMax = extraRoomsMax;
        _validationRuns = validationRuns;
        _templates = templates ?? Array.Empty<RoomTemplate>();
        _validationConfig = validationConfig;
        _outsideThemeProfile = outsideThemeProfile;
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
    public int AttemptIndex;
    public Vector2 WorldOffset;
    public readonly List<GeneratedRoom> Rooms = new List<GeneratedRoom>();
    public FloorResult FloorData;
    public ValidationReport ValidationReport;

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
    public ValidationReport DetailedReport;
    public readonly List<string> Issues = new List<string>();

    public string BuildSummary()
    {
        if (DetailedReport != null)
        {
            return DetailedReport.BuildSummary();
        }

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
        if (settings == null)
        {
            throw new ArgumentNullException(nameof(settings));
        }

        var validationConfig = settings.ValidationConfig ?? CreateFallbackValidationConfig();
        FloorGenerationResult lastCandidate = null;
        ValidationReport lastReport = null;

        for (var attempt = 0; attempt < Mathf.Max(1, validationConfig.MaxValidationAttempts); attempt++)
        {
            var effectiveSeed = HashSeed(seed, attempt);
            FloorGenerationResult candidate;
            try
            {
                candidate = GenerateCandidate(effectiveSeed, attempt, settings);
            }
            catch (Exception exception)
            {
                Debug.LogWarning(string.Format("[GEN] Candidate generation failed for seed {0} attempt {1}: {2}", seed, attempt, exception.Message));
                continue;
            }

            candidate.FloorData = FloorResultBuilder.Build(candidate, settings);
            candidate.FloorData.Seed = candidate.Seed;
            candidate.FloorData.AttemptIndex = attempt;

            var report = FloorGenValidator.Validate(candidate.FloorData, validationConfig);
            candidate.ValidationReport = report;
            candidate.FloorData.ValidationReport = report;
            if (report.Success)
            {
                return candidate;
            }

            if (validationConfig.AutoFixEnabled)
            {
                for (var fixIteration = 0; fixIteration < Mathf.Max(1, validationConfig.MaxAutoFixIterations); fixIteration++)
                {
                    if (!FloorGenAutoFixer.ApplyFixes(candidate.FloorData, report, validationConfig))
                    {
                        break;
                    }

                    report = FloorGenValidator.Validate(candidate.FloorData, validationConfig);
                    candidate.ValidationReport = report;
                    candidate.FloorData.ValidationReport = report;
                    if (report.Success)
                    {
                        return candidate;
                    }
                }
            }

            lastCandidate = candidate;
            lastReport = report;
        }

        if (lastCandidate == null)
        {
            var fallbackSeed = HashSeed(seed, 0);
            lastCandidate = GenerateCandidate(fallbackSeed, 0, settings);
            lastCandidate.FloorData = FloorResultBuilder.Build(lastCandidate, settings);
            lastReport = FloorGenValidator.Validate(lastCandidate.FloorData, validationConfig);
            lastCandidate.ValidationReport = lastReport;
            lastCandidate.FloorData.ValidationReport = lastReport;
        }

        return lastCandidate;
    }

    public static FloorGenerationValidationReport Validate(FloorGenerationResult result, FloorGeneratorSettings settings)
    {
        var report = new FloorGenerationValidationReport();
        report.Seed = result != null ? result.Seed : 0;

        if (result == null)
        {
            report.Issues.Add("Result is null");
            report.IsValid = false;
            return report;
        }

        if (result.FloorData == null)
        {
            result.FloorData = FloorResultBuilder.Build(result, settings);
        }

        var detailed = result.ValidationReport ?? FloorGenValidator.Validate(result.FloorData, settings.ValidationConfig ?? CreateFallbackValidationConfig());
        result.ValidationReport = detailed;
        result.FloorData.ValidationReport = detailed;
        report.DetailedReport = detailed;
        report.IsValid = detailed.Success;
        for (var i = 0; i < detailed.Errors.Count; i++)
        {
            report.Issues.Add(detailed.Errors[i].ToString());
        }

        return report;
    }

    private static FloorGenerationResult GenerateCandidate(int seed, int attemptIndex, FloorGeneratorSettings settings)
    {
        var random = new System.Random(seed);
        var parameters = CreateLayoutParameters(random);
        var supportOrder = random.NextDouble() < 0.5d
            ? new[] { RoomCategory.Utility, RoomCategory.Storage }
            : new[] { RoomCategory.Storage, RoomCategory.Utility };

        var result = new FloorGenerationResult();
        result.Seed = seed;
        result.AttemptIndex = attemptIndex;
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

        var sideExitWidth = Mathf.Max(3, (parameters.OpenWidth - 2) / 2);
        AddRoom(result, occupancy, settings.GetTemplate(RoomCategory.ExitElevator), new RectInt(parameters.OpenMinX, parameters.ExitY, sideExitWidth, parameters.ExitDepth), "ElevatorRoom");
        AddRoom(result, occupancy, settings.GetTemplate(RoomCategory.ExitShaft), new RectInt(parameters.OpenMinX + sideExitWidth, parameters.ExitY, 2, parameters.ExitDepth), "ShaftRoom");
        AddRoom(result, occupancy, settings.GetTemplate(RoomCategory.ExitStairs), new RectInt(parameters.OpenMinX + sideExitWidth + 2, parameters.ExitY, sideExitWidth, parameters.ExitDepth), "StairsRoom");

        RecalculateWorldOffset(result, settings);
        return result;
    }

    private static ValidationConfig CreateFallbackValidationConfig()
    {
        return new ValidationConfig();
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

    private static int HashSeed(int seed, int attempt)
    {
        unchecked
        {
            var value = seed;
            value = (value * 397) ^ (attempt * 7919);
            return value;
        }
    }
}
}

