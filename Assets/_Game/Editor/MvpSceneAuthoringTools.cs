using MurinoHDR.Core;
using MurinoHDR.Generation;
using MurinoHDR.Player;
using MurinoHDR.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

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

    [MenuItem("Tools/Murino/Validate Floor Generation")]
    public static void ValidateFloorGeneration()
    {
        MvpRuntimeContent.EnsureInitialized();
        var settings = MvpRuntimeContent.Catalog.GeneratorSettings;
        var failed = 0;
        for (var i = 0; i < settings.ValidationRuns; i++)
        {
            var seed = 1000 + i;
            var result = FloorGenerator.Generate(seed, settings);
            var report = FloorGenerator.Validate(result, settings);
            if (!report.IsValid)
            {
                failed++;
                Debug.LogError(string.Format("[GEN] {0}", report.BuildSummary()));
            }
        }

        if (failed == 0)
        {
            Debug.Log(string.Format("[GEN] Validation passed for {0} seeds", settings.ValidationRuns));
        }
        else
        {
            Debug.LogError(string.Format("[GEN] Validation failed for {0} seeds", failed));
        }
    }

    private static void BuildMvpContent()
    {
        CleanLegacySceneObjects();
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

    private static void CleanLegacySceneObjects()
    {
        var legacyRoots = new[] { "Main Camera", "Sun", "Sky and Fog Volume" };
        var roots = SceneManager.GetActiveScene().GetRootGameObjects();
        for (var i = 0; i < roots.Length; i++)
        {
            for (var j = 0; j < legacyRoots.Length; j++)
            {
                if (roots[i].name == legacyRoots[j])
                {
                    Object.DestroyImmediate(roots[i]);
                    break;
                }
            }
        }
    }

    private static void OnSceneOpened(UnityEngine.SceneManagement.Scene scene, OpenSceneMode mode)
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode || scene.path != GameScenePath)
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

        AddModelAtAnchor(visualsRoot, FurnitureBasePath + "bench.fbx", "ReceptionBench", "Anchor_ReceptionBench", Vector3.zero, new Vector3(2.4f, 0.9f, 0.8f), true);
        AddModelAtAnchor(visualsRoot, FurnitureBasePath + "bench.fbx", "HubBench", "Anchor_HubBench", new Vector3(0f, 90f, 0f), new Vector3(2.4f, 0.9f, 0.8f), true);
        AddModelAtAnchor(visualsRoot, FurnitureBasePath + "bench.fbx", "StorageBench", "Anchor_StorageBench", Vector3.zero, new Vector3(2.2f, 0.9f, 0.8f), true);
        AddModelAtAnchor(visualsRoot, FurnitureBasePath + "bench.fbx", "UtilityBench", "Anchor_UtilityBench", Vector3.zero, new Vector3(2.2f, 0.9f, 0.8f), true);

        AddModelAtAnchor(visualsRoot, FurnitureBasePath + "desk.fbx", "HubDesk", "Anchor_HubDesk", new Vector3(0f, -90f, 0f), new Vector3(2.1f, 1f, 1.15f), true);
        AddModelAtAnchor(visualsRoot, FurnitureBasePath + "desk.fbx", "OfficeDeskA", "Anchor_OfficeDeskA", new Vector3(0f, -90f, 0f), new Vector3(1.9f, 1f, 1.05f), true);
        AddModelAtAnchor(visualsRoot, FurnitureBasePath + "desk.fbx", "OfficeDeskB", "Anchor_OfficeDeskB", new Vector3(0f, -90f, 0f), new Vector3(1.9f, 1f, 1.05f), true);

        AddModelAtAnchor(visualsRoot, FurnitureBasePath + "chairDesk.fbx", "HubChair", "Anchor_HubChair", new Vector3(0f, 42f, 0f), new Vector3(0.95f, 1f, 0.95f), true);
        AddModelAtAnchor(visualsRoot, FurnitureBasePath + "chairDesk.fbx", "OfficeChairA", "Anchor_OfficeChairA", new Vector3(0f, 42f, 0f), new Vector3(0.95f, 1f, 0.95f), true);
        AddModelAtAnchor(visualsRoot, FurnitureBasePath + "chairDesk.fbx", "OfficeChairB", "Anchor_OfficeChairB", new Vector3(0f, 42f, 0f), new Vector3(0.95f, 1f, 0.95f), true);

        AddModelAtAnchor(visualsRoot, FurnitureBasePath + "televisionModern.fbx", "HubMonitor", "Anchor_HubMonitor", new Vector3(0f, -90f, 0f), new Vector3(0.75f, 0.75f, 0.35f), false);
        AddModelAtAnchor(visualsRoot, FurnitureBasePath + "televisionModern.fbx", "OfficeMonitorA", "Anchor_OfficeMonitorA", new Vector3(0f, -90f, 0f), new Vector3(0.68f, 0.68f, 0.3f), false);
        AddModelAtAnchor(visualsRoot, FurnitureBasePath + "televisionModern.fbx", "OfficeMonitorB", "Anchor_OfficeMonitorB", new Vector3(0f, -90f, 0f), new Vector3(0.68f, 0.68f, 0.3f), false);

        AddModelAtAnchor(visualsRoot, FurnitureBasePath + "cardboardBoxClosed.fbx", "StorageBoxA", "Anchor_StorageBoxesA", new Vector3(0f, 18f, 0f), new Vector3(1.3f, 1.3f, 1.3f), true);
        AddModelAtAnchor(visualsRoot, FurnitureBasePath + "cardboardBoxOpen.fbx", "StorageBoxB", "Anchor_StorageBoxesB", new Vector3(0f, -24f, 0f), new Vector3(0.9f, 0.9f, 0.9f), true, new Vector3(0.2f, 0f, 0.25f));
        AddModelAtAnchor(visualsRoot, FurnitureBasePath + "cardboardBoxClosed.fbx", "UtilityBox", "Anchor_UtilityBox", new Vector3(0f, 18f, 0f), new Vector3(1.1f, 1.1f, 1.1f), true);

        AddModelAtAnchor(visualsRoot, FurnitureBasePath + "trashcan.fbx", "ReceptionTrash", "Anchor_ReceptionTrash", Vector3.zero, new Vector3(0.65f, 0.85f, 0.65f), true);
        AddModelAtAnchor(visualsRoot, FurnitureBasePath + "trashcan.fbx", "OfficeTrash", "Anchor_OfficeTrash", Vector3.zero, new Vector3(0.65f, 0.85f, 0.65f), true);
        AddModelAtAnchor(visualsRoot, FurnitureBasePath + "trashcan.fbx", "UtilityTrash", "Anchor_UtilityTrash", Vector3.zero, new Vector3(0.65f, 0.85f, 0.65f), true);

        AddModelAtAnchor(visualsRoot, FurnitureBasePath + "stairsOpen.fbx", "StairsVisual", "Anchor_StairsModel", new Vector3(0f, 180f, 0f), new Vector3(3f, 2.2f, 5f), false);
        AddGeneratedDoors(visualsRoot, environmentRoot);
    }

    private static bool AssetsAvailable()
    {
        return AssetDatabase.LoadAssetAtPath<GameObject>(FurnitureBasePath + "bench.fbx") != null;
    }

    private static void AddGeneratedDoors(Transform parent, Transform environmentRoot)
    {
        var doorAnchors = environmentRoot.GetComponentsInChildren<GeneratedDoorAnchor>(true);
        for (var i = 0; i < doorAnchors.Length; i++)
        {
            var anchor = doorAnchors[i];
            switch (anchor.VisualType)
            {
                case GeneratedDoorVisualType.Office:
                    AddModelAtTransform(parent, BuildingBasePath + "door-rotate-square-a.fbx", "OfficeDoorVisual", anchor.transform, new Vector3(0f, 90f, 0f), new Vector3(1.7f, 2.2f, 0.16f), false, new Vector3(-0.42f, 0f, 0f));
                    break;
                case GeneratedDoorVisualType.Service:
                    AddModelAtTransform(parent, BuildingBasePath + "door-rotate-square-a.fbx", "ServiceDoorVisual", anchor.transform, new Vector3(0f, 90f, 0f), new Vector3(1.8f, 2.2f, 0.16f), false, new Vector3(-0.2f, 0f, 0f));
                    break;
                case GeneratedDoorVisualType.Exit:
                    AddModelAtTransform(parent, BuildingBasePath + "door-rotate-square-a.fbx", "ExitDoorVisual", anchor.transform, Vector3.zero, new Vector3(1.8f, 2.2f, 0.16f), false);
                    break;
                case GeneratedDoorVisualType.ElevatorLeft:
                    AddModelAtTransform(parent, BuildingBasePath + "door-rotate-square-a.fbx", "ElevatorLeftVisual", anchor.transform, Vector3.zero, new Vector3(1.95f, 2.2f, 0.14f), false);
                    break;
                case GeneratedDoorVisualType.ElevatorRight:
                    AddModelAtTransform(parent, BuildingBasePath + "door-rotate-square-a.fbx", "ElevatorRightVisual", anchor.transform, Vector3.zero, new Vector3(1.95f, 2.2f, 0.14f), false);
                    break;
            }
        }
    }

    private static void AddModelAtAnchor(Transform parent, string assetPath, string instanceName, string anchorName, Vector3 rotationEuler, Vector3 targetSize, bool addCollider)
    {
        AddModelAtAnchor(parent, assetPath, instanceName, anchorName, rotationEuler, targetSize, addCollider, Vector3.zero);
    }

    private static void AddModelAtAnchor(Transform parent, string assetPath, string instanceName, string anchorName, Vector3 rotationEuler, Vector3 targetSize, bool addCollider, Vector3 localOffset)
    {
        var anchor = FindDeepChild(parent.root, anchorName);
        if (anchor == null)
        {
            return;
        }

        AddModelAtTransform(parent, assetPath, instanceName, anchor, rotationEuler, targetSize, addCollider, localOffset);
    }

    private static void AddModelAtTransform(Transform parent, string assetPath, string instanceName, Transform anchor, Vector3 rotationEuler, Vector3 targetSize, bool addCollider)
    {
        AddModelAtTransform(parent, assetPath, instanceName, anchor, rotationEuler, targetSize, addCollider, Vector3.zero);
    }

    private static void AddModelAtTransform(Transform parent, string assetPath, string instanceName, Transform anchor, Vector3 rotationEuler, Vector3 targetSize, bool addCollider, Vector3 localOffset)
    {
        if (anchor == null)
        {
            return;
        }

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
        instance.transform.position = anchor.position + localOffset;
        instance.transform.rotation = anchor.rotation * Quaternion.Euler(rotationEuler);
        instance.transform.localScale = Vector3.one;
        FitObjectToBounds(instance, targetSize);
        if (addCollider)
        {
            AddMeshColliders(instance);
        }
    }

    private static void FitObjectToBounds(GameObject instance, Vector3 targetSize)
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
    }

    private static void AddMeshColliders(GameObject instance)
    {
        var meshFilters = instance.GetComponentsInChildren<MeshFilter>();
        for (var i = 0; i < meshFilters.Length; i++)
        {
            var meshFilter = meshFilters[i];
            if (meshFilter.sharedMesh == null)
            {
                continue;
            }

            var collider = meshFilter.gameObject.GetComponent<MeshCollider>();
            if (collider == null)
            {
                collider = meshFilter.gameObject.AddComponent<MeshCollider>();
            }

            collider.sharedMesh = meshFilter.sharedMesh;
        }
    }

    private static Transform FindDeepChild(Transform parent, string childName)
    {
        if (parent.name == childName)
        {
            return parent;
        }

        for (var i = 0; i < parent.childCount; i++)
        {
            var result = FindDeepChild(parent.GetChild(i), childName);
            if (result != null)
            {
                return result;
            }
        }

        return null;
    }
}
}
