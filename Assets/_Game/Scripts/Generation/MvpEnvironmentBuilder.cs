using MurinoHDR.Interaction;
using UnityEngine;

namespace MurinoHDR.Generation
{

public static class MvpEnvironmentBuilder
{
    private const string RootName = "MVP_Environment";
    private const float RoomHeight = 3f;
    private const float WallThickness = 0.25f;
    private const float CorridorWidth = 4f;

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

        var startCenter = new Vector3(0f, 0f, 0f);
        BuildRoom(root.transform, "StartRoom", startCenter, new Vector2(10f, 10f), new Color(0.22f, 0.25f, 0.3f));

        BuildCorridor(root.transform, "MainCorridor", new Vector3(0f, 0f, 9f), new Vector2(24f, CorridorWidth));

        BuildRoom(root.transform, "ElevatorWing", new Vector3(-8f, 0f, 16f), new Vector2(8f, 8f), new Color(0.22f, 0.3f, 0.22f));
        BuildRoom(root.transform, "ShaftWing", new Vector3(0f, 0f, 18f), new Vector2(8f, 8f), new Color(0.3f, 0.26f, 0.21f));
        BuildRoom(root.transform, "StairsWing", new Vector3(8f, 0f, 16f), new Vector2(8f, 8f), new Color(0.24f, 0.22f, 0.3f));

        BuildExitStation(root.transform, "ElevatorStation", new Vector3(-8f, 1f, 16f), new Color(0.2f, 0.75f, 0.2f));
        BuildExitStation(root.transform, "ShaftStation", new Vector3(0f, 1f, 18f), new Color(0.75f, 0.55f, 0.2f));
        BuildExitStation(root.transform, "StairsStation", new Vector3(8f, 1f, 16f), new Color(0.2f, 0.55f, 0.8f));

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
        lightObject.transform.rotation = Quaternion.Euler(45f, -25f, 0f);

        var light = lightObject.AddComponent<Light>();
        light.type = LightType.Directional;
        light.intensity = 1.2f;
    }

    private static void BuildCorridor(Transform root, string name, Vector3 center, Vector2 size)
    {
        BuildRoom(root, name, center, size, new Color(0.2f, 0.2f, 0.22f));
    }

    private static void BuildRoom(Transform root, string name, Vector3 center, Vector2 size, Color color)
    {
        var roomRoot = new GameObject(name);
        roomRoot.transform.SetParent(root, false);
        roomRoot.transform.position = center;

        BuildFloor(roomRoot.transform, "Floor", Vector3.zero, size, color * 0.95f);
        BuildFloor(roomRoot.transform, "Ceiling", new Vector3(0f, RoomHeight, 0f), size, color * 0.6f);

        BuildWall(roomRoot.transform, "Wall_N", new Vector3(0f, RoomHeight * 0.5f, size.y * 0.5f), new Vector3(size.x, RoomHeight, WallThickness), color);
        BuildWall(roomRoot.transform, "Wall_S", new Vector3(0f, RoomHeight * 0.5f, -size.y * 0.5f), new Vector3(size.x, RoomHeight, WallThickness), color);
        BuildWall(roomRoot.transform, "Wall_E", new Vector3(size.x * 0.5f, RoomHeight * 0.5f, 0f), new Vector3(WallThickness, RoomHeight, size.y), color);
        BuildWall(roomRoot.transform, "Wall_W", new Vector3(-size.x * 0.5f, RoomHeight * 0.5f, 0f), new Vector3(WallThickness, RoomHeight, size.y), color);
    }

    private static void BuildFloor(Transform parent, string name, Vector3 localPosition, Vector2 size, Color color)
    {
        var floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
        floor.name = name;
        floor.transform.SetParent(parent, false);
        floor.transform.localPosition = localPosition;
        floor.transform.localScale = new Vector3(size.x, WallThickness, size.y);

        var renderer = floor.GetComponent<Renderer>();
        if (renderer != null)
        {
            renderer.material.color = color;
        }
    }

    private static void BuildWall(Transform parent, string name, Vector3 localPosition, Vector3 localScale, Color color)
    {
        var wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
        wall.name = name;
        wall.transform.SetParent(parent, false);
        wall.transform.localPosition = localPosition;
        wall.transform.localScale = localScale;

        var renderer = wall.GetComponent<Renderer>();
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
        station.transform.position = position;
        station.transform.localScale = new Vector3(0.7f, 1f, 0.7f);

        station.AddComponent<DebugDoorInteractable>();

        var renderer = station.GetComponent<Renderer>();
        if (renderer != null)
        {
            renderer.material.color = color;
        }
    }
}
}
