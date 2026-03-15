using System.Collections.Generic;
using MurinoHDR.Generation;
using MurinoHDR.Inventory;
using MurinoHDR.Interaction;
using MurinoHDR.Player;
using UnityEngine;
using UnityEngine.UI;
using MurinoInventory = MurinoHDR.Inventory.Inventory;

namespace MurinoHDR.UI
{

public sealed class InteractionPromptUI : MonoBehaviour
{
    public static InteractionPromptUI Instance { get; private set; }

    private GameObject _promptPanel;
    private Text _promptText;
    private Text _statusText;
    private Text _inventoryText;
    private Text _objectivesText;
    private Text _repairStatusText;
    private Text _repairButtonText;
    private GameObject _craftPanel;
    private GameObject _repairPanel;
    private Transform _craftContentRoot;
    private Button _repairButton;
    private readonly List<Button> _recipeButtons = new List<Button>();
    private readonly List<Text> _recipeButtonTexts = new List<Text>();

    private PlayerMovement _playerMovement;
    private MurinoInventory _inventory;
    private CraftingSystem _craftingSystem;
    private FloorGoalController _goalController;
    private ElevatorRepairInteractable _activeRepairTarget;

    public bool IsModalOpen => (_craftPanel != null && _craftPanel.activeSelf) || (_repairPanel != null && _repairPanel.activeSelf);

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
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
        ResolveReferences();
        HandleUiInput();
        UpdateStatus();
        UpdateInventory();
        UpdateObjectives();
        UpdateCraftButtons();
        UpdateRepairPanel();
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

    public void OpenRepairPanel(ElevatorRepairInteractable target, MurinoInventory inventory)
    {
        _activeRepairTarget = target;
        _inventory = inventory != null ? inventory : _inventory;
        if (_repairPanel != null)
        {
            _repairPanel.SetActive(true);
        }

        if (_craftPanel != null)
        {
            _craftPanel.SetActive(false);
        }

        UnlockCursor();
        UpdateRepairPanel();
    }

    public void ClosePanels()
    {
        if (_craftPanel != null)
        {
            _craftPanel.SetActive(false);
        }

        if (_repairPanel != null)
        {
            _repairPanel.SetActive(false);
        }
    }

    private void ResolveReferences()
    {
        if (_playerMovement == null)
        {
            _playerMovement = FindFirstObjectByType<PlayerMovement>();
        }

        if (_inventory == null)
        {
            _inventory = FindFirstObjectByType<MurinoInventory>();
        }

        if (_craftingSystem == null)
        {
            _craftingSystem = FindFirstObjectByType<CraftingSystem>();
        }

        if (_goalController == null)
        {
            _goalController = FindFirstObjectByType<FloorGoalController>();
        }
    }

    private void HandleUiInput()
    {
        if (PlayerInputAdapter.WasCursorReleasePressed() && IsModalOpen)
        {
            ClosePanels();
            _activeRepairTarget = null;
            return;
        }

        if (PlayerInputAdapter.WasCraftTogglePressed())
        {
            var newState = _craftPanel != null && !_craftPanel.activeSelf;
            if (_craftPanel != null)
            {
                _craftPanel.SetActive(newState);
            }

            if (_repairPanel != null && newState)
            {
                _repairPanel.SetActive(false);
            }

            if (newState)
            {
                UnlockCursor();
            }
        }
    }

    private void UpdateStatus()
    {
        if (_playerMovement == null || _statusText == null)
        {
            return;
        }

        var stance = _playerMovement.IsCrouching ? "CROUCH" : "STAND";
        var grounded = _playerMovement.IsGrounded ? "GROUND" : "AIR";
        _statusText.text = string.Format("{0} | {1} | {2:0.0} m/s", stance, grounded, _playerMovement.CurrentHorizontalSpeed);
    }

    private void UpdateInventory()
    {
        if (_inventoryText == null)
        {
            return;
        }

        _inventoryText.text = _inventory == null ? "Пусто" : _inventory.BuildSummary();
    }

    private void UpdateObjectives()
    {
        if (_objectivesText == null)
        {
            return;
        }

        _objectivesText.text = _goalController == null ? "Цели недоступны" : _goalController.BuildObjectiveSummary();
    }

    private void UpdateCraftButtons()
    {
        if (_craftContentRoot == null || _craftingSystem == null)
        {
            return;
        }

        if (_recipeButtons.Count == 0)
        {
            BuildCraftRecipeButtons();
        }

        for (var i = 0; i < _craftingSystem.Recipes.Count && i < _recipeButtons.Count; i++)
        {
            var recipe = _craftingSystem.Recipes[i];
            var canCraft = _craftingSystem.CanCraft(recipe);
            _recipeButtons[i].interactable = canCraft;
            _recipeButtonTexts[i].text = string.Format("{0}\n{1}", recipe.DisplayName, BuildRecipeDescription(recipe));
            var image = _recipeButtons[i].GetComponent<Image>();
            if (image != null)
            {
                image.color = canCraft ? new Color(0.2f, 0.52f, 0.24f, 0.88f) : new Color(0.18f, 0.18f, 0.2f, 0.88f);
            }
        }
    }

