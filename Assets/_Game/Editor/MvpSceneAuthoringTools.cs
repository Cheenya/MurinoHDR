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
    private const string EnvironmentRootName = "MVP_Environment";
    private const string LayoutVersionName = "Layout_v2";

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

        Selection.activeGameObject = GameObject.Find("MVP_Environment");
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
        var root = GameObject.Find(EnvironmentRootName);
        if (root == null)
        {
            return true;
        }

        return root.transform.Find(LayoutVersionName) == null;
    }
}
}
