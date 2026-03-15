using System;
using System.Collections.Generic;
using UnityEngine;

namespace MurinoHDR.Inventory
{

[Serializable]
public sealed class InventorySlot
{
    [SerializeField] private ItemDefinition _item;
    [SerializeField] private int _count;

    public ItemDefinition Item => _item;
    public int Count => _count;
    public bool IsEmpty => _item == null || _count <= 0;

    public void Set(ItemDefinition item, int count)
    {
        _item = item;
        _count = Mathf.Max(0, count);
    }

    public void Clear()
    {
        _item = null;
        _count = 0;
    }

    public int Add(int amount)
    {
        if (_item == null || amount <= 0)
        {
            return amount;
        }

        var freeSpace = Mathf.Max(0, _item.MaxStack - _count);
        var toAdd = Mathf.Min(freeSpace, amount);
        _count += toAdd;
        return amount - toAdd;
    }

    public int Remove(int amount)
    {
        if (_item == null || amount <= 0)
        {
            return 0;
        }

        var removed = Mathf.Min(_count, amount);
        _count -= removed;
        if (_count <= 0)
        {
            Clear();
        }

        return removed;
    }
}

public sealed class Inventory : MonoBehaviour
{
    [SerializeField] private int _capacity = 12;
    [SerializeField] private List<InventorySlot> _slots = new List<InventorySlot>();

    public event Action Changed;

    public IReadOnlyList<InventorySlot> Slots => _slots;

    private void Awake()
    {
        EnsureCapacity();
    }

    public void EnsureCapacity()
    {
        while (_slots.Count < _capacity)
        {
            _slots.Add(new InventorySlot());
        }
    }

    public bool AddItem(ItemDefinition item, int count)
    {
        if (item == null || count <= 0)
        {
            return false;
        }

        EnsureCapacity();
        var remaining = count;

        if (item.Stackable)
        {
            for (var i = 0; i < _slots.Count && remaining > 0; i++)
            {
                var slot = _slots[i];
                if (slot.Item == item && slot.Count < item.MaxStack)
                {
                    remaining = slot.Add(remaining);
                }
            }
        }

        for (var i = 0; i < _slots.Count && remaining > 0; i++)
        {
            var slot = _slots[i];
            if (!slot.IsEmpty)
            {
                continue;
            }

            var amount = item.Stackable ? Mathf.Min(item.MaxStack, remaining) : 1;
            slot.Set(item, amount);
            remaining -= amount;
        }

        if (remaining != count)
        {
            Changed?.Invoke();
            Debug.Log(string.Format("[INV] Added {0} x{1}", item.DisplayName, count - remaining));
            return true;
        }

        return false;
    }

    public bool HasItem(string itemId, int count)
    {
        return GetCount(itemId) >= count;
    }

    public int GetCount(string itemId)
    {
        var total = 0;
        for (var i = 0; i < _slots.Count; i++)
        {
            var slot = _slots[i];
            if (!slot.IsEmpty && slot.Item.Id == itemId)
            {
                total += slot.Count;
            }
        }

        return total;
    }

    public bool HasTag(ItemTag tag, int count)
    {
        var total = 0;
        for (var i = 0; i < _slots.Count; i++)
        {
            var slot = _slots[i];
            if (slot.IsEmpty || !slot.Item.HasTag(tag))
            {
                continue;
            }

            total += slot.Count;
            if (total >= count)
            {
                return true;
            }
        }

        return false;
    }

    public bool TryRemoveItem(ItemDefinition item, int count)
    {
        if (item == null)
        {
            return false;
        }

        return TryRemoveItem(item.Id, count);
    }

    public bool TryRemoveItem(string itemId, int count)
    {
        if (count <= 0 || GetCount(itemId) < count)
        {
            return false;
        }

        var remaining = count;
        for (var i = 0; i < _slots.Count && remaining > 0; i++)
        {
            var slot = _slots[i];
            if (slot.IsEmpty || slot.Item.Id != itemId)
            {
                continue;
            }

            remaining -= slot.Remove(remaining);
        }

        Changed?.Invoke();
        return remaining == 0;
    }

    public string BuildSummary()
    {
        var parts = new List<string>();
        for (var i = 0; i < _slots.Count; i++)
        {
            var slot = _slots[i];
            if (slot.IsEmpty)
            {
                continue;
            }

            parts.Add(string.Format("{0} x{1}", slot.Item.DisplayName, slot.Count));
        }

        return parts.Count == 0 ? "Пусто" : string.Join("\n", parts);
    }
}
}
