using System;
using System.Collections.Generic;
using MurinoHDR.Core;
using UnityEngine;

namespace MurinoHDR.Generation
{

public static class FloorResultBuilder
{
    private enum BoundaryKind
    {
        None = 0,
        SolidWall = 1,
        OfficeDoor = 2,
        ServiceDoor = 3,
        ExitDoor = 4,
        ElevatorPortal = 5,
    }

    private static readonly (string itemId, PickupType type, RoomCategory roomCategory, Vector3 offset, bool mainGate)[] PickupPlan =
    {
        ("tape", PickupType.Tape, RoomCategory.Start, new Vector3(0.9f, 0f, 0.85f), true),
        ("keycard", PickupType.Keycard, RoomCategory.Office, new Vector3(0.85f, 0f, 0.65f), true),
        ("fuse", PickupType.Fuse, RoomCategory.Utility, new Vector3(-0.8f, 0f, 0.55f), true),
        ("crowbar", PickupType.Crowbar, RoomCategory.Storage, new Vector3(-0.7f, 0f, 0.7f), false),
        ("rope", PickupType.Rope, RoomCategory.Utility, new Vector3(0.8f, 0f, -0.55f), false),
        ("lockpick", PickupType.Lockpick, RoomCategory.Office, new Vector3(-0.85f, 0f, -0.55f), false),
    };

    public static FloorResult Build(FloorGenerationResult generated, FloorGeneratorSettings settings)
    {
        var result = new FloorResult();
        result.Seed = generated.Seed;
        result.AttemptIndex = generated.AttemptIndex;
        result.OutsideTheme = settings.OutsideThemeProfile;

        var layoutBounds = GetLayoutBounds(generated, settings);
        result.FootprintWorld = layoutBounds;

        var roomLookup = new Dictionary<int, RoomInstance>();
        var coarseBounds = GetCoarseLayoutBounds(generated);
        for (var i = 0; i < generated.Rooms.Count; i++)
        {
            var room = generated.Rooms[i];
            var worldBounds = GetRoomWorldBounds(room.Rect, generated.WorldOffset, settings);
            var tags = BuildRoomTags(room, coarseBounds);
            var instance = new RoomInstance
            {
                RoomId = room.Index,
                Name = room.InstanceId,
                RoomType = room.Category,
                Tags = tags,
                WorldBounds = worldBounds,
            };
            result.Rooms.Add(instance);
            roomLookup[instance.RoomId] = instance;

            if ((tags & RoomTags.ResourceRoom) != 0)
            {
                result.SupportRooms.Add(new SupportRoomMarker
                {
                    Type = SupportType.Resource,
                    RoomId = instance.RoomId,
                    RepresentativePos = worldBounds.center,
                });
            }

            if ((tags & RoomTags.LogicRoom) != 0)
            {
                result.SupportRooms.Add(new SupportRoomMarker
                {
                    Type = room.Category == RoomCategory.Utility ? SupportType.Electrical : SupportType.Logic,
                    RoomId = instance.RoomId,
                    RepresentativePos = worldBounds.center,
                });
            }

            if ((tags & RoomTags.Checkpoint) != 0)
            {
                result.SupportRooms.Add(new SupportRoomMarker
                {
                    Type = SupportType.Checkpoint,
                    RoomId = instance.RoomId,
                    RepresentativePos = worldBounds.center,
                });
            }
        }

        BuildBoundariesAndDoors(generated, settings, result, roomLookup);
        BuildWindows(generated, settings, result, coarseBounds);
        BuildSpawnAndMarkers(generated, settings, result);
        BuildPickups(generated, settings, result);
        return result;
    }

