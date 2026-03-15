using System;
using System.Collections.Generic;
using UnityEngine;

namespace MurinoHDR.Generation
{

public static class FloorResultBuilder
{
    private enum BoundaryKind
    {
        None = 0,
        SolidWall = 1,
        Door = 2,
        Open = 3,
    }

    private struct SharedBoundary
    {
        public RoomSide Side;
        public float Start;
        public float End;
        public float Fixed;
    }

    public static FloorResult Build(FloorGenerationResult generated, FloorGeneratorSettings settings)
    {
        if (generated == null)
        {
            throw new ArgumentNullException(nameof(generated));
        }

        var result = new FloorResult();
        result.Seed = generated.Seed;
        result.AttemptIndex = generated.AttemptIndex;
        result.Style = generated.Style;
        result.OutsideTheme = generated.OutsideTheme ?? settings.OutsideThemeProfile;
        result.FootprintWorld = BuildFootprint(generated, settings);

        var officeRules = settings != null ? settings.OfficeRules : null;
        var styleProfile = officeRules != null ? officeRules.GetStyleProfile(generated.Style) : null;
        var roomLookup = new Dictionary<string, RoomInstance>();
        var adjacency = BuildRoomAdjacency(generated.Rooms);

        for (var i = 0; i < generated.Rooms.Count; i++)
        {
            var generatedRoom = generated.Rooms[i];
            var detailedType = ResolveDetailedType(generatedRoom, generated.Style, i);
            var room = new RoomInstance();
            room.RoomId = i;
            room.Name = generatedRoom.InstanceId;
            room.RoomType = generatedRoom.Category;
            room.DetailedType = detailedType;
            room.Style = generated.Style;
            room.WorldBounds = BuildWorldBounds(generatedRoom.Rect, generated.WorldOffset, settings);
            room.Tags = BuildRoomTags(generatedRoom, detailedType, room.WorldBounds, result.FootprintWorld);
            room.FacadeSide = (room.Tags & RoomTags.FacadeRoom) != 0 ? GetFacadeSide(room.WorldBounds, result.FootprintWorld) : RoomSide.North;
            room.RequiresFacadeWindow = (room.Tags & (RoomTags.RequiresWindow | RoomTags.FacadeRequired)) != 0;
            room.WalkSpineRectXZ = BuildWalkSpineRect(generatedRoom, generated.WorldOffset, settings);
            room.PropPatternId = ResolvePattern(styleProfile, adjacency, generatedRoom.InstanceId, roomLookup);
            room.LightingProfileId = ResolveProfile(styleProfile != null ? styleProfile.LightingProfiles : null, adjacency, generatedRoom.InstanceId, roomLookup);
            room.DecorProfileId = ResolveProfile(styleProfile != null ? styleProfile.DecorProfiles : null, adjacency, generatedRoom.InstanceId, roomLookup);
            result.Rooms.Add(room);
            roomLookup[generatedRoom.InstanceId] = room;
        }

        BuildWalls(result, generated, settings);
        BuildDoors(result, generated, settings, officeRules);
        BuildWindows(result, generated, settings);
        BuildSpawn(result);
        BuildExits(result);
        BuildSupportRooms(result);
        BuildPickups(result);
        PropSpawner.Populate(result, settings.ValidationConfig ?? new ValidationConfig(), new System.Random(generated.Seed), null);
        FinalizeRoomSignatures(result);
        return result;
    }

    private static Bounds BuildFootprint(FloorGenerationResult generated, FloorGeneratorSettings settings)
    {
        var min = new Vector3(float.MaxValue, 0f, float.MaxValue);
        var max = new Vector3(float.MinValue, settings.WallHeight, float.MinValue);
        for (var i = 0; i < generated.Rooms.Count; i++)
        {
            var bounds = BuildWorldBounds(generated.Rooms[i].Rect, generated.WorldOffset, settings);
            min.x = Mathf.Min(min.x, bounds.min.x);
            min.z = Mathf.Min(min.z, bounds.min.z);
            max.x = Mathf.Max(max.x, bounds.max.x);
            max.z = Mathf.Max(max.z, bounds.max.z);
        }

        return new Bounds((min + max) * 0.5f, new Vector3(max.x - min.x, settings.WallHeight, max.z - min.z));
    }

