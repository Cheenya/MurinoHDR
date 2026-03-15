using MurinoHDR.Core;
using MurinoHDR.Generation;
using MurinoHDR.Inventory;
using MurinoHDR.UI;
using UnityEngine;
using MurinoInventory = MurinoHDR.Inventory.Inventory;

namespace MurinoHDR.Interaction
{

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
        return _isRepaired ? "Лифт готов к запуску" : "Осмотреть панель ремонта лифта";
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
}
