using System;
using System.Collections.Generic;
using MurinoHDR.Core;
using MurinoHDR.Interaction;
using MurinoHDR.Inventory;
using UnityEngine;

namespace MurinoHDR.Generation
{

public enum RoomSide
{
    North = 0,
    South = 1,
    East = 2,
    West = 3,
}

public enum GeneratedDoorVisualType
{
    Office = 0,
    Service = 1,
    Exit = 2,
    ElevatorLeft = 3,
    ElevatorRight = 4,
}

public static class MvpEnvironmentBuilder
{
    private enum BoundaryType
    {
        None = 0,
        ExteriorWall = 1,
        InteriorWall = 2,
        Doorway = 3,
        ServiceDoorway = 4,
        ExitDoor = 5,
        ElevatorPortal = 6,
    }

    private sealed class MaterialPalette
    {
        public Material ReceptionFloor;
        public Material CorridorFloor;
        public Material OfficeFloor;
        public Material StorageFloor;
        public Material UtilityFloor;
        public Material Ceiling;
        public Material WallPaint;
        public Material Partition;
        public Material Metal;
        public Material Wood;
        public Material Accent;
        public Material Fixture;
        public Material DarkConcrete;
    }

    public const string RootName = "MVP_Environment";
    public const string LayoutVersionName = "Layout_v5_Office";

    private static MaterialPalette _palette;

    public static GameObject EnsureEnvironment()
    {
        var existingRoot = GameObject.Find(RootName);
        if (existingRoot != null && existingRoot.transform.Find(LayoutVersionName) != null)
        {
            return existingRoot;
        }

        if (existingRoot != null)
        {
            UnityEngine.Object.DestroyImmediate(existingRoot);
        }

        return BuildEnvironment();
    }

    public static GameObject RebuildEnvironment()
    {
        var existingRoot = GameObject.Find(RootName);
        if (existingRoot != null)
        {
            UnityEngine.Object.DestroyImmediate(existingRoot);
        }

        return BuildEnvironment();
    }

    private static GameObject BuildEnvironment()
    {
        MvpRuntimeContent.EnsureInitialized();

        var settings = MvpRuntimeContent.Catalog.GeneratorSettings;
        var seed = Mathf.Abs(Environment.TickCount);
        var result = FloorGenerator.Generate(seed, settings);
        var report = FloorGenerator.Validate(result, settings);

        var root = new GameObject(RootName);
        new GameObject(LayoutVersionName).transform.SetParent(root.transform, false);
        var info = root.AddComponent<GeneratedFloorInfo>();
        info.Configure(seed, report.BuildSummary());

        var goalsObject = new GameObject("FloorGoals");
        goalsObject.transform.SetParent(root.transform, false);
        var goals = goalsObject.AddComponent<FloorGoalController>();
        goals.EnsureDefaults();

        BuildLighting(root.transform, result, settings);
        var roomLookup = BuildGeneratedRooms(root.transform, result, settings);
        BuildBoundaries(result, settings, roomLookup);
        BuildExitSetpieces(roomLookup, result, settings, goals);
        CreateRoomAnchors(result, settings, roomLookup);
        SpawnPickups(result, settings, roomLookup);

        if (!report.IsValid)
        {
            Debug.LogError(string.Format("[GEN] Floor validation failed: {0}", report.BuildSummary()));
        }
        else
        {
            Debug.Log(string.Format("[GEN] Generated office floor seed {0}", seed));
        }

        return root;
    }

    private static void BuildLighting(Transform root, FloorGenerationResult result, FloorGeneratorSettings settings)
    {
        var lightingRoot = new GameObject("Lighting").transform;
        lightingRoot.SetParent(root, false);

        var ambient = new GameObject("AmbientDirectional");
        ambient.transform.SetParent(lightingRoot, false);
        ambient.transform.rotation = Quaternion.Euler(55f, -35f, 0f);
        var directional = ambient.AddComponent<Light>();
        directional.type = LightType.Directional;
        directional.intensity = 0.18f;
        directional.color = new Color(0.72f, 0.75f, 0.82f);

        var bounds = GetLayoutBounds(result, settings);
        var fill = new GameObject("HubFillLight");
        fill.transform.SetParent(lightingRoot, false);
        fill.transform.position = new Vector3(bounds.center.x, 2.6f, bounds.center.z);
        var point = fill.AddComponent<Light>();
        point.type = LightType.Point;
        point.range = Mathf.Max(bounds.size.x, bounds.size.z) * 0.7f;
        point.intensity = 0.28f;
        point.color = new Color(0.72f, 0.78f, 0.9f);
    }

    private static Dictionary<string, Transform> BuildGeneratedRooms(Transform root, FloorGenerationResult result, FloorGeneratorSettings settings)
    {
        var roomsRoot = new GameObject("Rooms").transform;
        roomsRoot.SetParent(root, false);

        var roomLookup = new Dictionary<string, Transform>();
        for (var i = 0; i < result.Rooms.Count; i++)
        {
            var room = result.Rooms[i];
            var roomObject = new GameObject(room.InstanceId);
            roomObject.transform.SetParent(roomsRoot, false);

            var marker = roomObject.AddComponent<GeneratedRoomMarker>();
            marker.Configure(room.InstanceId, room.Category, room.Rect.size, GetRoomWorldCenter(room.Rect, settings, result.WorldOffset));

            CreateFloor(roomObject.transform, room, result.WorldOffset, settings);
            CreateCeiling(roomObject.transform, room, result.WorldOffset, settings);
            CreateCeilingLights(roomObject.transform, room, result.WorldOffset, settings);
            roomLookup[room.InstanceId] = roomObject.transform;
        }

        return roomLookup;
    }

