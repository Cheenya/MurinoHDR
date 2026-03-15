using MurinoHDR.Core;
using MurinoHDR.Generation;
using MurinoHDR.Inventory;
using MurinoHDR.UI;
using UnityEngine;
using MurinoInventory = MurinoHDR.Inventory.Inventory;

namespace MurinoHDR.Interaction
{

public sealed class ItemPickupInteractable : InteractableBase
{
    [SerializeField] private ItemDefinition _item;
    [SerializeField] private int _count = 1;

    public void Configure(ItemDefinition item, int count)
    {
        _item = item;
        _count = Mathf.Max(1, count);
    }

    public override string GetPrompt(GameObject interactor)
    {
        return _item == null ? "Поднять" : string.Format("Поднять {0}", _item.DisplayName);
    }

    public override void Interact(GameObject interactor)
    {
        var inventory = interactor.GetComponent<MurinoInventory>();
        if (_item == null || inventory == null)
        {
            return;
        }

        if (inventory.AddItem(_item, _count))
        {
            Debug.Log(string.Format("[INV] Picked up {0} x{1}", _item.DisplayName, _count));
            InteractionPromptUI.Instance?.Hide();
            Destroy(gameObject);
        }
    }
}

public abstract class ExitInteractableBase : InteractableBase
{
    [SerializeField] private ExitPathType _pathType;

    private FloorGoalController _goalController;
    private ItemDefinition[] _requirements = System.Array.Empty<ItemDefinition>();

    protected ExitPathType PathType => _pathType;

    public void Configure(FloorGoalController goalController, ExitPathType pathType, params ItemDefinition[] requirements)
    {
        _goalController = goalController;
        _pathType = pathType;
        _requirements = requirements ?? System.Array.Empty<ItemDefinition>();
    }

    public override string GetPrompt(GameObject interactor)
    {
        var inventory = interactor.GetComponent<MurinoInventory>();
        var path = _goalController != null ? _goalController.GetPath(_pathType) : null;
        if (path != null && path.State == ExitPathState.Open)
        {
            return string.Format("{0} уже открыт", path.DisplayName);
        }

        return HasRequirements(inventory) ? GetReadyPrompt() : BuildMissingPrompt(inventory);
    }

    public override void Interact(GameObject interactor)
    {
        var inventory = interactor.GetComponent<MurinoInventory>();
        if (!HasRequirements(inventory))
        {
            Debug.Log(string.Format("[INT] {0}", BuildMissingPrompt(inventory)));
            return;
        }

        _goalController?.MarkInProgress(_pathType);
        ConsumeRequirements(inventory);
        OnUnlocked();
        _goalController?.MarkOpen(_pathType);
    }

    protected virtual string GetReadyPrompt()
    {
        return "Открыть путь";
    }

    protected virtual void OnUnlocked()
    {
    }

    protected virtual bool ShouldConsume(ItemDefinition item)
    {
        return false;
    }

    private void ConsumeRequirements(MurinoInventory inventory)
    {
        if (inventory == null)
        {
            return;
        }

        for (var i = 0; i < _requirements.Length; i++)
        {
            var item = _requirements[i];
            if (item != null && ShouldConsume(item))
            {
                inventory.TryRemoveItem(item, 1);
            }
        }
    }

    private bool HasRequirements(MurinoInventory inventory)
    {
        if (inventory == null)
        {
            return false;
        }

        for (var i = 0; i < _requirements.Length; i++)
        {
            var item = _requirements[i];
            if (item == null)
            {
                continue;
            }

            if (!inventory.HasItem(item.Id, 1))
            {
                return false;
            }
        }

        return true;
    }

    private string BuildMissingPrompt(MurinoInventory inventory)
    {
        var parts = new System.Collections.Generic.List<string>();
        for (var i = 0; i < _requirements.Length; i++)
        {
            var item = _requirements[i];
            if (item == null)
            {
                continue;
            }

            if (inventory == null || !inventory.HasItem(item.Id, 1))
            {
                parts.Add(item.DisplayName);
            }
        }

        return parts.Count == 0 ? "Путь недоступен" : string.Format("Нужно: {0}", string.Join(", ", parts));
    }
}

public sealed class ElevatorRepairInteractable : InteractableBase
{
    private FloorGoalController _goalController;
    private ItemDefinition _keycard;
    private ItemDefinition _repairedFuse;
    private Transform _leftDoor;
    private Transform _rightDoor;
    private bool _isRepaired;

