using UnityEngine;

namespace MurinoHDR.UI;

public sealed class InteractionPromptUI : MonoBehaviour
{
    public static InteractionPromptUI Instance { get; private set; }

    private string _text = string.Empty;
    private bool _visible;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    public void Show(string text)
    {
        _text = text;
        _visible = true;
    }

    public void Hide()
    {
        _visible = false;
        _text = string.Empty;
    }

    private void OnGUI()
    {
        if (!_visible)
        {
            return;
        }

        const float width = 360f;
        const float height = 32f;
        var rect = new Rect((Screen.width - width) * 0.5f, Screen.height - 80f, width, height);
        GUI.Box(rect, _text);
    }
}