    private static void CreateFloor(Transform parent, GeneratedRoom room, Vector2 worldOffset, FloorGeneratorSettings settings)
    {
        var size = GetWorldSize(room.Rect, settings);
        var position = GetRoomWorldCenter(room.Rect, settings, worldOffset);
        position.y = -settings.FloorThickness * 0.5f;
        CreateCube(parent, "Floor", position, new Vector3(size.x, settings.FloorThickness, size.y), GetFloorMaterial(room.Category), true, new Vector2(size.x * 0.28f, size.y * 0.28f));
    }

    private static void CreateCeiling(Transform parent, GeneratedRoom room, Vector2 worldOffset, FloorGeneratorSettings settings)
    {
        var size = GetWorldSize(room.Rect, settings);
        var position = GetRoomWorldCenter(room.Rect, settings, worldOffset);
        position.y = settings.WallHeight - 0.06f;
        CreateCube(parent, "Ceiling", position, new Vector3(size.x, 0.12f, size.y), GetPalette().Ceiling, false, new Vector2(size.x * 0.3f, size.y * 0.3f));
    }

    private static void BuildBoundaries(FloorGenerationResult result, FloorGeneratorSettings settings, Dictionary<string, Transform> roomLookup)
    {
        var occupancy = BuildOccupancyLookup(result);
        for (var i = 0; i < result.Rooms.Count; i++)
        {
            var room = result.Rooms[i];
            Transform roomRoot;
            if (!roomLookup.TryGetValue(room.InstanceId, out roomRoot))
            {
                continue;
            }

            for (var x = room.Rect.xMin; x < room.Rect.xMax; x++)
            {
                for (var y = room.Rect.yMin; y < room.Rect.yMax; y++)
                {
                    var cell = new Vector2Int(x, y);
                    BuildBoundaryForCell(roomRoot, room, cell, RoomSide.North, occupancy, result.WorldOffset, settings);
                    BuildBoundaryForCell(roomRoot, room, cell, RoomSide.East, occupancy, result.WorldOffset, settings);
                    if (!occupancy.ContainsKey(cell + Vector2Int.down))
                    {
                        BuildBoundaryForCell(roomRoot, room, cell, RoomSide.South, occupancy, result.WorldOffset, settings);
                    }

                    if (!occupancy.ContainsKey(cell + Vector2Int.left))
                    {
                        BuildBoundaryForCell(roomRoot, room, cell, RoomSide.West, occupancy, result.WorldOffset, settings);
                    }
                }
            }
        }
    }

    private static void BuildBoundaryForCell(Transform roomRoot, GeneratedRoom room, Vector2Int cell, RoomSide side, Dictionary<Vector2Int, GeneratedRoom> occupancy, Vector2 worldOffset, FloorGeneratorSettings settings)
    {
        GeneratedRoom otherRoom;
        occupancy.TryGetValue(cell + GetOffset(side), out otherRoom);
        var boundaryType = DetermineBoundary(room, otherRoom);
        if (boundaryType == BoundaryType.None)
        {
            return;
        }

        var edgeCenter = GetEdgeCenter(cell, side, settings, worldOffset);
        switch (boundaryType)
        {
            case BoundaryType.ExteriorWall:
                CreateSolidBoundary(roomRoot, string.Format("Exterior_{0}_{1}_{2}", side, cell.x, cell.y), edgeCenter, side, settings.CellSize, settings.WallHeight, settings.WallThickness, GetPalette().WallPaint);
                break;
            case BoundaryType.InteriorWall:
                CreateSolidBoundary(roomRoot, string.Format("Partition_{0}_{1}_{2}", side, cell.x, cell.y), edgeCenter, side, settings.CellSize, settings.WallHeight, settings.WallThickness, GetPalette().Partition);
                break;
            case BoundaryType.Doorway:
                CreateDoorway(roomRoot, string.Format("Doorway_{0}_{1}_{2}", side, cell.x, cell.y), edgeCenter, side, settings, 1.55f, 2.25f, GetPalette().Partition, GeneratedDoorVisualType.Office);
                break;
            case BoundaryType.ServiceDoorway:
                CreateDoorway(roomRoot, string.Format("ServiceDoorway_{0}_{1}_{2}", side, cell.x, cell.y), edgeCenter, side, settings, 1.8f, 2.3f, GetPalette().Metal, GeneratedDoorVisualType.Service);
                break;
            case BoundaryType.ExitDoor:
                CreateDoorway(roomRoot, string.Format("ExitDoorway_{0}_{1}_{2}", side, cell.x, cell.y), edgeCenter, side, settings, 1.8f, 2.3f, GetPalette().Metal, GeneratedDoorVisualType.Exit);
                break;
            case BoundaryType.ElevatorPortal:
                CreateDoorway(roomRoot, string.Format("ElevatorPortal_{0}_{1}_{2}", side, cell.x, cell.y), edgeCenter, side, settings, 2.1f, 2.5f, GetPalette().Metal, GeneratedDoorVisualType.Office);
                break;
        }
    }

    private static BoundaryType DetermineBoundary(GeneratedRoom room, GeneratedRoom otherRoom)
    {
        if (otherRoom == null) return BoundaryType.ExteriorWall;
        if (room.Index == otherRoom.Index) return BoundaryType.None;
        if (IsOpenZone(room.Category) && IsOpenZone(otherRoom.Category)) return BoundaryType.None;
        if ((room.Category == RoomCategory.ExitElevator && IsOpenZone(otherRoom.Category)) || (otherRoom.Category == RoomCategory.ExitElevator && IsOpenZone(room.Category))) return BoundaryType.ElevatorPortal;
        if ((room.Category == RoomCategory.ExitShaft && IsOpenZone(otherRoom.Category)) || (otherRoom.Category == RoomCategory.ExitShaft && IsOpenZone(room.Category))) return BoundaryType.ServiceDoorway;
        if ((room.Category == RoomCategory.ExitStairs && IsOpenZone(otherRoom.Category)) || (otherRoom.Category == RoomCategory.ExitStairs && IsOpenZone(room.Category))) return BoundaryType.ExitDoor;
        if ((IsOpenZone(room.Category) && IsSupportRoom(otherRoom.Category)) || (IsOpenZone(otherRoom.Category) && IsSupportRoom(room.Category))) return BoundaryType.Doorway;
        return BoundaryType.InteriorWall;
    }

