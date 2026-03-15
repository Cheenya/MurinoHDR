using MurinoHDR.Core;
using MurinoHDR.Generation;
using MurinoHDR.Inventory;
using UnityEngine;

namespace MurinoHDR.Interaction
{

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
}
