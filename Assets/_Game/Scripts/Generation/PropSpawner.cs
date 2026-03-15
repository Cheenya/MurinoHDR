using System;
using System.Collections.Generic;
using UnityEngine;

namespace MurinoHDR.Generation
{

public static class PropSpawner
{
    public static void Populate(FloorResult result, ValidationConfig config, System.Random rng, PrefabLibrary prefabLibrary)
    {
        if (result == null || config == null || rng == null)
        {
            return;
        }

        var antiRepeat = new AntiRepeatState();
        var nextPropId = result.Props.Count;
        var roomDoors = BuildRoomDoorLookup(result.Doors);

        for (var roomIndex = 0; roomIndex < result.Rooms.Count; roomIndex++)
        {
            var room = result.Rooms[roomIndex];
            var definition = GenerationDefinitions.GetRoomDef(room.DetailedType);
            if (definition == null)
            {
                continue;
            }

            antiRepeat.BeginRoom();
            var pattern = definition.PickPattern(rng);
            room.PropPatternId = pattern != null ? pattern.PatternId : room.PropPatternId;
            var zones = RoomZoneGenerator.Generate(room, GetRoomDoors(roomDoors, room.RoomId), config, pattern);

            if (pattern != null)
            {
                SpawnRequirementList(result, room, zones, pattern.PatternId, pattern.RequiredSets, pattern, antiRepeat, rng, prefabLibrary, ref nextPropId, false);
                SpawnRequirementList(result, room, zones, pattern.PatternId, pattern.OptionalSets, pattern, antiRepeat, rng, prefabLibrary, ref nextPropId, true);
            }
        }
    }

    private static Dictionary<int, List<DoorInstance>> BuildRoomDoorLookup(IList<DoorInstance> doors)
    {
        var lookup = new Dictionary<int, List<DoorInstance>>();
        for (var i = 0; i < doors.Count; i++)
        {
            RegisterDoor(lookup, doors[i].RoomAId, doors[i]);
            RegisterDoor(lookup, doors[i].RoomBId, doors[i]);
        }

        return lookup;
    }

    private static IList<DoorInstance> GetRoomDoors(Dictionary<int, List<DoorInstance>> lookup, int roomId)
    {
        List<DoorInstance> doors;
        return lookup.TryGetValue(roomId, out doors) ? doors : Array.Empty<DoorInstance>();
    }

    private static void RegisterDoor(Dictionary<int, List<DoorInstance>> lookup, int roomId, DoorInstance door)
    {
        if (roomId < 0)
        {
            return;
        }

        List<DoorInstance> list;
        if (!lookup.TryGetValue(roomId, out list))
        {
            list = new List<DoorInstance>();
            lookup[roomId] = list;
        }

        list.Add(door);
    }

    private static void SpawnRequirementList(
        FloorResult result,
        RoomInstance room,
        RoomZones zones,
        string patternId,
        IReadOnlyList<PropRequirement> requirements,
        PropPatternDef pattern,
        AntiRepeatState antiRepeat,
        System.Random rng,
        PrefabLibrary prefabLibrary,
        ref int nextPropId,
        bool optional)
    {
        if (requirements == null || requirements.Count == 0)
        {
            return;
        }

        var optionalBudget = optional ? pattern.OptionalBudget : int.MaxValue;
        for (var i = 0; i < requirements.Count; i++)
        {
            if (optionalBudget <= 0)
            {
                break;
            }

            var requirement = requirements[i];
            var count = requirement.MinCount;
            if (requirement.MaxCount > requirement.MinCount)
            {
                count = rng.Next(requirement.MinCount, requirement.MaxCount + 1);
            }

            if (optional)
            {
                count = Mathf.Min(count, optionalBudget);
                optionalBudget -= count;
            }

            for (var spawnIndex = 0; spawnIndex < count; spawnIndex++)
            {
                PropInstance prop;
                if (!TryCreateProp(result, room, zones, requirement, pattern, antiRepeat, rng, prefabLibrary, nextPropId, patternId, out prop))
                {
                    continue;
                }

                result.Props.Add(prop);
                nextPropId++;
            }
        }
    }

