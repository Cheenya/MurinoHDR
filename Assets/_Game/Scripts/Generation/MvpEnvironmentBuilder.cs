using MurinoHDR.Interaction;
using UnityEngine;

namespace MurinoHDR.Generation
{

public static class MvpEnvironmentBuilder
{
    public const string RootName = "MVP_Environment";
    public const string LayoutVersionName = "Layout_v3";

    private const float FloorThickness = 0.3f;
    private const float WallHeight = 3.2f;
    private const float WallThickness = 0.3f;

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
        new GameObject(LayoutVersionName).transform.SetParent(root.transform, false);
        BuildLighting(root.transform);

        var baseFloorColor = new Color(0.16f, 0.18f, 0.21f);
        var roomFloorColor = new Color(0.22f, 0.26f, 0.31f);
        var wallColor = new Color(0.1f, 0.12f, 0.15f);
        var accentColor = new Color(0.24f, 0.29f, 0.34f);

        BuildBlock(root.transform, "LevelBaseFloor", new Vector3(0f, -0.15f, 2.5f), new Vector3(22f, FloorThickness, 36f), baseFloorColor);
        BuildBlock(root.transform, "StartRoomFloor", new Vector3(0f, 0.01f, -10f), new Vector3(10f, 0.04f, 8f), roomFloorColor);
        BuildBlock(root.transform, "CorridorFloor", new Vector3(0f, 0.01f, -3f), new Vector3(4f, 0.04f, 6f), accentColor);
        BuildBlock(root.transform, "HubFloor", new Vector3(0f, 0.01f, 4.5f), new Vector3(14f, 0.04f, 11f), roomFloorColor);
        BuildBlock(root.transform, "LeftBranchFloor", new Vector3(-6f, 0.01f, 12f), new Vector3(4f, 0.04f, 8f), accentColor);
        BuildBlock(root.transform, "CenterBranchFloor", new Vector3(0f, 0.01f, 12.5f), new Vector3(4f, 0.04f, 9f), accentColor);
        BuildBlock(root.transform, "RightBranchFloor", new Vector3(6f, 0.01f, 12f), new Vector3(4f, 0.04f, 8f), accentColor);
        BuildBlock(root.transform, "ElevatorRoomFloor", new Vector3(-6f, 0.01f, 17f), new Vector3(6f, 0.04f, 6f), roomFloorColor);
        BuildBlock(root.transform, "ShaftRoomFloor", new Vector3(0f, 0.01f, 18f), new Vector3(6f, 0.04f, 6f), roomFloorColor);
        BuildBlock(root.transform, "StairsRoomFloor", new Vector3(6f, 0.01f, 17f), new Vector3(6f, 0.04f, 6f), roomFloorColor);

        BuildBlock(root.transform, "OuterNorthWall", new Vector3(0f, WallHeight * 0.5f, 20.5f), new Vector3(22f, WallHeight, WallThickness), wallColor);
        BuildBlock(root.transform, "OuterSouthWall", new Vector3(0f, WallHeight * 0.5f, -15.5f), new Vector3(22f, WallHeight, WallThickness), wallColor);
        BuildBlock(root.transform, "OuterWestWall", new Vector3(-11f, WallHeight * 0.5f, 2.5f), new Vector3(WallThickness, WallHeight, 36f), wallColor);
        BuildBlock(root.transform, "OuterEastWall", new Vector3(11f, WallHeight * 0.5f, 2.5f), new Vector3(WallThickness, WallHeight, 36f), wallColor);

        BuildBlock(root.transform, "StartSouthWall", new Vector3(0f, WallHeight * 0.5f, -14f), new Vector3(10f, WallHeight, WallThickness), wallColor);
        BuildBlock(root.transform, "StartWestWall", new Vector3(-5f, WallHeight * 0.5f, -10f), new Vector3(WallThickness, WallHeight, 8f), wallColor);
        BuildBlock(root.transform, "StartEastWall", new Vector3(5f, WallHeight * 0.5f, -10f), new Vector3(WallThickness, WallHeight, 8f), wallColor);
        BuildBlock(root.transform, "StartNorthWallLeft", new Vector3(-3.25f, WallHeight * 0.5f, -6f), new Vector3(3.5f, WallHeight, WallThickness), wallColor);
        BuildBlock(root.transform, "StartNorthWallRight", new Vector3(3.25f, WallHeight * 0.5f, -6f), new Vector3(3.5f, WallHeight, WallThickness), wallColor);

        BuildBlock(root.transform, "CorridorWestWall", new Vector3(-2f, WallHeight * 0.5f, -3f), new Vector3(WallThickness, WallHeight, 6f), wallColor);
        BuildBlock(root.transform, "CorridorEastWall", new Vector3(2f, WallHeight * 0.5f, -3f), new Vector3(WallThickness, WallHeight, 6f), wallColor);

