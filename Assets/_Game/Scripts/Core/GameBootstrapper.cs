using MurinoHDR.Audio;
using MurinoHDR.Generation;
using MurinoHDR.Interaction;
using MurinoHDR.Player;
using MurinoHDR.Save;
using MurinoHDR.UI;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace MurinoHDR.Core
{

public sealed class GameBootstrapper : MonoBehaviour
{
    private const string GameSceneName = "Game";

    private static bool _initialized;
    private static GameBootstrapper _instance;

    public static InputService InputService { get; private set; }
    public static SaveSystem SaveSystem { get; private set; }
    public static AudioManager AudioManager { get; private set; }
    public static GameStateService GameStateService { get; private set; }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void RuntimeInit()
    {
        EnsureInstance();
    }

    private static void EnsureInstance()
    {
        if (_instance != null)
        {
            return;
        }

        var root = new GameObject("__GameBootstrapper");
        _instance = root.AddComponent<GameBootstrapper>();
        DontDestroyOnLoad(root);
    }

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }

        if (_initialized)
        {
            return;
        }

        InputService = gameObject.AddComponent<InputService>();
        SaveSystem = gameObject.AddComponent<SaveSystem>();
        AudioManager = gameObject.AddComponent<AudioManager>();
        GameStateService = gameObject.AddComponent<GameStateService>();

        SceneManager.sceneLoaded += OnSceneLoaded;
        _initialized = true;
        Debug.Log("[CORE] Bootstrap initialized");
    }

    private void Start()
    {
        if (SceneManager.GetActiveScene().name != GameSceneName)
        {
            SceneManager.LoadScene(GameSceneName);
        }
        else
        {
            EnsureGameSceneSetup();
        }
    }

    private void OnDestroy()
    {
        if (_instance == this)
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name == GameSceneName)
        {
            EnsureGameSceneSetup();
            GameStateService?.SetState(GameState.Playing);
        }
    }

    private static void EnsureGameSceneSetup()
    {
        CleanupLegacySceneObjects();
        var player = PlayerBuilder.EnsurePlayer();
        var environment = MvpEnvironmentBuilder.EnsureEnvironment();
        PlacePlayerAtSpawn(player, environment);

        if (FindFirstObjectByType<InteractionPromptUI>() == null)
        {
            var uiObject = new GameObject("InteractionPromptUI");
            uiObject.AddComponent<InteractionPromptUI>();
        }
    }

    private static void CleanupLegacySceneObjects()
    {
        var legacyRoots = new[] { "Main Camera", "Sun", "Sky and Fog Volume" };
        for (var i = 0; i < legacyRoots.Length; i++)
        {
            var root = GameObject.Find(legacyRoots[i]);
            if (root != null)
            {
                Destroy(root);
            }
        }
    }

    private static void PlacePlayerAtSpawn(GameObject player, GameObject environment)
    {
        if (player == null || environment == null)
        {
            return;
        }

        var spawn = environment.transform.Find("Rooms/Reception/Spawn_PlayerStart");
        if (spawn == null)
        {
            spawn = environment.transform.Find("Rooms/StartRoom/Spawn_PlayerStart");
        }
        if (spawn == null)
        {
            return;
        }

        var controller = player.GetComponent<CharacterController>();
        if (controller != null)
        {
            controller.enabled = false;
        }

        player.transform.position = spawn.position;

        if (controller != null)
        {
            controller.enabled = true;
        }
    }
}
}