    private static bool TryCreateProp(
        FloorResult result,
        RoomInstance room,
        RoomZones zones,
        PropRequirement requirement,
        PropPatternDef pattern,
        AntiRepeatState antiRepeat,
        System.Random rng,
        PrefabLibrary prefabLibrary,
        int propId,
        string patternId,
        out PropInstance prop)
    {
        prop = null;
        var footprint = GetPropFootprint(requirement.Category);
        var clearance = requirement.BlocksMovement ? pattern.Clearance.ComfortAisleMeters : 0.2f;
        var placedBounds = default(Bounds);
        Quaternion rotation;
        var attempts = 12;

        for (var attempt = 0; attempt < attempts; attempt++)
        {
            var center = PickPlacementPoint(zones, requirement, pattern, rng, footprint);
            rotation = requirement.PlacementHint == PlacementHint.AlongWall ? PickWallRotation(zones, center) : Quaternion.Euler(0f, rng.Next(0, 4) * 90f, 0f);
            placedBounds = BuildBounds(center, footprint, rotation);
            if (!IsPlacementValid(result, zones, placedBounds, requirement, pattern, clearance))
            {
                continue;
            }

            var prefab = prefabLibrary != null ? prefabLibrary.PickPrefab(requirement.Category, rng, antiRepeat, room.RoomId) : null;
            var prefabName = prefab != null ? prefab.name : requirement.Category.ToString();
            if (prefabLibrary == null)
            {
                antiRepeat.Register(
                    requirement.Category,
                    prefabName,
                    pattern.AntiRepeat.NoRepeatLastNInRoom,
                    pattern.AntiRepeat.NoRepeatLastNInFloor);
            }

            prop = new PropInstance
            {
                PropId = propId,
                Category = requirement.Category,
                PatternId = patternId,
                PrefabName = prefabName,
                DebugName = string.Format("{0}_{1}_{2}", room.Name, requirement.Category, propId),
                WorldBounds = placedBounds,
                Rotation = rotation,
                BlocksMovement = requirement.BlocksMovement,
                RoomId = room.RoomId,
            };
            return true;
        }

        return false;
    }

    private static Vector3 PickPlacementPoint(RoomZones zones, PropRequirement requirement, PropPatternDef pattern, System.Random rng, Vector3 footprint)
    {
        if (zones.PropZonesWorldXZ.Count == 0)
        {
            return zones.RoomBoundsWorld.center;
        }

        var zone = zones.PropZonesWorldXZ[rng.Next(0, zones.PropZonesWorldXZ.Count)];
        switch (requirement.PlacementHint)
        {
            case PlacementHint.Corner:
                return new Vector3(zone.xMin + footprint.x * 0.6f, 0f, zone.yMin + footprint.z * 0.6f);
            case PlacementHint.NearWindow:
                if (zones.HasWindowStrip)
                {
                    return new Vector3(zones.WindowStripWorldXZ.center.x, 0f, zones.WindowStripWorldXZ.center.y);
                }

                break;
            case PlacementHint.NearDoor:
                if (zones.DoorPocketsWorldXZ.Count > 0)
                {
                    var pocket = zones.DoorPocketsWorldXZ[rng.Next(0, zones.DoorPocketsWorldXZ.Count)];
                    var pocketCenter = new Vector3(pocket.center.x, 0f, pocket.center.y);
                    var roomCenter = zones.RoomBoundsWorld.center;
                    var direction = (new Vector3(roomCenter.x, 0f, roomCenter.z) - pocketCenter).normalized;
                    return pocketCenter + direction * Mathf.Max(footprint.x, footprint.z) * 0.8f;
                }

                break;
            case PlacementHint.Center:
                return new Vector3(zone.center.x, 0f, zone.center.y);
            case PlacementHint.Island:
                return new Vector3(
                    Mathf.Lerp(zone.xMin + footprint.x * 0.6f, zone.xMax - footprint.x * 0.6f, (float)rng.NextDouble()),
                    0f,
                    Mathf.Lerp(zone.yMin + footprint.z * 0.6f, zone.yMax - footprint.z * 0.6f, (float)rng.NextDouble()));
            case PlacementHint.AlongWall:
            default:
                var edge = rng.Next(0, 4);
                switch (edge)
                {
                    case 0:
                        return new Vector3(zone.center.x, 0f, zone.yMax - footprint.z * 0.55f);
                    case 1:
                        return new Vector3(zone.xMax - footprint.x * 0.55f, 0f, zone.center.y);
                    case 2:
                        return new Vector3(zone.center.x, 0f, zone.yMin + footprint.z * 0.55f);
                    default:
                        return new Vector3(zone.xMin + footprint.x * 0.55f, 0f, zone.center.y);
                }
        }

        return new Vector3(zone.center.x, 0f, zone.center.y);
    }

    private static Quaternion PickWallRotation(RoomZones zones, Vector3 center)
    {
        var room = zones.RoomBoundsWorld;
        var dxMin = Mathf.Abs(center.x - room.min.x);
        var dxMax = Mathf.Abs(center.x - room.max.x);
        var dzMin = Mathf.Abs(center.z - room.min.z);
        var dzMax = Mathf.Abs(center.z - room.max.z);
        var best = Mathf.Min(Mathf.Min(dxMin, dxMax), Mathf.Min(dzMin, dzMax));
        if (Mathf.Approximately(best, dxMin) || Mathf.Approximately(best, dxMax))
        {
            return Quaternion.Euler(0f, 90f, 0f);
        }

        return Quaternion.identity;
    }

    private static Bounds BuildBounds(Vector3 center, Vector3 footprint, Quaternion rotation)
    {
        var size = footprint;
        if (Mathf.Abs(Mathf.Round(rotation.eulerAngles.y / 90f) % 2) == 1)
        {
            size = new Vector3(footprint.z, footprint.y, footprint.x);
        }

        return new Bounds(new Vector3(center.x, size.y * 0.5f, center.z), size);
    }

