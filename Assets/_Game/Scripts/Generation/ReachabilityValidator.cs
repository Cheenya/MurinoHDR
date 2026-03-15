using System.Collections.Generic;
using UnityEngine;

namespace MurinoHDR.Generation
{

public static class ReachabilityValidator
{
    public static List<ValidationError> ValidateReachability(
        GridMap2D grid,
        bool[] reachableMask,
        int[] distanceFromSpawn,
        FloorResult result,
        ValidationConfig config)
    {
        var errors = new List<ValidationError>();
        if (grid == null || result == null)
        {
            return errors;
        }

        var spawnCell = grid.WorldToCell(result.SpawnWorld);
        if (!grid.InBounds(spawnCell) || !reachableMask[grid.Index(spawnCell.x, spawnCell.y)])
        {
            errors.Add(new ValidationError
            {
                Code = ValidationErrorCode.SpawnNotWalkable,
                Severity = ValidationSeverity.Fatal,
                Message = "Spawn находится вне walkable mask",
                Cell = spawnCell,
                WorldPos = result.SpawnWorld,
                SuggestedFix = SuggestedFix.RegenerateFloor,
            });
            return errors;
        }

        ValidateExits(grid, reachableMask, result, errors);
        ValidatePickups(grid, reachableMask, result, errors);
        ValidateSupportRooms(grid, reachableMask, result, errors);
        ValidateCheckpointAndSafeSpot(result, errors);
        ValidateFastAndLootTiming(grid, distanceFromSpawn, result, config, errors);
        ValidateActiveExit(result, errors);

        return errors;
    }

    private static void ValidateExits(GridMap2D grid, bool[] reachableMask, FloorResult result, List<ValidationError> errors)
    {
        for (var i = 0; i < result.Exits.Count; i++)
        {
            var exit = result.Exits[i];
            var cell = grid.WorldToCell(exit.WorldPos);
            if (grid.InBounds(cell) && reachableMask[grid.Index(cell.x, cell.y)])
            {
                continue;
            }

            errors.Add(new ValidationError
            {
                Code = ValidationErrorCode.ExitUnreachable,
                Severity = ValidationSeverity.Fatal,
                Message = string.Format("Выход {0} недостижим по walkmap", exit.DebugName),
                Cell = cell,
                WorldPos = exit.WorldPos,
                RoomId = exit.RoomId,
                SuggestedFix = SuggestedFix.RegenerateFloor,
            });
        }
    }

    private static void ValidatePickups(GridMap2D grid, bool[] reachableMask, FloorResult result, List<ValidationError> errors)
    {
        for (var i = 0; i < result.Pickups.Count; i++)
        {
            var pickup = result.Pickups[i];
            var cell = grid.WorldToCell(pickup.WorldPos);
            if (grid.InBounds(cell) && reachableMask[grid.Index(cell.x, cell.y)])
            {
                continue;
            }

            errors.Add(new ValidationError
            {
                Code = ValidationErrorCode.PickupUnreachable,
                Severity = ValidationSeverity.Error,
                Message = string.Format("Pickup {0} недостижим", pickup.ItemId),
                Cell = cell,
                WorldPos = pickup.WorldPos,
                RoomId = pickup.RoomId,
                SuggestedFix = SuggestedFix.RelocatePickup,
            });
        }
    }

    private static void ValidateSupportRooms(GridMap2D grid, bool[] reachableMask, FloorResult result, List<ValidationError> errors)
    {
        for (var i = 0; i < result.SupportRooms.Count; i++)
        {
            var support = result.SupportRooms[i];
            var cell = grid.WorldToCell(support.RepresentativePos);
            if (grid.InBounds(cell) && reachableMask[grid.Index(cell.x, cell.y)])
            {
                continue;
            }

            errors.Add(new ValidationError
            {
                Code = ValidationErrorCode.SupportRoomIsolated,
                Severity = ValidationSeverity.Error,
                Message = string.Format("Support room {0} изолирована", support.Type),
                Cell = cell,
                WorldPos = support.RepresentativePos,
                RoomId = support.RoomId,
                SuggestedFix = SuggestedFix.AddDoorConnection,
            });
        }
    }

    private static void ValidateCheckpointAndSafeSpot(FloorResult result, List<ValidationError> errors)
    {
        var checkpoints = 0;
        var safeSpots = 0;
        for (var i = 0; i < result.Rooms.Count; i++)
        {
            var tags = result.Rooms[i].Tags;
            if ((tags & RoomTags.Checkpoint) != 0)
            {
                checkpoints++;
            }

            if ((tags & RoomTags.SafeSpot) != 0)
            {
                safeSpots++;
            }
        }

        if (checkpoints == 0)
        {
            errors.Add(new ValidationError
            {
                Code = ValidationErrorCode.MissingCheckpoint,
                Severity = ValidationSeverity.Error,
                Message = "На этаже нет checkpoint room",
                SuggestedFix = SuggestedFix.RegenerateFloor,
            });
        }

        if (safeSpots < 1)
        {
            errors.Add(new ValidationError
            {
                Code = ValidationErrorCode.MissingSafeSpot,
                Severity = ValidationSeverity.Warning,
                Message = "На этаже нет safe spot возле окна/света",
                SuggestedFix = SuggestedFix.None,
            });
        }
    }

