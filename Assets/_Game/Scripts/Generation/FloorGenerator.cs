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
    [SerializeField] private OfficeGenerationRules _officeRules;

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
    public OfficeGenerationRules OfficeRules => _officeRules;

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
        OutsideThemeProfile outsideThemeProfile,
        OfficeGenerationRules officeRules)
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
        _officeRules = officeRules;
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
    public FloorStyle Style;
    public OutsideThemeProfile OutsideTheme;
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
        public FloorStyle Style;
        public FloorStyleProfile StyleProfile;
        public OfficeGenerationRules Rules;
        public int PlateHalfWidth;
        public int OpenHalfWidth;
        public int ReceptionHalfWidth;
        public int SouthOfficeDepth;
        public int FacadeBandDepth;
        public int MainCorridorWidth;
        public int SecondaryCorridorWidth;
        public int SideConnectorWidth;
        public int CoreDepth;
        public int ServiceDepth;
        public int ExitDepth;

        public int OpenMinX => -OpenHalfWidth;
        public int OpenWidth => OpenHalfWidth * 2;
        public int WestLoopMinX => -OpenHalfWidth - SideConnectorWidth;
        public int EastLoopMinX => OpenHalfWidth;
        public int OuterBandWidth => PlateHalfWidth - OpenHalfWidth - SideConnectorWidth;
        public int SouthCorridorY => SouthOfficeDepth;
        public int WorkY => SouthCorridorY + MainCorridorWidth;
        public int HubY => WorkY + MainCorridorWidth;
        public int CoreY => WorkY + FacadeBandDepth;
        public int ServiceY => CoreY + CoreDepth;
        public int ExitY => ServiceY + ServiceDepth;
        public int ConnectorHeight => ServiceY - WorkY;
    }

    public static FloorGenerationResult Generate(int seed, FloorGeneratorSettings settings, FloorStyle? styleOverride = null)
    {
        if (settings == null)
        {
            throw new ArgumentNullException(nameof(settings));
        }

        var validationConfig = settings.ValidationConfig ?? CreateFallbackValidationConfig();
        var officeRules = settings.OfficeRules ?? CreateFallbackOfficeRules(settings.OutsideThemeProfile);
        FloorGenerationResult lastCandidate = null;

        for (var attempt = 0; attempt < Mathf.Max(1, validationConfig.MaxValidationAttempts); attempt++)
        {
            var effectiveSeed = HashSeed(seed, attempt);
            FloorGenerationResult candidate;
            try
            {
                candidate = GenerateCandidate(effectiveSeed, attempt, settings, officeRules, styleOverride);
            }
            catch (Exception exception)
            {
                Debug.LogWarning(string.Format("[GEN] Candidate generation failed for seed {0} attempt {1}: {2}", seed, attempt, exception.Message));
                continue;
            }

            candidate.FloorData = FloorResultBuilder.Build(candidate, settings);
            candidate.FloorData.Seed = candidate.Seed;
            candidate.FloorData.AttemptIndex = attempt;
            candidate.FloorData.Style = candidate.Style;
            SortFloorData(candidate.FloorData);

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

                    SortFloorData(candidate.FloorData);
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
        }

        if (lastCandidate == null)
        {
            var fallbackSeed = HashSeed(seed, 0);
            lastCandidate = GenerateCandidate(fallbackSeed, 0, settings, officeRules, styleOverride);
            lastCandidate.FloorData = FloorResultBuilder.Build(lastCandidate, settings);
            lastCandidate.FloorData.Style = lastCandidate.Style;
            SortFloorData(lastCandidate.FloorData);
            var lastReport = FloorGenValidator.Validate(lastCandidate.FloorData, validationConfig);
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
            result.FloorData.Style = result.Style;
            SortFloorData(result.FloorData);
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

    private static FloorGenerationResult GenerateCandidate(int seed, int attemptIndex, FloorGeneratorSettings settings, OfficeGenerationRules officeRules, FloorStyle? styleOverride)
    {
        var random = new System.Random(seed);
        var style = officeRules.ResolveStyle(random, styleOverride);
        var parameters = CreateLayoutParameters(random, style, officeRules);
        var supportWestCategory = ResolveSupportCategory(random, style, officeRules, true);
        var supportEastCategory = supportWestCategory == RoomCategory.Storage ? RoomCategory.Utility : RoomCategory.Storage;

        var result = new FloorGenerationResult();
        result.Seed = seed;
        result.AttemptIndex = attemptIndex;
        result.Style = style;
        result.OutsideTheme = officeRules.ResolveTheme(seed, style) ?? settings.OutsideThemeProfile;

        var occupancy = new HashSet<Vector2Int>();
        AddRoom(result, occupancy, settings.GetTemplate(RoomCategory.Start), new RectInt(-parameters.ReceptionHalfWidth, 0, parameters.ReceptionHalfWidth * 2, parameters.SouthOfficeDepth), "Reception");
        AddRoom(result, occupancy, settings.GetTemplate(RoomCategory.Office), new RectInt(-parameters.PlateHalfWidth, 0, parameters.PlateHalfWidth - parameters.ReceptionHalfWidth, parameters.SouthOfficeDepth), "SouthWestOffice");
        AddRoom(result, occupancy, settings.GetTemplate(RoomCategory.Office), new RectInt(parameters.ReceptionHalfWidth, 0, parameters.PlateHalfWidth - parameters.ReceptionHalfWidth, parameters.SouthOfficeDepth), "SouthEastOffice");

        AddRoom(result, occupancy, settings.GetTemplate(RoomCategory.Corridor), new RectInt(-parameters.PlateHalfWidth, parameters.SouthCorridorY, parameters.PlateHalfWidth * 2, parameters.MainCorridorWidth), "SouthCrossCorridor");
        AddRoom(result, occupancy, settings.GetTemplate(RoomCategory.Corridor), new RectInt(parameters.WestLoopMinX, parameters.WorkY, parameters.SideConnectorWidth, parameters.ConnectorHeight), "WestLoopCorridor");
        AddRoom(result, occupancy, settings.GetTemplate(RoomCategory.Corridor), new RectInt(parameters.EastLoopMinX, parameters.WorkY, parameters.SideConnectorWidth, parameters.ConnectorHeight), "EastLoopCorridor");

        AddRoom(result, occupancy, settings.GetTemplate(RoomCategory.Corridor), new RectInt(parameters.OpenMinX, parameters.WorkY, parameters.OpenWidth, parameters.MainCorridorWidth), "MainCorridor");
        var hubDepth = Mathf.Max(2, parameters.FacadeBandDepth - parameters.MainCorridorWidth);
        AddRoom(result, occupancy, settings.GetTemplate(RoomCategory.Hub), new RectInt(parameters.OpenMinX, parameters.HubY, parameters.OpenWidth, hubDepth), "Hub");

        AddRoom(result, occupancy, settings.GetTemplate(RoomCategory.Office), new RectInt(-parameters.PlateHalfWidth, parameters.WorkY, parameters.OuterBandWidth, parameters.FacadeBandDepth), "WestOfficeSouth");
        AddRoom(result, occupancy, settings.GetTemplate(RoomCategory.Office), new RectInt(parameters.EastLoopMinX + parameters.SideConnectorWidth, parameters.WorkY, parameters.OuterBandWidth, parameters.FacadeBandDepth), "EastOfficeSouth");

        AddRoom(result, occupancy, settings.GetTemplate(RoomCategory.Office), new RectInt(-parameters.PlateHalfWidth, parameters.CoreY, parameters.OuterBandWidth, parameters.CoreDepth), "WestOfficeNorth");
        AddRoom(result, occupancy, settings.GetTemplate(supportWestCategory), new RectInt(parameters.OpenMinX, parameters.CoreY, parameters.OpenWidth / 2, parameters.CoreDepth), "CoreSupportWest");
        AddRoom(result, occupancy, settings.GetTemplate(supportEastCategory), new RectInt(parameters.OpenMinX + parameters.OpenWidth / 2, parameters.CoreY, parameters.OpenWidth - parameters.OpenWidth / 2, parameters.CoreDepth), "CoreSupportEast");
        AddRoom(result, occupancy, settings.GetTemplate(RoomCategory.Office), new RectInt(parameters.EastLoopMinX + parameters.SideConnectorWidth, parameters.CoreY, parameters.OuterBandWidth, parameters.CoreDepth), "EastOfficeNorth");

        AddRoom(result, occupancy, settings.GetTemplate(RoomCategory.Corridor), new RectInt(-parameters.PlateHalfWidth, parameters.ServiceY, parameters.PlateHalfWidth * 2, parameters.ServiceDepth), "ServiceCorridor");

        var elevatorWidth = Mathf.Max(3, parameters.OpenWidth - 3);
        var sideExitWidth = Mathf.Max(3, (parameters.PlateHalfWidth * 2 - elevatorWidth) / 2);
        AddRoom(result, occupancy, settings.GetTemplate(RoomCategory.ExitStairs), new RectInt(-parameters.PlateHalfWidth, parameters.ExitY, sideExitWidth, parameters.ExitDepth), "StairsRoom");
        AddRoom(result, occupancy, settings.GetTemplate(RoomCategory.ExitElevator), new RectInt(-elevatorWidth / 2, parameters.ExitY, elevatorWidth, parameters.ExitDepth), "ElevatorRoom");
        AddRoom(result, occupancy, settings.GetTemplate(RoomCategory.ExitShaft), new RectInt(parameters.PlateHalfWidth - sideExitWidth, parameters.ExitY, sideExitWidth, parameters.ExitDepth), "ShaftRoom");

        RecalculateWorldOffset(result, settings);
        return result;
    }

    private static RoomCategory ResolveSupportCategory(System.Random random, FloorStyle style, OfficeGenerationRules officeRules, bool west)
    {
        var utilityBias = style == FloorStyle.TechHeavy ? 0.72f : style == FloorStyle.Representative ? 0.35f : 0.5f;
        var storageBias = officeRules.GetAdjacencyWeight(RoomCategory.Storage, RoomCategory.Office) > officeRules.GetAdjacencyWeight(RoomCategory.Utility, RoomCategory.Office) ? 0.62f : 0.38f;
        var roll = random.NextDouble();
        if (west)
        {
            return roll < (utilityBias + storageBias) * 0.5f ? RoomCategory.Utility : RoomCategory.Storage;
        }

        return roll < (1d - utilityBias * 0.35d) ? RoomCategory.Storage : RoomCategory.Utility;
    }

    private static ValidationConfig CreateFallbackValidationConfig()
    {
        return new ValidationConfig();
    }

    private static OfficeGenerationRules CreateFallbackOfficeRules(OutsideThemeProfile fallbackTheme)
    {
        var rules = new OfficeGenerationRules();
        var profiles = new[]
        {
            CreateStyleProfile(FloorStyle.OpenSpaceHeavy, 3, 10, 12, 5, 6, 3, 4, 4, 5, 5, 6, 2, 2, 2, 4, 5, 2, 3, new[] { "open_islands", "meeting_edge", "window_desks" }, new[] { "cool_grid", "neutral_office" }, new[] { "plants_sparse", "glass_partitions" }),
            CreateStyleProfile(FloorStyle.CabinetHeavy, 4, 9, 11, 4, 5, 3, 4, 4, 5, 4, 5, 2, 2, 2, 4, 5, 2, 3, new[] { "cabinet_row", "paired_desks", "side_storage" }, new[] { "warm_grid", "neutral_office" }, new[] { "notice_board", "archive_trim" }),
            CreateStyleProfile(FloorStyle.TechHeavy, 2, 9, 10, 4, 5, 3, 4, 4, 4, 4, 5, 2, 2, 2, 5, 6, 2, 3, new[] { "tech_pods", "server_spine", "tool_wall" }, new[] { "cool_grid", "tech_strip" }, new[] { "cable_trays", "warning_panels" }),
            CreateStyleProfile(FloorStyle.Representative, 1, 10, 12, 5, 6, 4, 5, 4, 5, 5, 6, 2, 2, 2, 4, 5, 2, 3, new[] { "reception_focus", "executive_meeting", "quiet_bays" }, new[] { "warm_grid", "soft_spots" }, new[] { "plants_dense", "accent_wall" }),
        };

        var roomRules = new[]
        {
            CreateRoomRule(RoomCategory.Start, 0.95f, 0.05f, true, false),
            CreateRoomRule(RoomCategory.Corridor, 0.2f, 0.9f, false, false),
            CreateRoomRule(RoomCategory.Hub, 0.65f, 0.35f, false, false),
            CreateRoomRule(RoomCategory.Office, 0.85f, 0.15f, true, false),
            CreateRoomRule(RoomCategory.Storage, 0.1f, 0.95f, false, false),
            CreateRoomRule(RoomCategory.Utility, 0.05f, 0.98f, false, true),
            CreateRoomRule(RoomCategory.ExitElevator, 0.15f, 0.95f, false, false),
            CreateRoomRule(RoomCategory.ExitShaft, 0.1f, 0.95f, false, true),
            CreateRoomRule(RoomCategory.ExitStairs, 0.15f, 0.9f, false, false),
        };

        var adjacency = new[]
        {
            CreateAdjacencyRule(RoomCategory.Office, RoomCategory.Hub, 1.2f, false),
            CreateAdjacencyRule(RoomCategory.Office, RoomCategory.Corridor, 1.4f, false),
            CreateAdjacencyRule(RoomCategory.Storage, RoomCategory.Utility, 0.8f, false),
            CreateAdjacencyRule(RoomCategory.Storage, RoomCategory.Office, 0.6f, false),
            CreateAdjacencyRule(RoomCategory.Utility, RoomCategory.Hub, 0.3f, false),
            CreateAdjacencyRule(RoomCategory.Utility, RoomCategory.Start, 0.2f, false),
            CreateAdjacencyRule(RoomCategory.Utility, RoomCategory.Office, 0.35f, false),
            CreateAdjacencyRule(RoomCategory.Start, RoomCategory.ExitElevator, 1.5f, false),
            CreateAdjacencyRule(RoomCategory.Start, RoomCategory.Utility, -1f, true),
        };

        rules.Configure(FloorStyle.CabinetHeavy, profiles, roomRules, adjacency, fallbackTheme != null ? new[] { fallbackTheme } : Array.Empty<OutsideThemeProfile>(), 2, 1, 1, 1, 5, 10f, 20f);
        return rules;
    }

    private static LayoutParameters CreateLayoutParameters(System.Random random, FloorStyle style, OfficeGenerationRules officeRules)
    {
        var profile = officeRules.GetStyleProfile(style) ?? CreateFallbackStyleProfile(style);
        var parameters = new LayoutParameters();
        parameters.Style = style;
        parameters.StyleProfile = profile;
        parameters.Rules = officeRules;
        parameters.PlateHalfWidth = random.Next(profile.PlateHalfWidthMin, profile.PlateHalfWidthMax + 1);
        parameters.OpenHalfWidth = random.Next(profile.OpenHalfWidthMin, profile.OpenHalfWidthMax + 1);
        parameters.ReceptionHalfWidth = random.Next(profile.ReceptionHalfWidthMin, profile.ReceptionHalfWidthMax + 1);
        parameters.SouthOfficeDepth = random.Next(profile.SouthOfficeDepthMin, profile.SouthOfficeDepthMax + 1);
        parameters.FacadeBandDepth = random.Next(profile.FacadeBandDepthMin, profile.FacadeBandDepthMax + 1);
        parameters.MainCorridorWidth = profile.MainCorridorWidthCells;
        parameters.SecondaryCorridorWidth = profile.SecondaryCorridorWidthCells;
        parameters.SideConnectorWidth = profile.SideConnectorWidthCells;
        parameters.CoreDepth = random.Next(profile.CoreDepthMin, profile.CoreDepthMax + 1);
        parameters.ServiceDepth = profile.ServiceDepthCells;
        parameters.ExitDepth = profile.ExitDepthCells;

        if (parameters.OuterBandWidth < 3)
        {
            parameters.PlateHalfWidth += 2;
        }

        return parameters;
    }

    private static FloorStyleProfile CreateFallbackStyleProfile(FloorStyle style)
    {
        var profile = new FloorStyleProfile();
        profile.Configure(style, 1, 9, 11, 4, 5, 3, 4, 4, 5, 4, 5, 2, 2, 2, 4, 5, 2, 3, Array.Empty<string>(), Array.Empty<string>(), Array.Empty<string>());
        return profile;
    }

    private static FloorStyleProfile CreateStyleProfile(
        FloorStyle style,
        int weight,
        int plateHalfWidthMin,
        int plateHalfWidthMax,
        int openHalfWidthMin,
        int openHalfWidthMax,
        int receptionHalfWidthMin,
        int receptionHalfWidthMax,
        int southOfficeDepthMin,
        int southOfficeDepthMax,
        int facadeBandDepthMin,
        int facadeBandDepthMax,
        int mainCorridorWidthCells,
        int secondaryCorridorWidthCells,
        int sideConnectorWidthCells,
        int coreDepthMin,
        int coreDepthMax,
        int serviceDepthCells,
        int exitDepthCells,
        string[] propPatterns,
        string[] lightingProfiles,
        string[] decorProfiles)
    {
        var profile = new FloorStyleProfile();
        profile.Configure(style, weight, plateHalfWidthMin, plateHalfWidthMax, openHalfWidthMin, openHalfWidthMax, receptionHalfWidthMin, receptionHalfWidthMax, southOfficeDepthMin, southOfficeDepthMax, facadeBandDepthMin, facadeBandDepthMax, mainCorridorWidthCells, secondaryCorridorWidthCells, sideConnectorWidthCells, coreDepthMin, coreDepthMax, serviceDepthCells, exitDepthCells, propPatterns, lightingProfiles, decorProfiles);
        return profile;
    }

    private static RoomPlacementRule CreateRoomRule(RoomCategory category, float facade, float core, bool requiresWindow, bool allowChokepoint)
    {
        var rule = new RoomPlacementRule();
        rule.Configure(category, facade, core, requiresWindow, allowChokepoint);
        return rule;
    }

    private static AdjacencyRule CreateAdjacencyRule(RoomCategory a, RoomCategory b, float weight, bool forbidden)
    {
        var rule = new AdjacencyRule();
        rule.Configure(a, b, weight, forbidden);
        return rule;
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

    private static void SortFloorData(FloorResult result)
    {
        result.Rooms.Sort((a, b) => a.RoomId.CompareTo(b.RoomId));
        result.Walls.Sort((a, b) => a.WallId.CompareTo(b.WallId));
        result.Doors.Sort((a, b) => a.DoorId.CompareTo(b.DoorId));
        result.Props.Sort((a, b) => a.PropId.CompareTo(b.PropId));
        result.Exits.Sort((a, b) => a.Type.CompareTo(b.Type));
        result.Pickups.Sort((a, b) => string.CompareOrdinal(a.ItemId, b.ItemId));
        result.SupportRooms.Sort((a, b) => a.RoomId.CompareTo(b.RoomId));
        result.Windows.Sort((a, b) => a.WindowId.CompareTo(b.WindowId));
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
