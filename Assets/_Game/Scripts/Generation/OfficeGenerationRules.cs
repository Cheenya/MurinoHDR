using System;
using UnityEngine;

namespace MurinoHDR.Generation
{

public enum FloorStyle
{
    OpenSpaceHeavy = 0,
    CabinetHeavy = 1,
    TechHeavy = 2,
    Representative = 3,
}

[Serializable]
public sealed class RoomPlacementRule
{
    [SerializeField] private RoomCategory _category;
    [SerializeField] [Range(0f, 1f)] private float _facadePreference = 0.5f;
    [SerializeField] [Range(0f, 1f)] private float _corePreference = 0.5f;
    [SerializeField] private bool _requiresFacadeWindow;
    [SerializeField] private bool _allowChokepoint;

    public RoomCategory Category => _category;
    public float FacadePreference => _facadePreference;
    public float CorePreference => _corePreference;
    public bool RequiresFacadeWindow => _requiresFacadeWindow;
    public bool AllowChokepoint => _allowChokepoint;

    public void Configure(RoomCategory category, float facadePreference, float corePreference, bool requiresFacadeWindow, bool allowChokepoint)
    {
        _category = category;
        _facadePreference = Mathf.Clamp01(facadePreference);
        _corePreference = Mathf.Clamp01(corePreference);
        _requiresFacadeWindow = requiresFacadeWindow;
        _allowChokepoint = allowChokepoint;
    }
}

[Serializable]
public sealed class AdjacencyRule
{
    [SerializeField] private RoomCategory _a;
    [SerializeField] private RoomCategory _b;
    [SerializeField] private float _weight = 1f;
    [SerializeField] private bool _forbidden;

    public RoomCategory A => _a;
    public RoomCategory B => _b;
    public float Weight => _weight;
    public bool Forbidden => _forbidden;

    public void Configure(RoomCategory a, RoomCategory b, float weight, bool forbidden)
    {
        _a = a;
        _b = b;
        _weight = weight;
        _forbidden = forbidden;
    }

    public bool Matches(RoomCategory first, RoomCategory second)
    {
        return (_a == first && _b == second) || (_a == second && _b == first);
    }
}

[Serializable]
public sealed class FloorStyleProfile
{
    [SerializeField] private FloorStyle _style;
    [SerializeField] private int _weight = 1;
    [SerializeField] private int _plateHalfWidthMin = 9;
    [SerializeField] private int _plateHalfWidthMax = 11;
    [SerializeField] private int _openHalfWidthMin = 4;
    [SerializeField] private int _openHalfWidthMax = 5;
    [SerializeField] private int _receptionHalfWidthMin = 3;
    [SerializeField] private int _receptionHalfWidthMax = 4;
    [SerializeField] private int _southOfficeDepthMin = 4;
    [SerializeField] private int _southOfficeDepthMax = 5;
    [SerializeField] private int _facadeBandDepthMin = 4;
    [SerializeField] private int _facadeBandDepthMax = 6;
    [SerializeField] private int _mainCorridorWidthCells = 2;
    [SerializeField] private int _secondaryCorridorWidthCells = 2;
    [SerializeField] private int _sideConnectorWidthCells = 2;
    [SerializeField] private int _coreDepthMin = 4;
    [SerializeField] private int _coreDepthMax = 5;
    [SerializeField] private int _serviceDepthCells = 2;
    [SerializeField] private int _exitDepthCells = 3;
    [SerializeField] private string[] _propPatterns = Array.Empty<string>();
    [SerializeField] private string[] _lightingProfiles = Array.Empty<string>();
    [SerializeField] private string[] _decorProfiles = Array.Empty<string>();

    public FloorStyle Style => _style;
    public int Weight => Mathf.Max(1, _weight);
    public int PlateHalfWidthMin => _plateHalfWidthMin;
    public int PlateHalfWidthMax => Mathf.Max(_plateHalfWidthMin, _plateHalfWidthMax);
    public int OpenHalfWidthMin => _openHalfWidthMin;
    public int OpenHalfWidthMax => Mathf.Max(_openHalfWidthMin, _openHalfWidthMax);
    public int ReceptionHalfWidthMin => _receptionHalfWidthMin;
    public int ReceptionHalfWidthMax => Mathf.Max(_receptionHalfWidthMin, _receptionHalfWidthMax);
    public int SouthOfficeDepthMin => _southOfficeDepthMin;
    public int SouthOfficeDepthMax => Mathf.Max(_southOfficeDepthMin, _southOfficeDepthMax);
    public int FacadeBandDepthMin => _facadeBandDepthMin;
    public int FacadeBandDepthMax => Mathf.Max(_facadeBandDepthMin, _facadeBandDepthMax);
    public int MainCorridorWidthCells => Mathf.Max(1, _mainCorridorWidthCells);
    public int SecondaryCorridorWidthCells => Mathf.Max(1, _secondaryCorridorWidthCells);
    public int SideConnectorWidthCells => Mathf.Max(1, _sideConnectorWidthCells);
    public int CoreDepthMin => _coreDepthMin;
    public int CoreDepthMax => Mathf.Max(_coreDepthMin, _coreDepthMax);
    public int ServiceDepthCells => Mathf.Max(1, _serviceDepthCells);
    public int ExitDepthCells => Mathf.Max(2, _exitDepthCells);
    public string[] PropPatterns => _propPatterns;
    public string[] LightingProfiles => _lightingProfiles;
    public string[] DecorProfiles => _decorProfiles;

