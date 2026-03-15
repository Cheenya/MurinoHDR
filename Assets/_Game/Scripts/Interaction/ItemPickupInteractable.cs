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
}
