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
        if (FindFirstObjectByType<PlayerMovement>() == null)
        {
            SpawnPlayer();
        }

        if (FindFirstObjectByType<InteractionPromptUI>() == null)
        {
            var uiObject = new GameObject("InteractionPromptUI");
            uiObject.AddComponent<InteractionPromptUI>();
        }

        MvpEnvironmentBuilder.EnsureEnvironment();
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

        var visual = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        visual.name = "BodyVisual";
        visual.transform.SetParent(player.transform, false);
        visual.transform.localScale = new Vector3(0.75f, 0.95f, 0.75f);
        visual.transform.localPosition = new Vector3(0f, 0.9f, 0f);

        var visualCollider = visual.GetComponent<Collider>();
        if (visualCollider != null)
        {
            Destroy(visualCollider);
        }

        var bodyRenderer = visual.GetComponent<Renderer>();
        if (bodyRenderer != null)
        {
            bodyRenderer.material.color = new Color(0.22f, 0.35f, 0.48f);
        }

        var cameraObject = new GameObject("Main Camera");
        cameraObject.tag = "MainCamera";
        cameraObject.transform.SetParent(player.transform, false);
        cameraObject.transform.localPosition = new Vector3(0f, 0.72f, 0f);

        cameraObject.AddComponent<Camera>();
        cameraObject.AddComponent<AudioListener>();
    }
}
}


