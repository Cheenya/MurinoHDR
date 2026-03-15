using System;
using System.Collections.Generic;
using UnityEngine;

namespace MurinoHDR.Generation
{

[Flags]
public enum RoomTags
{
    None = 0,
    FacadeEligible = 1 << 0,
    FacadeRoom = 1 << 1,
    ResourceRoom = 1 << 2,
    LogicRoom = 1 << 3,
    Checkpoint = 1 << 4,
    SafeSpot = 1 << 5,
    MainPath = 1 << 6,
    SecondaryPath = 1 << 7,
    Tech = 1 << 8,
}

[Flags]
public enum WallTags
{
    None = 0,
    FacadeWall = 1 << 0,
    InternalWall = 1 << 1,
    WindowWall = 1 << 2,
}

[Flags]
public enum DoorTags
{
    None = 0,
    ExitDoor = 1 << 0,
    ServiceDoor = 1 << 1,
    Locked = 1 << 2,
    MainGate = 1 << 3,
}

[Flags]
public enum GridCellTags
{
    None = 0,
    Corridor = 1 << 0,
    MainCorridor = 1 << 1,
    SecondaryCorridor = 1 << 2,
    Room = 1 << 3,
    Facade = 1 << 4,
    Tech = 1 << 5,
    Door = 1 << 6,
    Window = 1 << 7,
}

public enum ExitType
{
    Lift = 0,
    Shaft = 1,
    Stairs = 2,
}

public enum PickupType
{
    Unknown = 0,
    Tape = 1,
    Keycard = 2,
    Fuse = 3,
    Crowbar = 4,
    Rope = 5,
    Lockpick = 6,
    RepairedFuse = 7,
}

public enum SupportType
{
    Resource = 0,
    Logic = 1,
    Storage = 2,
    Utility = 3,
    Security = 4,
    Electrical = 5,
    Checkpoint = 6,
}

public enum DoorOrientation
{
    Horizontal = 0,
    Vertical = 1,
}

[Serializable]
public sealed class RoomInstance
{
    public int RoomId;
    public string Name = string.Empty;
    public RoomCategory RoomType;
    public RoomTags Tags;
    public Bounds WorldBounds;
    public readonly List<int> DoorIds = new List<int>();
}

[Serializable]
public sealed class WallSegment
{
    public int WallId;
    public Vector3 AWorld;
    public Vector3 BWorld;
    public float ThicknessMeters;
    public WallTags Tags;
    public int RoomId = -1;
}

[Serializable]
public sealed class DoorInstance
{
    public int DoorId;
    public string DebugName = string.Empty;
    public Vector3 WorldPos;
    public DoorOrientation Orientation;
    public float ClearWidthMeters;
    public bool StartsClosed;
    public DoorTags Tags;
    public int RoomAId = -1;
    public int RoomBId = -1;
    public Rect WorldRectXZ;
}

[Serializable]
public sealed class PropInstance
{
    public int PropId;
    public string DebugName = string.Empty;
    public Bounds WorldBounds;
    public bool BlocksMovement;
    public int RoomId = -1;
}

[Serializable]
public sealed class ExitMarker
{
    public ExitType Type;
    public Vector3 WorldPos;
    public int RoomId = -1;
    public string DebugName = string.Empty;
}

[Serializable]
public sealed class PickupMarker
{
    public PickupType Type;
    public Vector3 WorldPos;
    public int RoomId = -1;
    public string ItemId = string.Empty;
    public bool RequiredForMainGate;
}

[Serializable]
public sealed class SupportRoomMarker
{
    public SupportType Type;
    public int RoomId = -1;
    public Vector3 RepresentativePos;
}

[Serializable]
public sealed class WindowMarker
{
    public int WindowId;
    public Vector3 WorldPos;
    public DoorOrientation Orientation;
    public float WidthMeters;
    public int RoomId = -1;
}

[Serializable]
public sealed class ValidationConfig
{
    [SerializeField] private float _cellSize = 0.25f;
    [SerializeField] private float _playerRadius = 0.35f;
    [SerializeField] private float _minCorridorWidthMainMeters = 1.2f;
    [SerializeField] private float _minCorridorWidthSecondaryMeters = 1.0f;
    [SerializeField] private float _minDoorClearWidthMeters = 0.8f;
    [SerializeField] private int _maxValidationAttempts = 5;
    [SerializeField] private int _corridorSampleStride = 3;
    [SerializeField] private float _targetFastSecondsMin = 20f;
    [SerializeField] private float _targetFastSecondsMax = 35f;
    [SerializeField] private float _targetLootSecondsMin = 45f;
    [SerializeField] private float _targetLootSecondsMax = 90f;
    [SerializeField] private bool _autoFixEnabled = true;
    [SerializeField] private int _maxAutoFixIterations = 2;
    [SerializeField] private bool _allowSingleCellTechChokepoints = true;
    [SerializeField] private float _playerSpeedMetersPerSecond = 4.5f;
    [SerializeField] private int _footprintPaddingCells = 2;