    private static void CreateCeilingLights(Transform parent, GeneratedRoom room, Vector2 worldOffset, FloorGeneratorSettings settings)
    {
        var positions = GetLightPositions(room, settings, worldOffset);
        for (var i = 0; i < positions.Count; i++)
        {
            CreateCube(parent, string.Format("CeilingLight_{0:00}", i + 1), positions[i], new Vector3(1.2f, 0.08f, 0.55f), GetPalette().Fixture, false, Vector2.one);
            var lightObject = new GameObject(string.Format("Light_{0:00}", i + 1));
            lightObject.transform.SetParent(parent, false);
            lightObject.transform.position = positions[i] + Vector3.down * 0.08f;
            var light = lightObject.AddComponent<Light>();
            light.type = LightType.Point;
            light.range = room.Category == RoomCategory.Hub ? 10f : 7.2f;
            light.intensity = room.Category == RoomCategory.Storage ? 1.2f : 1.5f;
            light.color = room.Category == RoomCategory.Utility ? new Color(0.9f, 0.96f, 1f) : new Color(1f, 0.98f, 0.94f);
        }
    }

    private static List<Vector3> GetLightPositions(GeneratedRoom room, FloorGeneratorSettings settings, Vector2 worldOffset)
    {
        var positions = new List<Vector3>();
        var min = GetRoomWorldMin(room.Rect, settings, worldOffset);
        var max = GetRoomWorldMax(room.Rect, settings, worldOffset);
        var y = settings.WallHeight - 0.14f;
        var columns = room.Rect.width >= 5 ? 2 : 1;
        var rows = room.Rect.height >= 4 ? 2 : 1;

        for (var x = 0; x < columns; x++)
        {
            for (var z = 0; z < rows; z++)
            {
                var tX = columns == 1 ? 0.5f : 0.3f + x * 0.4f;
                var tZ = rows == 1 ? 0.5f : 0.3f + z * 0.4f;
                positions.Add(new Vector3(Mathf.Lerp(min.x, max.x, tX), y, Mathf.Lerp(min.z, max.z, tZ)));
            }
        }

        return positions;
    }

