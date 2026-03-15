using MurinoHDR.Core;
using MurinoHDR.Generation;
using MurinoHDR.Inventory;
using UnityEngine;

namespace MurinoHDR.Interaction
{

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
