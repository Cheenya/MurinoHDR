using MurinoHDR.Player;
using UnityEngine;
using UnityEngine.UI;

namespace MurinoHDR.UI
{

[ExecuteAlways]
public sealed class InteractionPromptUI : MonoBehaviour
{
    public static InteractionPromptUI Instance { get; private set; }

    private GameObject _promptPanel;
    private Text _promptText;
    private Text _statusText;
    private PlayerMovement _playerMovement;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            if (Application.isPlaying)
            {
                Destroy(gameObject);
            }
            else
            {
                DestroyImmediate(gameObject);
            }

            return;
        }

        Instance = this;
        EnsureHud();
    }

    private void OnEnable()
    {
        EnsureHud();
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    private void Update()
    {
        if (_playerMovement == null)
        {
            _playerMovement = FindFirstObjectByType<PlayerMovement>();
        }

        if (_playerMovement == null || _statusText == null)
        {
            return;
        }

        var stance = _playerMovement.IsCrouching ? "CROUCH" : "STAND";
        var grounded = _playerMovement.IsGrounded ? "GROUND" : "AIR";
        _statusText.text = string.Format("{0} | {1} | {2:0.0} m/s", stance, grounded, _playerMovement.CurrentHorizontalSpeed);
    }

    public void Show(string text)
    {
        if (_promptPanel == null || _promptText == null)
        {
            return;
        }

        _promptText.text = text;
        _promptPanel.SetActive(true);
    }

    public void Hide()
    {
        if (_promptPanel != null)
        {
            _promptPanel.SetActive(false);
        }
    }

    private void EnsureHud()
    {
        if (transform.Find("HudCanvas") == null)
        {
            BuildHud();
        }

        Hide();
    }

    private void BuildHud()
    {
        var canvasObject = new GameObject("HudCanvas");
        canvasObject.transform.SetParent(transform, false);

        var canvas = canvasObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.pixelPerfect = true;

        var scaler = canvasObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        canvasObject.AddComponent<GraphicRaycaster>();

        var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (font == null)
        {
            font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        }

        BuildCrosshair(canvasObject.transform);

        _promptPanel = BuildPanel(
            canvasObject.transform,
            "PromptPanel",
            new Vector2(0.5f, 0f),
            new Vector2(0.5f, 0f),
            new Vector2(0.5f, 0f),
            new Vector2(0f, 36f),
            new Vector2(520f, 54f),
            new Color(0.05f, 0.06f, 0.08f, 0.82f));
        _promptText = BuildText(_promptPanel.transform, "PromptText", font, 24, TextAnchor.MiddleCenter, "Press E", Color.white);

        var controlsPanel = BuildPanel(
            canvasObject.transform,
            "ControlsPanel",
            new Vector2(0f, 1f),
            new Vector2(0f, 1f),
            new Vector2(0f, 1f),
            new Vector2(20f, -20f),
            new Vector2(520f, 96f),
            new Color(0.05f, 0.06f, 0.08f, 0.72f));
        BuildText(
            controlsPanel.transform,
            "ControlsText",
            font,
            20,
            TextAnchor.UpperLeft,
            "WASD move\nMouse look\nShift sprint | Space jump | Ctrl crouch\nE interact | Esc unlock cursor",
            new Color(0.85f, 0.9f, 0.96f));

        var statusPanel = BuildPanel(
            canvasObject.transform,
            "StatusPanel",
            new Vector2(1f, 1f),
            new Vector2(1f, 1f),
            new Vector2(1f, 1f),
            new Vector2(-20f, -20f),
            new Vector2(280f, 44f),
            new Color(0.05f, 0.06f, 0.08f, 0.72f));
        _statusText = BuildText(statusPanel.transform, "StatusText", font, 18, TextAnchor.MiddleCenter, "STAND | GROUND | 0.0 m/s", new Color(0.9f, 0.94f, 0.98f));
    }

    private static void BuildCrosshair(Transform parent)
    {
        BuildImage(parent, "CrosshairHorizontal", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(22f, 2f), Color.white);
        BuildImage(parent, "CrosshairVertical", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(2f, 22f), Color.white);
    }

    private static GameObject BuildPanel(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 anchoredPosition, Vector2 size, Color color)
    {
        var panel = new GameObject(name);
        panel.transform.SetParent(parent, false);

        var rect = panel.AddComponent<RectTransform>();
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.pivot = pivot;
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = size;

        var image = panel.AddComponent<Image>();
        image.color = color;

        return panel;
    }

    private static Text BuildText(Transform parent, string name, Font font, int fontSize, TextAnchor alignment, string content, Color color)
    {
        var textObject = new GameObject(name);
        textObject.transform.SetParent(parent, false);

        var rect = textObject.AddComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = new Vector2(14f, 10f);
        rect.offsetMax = new Vector2(-14f, -10f);

        var text = textObject.AddComponent<Text>();
        text.font = font;
        text.fontSize = fontSize;
        text.alignment = alignment;
        text.color = color;
        text.supportRichText = false;
        text.horizontalOverflow = HorizontalWrapMode.Wrap;
        text.verticalOverflow = VerticalWrapMode.Overflow;
        text.text = content;

        return text;
    }

    private static void BuildImage(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax, Vector2 anchoredPosition, Vector2 size, Color color)
    {
        var imageObject = new GameObject(name);
        imageObject.transform.SetParent(parent, false);

        var rect = imageObject.AddComponent<RectTransform>();
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.sizeDelta = size;
        rect.anchoredPosition = anchoredPosition;

        var image = imageObject.AddComponent<Image>();
        image.color = color;
    }
}
}