    public void Configure(
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
        _style = style;
        _weight = Mathf.Max(1, weight);
        _plateHalfWidthMin = plateHalfWidthMin;
        _plateHalfWidthMax = Mathf.Max(plateHalfWidthMin, plateHalfWidthMax);
        _openHalfWidthMin = openHalfWidthMin;
        _openHalfWidthMax = Mathf.Max(openHalfWidthMin, openHalfWidthMax);
        _receptionHalfWidthMin = receptionHalfWidthMin;
        _receptionHalfWidthMax = Mathf.Max(receptionHalfWidthMin, receptionHalfWidthMax);
        _southOfficeDepthMin = southOfficeDepthMin;
        _southOfficeDepthMax = Mathf.Max(southOfficeDepthMin, southOfficeDepthMax);
        _facadeBandDepthMin = facadeBandDepthMin;
        _facadeBandDepthMax = Mathf.Max(facadeBandDepthMin, facadeBandDepthMax);
        _mainCorridorWidthCells = Mathf.Max(1, mainCorridorWidthCells);
        _secondaryCorridorWidthCells = Mathf.Max(1, secondaryCorridorWidthCells);
        _sideConnectorWidthCells = Mathf.Max(1, sideConnectorWidthCells);
        _coreDepthMin = coreDepthMin;
        _coreDepthMax = Mathf.Max(coreDepthMin, coreDepthMax);
        _serviceDepthCells = Mathf.Max(1, serviceDepthCells);
        _exitDepthCells = Mathf.Max(2, exitDepthCells);
        _propPatterns = propPatterns ?? Array.Empty<string>();
        _lightingProfiles = lightingProfiles ?? Array.Empty<string>();
        _decorProfiles = decorProfiles ?? Array.Empty<string>();
    }
}

[CreateAssetMenu(menuName = "Murino/Generation/Office Generation Rules", fileName = "OfficeGenerationRules")]
public sealed class OfficeGenerationRules : ScriptableObject
{
    [SerializeField] private FloorStyle _defaultFloorStyle = FloorStyle.CabinetHeavy;
    [SerializeField] private FloorStyleProfile[] _styleProfiles = Array.Empty<FloorStyleProfile>();
    [SerializeField] private RoomPlacementRule[] _roomRules = Array.Empty<RoomPlacementRule>();
    [SerializeField] private AdjacencyRule[] _adjacencyRules = Array.Empty<AdjacencyRule>();
    [SerializeField] private OutsideThemeProfile[] _outsideThemes = Array.Empty<OutsideThemeProfile>();
    [SerializeField] private int _doorPocketMinCells = 2;
    [SerializeField] private int _doorIntersectionClearanceCells = 1;
    [SerializeField] private int _propSpineInsetCells = 1;
    [SerializeField] private int _propWallInsetCells = 1;
    [SerializeField] private int _maxSafeFixes = 5;
    [SerializeField] private float _landmarkSpacingMetersMin = 10f;
    [SerializeField] private float _landmarkSpacingMetersMax = 20f;

    public FloorStyle DefaultFloorStyle => _defaultFloorStyle;
    public int DoorPocketMinCells => Mathf.Max(1, _doorPocketMinCells);
    public int DoorIntersectionClearanceCells => Mathf.Max(0, _doorIntersectionClearanceCells);
    public int PropSpineInsetCells => Mathf.Max(1, _propSpineInsetCells);
    public int PropWallInsetCells => Mathf.Max(0, _propWallInsetCells);
    public int MaxSafeFixes => Mathf.Max(1, _maxSafeFixes);
    public float LandmarkSpacingMetersMin => _landmarkSpacingMetersMin;
    public float LandmarkSpacingMetersMax => Mathf.Max(_landmarkSpacingMetersMin, _landmarkSpacingMetersMax);
    public OutsideThemeProfile[] OutsideThemes => _outsideThemes;

