using MurinoHDR.Generation;
using MurinoHDR.Player;
using MurinoHDR.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace MurinoHDR.Editor
{

[InitializeOnLoad]
public static class MvpSceneAuthoringTools
{
    private const string GameScenePath = "Assets/_Game/Scenes/Game.unity";
    private const string FurnitureBasePath = "Assets/ThirdParty/Kenney/FurnitureKit/Models/FBX format/";
    private const string BuildingBasePath = "Assets/ThirdParty/Kenney/BuildingKit/Models/FBX format/";

    static MvpSceneAuthoringTools()
    {
        EditorSceneManager.sceneOpened += OnSceneOpened;
        EditorApplication.delayCall += TryBuildForActiveGameScene;
    }

    [MenuItem("Tools/Murino/Build MVP In Game Scene")]
    public static void BuildMvpInGameScene()
    {
        var scene = EditorSceneManager.OpenScene(GameScenePath, OpenSceneMode.Single);
        BuildMvpContent();
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log("[GEN] MVP content was built and saved in Game scene");
    }

    [MenuItem("Tools/Murino/Build MVP In Active Scene")]
    public static void BuildMvpInActiveScene()
    {
        BuildMvpContent();
        var scene = EditorSceneManager.GetActiveScene();
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log("[GEN] MVP content was built in active scene");
    }

    private static void BuildMvpContent()
    {
        MvpEnvironmentBuilder.RebuildEnvironment();
        PlayerBuilder.RebuildPlayer();

        var promptUi = Object.FindFirstObjectByType<InteractionPromptUI>();
        if (promptUi != null)
        {
            Object.DestroyImmediate(promptUi.gameObject);
        }

        var uiRoot = new GameObject("InteractionPromptUI");
        uiRoot.AddComponent<InteractionPromptUI>();

        var environmentRoot = GameObject.Find(MvpEnvironmentBuilder.RootName);
        if (environmentRoot != null)
        {
            TryDressSceneWithKenneyAssets(environmentRoot.transform);
            Selection.activeGameObject = environmentRoot;
        }
    }

    private static void OnSceneOpened(UnityEngine.SceneManagement.Scene scene, OpenSceneMode mode)
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            return;
        }

        if (scene.path != GameScenePath)
        {
            return;
        }

        if (NeedsEnvironmentRebuild())
        {
            BuildMvpContent();
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("[GEN] Auto-built MVP content for Game scene");
        }
    }

    private static void TryBuildForActiveGameScene()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            return;
        }

        var scene = EditorSceneManager.GetActiveScene();
        if (scene.path != GameScenePath)
        {
            return;
        }

        if (NeedsEnvironmentRebuild())
        {
            BuildMvpContent();
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("[GEN] Auto-built MVP content for active Game scene");
        }
    }

    private static bool NeedsEnvironmentRebuild()
    {
        var root = GameObject.Find(MvpEnvironmentBuilder.RootName);
        if (root == null)
        {
            return true;
        }

        return root.transform.Find(MvpEnvironmentBuilder.LayoutVersionName) == null;
    }

    private static void TryDressSceneWithKenneyAssets(Transform environmentRoot)
    {
        if (!AssetsAvailable())
        {
            return;
        }

        var visualsRoot = environmentRoot.Find("KenneyVisuals");
        if (visualsRoot != null)
        {
            Object.DestroyImmediate(visualsRoot.gameObject);
        }

        visualsRoot = new GameObject("KenneyVisuals").transform;
        visualsRoot.SetParent(environmentRoot, false);

        AddFittedModel(visualsRoot, FurnitureBasePath + "floorFull.fbx", "Floor_Start", new Vector3(0f, 0f, -10f), Vector3.zero, new Vector3(10f, 0.35f, 8f));
        AddFittedModel(visualsRoot, FurnitureBasePath + "floorFull.fbx", "Floor_Corridor", new Vector3(0f, 0f, -3f), Vector3.zero, new Vector3(4f, 0.35f, 6f));
        AddFittedModel(visualsRoot, FurnitureBasePath + "floorFull.fbx", "Floor_Hub", new Vector3(0f, 0f, 4.5f), Vector3.zero, new Vector3(14f, 0.35f, 11f));
        AddFittedModel(visualsRoot, FurnitureBasePath + "floorFull.fbx", "Floor_LeftBranch", new Vector3(-6f, 0f, 12f), Vector3.zero, new Vector3(4f, 0.35f, 8f));
        AddFittedModel(visualsRoot, FurnitureBasePath + "floorFull.fbx", "Floor_CenterBranch", new Vector3(0f, 0f, 12.5f), Vector3.zero, new Vector3(4f, 0.35f, 9f));
        AddFittedModel(visualsRoot, FurnitureBasePath + "floorFull.fbx", "Floor_RightBranch", new Vector3(6f, 0f, 12f), Vector3.zero, new Vector3(4f, 0.35f, 8f));
        AddFittedModel(visualsRoot, FurnitureBasePath + "floorFull.fbx", "Floor_ElevatorRoom", new Vector3(-6f, 0f, 17f), Vector3.zero, new Vector3(6f, 0.35f, 6f));
        AddFittedModel(visualsRoot, FurnitureBasePath + "floorFull.fbx", "Floor_ShaftRoom", new Vector3(0f, 0f, 18f), Vector3.zero, new Vector3(6f, 0.35f, 6f));
        AddFittedModel(visualsRoot, FurnitureBasePath + "floorFull.fbx", "Floor_StairsRoom", new Vector3(6f, 0f, 17f), Vector3.zero, new Vector3(6f, 0.35f, 6f));

        AddFittedModel(visualsRoot, FurnitureBasePath + "wall.fbx", "Wall_StartSouth", new Vector3(0f, 1.6f, -14f), Vector3.zero, new Vector3(10f, 3.2f, 0.4f));
        AddFittedModel(visualsRoot, FurnitureBasePath + "wall.fbx", "Wall_StartWest", new Vector3(-5f, 1.6f, -10f), new Vector3(0f, 90f, 0f), new Vector3(8f, 3.2f, 0.4f));
        AddFittedModel(visualsRoot, FurnitureBasePath + "wall.fbx", "Wall_StartEast", new Vector3(5f, 1.6f, -10f), new Vector3(0f, 90f, 0f), new Vector3(8f, 3.2f, 0.4f));
        AddFittedModel(visualsRoot, FurnitureBasePath + "wallDoorwayWide.fbx", "Wall_StartNorthDoor", new Vector3(0f, 1.6f, -6f), Vector3.zero, new Vector3(10f, 3.2f, 0.4f));

        AddFittedModel(visualsRoot, FurnitureBasePath + "wall.fbx", "Wall_CorridorWest", new Vector3(-2f, 1.6f, -3f), new Vector3(0f, 90f, 0f), new Vector3(6f, 3.2f, 0.4f));
        AddFittedModel(visualsRoot, FurnitureBasePath + "wall.fbx", "Wall_CorridorEast", new Vector3(2f, 1.6f, -3f), new Vector3(0f, 90f, 0f), new Vector3(6f, 3.2f, 0.4f));

        AddFittedModel(visualsRoot, FurnitureBasePath + "wallDoorwayWide.fbx", "Wall_HubSouth", new Vector3(0f, 1.6f, -1f), Vector3.zero, new Vector3(14f, 3.2f, 0.4f));
        AddFittedModel(visualsRoot, FurnitureBasePath + "wall.fbx", "Wall_HubWest", new Vector3(-7f, 1.6f, 4.5f), new Vector3(0f, 90f, 0f), new Vector3(11f, 3.2f, 0.4f));
        AddFittedModel(visualsRoot, FurnitureBasePath + "wall.fbx", "Wall_HubEast", new Vector3(7f, 1.6f, 4.5f), new Vector3(0f, 90f, 0f), new Vector3(11f, 3.2f, 0.4f));

        AddFittedModel(visualsRoot, FurnitureBasePath + "wallDoorwayWide.fbx", "Wall_ElevatorDoor", new Vector3(-6f, 1.6f, 14f), Vector3.zero, new Vector3(6f, 3.2f, 0.4f));
        AddFittedModel(visualsRoot, FurnitureBasePath + "wall.fbx", "Wall_ElevatorWest", new Vector3(-9f, 1.6f, 17f), new Vector3(0f, 90f, 0f), new Vector3(6f, 3.2f, 0.4f));
        AddFittedModel(visualsRoot, FurnitureBasePath + "wall.fbx", "Wall_ElevatorEast", new Vector3(-3f, 1.6f, 17f), new Vector3(0f, 90f, 0f), new Vector3(6f, 3.2f, 0.4f));

        AddFittedModel(visualsRoot, FurnitureBasePath + "wallDoorwayWide.fbx", "Wall_ShaftDoor", new Vector3(0f, 1.6f, 15f), Vector3.zero, new Vector3(6f, 3.2f, 0.4f));
        AddFittedModel(visualsRoot, FurnitureBasePath + "wall.fbx", "Wall_ShaftWest", new Vector3(-3f, 1.6f, 18f), new Vector3(0f, 90f, 0f), new Vector3(6f, 3.2f, 0.4f));
        AddFittedModel(visualsRoot, FurnitureBasePath + "wall.fbx", "Wall_ShaftEast", new Vector3(3f, 1.6f, 18f), new Vector3(0f, 90f, 0f), new Vector3(6f, 3.2f, 0.4f));

        AddFittedModel(visualsRoot, FurnitureBasePath + "wallDoorwayWide.fbx", "Wall_StairsDoor", new Vector3(6f, 1.6f, 14f), Vector3.zero, new Vector3(6f, 3.2f, 0.4f));
        AddFittedModel(visualsRoot, FurnitureBasePath + "wall.fbx", "Wall_StairsWest", new Vector3(3f, 1.6f, 17f), new Vector3(0f, 90f, 0f), new Vector3(6f, 3.2f, 0.4f));
        AddFittedModel(visualsRoot, FurnitureBasePath + "wall.fbx", "Wall_StairsEast", new Vector3(9f, 1.6f, 17f), new Vector3(0f, 90f, 0f), new Vector3(6f, 3.2f, 0.4f));

        AddFittedModel(visualsRoot, FurnitureBasePath + "bench.fbx", "Prop_Bench", new Vector3(2.2f, 0.35f, -12.2f), new Vector3(0f, 180f, 0f), new Vector3(2.4f, 0.9f, 0.8f));
        AddFittedModel(visualsRoot, FurnitureBasePath + "cardboardBoxClosed.fbx", "Prop_BoxClosed", new Vector3(-2.3f, 0.8f, -11.3f), new Vector3(0f, 14f, 0f), new Vector3(1.4f, 1.4f, 1.4f));
        AddFittedModel(visualsRoot, FurnitureBasePath + "cardboardBoxOpen.fbx", "Prop_BoxOpen", new Vector3(-1.1f, 0.55f, -10.5f), new Vector3(0f, -12f, 0f), new Vector3(1f, 1f, 1f));
        AddFittedModel(visualsRoot, FurnitureBasePath + "desk.fbx", "Prop_Desk", new Vector3(4.2f, 0.52f, 3.6f), new Vector3(0f, -90f, 0f), new Vector3(2.2f, 1f, 1.2f));
        AddFittedModel(visualsRoot, FurnitureBasePath + "chairDesk.fbx", "Prop_Chair", new Vector3(3.5f, 0.45f, 2.8f), new Vector3(0f, 45f, 0f), new Vector3(0.95f, 1f, 0.95f));
        AddFittedModel(visualsRoot, FurnitureBasePath + "trashcan.fbx", "Prop_Trashcan", new Vector3(-5.6f, 0.45f, 3f), Vector3.zero, new Vector3(0.8f, 1f, 0.8f));
        AddFittedModel(visualsRoot, FurnitureBasePath + "televisionModern.fbx", "Prop_Monitor", new Vector3(4.2f, 1.18f, 3.55f), new Vector3(0f, -90f, 0f), new Vector3(0.8f, 0.8f, 0.4f));
        AddFittedModel(visualsRoot, FurnitureBasePath + "stairsOpen.fbx", "Prop_Stairs", new Vector3(6f, 0.1f, 16.6f), new Vector3(0f, 180f, 0f), new Vector3(3.2f, 2.2f, 4.5f));
        AddFittedModel(visualsRoot, BuildingBasePath + "door-rotate-square-a.fbx", "Prop_ElevatorDoor", new Vector3(-6f, 1.2f, 19.7f), Vector3.zero, new Vector3(2f, 2.4f, 0.2f));
    }

    private static bool AssetsAvailable()
    {
        return AssetDatabase.LoadAssetAtPath<GameObject>(FurnitureBasePath + "floorFull.fbx") != null;
    }

    private static void AddFittedModel(Transform parent, string assetPath, string instanceName, Vector3 position, Vector3 rotationEuler, Vector3 targetSize)
    {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
        if (prefab == null)
        {
            return;
        }

        var instance = PrefabUtility.InstantiatePrefab(prefab, parent) as GameObject;
        if (instance == null)
        {
            return;
        }

        instance.name = instanceName;
        instance.transform.SetParent(parent, false);
        instance.transform.position = position;
        instance.transform.rotation = Quaternion.Euler(rotationEuler);
        instance.transform.localScale = Vector3.one;

        FitObjectToBounds(instance, position, targetSize);
    }

    private static void FitObjectToBounds(GameObject instance, Vector3 targetCenter, Vector3 targetSize)
    {
        var renderers = instance.GetComponentsInChildren<Renderer>();
        if (renderers.Length == 0)
        {
            return;
        }

        var bounds = renderers[0].bounds;
        for (var i = 1; i < renderers.Length; i++)
        {
            bounds.Encapsulate(renderers[i].bounds);
        }

        var sourceSize = bounds.size;
        var scale = Vector3.one;

        scale.x = sourceSize.x > 0.001f ? targetSize.x / sourceSize.x : 1f;
        scale.y = sourceSize.y > 0.001f ? targetSize.y / sourceSize.y : 1f;
        scale.z = sourceSize.z > 0.001f ? targetSize.z / sourceSize.z : 1f;

        instance.transform.localScale = scale;

        renderers = instance.GetComponentsInChildren<Renderer>();
        bounds = renderers[0].bounds;
        for (var i = 1; i < renderers.Length; i++)
        {
            bounds.Encapsulate(renderers[i].bounds);
        }

        instance.transform.position += targetCenter - bounds.center;
    }
}
}
