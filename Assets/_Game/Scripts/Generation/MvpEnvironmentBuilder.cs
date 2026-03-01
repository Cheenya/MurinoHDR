using MurinoHDR.Interaction;
using UnityEngine;

namespace MurinoHDR.Generation
{

public static class MvpEnvironmentBuilder
{
    private const string RootName = "MVP_Environment";
    private const float FloorThickness = 0.2f;
    private const float WallHeight = 3f;
    private const float WallThickness = 0.25f;

    public static void EnsureEnvironment()
    {
        if (GameObject.Find(RootName) != null)
        {
            return;
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
        BuildLighting(root.transform);

        BuildFloorZone(root.transform, "MainFloor", Vector3.zero, new Vector2(30f, 30f), new Color(0.17f, 0.19f, 0.23f));
        BuildFloorZone(root.transform, "StartZone", new Vector3(0f, 0f, -8f), new Vector2(12f, 8f), new Color(0.22f, 0.27f, 0.32f));
        BuildFloorZone(root.transform, "Corridor", new Vector3(0f, 0f, 2f), new Vector2(8f, 14f), new Color(0.2f, 0.22f, 0.27f));
        BuildFloorZone(root.transform, "ExitHub", new Vector3(0f, 0f, 11f), new Vector2(18f, 8f), new Color(0.23f, 0.23f, 0.23f));

        BuildBoundary(root.transform, "NorthBoundary", new Vector3(0f, WallHeight * 0.5f, 15f), new Vector3(30f, WallHeight, WallThickness));
        BuildBoundary(root.transform, "SouthBoundary", new Vector3(0f, WallHeight * 0.5f, -15f), new Vector3(30f, WallHeight, WallThickness));
        BuildBoundary(root.transform, "EastBoundary", new Vector3(15f, WallHeight * 0.5f, 0f), new Vector3(WallThickness, WallHeight, 30f));
        BuildBoundary(root.transform, "WestBoundary", new Vector3(-15f, WallHeight * 0.5f, 0f), new Vector3(WallThickness, WallHeight, 30f));

        BuildProp(root.transform, "Crate_A", new Vector3(-5f, 0.6f, -5f), new Vector3(1.5f, 1.2f, 1.5f), new Color(0.28f, 0.24f, 0.16f));
        BuildProp(root.transform, "Crate_B", new Vector3(4f, 0.5f, -2f), new Vector3(1.2f, 1f, 1.2f), new Color(0.24f, 0.22f, 0.18f));
        BuildProp(root.transform, "Barrier", new Vector3(0f, 0.7f, 6f), new Vector3(5f, 1.4f, 0.6f), new Color(0.18f, 0.18f, 0.22f));

        BuildExitStation(root.transform, "ElevatorStation", new Vector3(-6f, 1f, 12f), new Color(0.2f, 0.75f, 0.2f));
        BuildExitStation(root.transform, "ShaftStation", new Vector3(0f, 1f, 12f), new Color(0.75f, 0.55f, 0.2f));
        BuildExitStation(root.transform, "StairsStation", new Vector3(6f, 1f, 12f), new Color(0.2f, 0.55f, 0.8f));

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
        lightObject.transform.rotation = Quaternion.Euler(40f, -30f, 0f);

        var light = lightObject.AddComponent<Light>();
        light.type = LightType.Directional;
        light.intensity = 1.1f;
    }

    private static void BuildFloorZone(Transform parent, string name, Vector3 position, Vector2 size, Color color)
    {
        var floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
        floor.name = name;
        floor.transform.SetParent(parent, false);
        floor.transform.localPosition = position;
        floor.transform.localScale = new Vector3(size.x, FloorThickness, size.y);

        var renderer = floor.GetComponent<Renderer>();
        if (renderer != null)
        {
            renderer.material.color = color;
        }
    }

    private static void BuildBoundary(Transform parent, string name, Vector3 position, Vector3 scale)
    {
        var wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
        wall.name = name;
        wall.transform.SetParent(parent, false);
        wall.transform.localPosition = position;
        wall.transform.localScale = scale;

        var renderer = wall.GetComponent<Renderer>();
        if (renderer != null)
        {
            renderer.material.color = new Color(0.12f, 0.13f, 0.16f);
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

    private static void BuildExitStation(Transform root, string name, Vector3 position, Color color)
    {
        var station = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        station.name = name;
        station.transform.SetParent(root, false);
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