        BuildBlock(root.transform, "HubSouthWallLeft", new Vector3(-5f, WallHeight * 0.5f, -1f), new Vector3(6f, WallHeight, WallThickness), wallColor);
        BuildBlock(root.transform, "HubSouthWallRight", new Vector3(5f, WallHeight * 0.5f, -1f), new Vector3(6f, WallHeight, WallThickness), wallColor);
        BuildBlock(root.transform, "HubWestWall", new Vector3(-7f, WallHeight * 0.5f, 4.5f), new Vector3(WallThickness, WallHeight, 11f), wallColor);
        BuildBlock(root.transform, "HubEastWall", new Vector3(7f, WallHeight * 0.5f, 4.5f), new Vector3(WallThickness, WallHeight, 11f), wallColor);

        BuildBlock(root.transform, "ElevatorWestWall", new Vector3(-9f, WallHeight * 0.5f, 17f), new Vector3(WallThickness, WallHeight, 6f), wallColor);
        BuildBlock(root.transform, "ElevatorEastWall", new Vector3(-3f, WallHeight * 0.5f, 17f), new Vector3(WallThickness, WallHeight, 6f), wallColor);
        BuildBlock(root.transform, "ElevatorNorthWall", new Vector3(-6f, WallHeight * 0.5f, 20f), new Vector3(6f, WallHeight, WallThickness), wallColor);

        BuildBlock(root.transform, "ShaftWestWall", new Vector3(-3f, WallHeight * 0.5f, 18f), new Vector3(WallThickness, WallHeight, 6f), wallColor);
        BuildBlock(root.transform, "ShaftEastWall", new Vector3(3f, WallHeight * 0.5f, 18f), new Vector3(WallThickness, WallHeight, 6f), wallColor);
        BuildBlock(root.transform, "ShaftNorthWall", new Vector3(0f, WallHeight * 0.5f, 21f), new Vector3(6f, WallHeight, WallThickness), wallColor);

        BuildBlock(root.transform, "StairsWestWall", new Vector3(3f, WallHeight * 0.5f, 17f), new Vector3(WallThickness, WallHeight, 6f), wallColor);
        BuildBlock(root.transform, "StairsEastWall", new Vector3(9f, WallHeight * 0.5f, 17f), new Vector3(WallThickness, WallHeight, 6f), wallColor);
        BuildBlock(root.transform, "StairsNorthWall", new Vector3(6f, WallHeight * 0.5f, 20f), new Vector3(6f, WallHeight, WallThickness), wallColor);

        BuildBlock(root.transform, "HubDividerLeft", new Vector3(-2.5f, 1.1f, 8f), new Vector3(0.25f, 2.2f, 2.8f), wallColor);
        BuildBlock(root.transform, "HubDividerRight", new Vector3(2.5f, 1.1f, 8f), new Vector3(0.25f, 2.2f, 2.8f), wallColor);
        BuildBlock(root.transform, "LowBarrier", new Vector3(0f, 0.45f, 1.5f), new Vector3(2.2f, 0.9f, 0.5f), wallColor);

        BuildBlock(root.transform, "StartCrate", new Vector3(-2.5f, 0.8f, -11.4f), new Vector3(1.6f, 1.6f, 1.6f), new Color(0.33f, 0.24f, 0.16f));
        BuildBlock(root.transform, "StartBench", new Vector3(2.2f, 0.35f, -12.2f), new Vector3(2.2f, 0.7f, 0.7f), new Color(0.22f, 0.22f, 0.24f));

        BuildExitStation(root.transform, "ElevatorStation", new Vector3(-6f, 1f, 17f), new Color(0.2f, 0.75f, 0.2f));
        BuildExitStation(root.transform, "ShaftStation", new Vector3(0f, 1f, 18f), new Color(0.75f, 0.55f, 0.2f));
        BuildExitStation(root.transform, "StairsStation", new Vector3(6f, 1f, 17f), new Color(0.2f, 0.55f, 0.8f));

        Debug.Log("[GEN] MVP environment generated");
    }

    private static void BuildLighting(Transform root)
    {
        var existingLight = Object.FindFirstObjectByType<Light>();
        if (existingLight != null)
        {
            return;
        }

        var lightObject = new GameObject("Directional Light");
        lightObject.transform.SetParent(root, false);
        lightObject.transform.rotation = Quaternion.Euler(38f, -28f, 0f);

        var light = lightObject.AddComponent<Light>();
        light.type = LightType.Directional;
        light.intensity = 1.0f;
    }

    private static void BuildBlock(Transform parent, string name, Vector3 position, Vector3 scale, Color color)
    {
        var block = GameObject.CreatePrimitive(PrimitiveType.Cube);
        block.name = name;
        block.transform.SetParent(parent, false);
        block.transform.localPosition = position;
        block.transform.localScale = scale;

        var renderer = block.GetComponent<Renderer>();
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
