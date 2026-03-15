using UnityEngine;
using MurinoHDR.Inventory;

namespace MurinoHDR.Player
{

public static class PlayerBuilder
{
    private const string PlayerName = "Player";

    public static GameObject EnsurePlayer()
    {
        var existing = GameObject.Find(PlayerName);
        if (existing != null)
        {
            EnsureSingleGameplayCamera(existing);
            return existing;
        }

        return CreatePlayer();
    }

    public static GameObject RebuildPlayer()
    {
        var existing = GameObject.Find(PlayerName);
        if (existing != null)
        {
            Object.DestroyImmediate(existing);
        }

        return CreatePlayer();
    }

    private static GameObject CreatePlayer()
    {
        var player = new GameObject(PlayerName);
        player.tag = "Player";
        player.transform.position = new Vector3(0f, 1.15f, -11f);

        var controller = player.AddComponent<CharacterController>();
        controller.height = 1.8f;
        controller.radius = 0.35f;
        controller.center = new Vector3(0f, 0.9f, 0f);
        controller.stepOffset = 0.3f;
        controller.slopeLimit = 45f;

        player.AddComponent<PlayerMovement>();
        player.AddComponent<PlayerLook>();
        player.AddComponent<PlayerInteractor>();
        player.AddComponent<Inventory.Inventory>();
        player.AddComponent<CraftingSystem>();

        var visual = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        visual.name = "BodyVisual";
        visual.transform.SetParent(player.transform, false);
        visual.transform.localScale = new Vector3(0.75f, 0.95f, 0.75f);
        visual.transform.localPosition = new Vector3(0f, 0.9f, 0f);

        var visualCollider = visual.GetComponent<Collider>();
        if (visualCollider != null)
        {
            Object.DestroyImmediate(visualCollider);
        }

        var bodyRenderer = visual.GetComponent<Renderer>();
        if (bodyRenderer != null)
        {
            var shader = Shader.Find("HDRP/Lit") ?? Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            var material = new Material(shader != null ? shader : Shader.Find("Sprites/Default"));
            material.color = new Color(0.22f, 0.35f, 0.48f);
            bodyRenderer.sharedMaterial = material;
        }

        var cameraObject = new GameObject("PlayerCamera");
        cameraObject.tag = "MainCamera";
        cameraObject.transform.SetParent(player.transform, false);
        cameraObject.transform.localPosition = new Vector3(0f, 0.72f, 0f);

        var playerCamera = cameraObject.AddComponent<Camera>();
        playerCamera.enabled = true;
        playerCamera.nearClipPlane = 0.03f;
        playerCamera.fieldOfView = 78f;
        var audioListener = cameraObject.AddComponent<AudioListener>();
        EnsureSingleGameplayCamera(player, playerCamera, audioListener);

        return player;
    }

    private static void EnsureSingleGameplayCamera(GameObject playerRoot)
    {
        var playerCamera = playerRoot.GetComponentInChildren<Camera>();
        var audioListener = playerRoot.GetComponentInChildren<AudioListener>();
        EnsureSingleGameplayCamera(playerRoot, playerCamera, audioListener);
    }

    private static void EnsureSingleGameplayCamera(GameObject playerRoot, Camera playerCamera, AudioListener playerAudioListener)
    {
        if (playerCamera != null)
        {
            playerCamera.enabled = true;
        }

        var cameras = Object.FindObjectsByType<Camera>(FindObjectsSortMode.None);
        for (var i = 0; i < cameras.Length; i++)
        {
            var camera = cameras[i];
            if (camera == null || camera == playerCamera)
            {
                continue;
            }

            if (camera.transform.root == playerRoot.transform)
            {
                continue;
            }

            camera.enabled = false;
            if (camera.CompareTag("MainCamera"))
            {
                camera.tag = "Untagged";
            }
        }

        var listeners = Object.FindObjectsByType<AudioListener>(FindObjectsSortMode.None);
        for (var i = 0; i < listeners.Length; i++)
        {
            var listener = listeners[i];
            if (listener == null)
            {
                continue;
            }

            listener.enabled = listener == playerAudioListener;
        }
    }
}
}