    private static void BuildSpawnAndMarkers(FloorGenerationResult generated, FloorGeneratorSettings settings, FloorResult result)
    {
        var startRoom = generated.GetRoom(RoomCategory.Start);
        if (startRoom != null)
        {
            var min = GetRoomWorldMin(startRoom.Rect, settings, generated.WorldOffset);
            var center = GetRoomWorldCenter(startRoom.Rect, settings, generated.WorldOffset);
            result.SpawnWorld = new Vector3(center.x, 1.05f, min.z + 1.6f);
        }

        AddExitMarker(generated, settings, result, RoomCategory.ExitElevator, ExitType.Lift, "ElevatorLobby");
        AddExitMarker(generated, settings, result, RoomCategory.ExitShaft, ExitType.Shaft, "ShaftEntry");
        AddExitMarker(generated, settings, result, RoomCategory.ExitStairs, ExitType.Stairs, "StairsDoor");
    }

    private static void AddExitMarker(FloorGenerationResult generated, FloorGeneratorSettings settings, FloorResult result, RoomCategory category, ExitType exitType, string debugName)
    {
        var room = generated.GetRoom(category);
        if (room == null)
        {
            return;
        }

        var center = GetRoomWorldCenter(room.Rect, settings, generated.WorldOffset);
        result.Exits.Add(new ExitMarker
        {
            Type = exitType,
            WorldPos = center,
            RoomId = room.Index,
            DebugName = debugName,
        });
    }

    private static void BuildPickups(FloorGenerationResult generated, FloorGeneratorSettings settings, FloorResult result)
    {
        for (var i = 0; i < PickupPlan.Length; i++)
        {
            var room = FindPickupRoom(generated, PickupPlan[i].roomCategory, i);
            if (room == null)
            {
                continue;
            }

            result.Pickups.Add(new PickupMarker
            {
                Type = PickupPlan[i].type,
                WorldPos = GetRoomWorldCenter(room.Rect, settings, generated.WorldOffset) + PickupPlan[i].offset,
                RoomId = room.Index,
                ItemId = PickupPlan[i].itemId,
                RequiredForMainGate = PickupPlan[i].mainGate,
            });
        }
    }

    private static GeneratedRoom FindPickupRoom(FloorGenerationResult generated, RoomCategory preferredCategory, int index)
    {
        var matches = new List<GeneratedRoom>();
        for (var i = 0; i < generated.Rooms.Count; i++)
        {
            if (generated.Rooms[i].Category == preferredCategory)
            {
                matches.Add(generated.Rooms[i]);
            }
        }

        return matches.Count > 0 ? matches[index % matches.Count] : null;
    }

    private static void BuildBoundariesAndDoors(
        FloorGenerationResult generated,
        FloorGeneratorSettings settings,
        FloorResult result,
        Dictionary<int, RoomInstance> roomLookup)
    {
        var occupancy = BuildOccupancyLookup(generated);
        var wallId = 0;
        var doorId = 0;
        for (var i = 0; i < generated.Rooms.Count; i++)
        {
            var room = generated.Rooms[i];
            for (var x = room.Rect.xMin; x < room.Rect.xMax; x++)
            {
                for (var y = room.Rect.yMin; y < room.Rect.yMax; y++)
                {
                    var cell = new Vector2Int(x, y);
                    ProcessBoundary(room, cell, RoomSide.North, generated, settings, result, roomLookup, occupancy, ref wallId, ref doorId);
                    ProcessBoundary(room, cell, RoomSide.East, generated, settings, result, roomLookup, occupancy, ref wallId, ref doorId);
                    if (!occupancy.ContainsKey(cell + Vector2Int.down))
                    {
                        ProcessBoundary(room, cell, RoomSide.South, generated, settings, result, roomLookup, occupancy, ref wallId, ref doorId);
                    }

                    if (!occupancy.ContainsKey(cell + Vector2Int.left))
                    {
                        ProcessBoundary(room, cell, RoomSide.West, generated, settings, result, roomLookup, occupancy, ref wallId, ref doorId);
                    }
                }
            }
        }
    }

