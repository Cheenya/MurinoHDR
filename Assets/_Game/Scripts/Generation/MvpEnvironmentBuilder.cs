using MurinoHDR.Interaction;
using UnityEngine;

namespace MurinoHDR.Generation
{

public static class MvpEnvironmentBuilder
{
    private const string RootName = "MVP_Environment";
    private const string LayoutVersionName = "Layout_v2";
    private const float FloorThickness = 0.2f;
    private const float WallHeight = 3f;
    private const float WallThickness = 0.25f;

    public static void EnsureEnvironment()
    {
        var existingRoot = GameObject.Find(RootName);
        if (existingRoot != null && existingRoot.transform.Find(LayoutVersionName) != null)
        {
            return;
        }

        if (existingRoot != null)
        {
            Object.DestroyImmediate(existingRoot);
        }

        BuildEnvironment();
    }

    public static void RebuildEnvironment()
    {
        var existingRoot = GameObject.Find(RootName);
        if (existingRoot != null)
        {
            Object.DestroyImmediate(existingRoot);
        }

        BuildEnvironment();
    }

    private static void BuildEnvironment()
    {
        var root = new GameObject(RootName);
        var versionMarker = new GameObject(LayoutVersionName);
        versionMarker.transform.SetParent(root.transform, false);
        BuildLighting(root.transform);

        var paletteFloor = new Color(0.17f, 0.19f, 0.23f);
        var paletteAccent = new Color(0.24f, 0.28f, 0.34f);
        var paletteWall = new Color(0.11f, 0.13f, 0.17f);

        BuildFloor(root.transform, "StartRoomFloor", new Vector3(0f, -0.1f, -10f), new Vector3(10f, FloorThickness, 8f), paletteAccent);
        BuildFloor(root.transform, "CorridorFloor", new Vector3(0f, -0.1f, -3.5f), new Vector3(4f, FloorThickness, 5f), paletteFloor);
        BuildFloor(root.transform, "HubFloor", new Vector3(0f, -0.1f, 4f), new Vector3(12f, FloorThickness, 10f), paletteFloor);
        BuildFloor(root.transform, "LeftBranchFloor", new Vector3(-6f, -0.1f, 9.5f), new Vector3(4f, FloorThickness, 7f), paletteFloor);
        BuildFloor(root.transform, "CenterBranchFloor", new Vector3(0f, -0.1f, 11f), new Vector3(4f, FloorThickness, 10f), paletteFloor);
        BuildFloor(root.transform, "RightBranchFloor", new Vector3(6f, -0.1f, 9.5f), new Vector3(4f, FloorThickness, 7f), paletteFloor);
        BuildFloor(root.transform, "ElevatorRoomFloor", new Vector3(-6f, -0.1f, 14f), new Vector3(6f, FloorThickness, 6f), paletteAccent);
        BuildFloor(root.transform, "ShaftRoomFloor", new Vector3(0f, -0.1f, 16f), new Vector3(6f, FloorThickness, 6f), paletteAccent);
        BuildFloor(root.transform, "StairsRoomFloor", new Vector3(6f, -0.1f, 14f), new Vector3(6f, FloorThickness, 6f), paletteAccent);

        BuildWall(root.transform, "StartNorthWallLeft", new Vector3(-3.25f, WallHeight * 0.5f, -6f), new Vector3(3.5f, WallHeight, WallThickness), paletteWall);
        BuildWall(root.transform, "StartNorthWallRight", new Vector3(3.25f, WallHeight * 0.5f, -6f), new Vector3(3.5f, WallHeight, WallThickness), paletteWall);
        BuildWall(root.transform, "StartSouthWall", new Vector3(0f, WallHeight * 0.5f, -14f), new Vector3(10f, WallHeight, WallThickness), paletteWall);
        BuildWall(root.transform, "StartWestWall", new Vector3(-5f, WallHeight * 0.5f, -10f), new Vector3(WallThickness, WallHeight, 8f), paletteWall);
        BuildWall(root.transform, "StartEastWall", new Vector3(5f, WallHeight * 0.5f, -10f), new Vector3(WallThickness, WallHeight, 8f), paletteWall);

        BuildWall(root.transform, "CorridorWestWall", new Vector3(-2f, WallHeight * 0.5f, -3.5f), new Vector3(WallThickness, WallHeight, 5f), paletteWall);
        BuildWall(root.transform, "CorridorEastWall", new Vector3(2f, WallHeight * 0.5f, -3.5f), new Vector3(WallThickness, WallHeight, 5f), paletteWall);

        BuildWall(root.transform, "HubWestWallSouth", new Vector3(-6f, WallHeight * 0.5f, 1.5f), new Vector3(WallThickness, WallHeight, 5f), paletteWall);
        BuildWall(root.transform, "HubEastWallSouth", new Vector3(6f, WallHeight * 0.5f, 1.5f), new Vector3(WallThickness, WallHeight, 5f), paletteWall);
        BuildWall(root.transform, "HubWestWallNorth", new Vector3(-6f, WallHeight * 0.5f, 6.5f), new Vector3(WallThickness, WallHeight, 5f), paletteWall);
        BuildWall(root.transform, "HubEastWallNorth", new Vector3(6f, WallHeight * 0.5f, 6.5f), new Vector3(WallThickness, WallHeight, 5f), paletteWall);
        BuildWall(root.transform, "ElevatorWestWall", new Vector3(-9f, WallHeight * 0.5f, 14f), new Vector3(WallThickness, WallHeight, 6f), paletteWall);
        BuildWall(root.transform, "ElevatorEastWall", new Vector3(-3f, WallHeight * 0.5f, 14f), new Vector3(WallThickness, WallHeight, 6f), paletteWall);
        BuildWall(root.transform, "ElevatorNorthWall", new Vector3(-6f, WallHeight * 0.5f, 17f), new Vector3(6f, WallHeight, WallThickness), paletteWall);

        BuildWall(root.transform, "ShaftWestWall", new Vector3(-3f, WallHeight * 0.5f, 16f), new Vector3(WallThickness, WallHeight, 6f), paletteWall);
        BuildWall(root.transform, "ShaftEastWall", new Vector3(3f, WallHeight * 0.5f, 16f), new Vector3(WallThickness, WallHeight, 6f), paletteWall);
        BuildWall(root.transform, "ShaftNorthWall", new Vector3(0f, WallHeight * 0.5f, 19f), new Vector3(6f, WallHeight, WallThickness), paletteWall);

        BuildWall(root.transform, "StairsWestWall", new Vector3(3f, WallHeight * 0.5f, 14f), new Vector3(WallThickness, WallHeight, 6f), paletteWall);
        BuildWall(root.transform, "StairsEastWall", new Vector3(9f, WallHeight * 0.5f, 14f), new Vector3(WallThickness, WallHeight, 6f), paletteWall);
        BuildWall(root.transform, "StairsNorthWall", new Vector3(6f, WallHeight * 0.5f, 17f), new Vector3(6f, WallHeight, WallThickness), paletteWall);

        BuildProp(root.transform, "StartCrateStack", new Vector3(-2.5f, 0.7f, -11.5f), new Vector3(1.8f, 1.4f, 1.8f), new Color(0.3f, 0.22f, 0.15f));
        BuildProp(root.transform, "CorridorBarrier", new Vector3(0f, 0.45f, -1.5f), new Vector3(1.4f, 0.9f, 0.35f), new Color(0.19f, 0.2f, 0.22f));
        BuildProp(root.transform, "HubBench", new Vector3(3f, 0.4f, 4.5f), new Vector3(2.5f, 0.8f, 0.6f), new Color(0.23f, 0.24f, 0.28f));

        BuildExitStation(root.transform, "ElevatorStation", new Vector3(-6f, 1f, 14f), new Color(0.2f, 0.75f, 0.2f));
        BuildExitStation(root.transform, "ShaftStation", new Vector3(0f, 1f, 16f), new Color(0.75f, 0.55f, 0.2f));
        BuildExitStation(root.transform, "StairsStation", new Vector3(6f, 1f, 14f), new Color(0.2f, 0.55f, 0.8f));

        Debug.Log("[GEN] MVP environment generated");
    }

