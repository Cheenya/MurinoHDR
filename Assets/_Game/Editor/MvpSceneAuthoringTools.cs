using MurinoHDR.Generation;
using MurinoHDR.Player;
using MurinoHDR.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace MurinoHDR.Editor
{

public static class MvpSceneAuthoringTools
{
    private const string GameScenePath = "Assets/_Game/Scenes/Game.unity";

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
        if (promptUi == null)
        {
            var uiRoot = new GameObject("InteractionPromptUI");
            uiRoot.AddComponent<InteractionPromptUI>();
        }

        Selection.activeGameObject = GameObject.Find("MVP_Environment");
    }
}
}