    public void Configure(
        FloorStyle defaultFloorStyle,
        FloorStyleProfile[] styleProfiles,
        RoomPlacementRule[] roomRules,
        AdjacencyRule[] adjacencyRules,
        OutsideThemeProfile[] outsideThemes,
        int doorPocketMinCells,
        int doorIntersectionClearanceCells,
        int propSpineInsetCells,
        int propWallInsetCells,
        int maxSafeFixes,
        float landmarkSpacingMetersMin,
        float landmarkSpacingMetersMax)
    {
        _defaultFloorStyle = defaultFloorStyle;
        _styleProfiles = styleProfiles ?? Array.Empty<FloorStyleProfile>();
        _roomRules = roomRules ?? Array.Empty<RoomPlacementRule>();
        _adjacencyRules = adjacencyRules ?? Array.Empty<AdjacencyRule>();
        _outsideThemes = outsideThemes ?? Array.Empty<OutsideThemeProfile>();
        _doorPocketMinCells = Mathf.Max(1, doorPocketMinCells);
        _doorIntersectionClearanceCells = Mathf.Max(0, doorIntersectionClearanceCells);
        _propSpineInsetCells = Mathf.Max(1, propSpineInsetCells);
        _propWallInsetCells = Mathf.Max(0, propWallInsetCells);
        _maxSafeFixes = Mathf.Max(1, maxSafeFixes);
        _landmarkSpacingMetersMin = landmarkSpacingMetersMin;
        _landmarkSpacingMetersMax = Mathf.Max(landmarkSpacingMetersMin, landmarkSpacingMetersMax);
    }

    public FloorStyle ResolveStyle(System.Random random, FloorStyle? overrideStyle)
    {
        if (overrideStyle.HasValue)
        {
            return overrideStyle.Value;
        }

        if (_styleProfiles == null || _styleProfiles.Length == 0)
        {
            return _defaultFloorStyle;
        }

        var totalWeight = 0;
        for (var i = 0; i < _styleProfiles.Length; i++)
        {
            if (_styleProfiles[i] != null)
            {
                totalWeight += _styleProfiles[i].Weight;
            }
        }

        if (totalWeight <= 0)
        {
            return _defaultFloorStyle;
        }

        var roll = random.Next(0, totalWeight);
        for (var i = 0; i < _styleProfiles.Length; i++)
        {
            var profile = _styleProfiles[i];
            if (profile == null)
            {
                continue;
            }

            roll -= profile.Weight;
            if (roll < 0)
            {
                return profile.Style;
            }
        }

        return _defaultFloorStyle;
    }

    public FloorStyleProfile GetStyleProfile(FloorStyle style)
    {
        if (_styleProfiles != null)
        {
            for (var i = 0; i < _styleProfiles.Length; i++)
            {
                if (_styleProfiles[i] != null && _styleProfiles[i].Style == style)
                {
                    return _styleProfiles[i];
                }
            }
        }

        if (_styleProfiles != null)
        {
            for (var i = 0; i < _styleProfiles.Length; i++)
            {
                if (_styleProfiles[i] != null)
                {
                    return _styleProfiles[i];
                }
            }
        }

        return null;
    }

    public RoomPlacementRule GetRoomRule(RoomCategory category)
    {
        if (_roomRules != null)
        {
            for (var i = 0; i < _roomRules.Length; i++)
            {
                if (_roomRules[i] != null && _roomRules[i].Category == category)
                {
                    return _roomRules[i];
                }
            }
        }

        var fallback = new RoomPlacementRule();
        fallback.Configure(category, 0.5f, 0.5f, false, category == RoomCategory.ExitShaft || category == RoomCategory.Utility);
        return fallback;
    }

    public float GetAdjacencyWeight(RoomCategory a, RoomCategory b)
    {
        if (_adjacencyRules != null)
        {
            for (var i = 0; i < _adjacencyRules.Length; i++)
            {
                var rule = _adjacencyRules[i];
                if (rule == null || !rule.Matches(a, b))
                {
                    continue;
                }

                return rule.Forbidden ? -1f : rule.Weight;
            }
        }

        return 1f;
    }

    public bool IsForbiddenAdjacency(RoomCategory a, RoomCategory b)
    {
        return GetAdjacencyWeight(a, b) < 0f;
    }

    public OutsideThemeProfile ResolveTheme(int effectiveSeed, FloorStyle style)
    {
        if (_outsideThemes == null || _outsideThemes.Length == 0)
        {
            return null;
        }

        var available = 0;
        for (var i = 0; i < _outsideThemes.Length; i++)
        {
            if (_outsideThemes[i] != null)
            {
                available++;
            }
        }

        if (available == 0)
        {
            return null;
        }

        unchecked
        {
            var index = Mathf.Abs(effectiveSeed * 17 + (int)style * 31) % available;
            for (var i = 0; i < _outsideThemes.Length; i++)
            {
                var profile = _outsideThemes[(index + i) % _outsideThemes.Length];
                if (profile != null)
                {
                    return profile;
                }
            }
        }

        return null;
    }

    public string ResolvePatternId(string[] pool, RoomCategory category, FloorStyle style, int roomIndex, string fallbackPrefix)
    {
        if (pool == null || pool.Length == 0)
        {
            return string.Format("{0}_{1}_{2}", fallbackPrefix, style, roomIndex % 3);
        }

        var hash = Mathf.Abs(((int)category + 1) * 193 + ((int)style + 1) * 389 + roomIndex * 17);
        return pool[hash % pool.Length];
    }
}
}