    private static void BuildLighting(Transform root)
    {
        if (Object.FindFirstObjectByType<Light>() != null)
        {
            return;
        }

        var lightObject = new GameObject("Directional Light");
        lightObject.transform.SetParent(root, false);
        lightObject.transform.rotation = Quaternion.Euler(42f, -26f, 0f);

        var light = lightObject.AddComponent<Light>();
        light.type = LightType.Directional;
        light.intensity = 1.05f;
    }

    private static void BuildFloor(Transform parent, string name, Vector3 position, Vector3 scale, Color color)
    {
        var floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
        floor.name = name;
        floor.transform.SetParent(parent, false);
        floor.transform.localPosition = position;
        floor.transform.localScale = scale;

        var renderer = floor.GetComponent<Renderer>();
        if (renderer != null)
        {
            renderer.material.color = color;
        }
    }

    private static void BuildWall(Transform parent, string name, Vector3 position, Vector3 scale, Color color)
    {
        var wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
        wall.name = name;
        wall.transform.SetParent(parent, false);
        wall.transform.localPosition = position;
        wall.transform.localScale = scale;

        var renderer = wall.GetComponent<Renderer>();
        if (renderer != null)
        {
            renderer.material.color = color;
        }
    }

    private static void BuildProp(Transform parent, string name, Vector3 position, Vector3 scale, Color color)
    {
        var prop = GameObject.CreatePrimitive(PrimitiveType.Cube);
        prop.name = name;
        prop.transform.SetParent(parent, false);
        prop.transform.localPosition = position;
        prop.transform.localScale = scale;

        var renderer = prop.GetComponent<Renderer>();
        if (renderer != null)
        {
            renderer.material.color = color;
        }
    }

    private static void BuildExitStation(Transform parent, string name, Vector3 position, Color color)
    {
        var station = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        station.name = name;
        station.transform.SetParent(parent, false);
        station.transform.localPosition = position;
        station.transform.localScale = new Vector3(0.8f, 1f, 0.8f);

        station.AddComponent<DebugDoorInteractable>();

        var renderer = station.GetComponent<Renderer>();
        if (renderer != null)
        {
            renderer.material.color = color;
        }
    }
}
}
