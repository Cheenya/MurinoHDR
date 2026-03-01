using UnityEngine;

namespace MurinoHDR.Interaction
{

public interface IInteractable
{
    string GetPrompt(GameObject interactor);
    bool CanInteract(GameObject interactor);
    void Interact(GameObject interactor);
}
}