    public float CellSize => _cellSize;
    public float PlayerRadius => _playerRadius;
    public float MinCorridorWidthMainMeters => _minCorridorWidthMainMeters;
    public float MinCorridorWidthSecondaryMeters => _minCorridorWidthSecondaryMeters;
    public float MinDoorClearWidthMeters => _minDoorClearWidthMeters;
    public int MaxValidationAttempts => _maxValidationAttempts;
    public int CorridorSampleStride => _corridorSampleStride;
    public float TargetFastSecondsMin => _targetFastSecondsMin;
    public float TargetFastSecondsMax => _targetFastSecondsMax;
    public float TargetLootSecondsMin => _targetLootSecondsMin;
    public float TargetLootSecondsMax => _targetLootSecondsMax;
    public bool AutoFixEnabled => _autoFixEnabled;
    public int MaxAutoFixIterations => _maxAutoFixIterations;
    public bool AllowSingleCellTechChokepoints => _allowSingleCellTechChokepoints;
    public float PlayerSpeedMetersPerSecond => _playerSpeedMetersPerSecond;
    public int FootprintPaddingCells => _footprintPaddingCells;

    public int InflateRadiusCells => Mathf.CeilToInt(_playerRadius / _cellSize);
    public int MainCorridorMinWidthCells => Mathf.CeilToInt(_minCorridorWidthMainMeters / _cellSize);
    public int SecondaryCorridorMinWidthCells => Mathf.CeilToInt(_minCorridorWidthSecondaryMeters / _cellSize);
    public int MinDoorClearWidthCells => Mathf.CeilToInt(_minDoorClearWidthMeters / _cellSize);
}

[Serializable]
public sealed class FloorResult
{
    public int Seed;
    public int AttemptIndex;
    public Bounds FootprintWorld;
    public OutsideThemeProfile OutsideTheme;
    public readonly List<RoomInstance> Rooms = new List<RoomInstance>();
    public readonly List<WallSegment> Walls = new List<WallSegment>();
    public readonly List<DoorInstance> Doors = new List<DoorInstance>();
    public readonly List<PropInstance> Props = new List<PropInstance>();
    public readonly List<ExitMarker> Exits = new List<ExitMarker>();
    public readonly List<PickupMarker> Pickups = new List<PickupMarker>();
    public readonly List<SupportRoomMarker> SupportRooms = new List<SupportRoomMarker>();
    public readonly List<WindowMarker> Windows = new List<WindowMarker>();
    public Vector3 SpawnWorld;
    public ValidationReport ValidationReport;

    public RoomInstance GetRoom(int roomId)
    {
        for (var i = 0; i < Rooms.Count; i++)
        {
            if (Rooms[i].RoomId == roomId)
            {
                return Rooms[i];
            }
        }

        return null;
    }
}
}