    public void Configure(FloorGoalController goalController, Transform leftDoor, Transform rightDoor)
    {
        _goalController = goalController;
        _leftDoor = leftDoor;
        _rightDoor = rightDoor;
        _keycard = MvpRuntimeContent.GetItem("keycard");
        _repairedFuse = MvpRuntimeContent.GetItem("repaired_fuse");
    }

    public override string GetPrompt(GameObject interactor)
    {
        if (_isRepaired)
        {
            return "Лифт готов к запуску";
        }

        return "Осмотреть панель ремонта лифта";
    }

    public override void Interact(GameObject interactor)
    {
        var inventory = interactor.GetComponent<MurinoInventory>();
        InteractionPromptUI.Instance?.OpenRepairPanel(this, inventory);
    }

    public string BuildStatus(MurinoInventory inventory)
    {
        if (_isRepaired)
        {
            return "Лифт восстановлен. Двери открыты.";
        }

        var hasCard = inventory != null && _keycard != null && inventory.HasItem(_keycard.Id, 1);
        var hasFuse = inventory != null && _repairedFuse != null && inventory.HasItem(_repairedFuse.Id, 1);
        return string.Format("Ключ-карта: {0}\nПочиненный предохранитель: {1}", hasCard ? "есть" : "нет", hasFuse ? "есть" : "нет");
    }

    public bool CanRepair(MurinoInventory inventory)
    {
        return !_isRepaired && inventory != null && _keycard != null && _repairedFuse != null && inventory.HasItem(_keycard.Id, 1) && inventory.HasItem(_repairedFuse.Id, 1);
    }

    public bool TryRepair(GameObject interactor)
    {
        var inventory = interactor.GetComponent<MurinoInventory>();
        if (!CanRepair(inventory))
        {
            Debug.Log("[INT] Elevator requirements are not met");
            return false;
        }

        inventory.TryRemoveItem(_repairedFuse, 1);
        _isRepaired = true;
        _goalController?.MarkInProgress(ExitPathType.Elevator);
        OpenDoor(_leftDoor, Vector3.left * 0.8f);
        OpenDoor(_rightDoor, Vector3.right * 0.8f);
        _goalController?.MarkOpen(ExitPathType.Elevator);
        Debug.Log("[INT] Elevator repaired and ready");
        return true;
    }

    private static void OpenDoor(Transform door, Vector3 offset)
    {
        if (door == null)
        {
            return;
        }

        door.localPosition += offset;
        var collider = door.GetComponent<Collider>();
        if (collider != null)
        {
            collider.enabled = false;
        }
    }
}

public sealed class ShaftHatchInteractable : ExitInteractableBase
{
    [SerializeField] private Transform _hatch;

    public void Configure(FloorGoalController goalController, Transform hatch)
    {
        _hatch = hatch;
        base.Configure(goalController, ExitPathType.Shaft, MvpRuntimeContent.GetItem("crowbar"), MvpRuntimeContent.GetItem("rope"));
    }

    protected override string GetReadyPrompt()
    {
        return "Открыть шахту";
    }

    protected override bool ShouldConsume(ItemDefinition item)
    {
        return item != null && item.Id == "rope";
    }

    protected override void OnUnlocked()
    {
        if (_hatch != null)
        {
            _hatch.localRotation = Quaternion.Euler(0f, 0f, -75f);
        }
    }
}

public sealed class StairsDoorInteractable : ExitInteractableBase
{
    [SerializeField] private Transform _door;

    public void Configure(FloorGoalController goalController, Transform door)
    {
        _door = door;
        base.Configure(goalController, ExitPathType.Stairs, MvpRuntimeContent.GetItem("lockpick"));
    }

    protected override string GetReadyPrompt()
    {
        return "Вскрыть дверь лестницы";
    }

    protected override bool ShouldConsume(ItemDefinition item)
    {
        return true;
    }

    protected override void OnUnlocked()
    {
        if (_door != null)
        {
            _door.localRotation = Quaternion.Euler(0f, -105f, 0f);
            var collider = _door.GetComponent<Collider>();
            if (collider != null)
            {
                collider.enabled = false;
            }
        }
    }
}
}