    private static void ProcessBoundary(
        GeneratedRoom room,
        Vector2Int cell,
        RoomSide side,
        FloorGenerationResult generated,
        FloorGeneratorSettings settings,
        FloorResult result,
        Dictionary<int, RoomInstance> roomLookup,
        Dictionary<Vector2Int, GeneratedRoom> occupancy,
        ref int wallId,
        ref int doorId)
    {
        GeneratedRoom otherRoom;
        occupancy.TryGetValue(cell + GetOffset(side), out otherRoom);
        var boundary = DetermineBoundary(room, otherRoom);
        if (boundary == BoundaryKind.None)
        {
            return;
        }

        var endpoints = GetEdgeEndpoints(cell, side, settings, generated.WorldOffset);
        result.Walls.Add(new WallSegment
        {
            WallId = wallId++,
            AWorld = endpoints.Item1,
            BWorld = endpoints.Item2,
            ThicknessMeters = settings.WallThickness,
            Tags = otherRoom == null ? WallTags.FacadeWall : WallTags.InternalWall,
            RoomId = room.Index,
        });

        if (boundary == BoundaryKind.SolidWall)
        {
            return;
        }

        var clearWidth = GetDoorWidth(boundary, settings);
        var worldPos = GetEdgeCenter(cell, side, settings, generated.WorldOffset);
        var orientation = side == RoomSide.North || side == RoomSide.South ? DoorOrientation.Horizontal : DoorOrientation.Vertical;
        var doorRect = BuildDoorRect(worldPos, orientation, clearWidth, settings.WallThickness);
        var door = new DoorInstance
        {
            DoorId = doorId++,
            DebugName = string.Format("{0}_{1}_{2}_{3}", boundary, room.InstanceId, cell.x, cell.y),
            WorldPos = worldPos,
            Orientation = orientation,
            ClearWidthMeters = clearWidth,
            StartsClosed = false,
            Tags = GetDoorTags(boundary),
            RoomAId = room.Index,
            RoomBId = otherRoom != null ? otherRoom.Index : -1,
            WorldRectXZ = doorRect,
        };
        result.Doors.Add(door);
        if (roomLookup.ContainsKey(room.Index))
        {
            roomLookup[room.Index].DoorIds.Add(door.DoorId);
        }

        if (otherRoom != null && roomLookup.ContainsKey(otherRoom.Index))
        {
            roomLookup[otherRoom.Index].DoorIds.Add(door.DoorId);
        }
    }

    private static void BuildWindows(FloorGenerationResult generated, FloorGeneratorSettings settings, FloorResult result, RectInt coarseBounds)
    {
        var windowId = 0;
        for (var i = 0; i < generated.Rooms.Count; i++)
        {
            var room = generated.Rooms[i];
            var roomInstance = result.GetRoom(room.Index);
            if (roomInstance == null || (roomInstance.Tags & RoomTags.FacadeRoom) == 0)
            {
                continue;
            }

            RoomSide bestSide;
            if (!TryGetWindowSide(room.Rect, coarseBounds, out bestSide))
            {
                continue;
            }

            var lengthMeters = (bestSide == RoomSide.North || bestSide == RoomSide.South ? room.Rect.width : room.Rect.height) * settings.CellSize;
            var widthMeters = Mathf.Clamp(lengthMeters - 1.0f, 1.6f, 3.2f);
            var worldPos = GetRoomSideCenter(room.Rect, bestSide, settings, generated.WorldOffset);
            result.Windows.Add(new WindowMarker
            {
                WindowId = windowId++,
                RoomId = room.Index,
                WorldPos = worldPos,
                Orientation = bestSide == RoomSide.North || bestSide == RoomSide.South ? DoorOrientation.Horizontal : DoorOrientation.Vertical,
                WidthMeters = widthMeters,
            });
        }
    }

    private static bool TryGetWindowSide(RectInt rect, RectInt bounds, out RoomSide side)
    {
        if (rect.yMin == bounds.yMin)
        {
            side = RoomSide.South;
            return true;
        }

        if (rect.yMax == bounds.yMax)
        {
            side = RoomSide.North;
            return true;
        }

        if (rect.xMin == bounds.xMin)
        {
            side = RoomSide.West;
            return true;
        }

        if (rect.xMax == bounds.xMax)
        {
            side = RoomSide.East;
            return true;
        }

        side = RoomSide.North;
        return false;
    }

