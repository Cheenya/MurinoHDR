using MurinoHDR.Core;
using MurinoHDR.Generation;
using MurinoHDR.Inventory;
using UnityEngine;
using MurinoInventory = MurinoHDR.Inventory.Inventory;

namespace MurinoHDR.Interaction
{

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
}
