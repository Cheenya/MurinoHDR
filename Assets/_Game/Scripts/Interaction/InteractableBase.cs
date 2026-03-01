using UnityEngine;

namespace MurinoHDR.Interaction;

public abstract class InteractableBase : MonoBehaviour, IInteractable
{
    [SerializeField] private string _prompt = "Взаимодействовать";

    public virtual string GetPrompt(GameObject interactor)
    {
        return _prompt;
    }

    public virtual bool CanInteract(GameObject interactor)
    {
        return true;
    }

    public abstract void Interact(GameObject interactor);
}