    private static BoundaryKind DetermineBoundary(GeneratedRoom room, GeneratedRoom otherRoom)
    {
        if (otherRoom == null)
        {
            return BoundaryKind.SolidWall;
        }

        if (room.Index == otherRoom.Index)
        {
            return BoundaryKind.None;
        }

        if (IsOpenZone(room.Category) && IsOpenZone(otherRoom.Category))
        {
            return BoundaryKind.None;
        }

        if ((room.Category == RoomCategory.ExitElevator && IsOpenZone(otherRoom.Category)) ||
            (otherRoom.Category == RoomCategory.ExitElevator && IsOpenZone(room.Category)))
        {
            return BoundaryKind.ElevatorPortal;
        }

        if ((room.Category == RoomCategory.ExitShaft && IsOpenZone(otherRoom.Category)) ||
            (otherRoom.Category == RoomCategory.ExitShaft && IsOpenZone(room.Category)))
        {
            return BoundaryKind.ServiceDoor;
        }

        if ((room.Category == RoomCategory.ExitStairs && IsOpenZone(otherRoom.Category)) ||
            (otherRoom.Category == RoomCategory.ExitStairs && IsOpenZone(room.Category)))
        {
            return BoundaryKind.ExitDoor;
        }

        if ((IsOpenZone(room.Category) && IsEnclosedProgram(otherRoom.Category)) ||
            (IsOpenZone(otherRoom.Category) && IsEnclosedProgram(room.Category)))
        {
            return BoundaryKind.OfficeDoor;
        }

        return BoundaryKind.SolidWall;
    }

    private static DoorTags GetDoorTags(BoundaryKind boundary)
    {
        switch (boundary)
        {
            case BoundaryKind.ServiceDoor:
                return DoorTags.ServiceDoor;
            case BoundaryKind.ExitDoor:
                return DoorTags.ExitDoor;
            case BoundaryKind.ElevatorPortal:
                return DoorTags.ExitDoor | DoorTags.MainGate;
            default:
                return DoorTags.None;
        }
    }

    private static float GetDoorWidth(BoundaryKind boundary, FloorGeneratorSettings settings)
    {
        switch (boundary)
        {
            case BoundaryKind.ServiceDoor:
            case BoundaryKind.ExitDoor:
                return Mathf.Min(1.8f, settings.CellSize - 0.35f);
            case BoundaryKind.ElevatorPortal:
                return Mathf.Min(2.1f, settings.CellSize - 0.2f);
            default:
                return Mathf.Min(1.55f, settings.CellSize - 0.45f);
        }
    }

    private static RoomTags BuildRoomTags(GeneratedRoom room, RectInt coarseBounds)
    {
        var tags = RoomTags.None;
        switch (room.Category)
        {
            case RoomCategory.Start:
                tags |= RoomTags.LogicRoom | RoomTags.Checkpoint | RoomTags.SafeSpot | RoomTags.MainPath;
                break;
            case RoomCategory.Corridor:
                tags |= room.InstanceId == "MainCorridor" || room.InstanceId == "SouthCrossCorridor" || room.InstanceId == "ServiceCorridor"
                    ? RoomTags.MainPath
                    : RoomTags.SecondaryPath;
                break;
            case RoomCategory.Hub:
                tags |= RoomTags.MainPath | RoomTags.SafeSpot;
                break;
            case RoomCategory.Office:
                tags |= RoomTags.FacadeEligible;
                break;
            case RoomCategory.Storage:
                tags |= RoomTags.ResourceRoom;
                break;
            case RoomCategory.Utility:
                tags |= RoomTags.LogicRoom | RoomTags.Tech;
                break;
            case RoomCategory.ExitShaft:
                tags |= RoomTags.Tech;
                break;
        }

        if ((room.Category == RoomCategory.Start || room.Category == RoomCategory.Office) && TouchesPerimeter(room.Rect, coarseBounds))
        {
            tags |= RoomTags.FacadeEligible | RoomTags.FacadeRoom;
        }

        return tags;
    }