    private static bool IsPlacementValid(FloorResult result, RoomZones zones, Bounds bounds, PropRequirement requirement, PropPatternDef pattern, float clearance)
    {
        var rect = BoundsToRect(bounds, clearance);
        for (var i = 0; i < zones.DoorPocketsWorldXZ.Count; i++)
        {
            if (zones.DoorPocketsWorldXZ[i].Overlaps(rect))
            {
                return false;
            }
        }

        if (requirement.BlocksMovement && pattern.PlacementRules.ForbidBlockingPropsInWalkSpine && zones.WalkSpineWorldXZ.Overlaps(rect))
        {
            return false;
        }

        if (zones.HasWindowStrip && pattern.PlacementRules.ForbidBlockingPropsInWindowStrip && IsTall(requirement.Category) && zones.WindowStripWorldXZ.Overlaps(rect))
        {
            return false;
        }

        for (var i = 0; i < result.Props.Count; i++)
        {
            if (BoundsToRect(result.Props[i].WorldBounds, clearance).Overlaps(rect))
            {
                return false;
            }
        }

        return zones.RoomBoundsWorld.Contains(new Vector3(bounds.min.x, zones.RoomBoundsWorld.center.y, bounds.min.z))
            && zones.RoomBoundsWorld.Contains(new Vector3(bounds.max.x, zones.RoomBoundsWorld.center.y, bounds.max.z));
    }

    private static Rect BoundsToRect(Bounds bounds, float padding)
    {
        return Rect.MinMaxRect(bounds.min.x - padding, bounds.min.z - padding, bounds.max.x + padding, bounds.max.z + padding);
    }

    private static bool IsTall(PropCategory category)
    {
        switch (category)
        {
            case PropCategory.Cabinet:
            case PropCategory.Shelf:
            case PropCategory.Fridge:
            case PropCategory.VendingMachine:
            case PropCategory.ServerRack:
            case PropCategory.ACUnit:
            case PropCategory.NetworkCabinet:
            case PropCategory.ElectricalPanel:
            case PropCategory.Rack:
            case PropCategory.Container:
            case PropCategory.ReceptionDesk:
                return true;
            default:
                return false;
        }
    }

    private static Vector3 GetPropFootprint(PropCategory category)
    {
        switch (category)
        {
            case PropCategory.Desk: return new Vector3(1.6f, 0.75f, 0.8f);
            case PropCategory.Chair: return new Vector3(0.65f, 0.9f, 0.65f);
            case PropCategory.Monitor: return new Vector3(0.45f, 0.35f, 0.18f);
            case PropCategory.KitchenCounter: return new Vector3(1.2f, 0.95f, 0.65f);
            case PropCategory.Microwave: return new Vector3(0.5f, 0.35f, 0.4f);
            case PropCategory.CoffeeMachine: return new Vector3(0.4f, 0.45f, 0.35f);
            case PropCategory.Fridge: return new Vector3(0.8f, 1.9f, 0.8f);
            case PropCategory.TableSmall: return new Vector3(0.9f, 0.75f, 0.9f);
            case PropCategory.TableLarge: return new Vector3(1.8f, 0.75f, 1f);
            case PropCategory.Sofa: return new Vector3(1.8f, 0.9f, 0.8f);
            case PropCategory.Armchair: return new Vector3(0.9f, 0.9f, 0.9f);
            case PropCategory.Rack: return new Vector3(1.4f, 2.2f, 0.7f);
            case PropCategory.Pallet: return new Vector3(1.2f, 0.18f, 1.2f);
            case PropCategory.Box: return new Vector3(0.45f, 0.45f, 0.45f);
            case PropCategory.Container: return new Vector3(1.4f, 1.2f, 1.2f);
            case PropCategory.HandTruck: return new Vector3(0.7f, 1.2f, 0.4f);
            case PropCategory.Pipe: return new Vector3(1.4f, 0.18f, 0.18f);
            case PropCategory.Duct: return new Vector3(1.4f, 0.32f, 0.32f);
            case PropCategory.CableTray: return new Vector3(1.2f, 0.15f, 0.2f);
            case PropCategory.VentGrate: return new Vector3(0.8f, 0.08f, 0.8f);
            case PropCategory.Ladder: return new Vector3(0.5f, 2.2f, 0.2f);
            case PropCategory.WaterCooler: return new Vector3(0.55f, 1.3f, 0.55f);
            case PropCategory.TrashBin: return new Vector3(0.4f, 0.55f, 0.4f);
            case PropCategory.PlantLarge: return new Vector3(0.7f, 1.4f, 0.7f);
            case PropCategory.PlantSmall: return new Vector3(0.35f, 0.45f, 0.35f);
            case PropCategory.ReceptionDesk: return new Vector3(2.4f, 1.1f, 1.1f);
            case PropCategory.Turnstile: return new Vector3(1f, 1f, 0.6f);
            default: return new Vector3(0.6f, 0.6f, 0.6f);
        }
    }
}
}