    private static Dictionary<Vector2Int, GeneratedRoom> BuildOccupancyLookup(FloorGenerationResult result)
    {
        var occupancy = new Dictionary<Vector2Int, GeneratedRoom>();
        for (var i = 0; i < result.Rooms.Count; i++)
        {
            var room = result.Rooms[i];
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

    private static void BuildExitSetpieces(Dictionary<string, Transform> roomLookup, FloorGenerationResult result, FloorGeneratorSettings settings, FloorGoalController goals)
    {
        BuildElevatorRoom(roomLookup, result.GetRoom(RoomCategory.ExitElevator), result.WorldOffset, settings, goals);
        BuildShaftRoom(roomLookup, result.GetRoom(RoomCategory.ExitShaft), result.WorldOffset, settings, goals);
        BuildStairsRoom(roomLookup, result.GetRoom(RoomCategory.ExitStairs), result.WorldOffset, settings, goals);
    }

    private static void BuildElevatorRoom(Dictionary<string, Transform> roomLookup, GeneratedRoom room, Vector2 worldOffset, FloorGeneratorSettings settings, FloorGoalController goals)
    {
        if (room == null || !roomLookup.ContainsKey(room.InstanceId))
        {
            return;
        }

        var roomRoot = roomLookup[room.InstanceId];
        var center = GetRoomWorldCenter(room.Rect, settings, worldOffset);
        var min = GetRoomWorldMin(room.Rect, settings, worldOffset);
        var max = GetRoomWorldMax(room.Rect, settings, worldOffset);

        CreateCube(roomRoot, "ElevatorBackPanel", new Vector3(center.x, 1.2f, max.z - 0.35f), new Vector3(6.8f, 2.4f, 0.18f), GetPalette().Metal, true, new Vector2(1.5f, 1f));
        CreateCube(roomRoot, "ElevatorLeftWall", new Vector3(min.x + 0.32f, 1.2f, center.z + 1.2f), new Vector3(0.18f, 2.4f, 4.2f), GetPalette().Metal, true, Vector2.one);
        CreateCube(roomRoot, "ElevatorRightWall", new Vector3(max.x - 0.32f, 1.2f, center.z + 1.2f), new Vector3(0.18f, 2.4f, 4.2f), GetPalette().Metal, true, Vector2.one);
        CreateCube(roomRoot, "ElevatorCabinCeiling", new Vector3(center.x, 2.45f, center.z + 1.1f), new Vector3(6.4f, 0.12f, 4f), GetPalette().Metal, false, new Vector2(2f, 1f));
        CreateCube(roomRoot, "ElevatorThreshold", new Vector3(center.x, 0.03f, min.z + 0.5f), new Vector3(3.1f, 0.06f, 0.7f), GetPalette().Accent, true, Vector2.one);

        var leftDoor = CreateCube(roomRoot, "ElevatorDoorLeft", new Vector3(center.x - 1.02f, 1.1f, min.z + 0.16f), new Vector3(1.95f, 2.2f, 0.12f), GetPalette().Metal, true, Vector2.one);
        var rightDoor = CreateCube(roomRoot, "ElevatorDoorRight", new Vector3(center.x + 1.02f, 1.1f, min.z + 0.16f), new Vector3(1.95f, 2.2f, 0.12f), GetPalette().Metal, true, Vector2.one);
        CreateDoorAnchor(roomRoot, "Anchor_ElevatorDoorLeft", leftDoor.transform.position, Quaternion.identity, GeneratedDoorVisualType.ElevatorLeft);
        CreateDoorAnchor(roomRoot, "Anchor_ElevatorDoorRight", rightDoor.transform.position, Quaternion.identity, GeneratedDoorVisualType.ElevatorRight);

        var panel = CreateCube(roomRoot, "ElevatorPanel", new Vector3(max.x - 0.6f, 1.2f, min.z + 1.45f), new Vector3(0.28f, 1.1f, 0.44f), GetPalette().Accent, true, Vector2.one);
        var interactable = panel.AddComponent<ElevatorRepairInteractable>();
        interactable.Configure(goals, leftDoor.transform, rightDoor.transform);
        CreateAnchor(roomRoot, "Anchor_ElevatorPanel", panel.transform.position);
    }

    private static void BuildShaftRoom(Dictionary<string, Transform> roomLookup, GeneratedRoom room, Vector2 worldOffset, FloorGeneratorSettings settings, FloorGoalController goals)
    {
        if (room == null || !roomLookup.ContainsKey(room.InstanceId))
        {
            return;
        }

        var roomRoot = roomLookup[room.InstanceId];
        var center = GetRoomWorldCenter(room.Rect, settings, worldOffset);
        var min = GetRoomWorldMin(room.Rect, settings, worldOffset);
        var max = GetRoomWorldMax(room.Rect, settings, worldOffset);

        CreateCube(roomRoot, "ServiceCabinet", new Vector3(max.x - 0.6f, 1f, max.z - 0.75f), new Vector3(0.9f, 2f, 0.6f), GetPalette().Metal, true, Vector2.one);
        CreateCube(roomRoot, "CableCrate", new Vector3(min.x + 0.75f, 0.32f, max.z - 0.8f), new Vector3(1f, 0.64f, 1f), GetPalette().Wood, true, Vector2.one);
        var hatch = CreateCube(roomRoot, "ShaftHatch", new Vector3(center.x, 0.08f, center.z + 0.4f), new Vector3(2.3f, 0.12f, 2.3f), GetPalette().Metal, true, Vector2.one);
        var hatchInteractable = hatch.AddComponent<ShaftHatchInteractable>();
        hatchInteractable.Configure(goals, hatch.transform);
    }

    private static void BuildStairsRoom(Dictionary<string, Transform> roomLookup, GeneratedRoom room, Vector2 worldOffset, FloorGeneratorSettings settings, FloorGoalController goals)
    {
        if (room == null || !roomLookup.ContainsKey(room.InstanceId))
        {
            return;
        }

        var roomRoot = roomLookup[room.InstanceId];
        var center = GetRoomWorldCenter(room.Rect, settings, worldOffset);
        var min = GetRoomWorldMin(room.Rect, settings, worldOffset);

        var door = CreateCube(roomRoot, "StairsDoor", new Vector3(center.x, 1.1f, min.z + 0.16f), new Vector3(1.8f, 2.2f, 0.12f), GetPalette().Wood, true, Vector2.one);
        var stairsDoor = door.AddComponent<StairsDoorInteractable>();
        stairsDoor.Configure(goals, door.transform);
        CreateDoorAnchor(roomRoot, "Anchor_StairsDoor", door.transform.position, Quaternion.identity, GeneratedDoorVisualType.Exit);

        CreateCube(roomRoot, "StairsLowerLanding", new Vector3(center.x, 0.12f, center.z + 1.7f), new Vector3(3.4f, 0.24f, 2f), GetPalette().DarkConcrete, true, Vector2.one);
        var ramp = CreateCube(roomRoot, "StairsRamp", new Vector3(center.x, 1.05f, center.z + 0.2f), new Vector3(3.2f, 0.2f, 4.8f), GetPalette().DarkConcrete, true, Vector2.one);
        ramp.transform.rotation = Quaternion.Euler(-22f, 0f, 0f);
        CreateCube(roomRoot, "StairsUpperLanding", new Vector3(center.x, 2f, center.z - 1.65f), new Vector3(3.4f, 0.24f, 2f), GetPalette().DarkConcrete, true, Vector2.one);
        CreateAnchor(roomRoot, "Anchor_StairsModel", new Vector3(center.x, 0f, center.z + 0.25f));
    }

    private static void SpawnPickups(FloorGenerationResult result, FloorGeneratorSettings settings, Dictionary<string, Transform> roomLookup)
    {
        var spawnPlan = new List<Tuple<string, RoomCategory, Vector3>>
        {
            Tuple.Create("tape", RoomCategory.Start, new Vector3(0.9f, 0f, 0.85f)),
            Tuple.Create("keycard", RoomCategory.Office, new Vector3(0.85f, 0f, 0.65f)),
            Tuple.Create("fuse", RoomCategory.Utility, new Vector3(-0.8f, 0f, 0.55f)),
            Tuple.Create("crowbar", RoomCategory.Storage, new Vector3(-0.7f, 0f, 0.7f)),
            Tuple.Create("rope", RoomCategory.Utility, new Vector3(0.8f, 0f, -0.55f)),
            Tuple.Create("lockpick", RoomCategory.Office, new Vector3(-0.85f, 0f, -0.55f)),
        };

        for (var i = 0; i < spawnPlan.Count; i++)
        {
            var room = FindPickupRoom(result, spawnPlan[i].Item2, i);
            if (room == null || !roomLookup.ContainsKey(room.InstanceId))
            {
                continue;
            }

            var roomRoot = roomLookup[room.InstanceId];
            var worldPosition = GetRoomWorldCenter(room.Rect, settings, result.WorldOffset) + spawnPlan[i].Item3;
            CreatePickup(roomRoot, MvpRuntimeContent.GetItem(spawnPlan[i].Item1), worldPosition);
        }
    }

    private static GeneratedRoom FindPickupRoom(FloorGenerationResult result, RoomCategory preferredCategory, int index)
    {
        var matches = new List<GeneratedRoom>();
        for (var i = 0; i < result.Rooms.Count; i++)
        {
            if (result.Rooms[i].Category == preferredCategory)
            {
                matches.Add(result.Rooms[i]);
            }
        }

        return matches.Count > 0 ? matches[index % matches.Count] : (result.Rooms.Count > 0 ? result.Rooms[0] : null);
    }

    private static void CreatePickup(Transform parent, ItemDefinition item, Vector3 worldPosition)
    {
        if (item == null)
        {
            return;
        }

        var stand = CreateCube(parent, string.Format("PickupStand_{0}", item.Id), new Vector3(worldPosition.x, 0.22f, worldPosition.z), new Vector3(0.65f, 0.44f, 0.65f), GetPalette().Wood, true, Vector2.one);
        var pickup = CreateCube(parent, string.Format("Pickup_{0}", item.Id), stand.transform.position + Vector3.up * 0.4f, new Vector3(0.28f, 0.18f, 0.42f), GetPalette().Accent, true, Vector2.one);
        var pickupCollider = pickup.GetComponent<BoxCollider>();
        if (pickupCollider != null)
        {
            pickupCollider.size = new Vector3(1.5f, 2f, 1.5f);
            pickupCollider.center = new Vector3(0f, 0.2f, 0f);
        }

        var interactable = pickup.AddComponent<ItemPickupInteractable>();
        interactable.Configure(item, 1);
    }

    private static void CreateRoomAnchors(FloorGenerationResult result, FloorGeneratorSettings settings, Dictionary<string, Transform> roomLookup)
    {
        for (var i = 0; i < result.Rooms.Count; i++)
        {
            var room = result.Rooms[i];
            Transform roomRoot;
            if (!roomLookup.TryGetValue(room.InstanceId, out roomRoot))
            {
                continue;
            }

            var min = GetRoomWorldMin(room.Rect, settings, result.WorldOffset);
            var max = GetRoomWorldMax(room.Rect, settings, result.WorldOffset);
            var center = GetRoomWorldCenter(room.Rect, settings, result.WorldOffset);

            if (room.Category == RoomCategory.Start)
            {
                CreateAnchor(roomRoot, "Spawn_PlayerStart", new Vector3(center.x, 1.05f, min.z + 1.6f));
                CreateAnchor(roomRoot, "Anchor_ReceptionBench", new Vector3(min.x + 1.4f, 0f, min.z + 1.15f));
                CreateAnchor(roomRoot, "Anchor_ReceptionTrash", new Vector3(max.x - 0.9f, 0f, min.z + 1.1f));
            }
            else if (room.Category == RoomCategory.Hub)
            {
                CreateAnchor(roomRoot, "Anchor_HubBench", new Vector3(min.x + 1.15f, 0f, center.z));
                CreateAnchor(roomRoot, "Anchor_HubDesk", new Vector3(max.x - 1.3f, 0f, center.z + 0.4f));
                CreateAnchor(roomRoot, "Anchor_HubChair", new Vector3(max.x - 2f, 0f, center.z - 0.1f));
                CreateAnchor(roomRoot, "Anchor_HubMonitor", new Vector3(max.x - 1.25f, 0.88f, center.z + 0.5f));
            }
            else if (room.Category == RoomCategory.Office)
            {
                CreateAnchor(roomRoot, "Anchor_OfficeDeskA", new Vector3(min.x + 1.35f, 0f, center.z + 0.55f));
                CreateAnchor(roomRoot, "Anchor_OfficeChairA", new Vector3(min.x + 0.7f, 0f, center.z + 0.55f));
                CreateAnchor(roomRoot, "Anchor_OfficeMonitorA", new Vector3(min.x + 1.45f, 0.88f, center.z + 0.55f));
                CreateAnchor(roomRoot, "Anchor_OfficeDeskB", new Vector3(max.x - 1.55f, 0f, center.z - 0.65f));
                CreateAnchor(roomRoot, "Anchor_OfficeChairB", new Vector3(max.x - 2.15f, 0f, center.z - 0.65f));
                CreateAnchor(roomRoot, "Anchor_OfficeMonitorB", new Vector3(max.x - 1.45f, 0.88f, center.z - 0.65f));
                CreateAnchor(roomRoot, "Anchor_OfficeTrash", new Vector3(center.x, 0f, max.z - 0.9f));
            }
            else if (room.Category == RoomCategory.Storage)
            {
                CreateAnchor(roomRoot, "Anchor_StorageBoxesA", new Vector3(min.x + 0.95f, 0f, min.z + 0.95f));
                CreateAnchor(roomRoot, "Anchor_StorageBoxesB", new Vector3(max.x - 0.95f, 0f, max.z - 0.95f));
                CreateAnchor(roomRoot, "Anchor_StorageBench", new Vector3(center.x, 0f, min.z + 1f));
            }
            else if (room.Category == RoomCategory.Utility)
            {
                CreateAnchor(roomRoot, "Anchor_UtilityBench", new Vector3(min.x + 1.15f, 0f, min.z + 1.05f));
                CreateAnchor(roomRoot, "Anchor_UtilityBox", new Vector3(max.x - 1.05f, 0f, center.z));
                CreateAnchor(roomRoot, "Anchor_UtilityTrash", new Vector3(max.x - 0.9f, 0f, max.z - 0.9f));
            }
        }
    }

    private static GameObject CreateAnchor(Transform parent, string name, Vector3 worldPosition)
    {
        var anchor = new GameObject(name);
        anchor.transform.SetParent(parent, false);
        anchor.transform.position = worldPosition;
        return anchor;
    }

    private static GameObject CreateDoorAnchor(Transform parent, string name, Vector3 worldPosition, Quaternion rotation, GeneratedDoorVisualType visualType)
    {
        var anchor = CreateAnchor(parent, name, worldPosition);
        anchor.transform.rotation = rotation;
        anchor.AddComponent<GeneratedDoorAnchor>().Configure(visualType);
        return anchor;
    }

    private static GameObject CreateCube(Transform parent, string name, Vector3 worldPosition, Vector3 localScale, Material material, bool withCollider, Vector2 textureScale)
    {
        var cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
        cube.name = name;
        cube.transform.SetParent(parent, false);
        cube.transform.position = worldPosition;
        cube.transform.localScale = localScale;

        if (!withCollider)
        {
            var collider = cube.GetComponent<Collider>();
            if (collider != null)
            {
                UnityEngine.Object.DestroyImmediate(collider);
            }
        }

        var renderer = cube.GetComponent<Renderer>();
        if (renderer != null && material != null)
        {
            var materialInstance = new Material(material);
            materialInstance.hideFlags = HideFlags.HideAndDontSave;
            SetTextureScale(materialInstance, textureScale);
            renderer.sharedMaterial = materialInstance;
        }

        return cube;
    }

    private static Vector2 GetWorldSize(RectInt rect, FloorGeneratorSettings settings)
    {
        return new Vector2(rect.width * settings.CellSize, rect.height * settings.CellSize);
    }

    private static Vector3 GetRoomWorldCenter(RectInt rect, FloorGeneratorSettings settings, Vector2 worldOffset)
    {
        var size = GetWorldSize(rect, settings);
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

    private static Bounds GetLayoutBounds(FloorGenerationResult result, FloorGeneratorSettings settings)
    {
        var first = GetRoomWorldCenter(result.Rooms[0].Rect, settings, result.WorldOffset);
        var bounds = new Bounds(first, Vector3.zero);
        for (var i = 0; i < result.Rooms.Count; i++)
        {
            var room = result.Rooms[i];
            var center = GetRoomWorldCenter(room.Rect, settings, result.WorldOffset);
            var size = new Vector3(room.Rect.width * settings.CellSize, settings.WallHeight, room.Rect.height * settings.CellSize);
            bounds.Encapsulate(new Bounds(center + Vector3.up * settings.WallHeight * 0.5f, size));
        }

        return bounds;
    }

    private static Vector3 GetCellWorldCenter(Vector2Int cell, FloorGeneratorSettings settings, Vector2 worldOffset)
    {
        return new Vector3((cell.x + 0.5f) * settings.CellSize + worldOffset.x, 0f, (cell.y + 0.5f) * settings.CellSize + worldOffset.y);
    }

    private static Vector3 GetEdgeCenter(Vector2Int cell, RoomSide side, FloorGeneratorSettings settings, Vector2 worldOffset)
    {
        var center = GetCellWorldCenter(cell, settings, worldOffset);
        switch (side)
        {
            case RoomSide.North:
                return new Vector3(center.x, settings.WallHeight * 0.5f, center.z + settings.CellSize * 0.5f - settings.WallThickness * 0.5f);
            case RoomSide.South:
                return new Vector3(center.x, settings.WallHeight * 0.5f, center.z - settings.CellSize * 0.5f + settings.WallThickness * 0.5f);
            case RoomSide.East:
                return new Vector3(center.x + settings.CellSize * 0.5f - settings.WallThickness * 0.5f, settings.WallHeight * 0.5f, center.z);
            default:
                return new Vector3(center.x - settings.CellSize * 0.5f + settings.WallThickness * 0.5f, settings.WallHeight * 0.5f, center.z);
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

    private static bool IsOpenZone(RoomCategory category)
    {
        return category == RoomCategory.Start || category == RoomCategory.Corridor || category == RoomCategory.Hub;
    }

    private static bool IsSupportRoom(RoomCategory category)
    {
        return category == RoomCategory.Office || category == RoomCategory.Storage || category == RoomCategory.Utility;
    }

    private static Material GetFloorMaterial(RoomCategory category)
    {
        switch (category)
        {
            case RoomCategory.Start:
                return GetPalette().ReceptionFloor;
            case RoomCategory.Office:
                return GetPalette().OfficeFloor;
            case RoomCategory.Storage:
                return GetPalette().StorageFloor;
            case RoomCategory.Utility:
                return GetPalette().UtilityFloor;
            case RoomCategory.ExitStairs:
            case RoomCategory.ExitShaft:
                return GetPalette().DarkConcrete;
            default:
                return GetPalette().CorridorFloor;
        }
    }

    private static MaterialPalette GetPalette()
    {
        if (_palette != null)
        {
            return _palette;
        }

        _palette = new MaterialPalette();
        _palette.ReceptionFloor = CreateMaterial("Mat_ReceptionFloor", new Color(0.27f, 0.29f, 0.33f), CreateCheckerTexture("Tex_Reception", new Color(0.2f, 0.23f, 0.28f), new Color(0.16f, 0.18f, 0.22f), new Color(0.33f, 0.37f, 0.41f)), 0.08f, 0.2f);
        _palette.CorridorFloor = CreateMaterial("Mat_CorridorFloor", new Color(0.42f, 0.45f, 0.49f), CreateCheckerTexture("Tex_Corridor", new Color(0.35f, 0.38f, 0.42f), new Color(0.28f, 0.31f, 0.35f), new Color(0.56f, 0.59f, 0.63f)), 0.04f, 0.12f);
        _palette.OfficeFloor = CreateMaterial("Mat_OfficeFloor", new Color(0.24f, 0.29f, 0.34f), CreateFabricTexture("Tex_OfficeCarpet", new Color(0.18f, 0.24f, 0.3f), new Color(0.12f, 0.17f, 0.22f)), 0.02f, 0.08f);
        _palette.StorageFloor = CreateMaterial("Mat_StorageFloor", new Color(0.39f, 0.34f, 0.29f), CreateCheckerTexture("Tex_Storage", new Color(0.35f, 0.31f, 0.26f), new Color(0.28f, 0.24f, 0.2f), new Color(0.5f, 0.43f, 0.35f)), 0.03f, 0.06f);
        _palette.UtilityFloor = CreateMaterial("Mat_UtilityFloor", new Color(0.3f, 0.33f, 0.31f), CreateCheckerTexture("Tex_Utility", new Color(0.23f, 0.26f, 0.24f), new Color(0.18f, 0.21f, 0.19f), new Color(0.4f, 0.44f, 0.41f)), 0.02f, 0.1f);
        _palette.Ceiling = CreateMaterial("Mat_Ceiling", new Color(0.84f, 0.86f, 0.88f), CreateCheckerTexture("Tex_Ceiling", new Color(0.78f, 0.8f, 0.82f), new Color(0.74f, 0.76f, 0.78f), new Color(0.92f, 0.93f, 0.95f)), 0f, 0.02f);
        _palette.WallPaint = CreateMaterial("Mat_WallPaint", new Color(0.81f, 0.83f, 0.86f), CreateNoiseTexture("Tex_Walls", new Color(0.75f, 0.77f, 0.8f), new Color(0.69f, 0.71f, 0.74f)), 0f, 0.03f);
        _palette.Partition = CreateMaterial("Mat_Partition", new Color(0.67f, 0.7f, 0.74f), CreateNoiseTexture("Tex_Partition", new Color(0.62f, 0.66f, 0.7f), new Color(0.54f, 0.58f, 0.62f)), 0f, 0.05f);
        _palette.Metal = CreateMaterial("Mat_Metal", new Color(0.37f, 0.4f, 0.44f), CreateLinearTexture("Tex_Metal", new Color(0.32f, 0.35f, 0.38f), new Color(0.44f, 0.47f, 0.5f)), 0.22f, 0.48f);
        _palette.Wood = CreateMaterial("Mat_Wood", new Color(0.49f, 0.35f, 0.23f), CreateLinearTexture("Tex_Wood", new Color(0.43f, 0.29f, 0.18f), new Color(0.58f, 0.41f, 0.27f)), 0.05f, 0.16f);
        _palette.Accent = CreateMaterial("Mat_Accent", new Color(0.23f, 0.56f, 0.71f), CreateLinearTexture("Tex_Accent", new Color(0.18f, 0.44f, 0.56f), new Color(0.28f, 0.68f, 0.86f)), 0.1f, 0.22f);
        _palette.Fixture = CreateEmissiveMaterial("Mat_Fixture", new Color(0.92f, 0.93f, 0.88f), new Color(2.5f, 2.45f, 2.3f));
        _palette.DarkConcrete = CreateMaterial("Mat_DarkConcrete", new Color(0.26f, 0.28f, 0.31f), CreateNoiseTexture("Tex_Concrete", new Color(0.21f, 0.23f, 0.26f), new Color(0.3f, 0.32f, 0.35f)), 0.01f, 0.12f);
        return _palette;
    }

    private static Material CreateMaterial(string name, Color tint, Texture2D texture, float metallic, float smoothness)
    {
        var material = new Material(FindBestLitShader());
        material.name = name;
        material.hideFlags = HideFlags.HideAndDontSave;
        SetColor(material, tint);
        SetTexture(material, texture);
        if (material.HasProperty("_Metallic")) material.SetFloat("_Metallic", metallic);
        if (material.HasProperty("_Smoothness")) material.SetFloat("_Smoothness", smoothness);
        return material;
    }

    private static Material CreateEmissiveMaterial(string name, Color tint, Color emission)
    {
        var material = CreateMaterial(name, tint, null, 0f, 0.05f);
        if (material.HasProperty("_EmissionColor"))
        {
            material.SetColor("_EmissionColor", emission);
            material.EnableKeyword("_EMISSION");
        }
        return material;
    }

    private static Shader FindBestLitShader()
    {
        var shader = Shader.Find("HDRP/Lit") ?? Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard") ?? Shader.Find("Legacy Shaders/Diffuse");
        return shader ?? Shader.Find("Sprites/Default");
    }

    private static void SetColor(Material material, Color color)
    {
        if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", color);
        else if (material.HasProperty("_Color")) material.color = color;
    }

    private static void SetTexture(Material material, Texture2D texture)
    {
        if (texture == null) return;
        if (material.HasProperty("_BaseColorMap")) material.SetTexture("_BaseColorMap", texture);
        else if (material.HasProperty("_BaseMap")) material.SetTexture("_BaseMap", texture);
        else if (material.HasProperty("_MainTex")) material.SetTexture("_MainTex", texture);
    }

    private static void SetTextureScale(Material material, Vector2 scale)
    {
        if (material.HasProperty("_BaseColorMap")) material.SetTextureScale("_BaseColorMap", scale);
        else if (material.HasProperty("_BaseMap")) material.SetTextureScale("_BaseMap", scale);
        else if (material.HasProperty("_MainTex")) material.SetTextureScale("_MainTex", scale);
    }

    private static Texture2D CreateCheckerTexture(string name, Color colorA, Color colorB, Color lineColor)
    {
        var texture = new Texture2D(64, 64, TextureFormat.RGBA32, false);
        texture.name = name;
        texture.hideFlags = HideFlags.HideAndDontSave;
        texture.wrapMode = TextureWrapMode.Repeat;
        texture.filterMode = FilterMode.Bilinear;
        for (var x = 0; x < texture.width; x++)
        {
            for (var y = 0; y < texture.height; y++)
            {
                var pixel = ((x / 16) + (y / 16)) % 2 == 0 ? colorA : colorB;
                if (x % 16 == 0 || y % 16 == 0) pixel = lineColor;
                texture.SetPixel(x, y, pixel);
            }
        }
        texture.Apply();
        return texture;
    }

    private static Texture2D CreateLinearTexture(string name, Color colorA, Color colorB)
    {
        var texture = new Texture2D(32, 32, TextureFormat.RGBA32, false);
        texture.name = name;
        texture.hideFlags = HideFlags.HideAndDontSave;
        texture.wrapMode = TextureWrapMode.Repeat;
        texture.filterMode = FilterMode.Bilinear;
        for (var x = 0; x < texture.width; x++)
        {
            var stripe = Mathf.PingPong(x / 31f * 3f, 1f);
            var color = Color.Lerp(colorA, colorB, stripe);
            for (var y = 0; y < texture.height; y++) texture.SetPixel(x, y, color);
        }
        texture.Apply();
        return texture;
    }

    private static Texture2D CreateNoiseTexture(string name, Color colorA, Color colorB)
    {
        var texture = new Texture2D(48, 48, TextureFormat.RGBA32, false);
        texture.name = name;
        texture.hideFlags = HideFlags.HideAndDontSave;
        texture.wrapMode = TextureWrapMode.Repeat;
        texture.filterMode = FilterMode.Bilinear;
        for (var x = 0; x < texture.width; x++)
        {
            for (var y = 0; y < texture.height; y++)
            {
                texture.SetPixel(x, y, Color.Lerp(colorA, colorB, Mathf.PerlinNoise(x * 0.14f, y * 0.14f)));
            }
        }
        texture.Apply();
        return texture;
    }

    private static Texture2D CreateFabricTexture(string name, Color colorA, Color colorB)
    {
        var texture = new Texture2D(48, 48, TextureFormat.RGBA32, false);
        texture.name = name;
        texture.hideFlags = HideFlags.HideAndDontSave;
        texture.wrapMode = TextureWrapMode.Repeat;
        texture.filterMode = FilterMode.Bilinear;
        for (var x = 0; x < texture.width; x++)
        {
            for (var y = 0; y < texture.height; y++)
            {
                var stripe = ((x + y) % 6 == 0 || Mathf.Abs(x - y) % 7 == 0) ? 0.75f : 0.25f;
                var noise = Mathf.PerlinNoise(x * 0.23f, y * 0.23f) * 0.4f;
                texture.SetPixel(x, y, Color.Lerp(colorA, colorB, stripe + noise));
            }
        }
        texture.Apply();
        return texture;
    }

    private static void CreateSolidBoundary(Transform parent, string name, Vector3 edgeCenter, RoomSide side, float length, float height, float thickness, Material material)
    {
        var scale = side == RoomSide.North || side == RoomSide.South ? new Vector3(length, height, thickness) : new Vector3(thickness, height, length);
        CreateCube(parent, name, edgeCenter, scale, material, true, Vector2.one);
    }

    private static void CreateDoorway(Transform parent, string name, Vector3 edgeCenter, RoomSide side, FloorGeneratorSettings settings, float openingWidth, float openingHeight, Material material, GeneratedDoorVisualType visualType)
    {
        var sideWidth = Mathf.Max(0.25f, (settings.CellSize - openingWidth) * 0.5f);
        var lintelHeight = Mathf.Max(0.45f, settings.WallHeight - openingHeight);
        var crossAxis = side == RoomSide.North || side == RoomSide.South ? Vector3.right : Vector3.forward;
        var leftCenter = edgeCenter - crossAxis * (openingWidth * 0.5f + sideWidth * 0.5f);
        var rightCenter = edgeCenter + crossAxis * (openingWidth * 0.5f + sideWidth * 0.5f);
        var lintelCenter = edgeCenter + Vector3.up * (openingHeight * 0.5f + lintelHeight * 0.5f);
        CreateSolidBoundary(parent, name + "_LeftJamb", leftCenter, side, sideWidth, settings.WallHeight, settings.WallThickness, material);
        CreateSolidBoundary(parent, name + "_RightJamb", rightCenter, side, sideWidth, settings.WallHeight, settings.WallThickness, material);
        var lintelScale = side == RoomSide.North || side == RoomSide.South ? new Vector3(settings.CellSize, lintelHeight, settings.WallThickness) : new Vector3(settings.WallThickness, lintelHeight, settings.CellSize);
        CreateCube(parent, name + "_Lintel", lintelCenter, lintelScale, material, true, Vector2.one);
        if (visualType == GeneratedDoorVisualType.Office || visualType == GeneratedDoorVisualType.Service)
        {
            CreateDoorAnchor(parent, name + "_Anchor", edgeCenter, side == RoomSide.East || side == RoomSide.West ? Quaternion.Euler(0f, 90f, 0f) : Quaternion.identity, visualType);
        }
    }
}

public sealed class GeneratedDoorAnchor : MonoBehaviour
{
    [SerializeField] private GeneratedDoorVisualType _visualType;

    public GeneratedDoorVisualType VisualType => _visualType;

    public void Configure(GeneratedDoorVisualType visualType)
    {
        _visualType = visualType;
    }
}

public sealed class GeneratedRoomMarker : MonoBehaviour
{
    [SerializeField] private string _instanceId = string.Empty;
    [SerializeField] private RoomCategory _category;
    [SerializeField] private Vector2Int _gridSize;
    [SerializeField] private Vector3 _worldCenter;

    public string InstanceId => _instanceId;
    public RoomCategory Category => _category;
    public Vector2Int GridSize => _gridSize;
    public Vector3 WorldCenter => _worldCenter;

    public void Configure(string instanceId, RoomCategory category, Vector2Int gridSize, Vector3 worldCenter)
    {
        _instanceId = instanceId;
        _category = category;
        _gridSize = gridSize;
        _worldCenter = worldCenter;
    }
}

public sealed class GeneratedFloorInfo : MonoBehaviour
{
    [SerializeField] private int _seed;
    [SerializeField] private string _validationSummary = string.Empty;

    public int Seed => _seed;
    public string ValidationSummary => _validationSummary;

    public void Configure(int seed, string validationSummary)
    {
        _seed = seed;
        _validationSummary = validationSummary;
    }
}
}


