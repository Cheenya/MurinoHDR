using MurinoHDR.Audio;
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
        if (FindFirstObjectByType<PlayerMovement>() == null)
        {
            SpawnPlayer();
        }

        if (FindFirstObjectByType<InteractionPromptUI>() == null)
        {
            var uiObject = new GameObject("InteractionPromptUI");
            uiObject.AddComponent<InteractionPromptUI>();
        }

        if (GameObject.Find("Ground") == null)
        {
            var ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ground.name = "Ground";
            ground.transform.position = Vector3.zero;
        }

        if (GameObject.Find("InteractableCube") == null)
        {
            var cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cube.name = "InteractableCube";
            cube.transform.position = new Vector3(0f, 1f, 4f);
            cube.AddComponent<DebugDoorInteractable>();
        }

        if (FindFirstObjectByType<Light>() == null)
        {
            var lightObject = new GameObject("Directional Light");
            var lightComponent = lightObject.AddComponent<Light>();
            lightComponent.type = LightType.Directional;
            lightObject.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
        }
    }

    private static void SpawnPlayer()
    {
        var player = new GameObject("Player");
        player.tag = "Player";
        player.transform.position = new Vector3(0f, 1.1f, 0f);

        var controller = player.AddComponent<CharacterController>();
        controller.height = 1.8f;
        controller.radius = 0.35f;
        controller.center = new Vector3(0f, 0.9f, 0f);

        player.AddComponent<PlayerMovement>();
        player.AddComponent<PlayerLook>();
        player.AddComponent<PlayerInteractor>();

        var cameraObject = new GameObject("Main Camera");
        cameraObject.tag = "MainCamera";
        cameraObject.transform.SetParent(player.transform, false);
        cameraObject.transform.localPosition = new Vector3(0f, 0.72f, 0f);

        cameraObject.AddComponent<Camera>();
        cameraObject.AddComponent<AudioListener>();
    }
}
}


