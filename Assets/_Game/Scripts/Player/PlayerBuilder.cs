using UnityEngine;

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
            bodyRenderer.material.color = new Color(0.22f, 0.35f, 0.48f);
        }

        var cameraObject = new GameObject("Main Camera");
        cameraObject.tag = "MainCamera";
        cameraObject.transform.SetParent(player.transform, false);
        cameraObject.transform.localPosition = new Vector3(0f, 0.72f, 0f);

        var playerCamera = cameraObject.AddComponent<Camera>();
        playerCamera.nearClipPlane = 0.03f;
        playerCamera.fieldOfView = 78f;
        cameraObject.AddComponent<AudioListener>();

        return player;
    }
}
}