    private static void ValidateFastAndLootTiming(GridMap2D grid, int[] distanceFromSpawn, FloorResult result, ValidationConfig config, List<ValidationError> errors)
    {
        if (distanceFromSpawn == null || distanceFromSpawn.Length == 0)
        {
            return;
        }

        var fastDistanceCells = int.MaxValue;
        for (var i = 0; i < result.Exits.Count; i++)
        {
            var cell = grid.WorldToCell(result.Exits[i].WorldPos);
            if (!grid.InBounds(cell))
            {
                continue;
            }

            var value = distanceFromSpawn[grid.Index(cell.x, cell.y)];
            if (value >= 0)
            {
                fastDistanceCells = Mathf.Min(fastDistanceCells, value);
            }
        }

        if (fastDistanceCells < int.MaxValue)
        {
            ValidateRouteTime(config, errors, ValidationErrorCode.RouteTooShort, ValidationErrorCode.RouteTooLong, "FastPath", fastDistanceCells);
        }

        var lootDistanceCells = 0;
        for (var i = 0; i < result.Pickups.Count; i++)
        {
            if (!result.Pickups[i].RequiredForMainGate)
            {
                continue;
            }

            var cell = grid.WorldToCell(result.Pickups[i].WorldPos);
            if (!grid.InBounds(cell))
            {
                continue;
            }

            var value = distanceFromSpawn[grid.Index(cell.x, cell.y)];
            if (value > lootDistanceCells)
            {
                lootDistanceCells = value;
            }
        }

        if (lootDistanceCells > 0)
        {
            ValidateRouteTime(config, errors, ValidationErrorCode.RouteTooShort, ValidationErrorCode.RouteTooLong, "LootPath", lootDistanceCells);
        }
    }

    private static void ValidateRouteTime(
        ValidationConfig config,
        List<ValidationError> errors,
        ValidationErrorCode shortCode,
        ValidationErrorCode longCode,
        string routeName,
        int distanceCells)
    {
        var meters = distanceCells * config.CellSize;
        var seconds = meters / Mathf.Max(0.01f, config.PlayerSpeedMetersPerSecond);
        if (routeName == "FastPath")
        {
            if (seconds < config.TargetFastSecondsMin)
            {
                errors.Add(new ValidationError
                {
                    Code = shortCode,
                    Severity = ValidationSeverity.Warning,
                    Message = string.Format("{0} слишком короткий: {1:0.0}s", routeName, seconds),
                });
            }
            else if (seconds > config.TargetFastSecondsMax)
            {
                errors.Add(new ValidationError
                {
                    Code = longCode,
                    Severity = ValidationSeverity.Warning,
                    Message = string.Format("{0} слишком длинный: {1:0.0}s", routeName, seconds),
                });
            }
        }
        else
        {
            if (seconds < config.TargetLootSecondsMin)
            {
                errors.Add(new ValidationError
                {
                    Code = shortCode,
                    Severity = ValidationSeverity.Warning,
                    Message = string.Format("{0} слишком короткий: {1:0.0}s", routeName, seconds),
                });
            }
            else if (seconds > config.TargetLootSecondsMax)
            {
                errors.Add(new ValidationError
                {
                    Code = longCode,
                    Severity = ValidationSeverity.Warning,
                    Message = string.Format("{0} слишком длинный: {1:0.0}s", routeName, seconds),
                });
            }
        }
    }

    private static void ValidateActiveExit(FloorResult result, List<ValidationError> errors)
    {
        var hasKeycard = false;
        var hasFuse = false;
        var hasTape = false;
        var hasCrowbar = false;
        var hasRope = false;
        var hasLockpick = false;
        for (var i = 0; i < result.Pickups.Count; i++)
        {
            switch (result.Pickups[i].Type)
            {
                case PickupType.Keycard:
                    hasKeycard = true;
                    break;
                case PickupType.Fuse:
                    hasFuse = true;
                    break;
                case PickupType.Tape:
                    hasTape = true;
                    break;
                case PickupType.Crowbar:
                    hasCrowbar = true;
                    break;
                case PickupType.Rope:
                    hasRope = true;
                    break;
                case PickupType.Lockpick:
                    hasLockpick = true;
                    break;
            }
        }

        var elevatorPossible = hasKeycard && hasFuse && hasTape;
        var shaftPossible = hasCrowbar && hasRope;
        var stairsPossible = hasLockpick;
        if (elevatorPossible || shaftPossible || stairsPossible)
        {
            return;
        }

        errors.Add(new ValidationError
        {
            Code = ValidationErrorCode.NoActiveExitOnFloor,
            Severity = ValidationSeverity.Fatal,
            Message = "На текущем этаже нельзя открыть ни один из выходов имеющимися предметами",
            SuggestedFix = SuggestedFix.RegenerateFloor,
        });
    }
}
}

