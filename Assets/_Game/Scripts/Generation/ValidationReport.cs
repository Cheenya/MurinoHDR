using System;
using System.Collections.Generic;
using UnityEngine;

namespace MurinoHDR.Generation
{

public enum ValidationSeverity
{
    Info = 0,
    Warning = 1,
    Error = 2,
    Fatal = 3,
}

public enum ValidationErrorCode
{
    ExitUnreachable = 0,
    PickupUnreachable = 1,
    SupportRoomIsolated = 2,
    DoorSealedByObstacle = 3,
    DoorNotConnectingTwoZones = 4,
    DoorConnectsMoreThanTwoZones = 5,
    DoorSideNotWalkable = 6,
    CorridorTooNarrow = 7,
    ForbiddenChokepoint = 8,
    SpawnNotWalkable = 9,
    InvalidFootprintOrGrid = 10,
    RoomDisconnectedFromCorridor = 11,
    WindowNotOnFacade = 12,
    FacadeRoomMissingWindow = 13,
    NonDeterministicDataDetected = 14,
    RouteTooShort = 15,
    RouteTooLong = 16,
    NoActiveExitOnFloor = 17,
    KeyPlacedAfterGate = 18,
    MissingCheckpoint = 19,
    MissingSafeSpot = 20,
    NoAlternativeLoop = 21,
    DoorTooNarrow = 22,
}

public enum SuggestedFix
{
    None = 0,
    RemoveBlockingProp = 1,
    NudgeBlockingProp = 2,
    OpenOrWidenDoor = 3,
    RelocatePickup = 4,
    AddDoorConnection = 5,
    ConvertDoorToAlwaysOpen = 6,
    WidenCorridor = 7,
    RemoveRoom = 8,
    RebuildSubgraph = 9,
    RegenerateFloor = 10,
}

[Serializable]
public sealed class ValidationError
{
    public ValidationErrorCode Code;
    public ValidationSeverity Severity;
    public string Message = string.Empty;
    public Vector2Int Cell = new Vector2Int(-1, -1);
    public Vector3 WorldPos;
    public int RoomId = -1;
    public int DoorId = -1;
    public int PropId = -1;
    public int ZoneA = -1;
    public int ZoneB = -1;
    public int ExpectedMinWidthCells = -1;
    public int ActualWidthCells = -1;
    public SuggestedFix SuggestedFix = SuggestedFix.None;

    public override string ToString()
    {
        return string.Format(
            "{0}/{1}: {2} cell={3} room={4} door={5} prop={6}",
            Severity,
            Code,
            Message,
            Cell,
            RoomId,
            DoorId,
            PropId);
    }
}

[Serializable]
public sealed class ValidationReport
{
    public int Seed;
    public int AttemptIndex;
    public int GridWidth;
    public int GridHeight;
    public float CellSize;
    public bool Success;
    public int ErrorCount;
    public int FatalCount;
    public readonly List<ValidationError> Errors = new List<ValidationError>();

    [NonSerialized] public GridMap2D Grid;
    [NonSerialized] public bool[] Walkable;
    [NonSerialized] public bool[] Reachable;
    [NonSerialized] public int[] ZoneId;
    [NonSerialized] public int[] DistanceFromSpawn;

    public void Add(ValidationError error)
    {
        if (error == null)
        {
            return;
        }

        Errors.Add(error);
        if (error.Severity == ValidationSeverity.Error)
        {
            ErrorCount++;
        }
        else if (error.Severity == ValidationSeverity.Fatal)
        {
            FatalCount++;
        }
    }

    public void Add(
        ValidationErrorCode code,
        ValidationSeverity severity,
        string message,
        Vector2Int cell,
        Vector3 worldPos,
        int roomId,
        int doorId,
        int propId,
        SuggestedFix suggestedFix)
    {
        Add(new ValidationError
        {
            Code = code,
            Severity = severity,
            Message = message,
            Cell = cell,
            WorldPos = worldPos,
            RoomId = roomId,
            DoorId = doorId,
            PropId = propId,
            SuggestedFix = suggestedFix,
        });
    }

    public IReadOnlyList<ValidationError> FatalErrors()
    {
        var result = new List<ValidationError>();
        for (var i = 0; i < Errors.Count; i++)
        {
            if (Errors[i].Severity == ValidationSeverity.Fatal)
            {
                result.Add(Errors[i]);
            }
        }

        return result;
    }

    public IReadOnlyList<ValidationError> FixableErrors()
    {
        var result = new List<ValidationError>();
        for (var i = 0; i < Errors.Count; i++)
        {
            var error = Errors[i];
            if (error.Severity != ValidationSeverity.Error)
            {
                continue;
            }

            if (error.SuggestedFix != SuggestedFix.None && error.SuggestedFix != SuggestedFix.RegenerateFloor)
            {
                result.Add(error);
            }
        }

        return result;
    }

    public string BuildSummary()
    {
        if (Errors.Count == 0)
        {
            return string.Format("Seed {0} attempt {1}: OK ({2}x{3} @ {4:0.##}m)", Seed, AttemptIndex, GridWidth, GridHeight, CellSize);
        }

        var count = Mathf.Min(4, Errors.Count);
        var parts = new string[count];
        for (var i = 0; i < count; i++)
        {
            parts[i] = string.Format("{0}:{1}", Errors[i].Code, Errors[i].Message);
        }

        return string.Format(
            "Seed {0} attempt {1}: {2} error(s), {3} fatal(s) | {4}",
            Seed,
            AttemptIndex,
            ErrorCount,
            FatalCount,
            string.Join(" | ", parts));
    }
}
}