    private static bool IsOpenZone(RoomCategory category)
    {
        return category == RoomCategory.Start || category == RoomCategory.Corridor || category == RoomCategory.Hub;
    }

    private static bool IsEnclosedProgram(RoomCategory category)
    {
        return category == RoomCategory.Office ||
               category == RoomCategory.Storage ||
               category == RoomCategory.Utility ||
               category == RoomCategory.ExitElevator ||
               category == RoomCategory.ExitShaft ||
               category == RoomCategory.ExitStairs;
    }

    private static Dictionary<Vector2Int, GeneratedRoom> BuildOccupancyLookup(FloorGenerationResult generated)
    {
        var occupancy = new Dictionary<Vector2Int, GeneratedRoom>();
        for (var i = 0; i < generated.Rooms.Count; i++)
        {
            var room = generated.Rooms[i];
            for (var x = room.Rect.xMin; x < room.Rect.xMax; x++)
            {
                for (var y = room.Rect.yMin; y < room.Rect.yMax; y++)
                {
                    occupancy[new Vector2Int(x, y)] = room;
                }
            }
        }

        return occupancy;
    }

    private static Rect BuildDoorRect(Vector3 worldPos, DoorOrientation orientation, float clearWidth, float thickness)
    {
        if (orientation == DoorOrientation.Horizontal)
        {
            return new Rect(worldPos.x - clearWidth * 0.5f, worldPos.z - thickness * 0.5f, clearWidth, thickness);
        }

        return new Rect(worldPos.x - thickness * 0.5f, worldPos.z - clearWidth * 0.5f, thickness, clearWidth);
    }

    private static Tuple<Vector3, Vector3> GetEdgeEndpoints(Vector2Int cell, RoomSide side, FloorGeneratorSettings settings, Vector2 worldOffset)
    {
        var minX = cell.x * settings.CellSize + worldOffset.x;
        var maxX = (cell.x + 1) * settings.CellSize + worldOffset.x;
        var minZ = cell.y * settings.CellSize + worldOffset.y;
        var maxZ = (cell.y + 1) * settings.CellSize + worldOffset.y;
        switch (side)
        {
            case RoomSide.North:
                return Tuple.Create(new Vector3(minX, 0f, maxZ), new Vector3(maxX, 0f, maxZ));
            case RoomSide.South:
                return Tuple.Create(new Vector3(minX, 0f, minZ), new Vector3(maxX, 0f, minZ));
            case RoomSide.East:
                return Tuple.Create(new Vector3(maxX, 0f, minZ), new Vector3(maxX, 0f, maxZ));
            default:
                return Tuple.Create(new Vector3(minX, 0f, minZ), new Vector3(minX, 0f, maxZ));
        }
    }

    private static Vector3 GetEdgeCenter(Vector2Int cell, RoomSide side, FloorGeneratorSettings settings, Vector2 worldOffset)
    {
        var minX = cell.x * settings.CellSize + worldOffset.x;
        var maxX = (cell.x + 1) * settings.CellSize + worldOffset.x;
        var minZ = cell.y * settings.CellSize + worldOffset.y;
        var maxZ = (cell.y + 1) * settings.CellSize + worldOffset.y;
        switch (side)
        {
            case RoomSide.North:
                return new Vector3((minX + maxX) * 0.5f, 0f, maxZ - settings.WallThickness * 0.5f);
            case RoomSide.South:
                return new Vector3((minX + maxX) * 0.5f, 0f, minZ + settings.WallThickness * 0.5f);
            case RoomSide.East:
                return new Vector3(maxX - settings.WallThickness * 0.5f, 0f, (minZ + maxZ) * 0.5f);
            default:
                return new Vector3(minX + settings.WallThickness * 0.5f, 0f, (minZ + maxZ) * 0.5f);
        }
    }