    private static Bounds BuildWorldBounds(RectInt rect, Vector2 worldOffset, FloorGeneratorSettings settings)
    {
        var size = new Vector3(rect.width * settings.CellSize, settings.WallHeight, rect.height * settings.CellSize);
        var center = new Vector3(
            worldOffset.x + (rect.xMin + rect.width * 0.5f) * settings.CellSize,
            settings.WallHeight * 0.5f,
            worldOffset.y + (rect.yMin + rect.height * 0.5f) * settings.CellSize);
        return new Bounds(center, size);
    }

    private static RoomType ResolveDetailedType(GeneratedRoom room, FloorStyle style, int ordinal)
    {
        switch (room.Category)
        {
            case RoomCategory.Start:
                return RoomType.Reception;
            case RoomCategory.Corridor:
                return room.InstanceId.IndexOf("Service", StringComparison.OrdinalIgnoreCase) >= 0 ? RoomType.TechCorridor : RoomType.MainCorridor;
            case RoomCategory.Hub:
                return RoomType.OpenSpace;
            case RoomCategory.Storage:
                return RoomType.Warehouse;
            case RoomCategory.Utility:
                if (room.InstanceId.IndexOf("West", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    return style == FloorStyle.TechHeavy ? RoomType.ServerRoom : RoomType.SecurityRoom;
                }

                return style == FloorStyle.TechHeavy ? RoomType.TechCorridor : RoomType.Restroom;
            case RoomCategory.ExitElevator:
                return RoomType.ElevatorLobby;
            case RoomCategory.ExitShaft:
                return RoomType.ShaftAccess;
            case RoomCategory.ExitStairs:
                return RoomType.StairsLobby;
            case RoomCategory.Office:
                if (room.InstanceId.IndexOf("SouthEast", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    return RoomType.KitchenBreak;
                }

                if (room.InstanceId.IndexOf("North", StringComparison.OrdinalIgnoreCase) >= 0 && style == FloorStyle.CabinetHeavy)
                {
                    return RoomType.ManagerOffice;
                }

                if (room.InstanceId.IndexOf("North", StringComparison.OrdinalIgnoreCase) >= 0 && style == FloorStyle.Representative)
                {
                    return RoomType.MeetingRoom;
                }

                if (room.InstanceId.IndexOf("SouthWest", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    return RoomType.PrintCopyRoom;
                }

                if (ordinal % 3 == 0 && style == FloorStyle.CabinetHeavy)
                {
                    return RoomType.CabinetsCluster;
                }

                return RoomType.OpenSpace;
            default:
                return RoomType.OpenSpace;
        }
    }

    private static RoomTags BuildRoomTags(GeneratedRoom generatedRoom, RoomType detailedType, Bounds roomWorldBounds, Bounds footprint)
    {
        var tags = RoomTags.None;
        var roomBounds = generatedRoom.Rect;
        if (generatedRoom.Category == RoomCategory.Corridor)
        {
            tags |= RoomTags.Corridor;
        }

        if (generatedRoom.Category == RoomCategory.Corridor || generatedRoom.Category == RoomCategory.Hub || generatedRoom.Category == RoomCategory.Start)
        {
            tags |= RoomTags.MainPath;
        }

        if (generatedRoom.Category == RoomCategory.Utility || generatedRoom.Category == RoomCategory.ExitShaft)
        {
            tags |= RoomTags.Tech;
        }

        if (TouchesFacade(roomWorldBounds, footprint))
        {
            tags |= RoomTags.FacadeEligible | RoomTags.FacadeRoom;
        }

        switch (detailedType)
        {
            case RoomType.Reception:
                tags |= RoomTags.Checkpoint | RoomTags.LogicRoom | RoomTags.SafeSpot | RoomTags.Landmark | RoomTags.FacadePreferred;
                break;
            case RoomType.KitchenBreak:
                tags |= RoomTags.SafeSpot | RoomTags.SafeSpotCandidate | RoomTags.FacadePreferred | RoomTags.RequiresWindow;
                break;
            case RoomType.OpenSpace:
                tags |= RoomTags.FacadePreferred | RoomTags.RequiresWindow;
                break;
            case RoomType.MeetingRoom:
            case RoomType.ManagerOffice:
                tags |= RoomTags.FacadePreferred | RoomTags.RequiresWindow | RoomTags.LogicRoom;
                break;
            case RoomType.Warehouse:
                tags |= RoomTags.ResourceRoom | RoomTags.Support | RoomTags.CorePreferred | RoomTags.LootSource;
                break;
            case RoomType.PrintCopyRoom:
                tags |= RoomTags.Support | RoomTags.LootSource;
                break;
            case RoomType.SecurityRoom:
                tags |= RoomTags.Support | RoomTags.LogicRoom | RoomTags.CorePreferred;
                break;
            case RoomType.ServerRoom:
            case RoomType.Restroom:
                tags |= RoomTags.Support | RoomTags.CorePreferred;
                break;
            case RoomType.TechCorridor:
            case RoomType.VentSegment:
                tags |= RoomTags.Support | RoomTags.Tech | RoomTags.CorePreferred | RoomTags.ChokepointsAllowed;
                break;
            case RoomType.ElevatorLobby:
                tags |= RoomTags.LogicRoom | RoomTags.Landmark | RoomTags.CorePreferred;
                break;
        }

        return tags;
    }

    private static bool TouchesFacade(Bounds roomWorldBounds, Bounds footprint)
    {
        const float epsilon = 0.05f;
        return Mathf.Abs(roomWorldBounds.min.x - footprint.min.x) <= epsilon
            || Mathf.Abs(roomWorldBounds.max.x - footprint.max.x) <= epsilon
            || Mathf.Abs(roomWorldBounds.min.z - footprint.min.z) <= epsilon
            || Mathf.Abs(roomWorldBounds.max.z - footprint.max.z) <= epsilon;
    }

    private static Rect BuildWalkSpineRect(GeneratedRoom room, Vector2 worldOffset, FloorGeneratorSettings settings)
    {
        var worldBounds = BuildWorldBounds(room.Rect, worldOffset, settings);
        var spineWidth = Mathf.Min(worldBounds.size.x, 1.6f);
        var spineDepth = Mathf.Min(worldBounds.size.z, 1.6f);

        if (room.Rect.width >= room.Rect.height)
        {
            return Rect.MinMaxRect(worldBounds.min.x + 0.6f, worldBounds.center.z - spineDepth * 0.5f, worldBounds.max.x - 0.6f, worldBounds.center.z + spineDepth * 0.5f);
        }

        return Rect.MinMaxRect(worldBounds.center.x - spineWidth * 0.5f, worldBounds.min.z + 0.6f, worldBounds.center.x + spineWidth * 0.5f, worldBounds.max.z - 0.6f);
    }

    private static string ResolvePattern(FloorStyleProfile styleProfile, Dictionary<string, HashSet<string>> adjacency, string roomId, Dictionary<string, RoomInstance> roomLookup)
    {
        var candidates = styleProfile != null && styleProfile.PropPatterns != null && styleProfile.PropPatterns.Length > 0
            ? styleProfile.PropPatterns
            : new[] { "default_office" };
        return ResolveUniqueString(candidates, adjacency, roomId, roomLookup, true);
    }

    private static string ResolveProfile(string[] candidates, Dictionary<string, HashSet<string>> adjacency, string roomId, Dictionary<string, RoomInstance> roomLookup)
    {
        var pool = candidates != null && candidates.Length > 0 ? candidates : new[] { "default" };
        return ResolveUniqueString(pool, adjacency, roomId, roomLookup, false);
    }

    private static string ResolveUniqueString(string[] candidates, Dictionary<string, HashSet<string>> adjacency, string roomId, Dictionary<string, RoomInstance> roomLookup, bool pattern)
    {
        for (var i = 0; i < candidates.Length; i++)
        {
            var candidate = candidates[(StableHash(roomId) + i) % candidates.Length];
            if (candidate == null)
            {
                continue;
            }

            if (!HasNeighborValue(adjacency, roomId, candidate, roomLookup, pattern))
            {
                return candidate;
            }
        }

        return candidates[0];
    }

    private static bool HasNeighborValue(Dictionary<string, HashSet<string>> adjacency, string roomId, string candidate, Dictionary<string, RoomInstance> roomLookup, bool pattern)
    {
        HashSet<string> neighbors;
        if (!adjacency.TryGetValue(roomId, out neighbors))
        {
            return false;
        }

        foreach (var neighbor in neighbors)
        {
            RoomInstance neighborRoom;
            if (!roomLookup.TryGetValue(neighbor, out neighborRoom))
            {
                continue;
            }

            var value = pattern ? neighborRoom.PropPatternId : neighborRoom.LightingProfileId;
            if (string.Equals(value, candidate, StringComparison.Ordinal))
            {
                return true;
            }
        }

        return false;
    }

    private static Dictionary<string, HashSet<string>> BuildRoomAdjacency(IList<GeneratedRoom> rooms)
    {
        var adjacency = new Dictionary<string, HashSet<string>>();
        for (var i = 0; i < rooms.Count; i++)
        {
            if (!adjacency.ContainsKey(rooms[i].InstanceId))
            {
                adjacency.Add(rooms[i].InstanceId, new HashSet<string>());
            }

            for (var j = i + 1; j < rooms.Count; j++)
            {
                SharedBoundary boundary;
                if (!TryGetSharedBoundary(rooms[i].Rect, rooms[j].Rect, out boundary))
                {
                    continue;
                }

                adjacency[rooms[i].InstanceId].Add(rooms[j].InstanceId);
                if (!adjacency.ContainsKey(rooms[j].InstanceId))
                {
                    adjacency.Add(rooms[j].InstanceId, new HashSet<string>());
                }

                adjacency[rooms[j].InstanceId].Add(rooms[i].InstanceId);
            }
        }

        return adjacency;
    }

    private static void BuildWalls(FloorResult result, FloorGenerationResult generated, FloorGeneratorSettings settings)
    {
        var wallId = 0;
        for (var i = 0; i < generated.Rooms.Count; i++)
        {
            var room = generated.Rooms[i];
            for (var sideIndex = 0; sideIndex < 4; sideIndex++)
            {
                var side = (RoomSide)sideIndex;
                GeneratedRoom otherRoom;
                SharedBoundary boundary;
                if (!TryFindNeighbor(generated.Rooms, room, side, out otherRoom, out boundary))
                {
                    AddWall(result, ref wallId, i, boundary, settings, generated.WorldOffset, true);
                    continue;
                }

                if (room.Index > otherRoom.Index)
                {
                    continue;
                }

                var kind = DetermineBoundaryKind(room, otherRoom);
                if (kind == BoundaryKind.Open)
                {
                    continue;
                }

                AddWall(result, ref wallId, i, boundary, settings, generated.WorldOffset, false);
            }
        }
    }

    private static void BuildDoors(FloorResult result, FloorGenerationResult generated, FloorGeneratorSettings settings, OfficeGenerationRules officeRules)
    {
        var doorId = 0;
        var minClear = settings.ValidationConfig != null ? settings.ValidationConfig.MinDoorClearWidthMeters : 0.8f;
        var clearanceCells = officeRules != null ? officeRules.DoorIntersectionClearanceCells : 1;
        for (var i = 0; i < generated.Rooms.Count; i++)
        {
            for (var j = i + 1; j < generated.Rooms.Count; j++)
            {
                SharedBoundary boundary;
                if (!TryGetSharedBoundary(generated.Rooms[i].Rect, generated.Rooms[j].Rect, out boundary))
                {
                    continue;
                }

                var kind = DetermineBoundaryKind(generated.Rooms[i], generated.Rooms[j]);
                if (kind != BoundaryKind.Door)
                {
                    continue;
                }

                if (!PassDoorPlacementRules(boundary, settings, minClear, clearanceCells))
                {
                    continue;
                }

                var door = BuildDoor(doorId++, generated.Rooms[i], generated.Rooms[j], boundary, settings, generated.WorldOffset, minClear);
                result.Doors.Add(door);
                result.Rooms[i].DoorIds.Add(door.DoorId);
                result.Rooms[j].DoorIds.Add(door.DoorId);
            }
        }
    }

    private static void BuildWindows(FloorResult result, FloorGenerationResult generated, FloorGeneratorSettings settings)
    {
        var windowId = 0;
        for (var i = 0; i < result.Rooms.Count; i++)
        {
            var room = result.Rooms[i];
            if ((room.Tags & RoomTags.FacadeRoom) == 0 || !room.RequiresFacadeWindow)
            {
                continue;
            }

            var bounds = room.WorldBounds;
            var side = GetFacadeSide(bounds, result.FootprintWorld);
            var span = side == RoomSide.North || side == RoomSide.South ? bounds.size.x : bounds.size.z;
            var count = Mathf.Clamp(Mathf.FloorToInt(span / 4f), 1, 3);
            for (var windowIndex = 0; windowIndex < count; windowIndex++)
            {
                var t = count == 1 ? 0.5f : (windowIndex + 1f) / (count + 1f);
                var marker = new WindowMarker();
                marker.WindowId = windowId++;
                marker.RoomId = room.RoomId;
                marker.WidthMeters = Mathf.Min(2.6f, span / Mathf.Max(1, count) - 0.4f);
                marker.Orientation = side == RoomSide.North || side == RoomSide.South ? DoorOrientation.Horizontal : DoorOrientation.Vertical;
                marker.WorldPos = BuildFacadePoint(bounds, side, t);
                result.Windows.Add(marker);
                room.HasWindow = true;
            }
        }
    }

    private static void BuildSpawn(FloorResult result)
    {
        for (var i = 0; i < result.Rooms.Count; i++)
        {
            if (result.Rooms[i].DetailedType != RoomType.Reception)
            {
                continue;
            }

            result.SpawnWorld = result.Rooms[i].WorldBounds.center + Vector3.up * 0.5f;
            return;
        }

        result.SpawnWorld = result.Rooms.Count > 0 ? result.Rooms[0].WorldBounds.center + Vector3.up * 0.5f : Vector3.up;
    }

    private static void BuildExits(FloorResult result)
    {
        for (var i = 0; i < result.Rooms.Count; i++)
        {
            var room = result.Rooms[i];
            ExitType? exitType = null;
            switch (room.DetailedType)
            {
                case RoomType.ElevatorLobby:
                    exitType = ExitType.Lift;
                    break;
                case RoomType.ShaftAccess:
                    exitType = ExitType.Shaft;
                    break;
                case RoomType.StairsLobby:
                    exitType = ExitType.Stairs;
                    break;
            }

            if (!exitType.HasValue)
            {
                continue;
            }

            result.Exits.Add(new ExitMarker
            {
                Type = exitType.Value,
                WorldPos = room.WorldBounds.center + Vector3.up * 0.2f,
                RoomId = room.RoomId,
                DebugName = room.Name,
            });
        }
    }

    private static void BuildSupportRooms(FloorResult result)
    {
        for (var i = 0; i < result.Rooms.Count; i++)
        {
            var room = result.Rooms[i];
            if ((room.Tags & RoomTags.ResourceRoom) != 0)
            {
                result.SupportRooms.Add(new SupportRoomMarker { Type = SupportType.Resource, RoomId = room.RoomId, RepresentativePos = room.WorldBounds.center });
            }

            if ((room.Tags & RoomTags.LogicRoom) != 0)
            {
                result.SupportRooms.Add(new SupportRoomMarker { Type = SupportType.Logic, RoomId = room.RoomId, RepresentativePos = room.WorldBounds.center });
            }

            if ((room.Tags & RoomTags.Support) != 0)
            {
                result.SupportRooms.Add(new SupportRoomMarker { Type = SupportType.Utility, RoomId = room.RoomId, RepresentativePos = room.WorldBounds.center });
            }
        }
    }

    private static void BuildPickups(FloorResult result)
    {
        AddPickup(result, PickupType.Keycard, "keycard", RoomType.Reception, true, 0.2f, -0.2f);
        AddPickup(result, PickupType.Fuse, "fuse", RoomType.Warehouse, true, -0.5f, 0.2f);
        AddPickup(result, PickupType.Tape, "tape", RoomType.KitchenBreak, true, 0.35f, 0.1f);
        AddPickup(result, PickupType.Crowbar, "crowbar", RoomType.Warehouse, false, -0.25f, -0.35f);
        AddPickup(result, PickupType.Rope, "rope", RoomType.Warehouse, false, 0.45f, -0.15f);
        AddPickup(result, PickupType.Lockpick, "lockpick", RoomType.TechCorridor, false, -0.15f, 0.25f);
    }

    private static void AddPickup(FloorResult result, PickupType type, string itemId, RoomType preferredType, bool requiredForMainGate, float offsetX, float offsetZ)
    {
        for (var i = 0; i < result.Rooms.Count; i++)
        {
            if (result.Rooms[i].DetailedType != preferredType)
            {
                continue;
            }

            result.Pickups.Add(new PickupMarker
            {
                Type = type,
                ItemId = itemId,
                RoomId = result.Rooms[i].RoomId,
                RequiredForMainGate = requiredForMainGate,
                WorldPos = result.Rooms[i].WorldBounds.center + new Vector3(offsetX, 0.2f, offsetZ),
            });
            return;
        }

        if (result.Rooms.Count > 0)
        {
            result.Pickups.Add(new PickupMarker
            {
                Type = type,
                ItemId = itemId,
                RoomId = result.Rooms[0].RoomId,
                RequiredForMainGate = requiredForMainGate,
                WorldPos = result.Rooms[0].WorldBounds.center + new Vector3(offsetX, 0.2f, offsetZ),
            });
        }
    }

    private static void FinalizeRoomSignatures(FloorResult result)
    {
        for (var i = 0; i < result.Rooms.Count; i++)
        {
            var room = result.Rooms[i];
            room.Signature = string.Format(
                "{0}|{1:0.0}x{2:0.0}|{3}|{4}|{5}|{6}",
                room.DetailedType,
                room.WorldBounds.size.x,
                room.WorldBounds.size.z,
                room.PropPatternId,
                room.LightingProfileId,
                room.DecorProfileId,
                room.HasWindow ? "window" : "solid");
        }
    }

    private static void AddWall(FloorResult result, ref int wallId, int roomId, SharedBoundary boundary, FloorGeneratorSettings settings, Vector2 worldOffset, bool facade)
    {
        var start = BoundaryPoint(boundary, boundary.Start, settings, worldOffset);
        var end = BoundaryPoint(boundary, boundary.End, settings, worldOffset);
        result.Walls.Add(new WallSegment
        {
            WallId = wallId++,
            RoomId = roomId,
            AWorld = start,
            BWorld = end,
            ThicknessMeters = settings.WallThickness,
            Tags = facade ? WallTags.FacadeWall : WallTags.InternalWall,
        });
    }

    private static DoorInstance BuildDoor(int doorId, GeneratedRoom a, GeneratedRoom b, SharedBoundary boundary, FloorGeneratorSettings settings, Vector2 worldOffset, float clearWidth)
    {
        var mid = (boundary.Start + boundary.End) * 0.5f;
        var pos = BoundaryPoint(boundary, mid, settings, worldOffset);
        var orientation = boundary.Side == RoomSide.North || boundary.Side == RoomSide.South ? DoorOrientation.Horizontal : DoorOrientation.Vertical;
        return new DoorInstance
        {
            DoorId = doorId,
            DebugName = string.Format("{0}_to_{1}", a.InstanceId, b.InstanceId),
            Orientation = orientation,
            WorldPos = pos,
            ClearWidthMeters = clearWidth,
            StartsClosed = false,
            RoomAId = a.Index,
            RoomBId = b.Index,
            Tags = (a.Category == RoomCategory.ExitElevator || a.Category == RoomCategory.ExitShaft || a.Category == RoomCategory.ExitStairs || b.Category == RoomCategory.ExitElevator || b.Category == RoomCategory.ExitShaft || b.Category == RoomCategory.ExitStairs)
                ? DoorTags.ExitDoor
                : DoorTags.None,
            WorldRectXZ = orientation == DoorOrientation.Horizontal
                ? new Rect(pos.x - clearWidth * 0.5f, pos.z - settings.WallThickness * 0.5f, clearWidth, settings.WallThickness)
                : new Rect(pos.x - settings.WallThickness * 0.5f, pos.z - clearWidth * 0.5f, settings.WallThickness, clearWidth),
        };
    }

    private static BoundaryKind DetermineBoundaryKind(GeneratedRoom a, GeneratedRoom b)
    {
        var openA = IsOpenRoom(a.Category);
        var openB = IsOpenRoom(b.Category);
        if (openA && openB)
        {
            return BoundaryKind.Open;
        }

        if (a.Category == RoomCategory.Corridor || a.Category == RoomCategory.Hub || a.Category == RoomCategory.Start || b.Category == RoomCategory.Corridor || b.Category == RoomCategory.Hub || b.Category == RoomCategory.Start)
        {
            return BoundaryKind.Door;
        }

        if (a.Category == RoomCategory.ExitElevator || a.Category == RoomCategory.ExitShaft || a.Category == RoomCategory.ExitStairs || b.Category == RoomCategory.ExitElevator || b.Category == RoomCategory.ExitShaft || b.Category == RoomCategory.ExitStairs)
        {
            return BoundaryKind.Door;
        }

        return BoundaryKind.SolidWall;
    }

    private static bool IsOpenRoom(RoomCategory category)
    {
        return category == RoomCategory.Corridor || category == RoomCategory.Hub || category == RoomCategory.Start;
    }

    private static bool PassDoorPlacementRules(SharedBoundary boundary, FloorGeneratorSettings settings, float clearWidth, int clearanceCells)
    {
        var available = (boundary.End - boundary.Start) * settings.CellSize;
        var reserved = clearWidth + clearanceCells * settings.CellSize * 2f;
        return available >= reserved;
    }

    private static bool TryFindNeighbor(IList<GeneratedRoom> rooms, GeneratedRoom room, RoomSide side, out GeneratedRoom neighbor, out SharedBoundary boundary)
    {
        for (var i = 0; i < rooms.Count; i++)
        {
            if (rooms[i].Index == room.Index)
            {
                continue;
            }

            if (!TryGetSharedBoundary(room.Rect, rooms[i].Rect, out boundary) || boundary.Side != side)
            {
                continue;
            }

            neighbor = rooms[i];
            return true;
        }

        boundary = BuildExteriorBoundary(room.Rect, side);
        neighbor = null;
        return false;
    }

    private static SharedBoundary BuildExteriorBoundary(RectInt rect, RoomSide side)
    {
        switch (side)
        {
            case RoomSide.North:
                return new SharedBoundary { Side = side, Start = rect.xMin, End = rect.xMax, Fixed = rect.yMax };
            case RoomSide.South:
                return new SharedBoundary { Side = side, Start = rect.xMin, End = rect.xMax, Fixed = rect.yMin };
            case RoomSide.East:
                return new SharedBoundary { Side = side, Start = rect.yMin, End = rect.yMax, Fixed = rect.xMax };
            default:
                return new SharedBoundary { Side = side, Start = rect.yMin, End = rect.yMax, Fixed = rect.xMin };
        }
    }

    private static bool TryGetSharedBoundary(RectInt a, RectInt b, out SharedBoundary boundary)
    {
        if (a.yMax == b.yMin)
        {
            var start = Mathf.Max(a.xMin, b.xMin);
            var end = Mathf.Min(a.xMax, b.xMax);
            if (end > start)
            {
                boundary = new SharedBoundary { Side = RoomSide.North, Start = start, End = end, Fixed = a.yMax };
                return true;
            }
        }

        if (a.yMin == b.yMax)
        {
            var start = Mathf.Max(a.xMin, b.xMin);
            var end = Mathf.Min(a.xMax, b.xMax);
            if (end > start)
            {
                boundary = new SharedBoundary { Side = RoomSide.South, Start = start, End = end, Fixed = a.yMin };
                return true;
            }
        }

        if (a.xMax == b.xMin)
        {
            var start = Mathf.Max(a.yMin, b.yMin);
            var end = Mathf.Min(a.yMax, b.yMax);
            if (end > start)
            {
                boundary = new SharedBoundary { Side = RoomSide.East, Start = start, End = end, Fixed = a.xMax };
                return true;
            }
        }

        if (a.xMin == b.xMax)
        {
            var start = Mathf.Max(a.yMin, b.yMin);
            var end = Mathf.Min(a.yMax, b.yMax);
            if (end > start)
            {
                boundary = new SharedBoundary { Side = RoomSide.West, Start = start, End = end, Fixed = a.xMin };
                return true;
            }
        }

        boundary = default(SharedBoundary);
        return false;
    }

    private static Vector3 BoundaryPoint(SharedBoundary boundary, float variable, FloorGeneratorSettings settings, Vector2 worldOffset)
    {
        switch (boundary.Side)
        {
            case RoomSide.North:
            case RoomSide.South:
                return new Vector3(worldOffset.x + variable * settings.CellSize, 0f, worldOffset.y + boundary.Fixed * settings.CellSize);
            case RoomSide.East:
            case RoomSide.West:
            default:
                return new Vector3(worldOffset.x + boundary.Fixed * settings.CellSize, 0f, worldOffset.y + variable * settings.CellSize);
        }
    }

    private static RoomSide GetFacadeSide(Bounds roomBounds, Bounds footprint)
    {
        var distanceNorth = Mathf.Abs(roomBounds.max.z - footprint.max.z);
        var distanceSouth = Mathf.Abs(roomBounds.min.z - footprint.min.z);
        var distanceEast = Mathf.Abs(roomBounds.max.x - footprint.max.x);
        var distanceWest = Mathf.Abs(roomBounds.min.x - footprint.min.x);
        var best = Mathf.Min(Mathf.Min(distanceNorth, distanceSouth), Mathf.Min(distanceEast, distanceWest));
        if (Mathf.Approximately(best, distanceNorth)) return RoomSide.North;
        if (Mathf.Approximately(best, distanceSouth)) return RoomSide.South;
        if (Mathf.Approximately(best, distanceEast)) return RoomSide.East;
        return RoomSide.West;
    }

    private static Vector3 BuildFacadePoint(Bounds roomBounds, RoomSide side, float t)
    {
        switch (side)
        {
            case RoomSide.North:
                return new Vector3(Mathf.Lerp(roomBounds.min.x + 1f, roomBounds.max.x - 1f, t), 1.5f, roomBounds.max.z);
            case RoomSide.South:
                return new Vector3(Mathf.Lerp(roomBounds.min.x + 1f, roomBounds.max.x - 1f, t), 1.5f, roomBounds.min.z);
            case RoomSide.East:
                return new Vector3(roomBounds.max.x, 1.5f, Mathf.Lerp(roomBounds.min.z + 1f, roomBounds.max.z - 1f, t));
            default:
                return new Vector3(roomBounds.min.x, 1.5f, Mathf.Lerp(roomBounds.min.z + 1f, roomBounds.max.z - 1f, t));
        }
    }

    private static int StableHash(string value)
    {
        unchecked
        {
            var hash = 23;
            for (var i = 0; i < value.Length; i++)
            {
                hash = hash * 31 + value[i];
            }

            return Mathf.Abs(hash);
        }
    }
}
}
