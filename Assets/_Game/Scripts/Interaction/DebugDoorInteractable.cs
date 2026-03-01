using UnityEngine;

namespace MurinoHDR.Interaction
{

public sealed class DebugDoorInteractable : InteractableBase
{
    public override string GetPrompt(GameObject interactor)
    {
        return "Открыть дверь";
    }

    public override void Interact(GameObject interactor)
    {
        Debug.Log("[INT] Door interaction triggered");
    }
}
}