    private static Vector3 GetRoomSideCenter(RectInt rect, RoomSide side, FloorGeneratorSettings settings, Vector2 worldOffset)
    {
        var min = GetRoomWorldMin(rect, settings, worldOffset);
        var max = GetRoomWorldMax(rect, settings, worldOffset);
        switch (side)
        {
            case RoomSide.North:
                return new Vector3((min.x + max.x) * 0.5f, 1.6f, max.z - settings.WallThickness * 0.5f);
            case RoomSide.South:
                return new Vector3((min.x + max.x) * 0.5f, 1.6f, min.z + settings.WallThickness * 0.5f);
            case RoomSide.East:
                return new Vector3(max.x - settings.WallThickness * 0.5f, 1.6f, (min.z + max.z) * 0.5f);
            default:
                return new Vector3(min.x + settings.WallThickness * 0.5f, 1.6f, (min.z + max.z) * 0.5f);
        }
    }

    private static Vector2Int GetOffset(RoomSide side)
    {
        switch (side)
        {
            case RoomSide.North:
                return Vector2Int.up;
            case RoomSide.South:
                return Vector2Int.down;
            case RoomSide.East:
                return Vector2Int.right;
            default:
                return Vector2Int.left;
        }
    }

    private static Vector3 GetRoomWorldCenter(RectInt rect, FloorGeneratorSettings settings, Vector2 worldOffset)
    {
        var size = new Vector2(rect.width * settings.CellSize, rect.height * settings.CellSize);
        return new Vector3(rect.xMin * settings.CellSize + size.x * 0.5f + worldOffset.x, 0f, rect.yMin * settings.CellSize + size.y * 0.5f + worldOffset.y);
    }

    private static Vector3 GetRoomWorldMin(RectInt rect, FloorGeneratorSettings settings, Vector2 worldOffset)
    {
        return new Vector3(rect.xMin * settings.CellSize + worldOffset.x, 0f, rect.yMin * settings.CellSize + worldOffset.y);
    }

    private static Vector3 GetRoomWorldMax(RectInt rect, FloorGeneratorSettings settings, Vector2 worldOffset)
    {
        return new Vector3(rect.xMax * settings.CellSize + worldOffset.x, 0f, rect.yMax * settings.CellSize + worldOffset.y);
    }

    private static Bounds GetRoomWorldBounds(RectInt rect, Vector2 worldOffset, FloorGeneratorSettings settings)
    {
        var center = GetRoomWorldCenter(rect, settings, worldOffset);
        return new Bounds(center + Vector3.up * settings.WallHeight * 0.5f, new Vector3(rect.width * settings.CellSize, settings.WallHeight, rect.height * settings.CellSize));
    }

    private static RectInt GetCoarseLayoutBounds(FloorGenerationResult generated)
    {
        var minX = int.MaxValue;
        var minY = int.MaxValue;
        var maxX = int.MinValue;
        var maxY = int.MinValue;
        for (var i = 0; i < generated.Rooms.Count; i++)
        {
            minX = Mathf.Min(minX, generated.Rooms[i].Rect.xMin);
            minY = Mathf.Min(minY, generated.Rooms[i].Rect.yMin);
            maxX = Mathf.Max(maxX, generated.Rooms[i].Rect.xMax);
            maxY = Mathf.Max(maxY, generated.Rooms[i].Rect.yMax);
        }

        return new RectInt(minX, minY, maxX - minX, maxY - minY);
    }

    private static Bounds GetLayoutBounds(FloorGenerationResult generated, FloorGeneratorSettings settings)
    {
        if (generated.Rooms.Count == 0)
        {
            return new Bounds(Vector3.zero, Vector3.one);
        }

        var bounds = GetRoomWorldBounds(generated.Rooms[0].Rect, generated.WorldOffset, settings);
        for (var i = 1; i < generated.Rooms.Count; i++)
        {
            bounds.Encapsulate(GetRoomWorldBounds(generated.Rooms[i].Rect, generated.WorldOffset, settings));
        }

        return bounds;
    }

    private static bool TouchesPerimeter(RectInt rect, RectInt bounds)
    {
        return rect.xMin == bounds.xMin || rect.xMax == bounds.xMax || rect.yMin == bounds.yMin || rect.yMax == bounds.yMax;
    }
}
}