    private void UpdateRepairPanel()
    {
        if (_repairPanel == null || _repairStatusText == null || _repairButton == null || _repairButtonText == null)
        {
            return;
        }

        if (_activeRepairTarget == null)
        {
            _repairStatusText.text = "Подойдите к панели лифта и нажмите E.";
            _repairButton.interactable = false;
            _repairButtonText.text = "Лифт не выбран";
            return;
        }

        _repairStatusText.text = _activeRepairTarget.BuildStatus(_inventory);
        var canRepair = _activeRepairTarget.CanRepair(_inventory);
        _repairButton.interactable = canRepair;
        _repairButtonText.text = canRepair ? "Починить лифт" : "Требуются детали";
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

        _promptPanel = BuildPanel(canvasObject.transform, "PromptPanel", new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 34f), new Vector2(560f, 52f), new Color(0.05f, 0.06f, 0.08f, 0.82f));
        _promptText = BuildText(_promptPanel.transform, "PromptText", font, 24, TextAnchor.MiddleCenter, "Нажмите E", Color.white);

        var controlsPanel = BuildPanel(canvasObject.transform, "ControlsPanel", new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(20f, -20f), new Vector2(460f, 110f), new Color(0.05f, 0.06f, 0.08f, 0.72f));
        BuildText(controlsPanel.transform, "ControlsText", font, 18, TextAnchor.UpperLeft, "WASD move\nMouse look\nShift sprint | Space jump | Ctrl crouch\nE interact | Tab craft | Esc unlock cursor", new Color(0.85f, 0.9f, 0.96f));

        var objectivesPanel = BuildPanel(canvasObject.transform, "ObjectivesPanel", new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-20f, -20f), new Vector2(360f, 170f), new Color(0.05f, 0.06f, 0.08f, 0.74f));
        _objectivesText = BuildText(objectivesPanel.transform, "ObjectivesText", font, 18, TextAnchor.UpperLeft, "Загрузка целей...", new Color(0.92f, 0.95f, 0.98f));

        var inventoryPanel = BuildPanel(canvasObject.transform, "InventoryPanel", new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(20f, 20f), new Vector2(360f, 200f), new Color(0.05f, 0.06f, 0.08f, 0.74f));
        BuildText(inventoryPanel.transform, "InventoryTitle", font, 20, TextAnchor.UpperLeft, "Инвентарь", new Color(0.92f, 0.95f, 0.98f));
        _inventoryText = BuildText(inventoryPanel.transform, "InventoryText", font, 18, TextAnchor.UpperLeft, "Пусто", new Color(0.82f, 0.88f, 0.94f), new Vector2(14f, 48f), new Vector2(-14f, -10f));

        var statusPanel = BuildPanel(canvasObject.transform, "StatusPanel", new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-20f, 20f), new Vector2(280f, 46f), new Color(0.05f, 0.06f, 0.08f, 0.72f));
        _statusText = BuildText(statusPanel.transform, "StatusText", font, 18, TextAnchor.MiddleCenter, "STAND | GROUND | 0.0 m/s", new Color(0.9f, 0.94f, 0.98f));

        BuildCraftPanel(canvasObject.transform, font);
        BuildRepairPanel(canvasObject.transform, font);
    }

    private void BuildCraftPanel(Transform parent, Font font)
    {
        _craftPanel = BuildPanel(parent, "CraftPanel", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-230f, 0f), new Vector2(460f, 320f), new Color(0.04f, 0.05f, 0.07f, 0.92f));
        BuildText(_craftPanel.transform, "CraftTitle", font, 24, TextAnchor.UpperLeft, "Крафт", Color.white);
        BuildCloseButton(_craftPanel.transform, font, "CraftClose", new Vector2(180f, -18f), ClosePanels);

        var contentRoot = new GameObject("CraftContent");
        contentRoot.transform.SetParent(_craftPanel.transform, false);
        var rect = contentRoot.AddComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = new Vector2(16f, 16f);
        rect.offsetMax = new Vector2(-16f, -56f);
        _craftContentRoot = contentRoot.transform;

        _craftPanel.SetActive(false);
    }

    private void BuildRepairPanel(Transform parent, Font font)
    {
        _repairPanel = BuildPanel(parent, "RepairPanel", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(250f, 0f), new Vector2(420f, 250f), new Color(0.04f, 0.05f, 0.07f, 0.92f));
        BuildText(_repairPanel.transform, "RepairTitle", font, 24, TextAnchor.UpperLeft, "Ремонт лифта", Color.white);
        BuildCloseButton(_repairPanel.transform, font, "RepairClose", new Vector2(160f, -18f), delegate { _activeRepairTarget = null; ClosePanels(); });
        _repairStatusText = BuildText(_repairPanel.transform, "RepairStatus", font, 18, TextAnchor.UpperLeft, "Подойдите к панели лифта и нажмите E.", new Color(0.84f, 0.9f, 0.95f), new Vector2(14f, 54f), new Vector2(-14f, -80f));

        Text buttonText;
        _repairButton = BuildButton(_repairPanel.transform, "RepairButton", font, "Починить лифт", new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 20f), new Vector2(250f, 46f), out buttonText);
        _repairButtonText = buttonText;
        _repairButton.onClick.AddListener(OnRepairButtonPressed);
        _repairPanel.SetActive(false);
    }

    private void BuildCraftRecipeButtons()
    {
        for (var i = 0; i < _craftingSystem.Recipes.Count; i++)
        {
            var recipe = _craftingSystem.Recipes[i];
            Text buttonText;
            var button = BuildButton(_craftContentRoot, string.Format("RecipeButton_{0}", i), Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"), recipe.DisplayName, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -i * 76f), new Vector2(-10f, 68f), out buttonText);
            var capturedRecipe = recipe;
            button.onClick.AddListener(delegate { TryCraft(capturedRecipe); });
            _recipeButtons.Add(button);
            _recipeButtonTexts.Add(buttonText);
        }
    }

    private void TryCraft(CraftRecipe recipe)
    {
        if (_craftingSystem == null || recipe == null)
        {
            return;
        }

        if (_craftingSystem.TryCraft(recipe))
        {
            UpdateCraftButtons();
            UpdateInventory();
            UpdateRepairPanel();
        }
    }

    private void OnRepairButtonPressed()
    {
        if (_activeRepairTarget == null || _playerMovement == null)
        {
            return;
        }

        if (_activeRepairTarget.TryRepair(_playerMovement.gameObject))
        {
            _activeRepairTarget = null;
            ClosePanels();
        }

        UpdateRepairPanel();
        UpdateObjectives();
    }

    private static string BuildRecipeDescription(CraftRecipe recipe)
    {
        var parts = new List<string>();
        for (var i = 0; i < recipe.Inputs.Length; i++)
        {
            var input = recipe.Inputs[i];
            if (input.Item != null)
            {
                parts.Add(string.Format("{0} x{1}", input.Item.DisplayName, input.Count));
            }
        }

        return string.Format("{0} -> {1} x{2}", string.Join(" + ", parts), recipe.Output.DisplayName, recipe.OutputCount);
    }

    private static void UnlockCursor()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
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
        return BuildText(parent, name, font, fontSize, alignment, content, color, new Vector2(14f, 10f), new Vector2(-14f, -10f));
    }

    private static Text BuildText(Transform parent, string name, Font font, int fontSize, TextAnchor alignment, string content, Color color, Vector2 offsetMin, Vector2 offsetMax)
    {
        var textObject = new GameObject(name);
        textObject.transform.SetParent(parent, false);

        var rect = textObject.AddComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = offsetMin;
        rect.offsetMax = offsetMax;

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

    private static Button BuildButton(Transform parent, string name, Font font, string label, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 anchoredPosition, Vector2 sizeDelta, out Text buttonText)
    {
        var buttonObject = new GameObject(name);
        buttonObject.transform.SetParent(parent, false);
        var rect = buttonObject.AddComponent<RectTransform>();
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.pivot = pivot;
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = sizeDelta;

        var image = buttonObject.AddComponent<Image>();
        image.color = new Color(0.18f, 0.18f, 0.2f, 0.88f);
        var button = buttonObject.AddComponent<Button>();
        button.targetGraphic = image;

        buttonText = BuildText(buttonObject.transform, "Label", font, 18, TextAnchor.MiddleCenter, label, Color.white, new Vector2(8f, 8f), new Vector2(-8f, -8f));
        return button;
    }

    private static void BuildCloseButton(Transform parent, Font font, string name, Vector2 anchoredPosition, UnityEngine.Events.UnityAction onClick)
    {
        Text buttonText;
        var button = BuildButton(parent, name, font, "X", new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(1f, 1f), anchoredPosition, new Vector2(38f, 32f), out buttonText);
        buttonText.fontSize = 16;
        button.onClick.AddListener(onClick);
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

