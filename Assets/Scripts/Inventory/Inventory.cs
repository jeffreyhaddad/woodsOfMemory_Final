using System;
using UnityEngine;

[System.Serializable]
public class InventorySlot
{
    public ItemData item;
    public int quantity;

    public bool IsEmpty => item == null;

    public void Clear()
    {
        item = null;
        quantity = 0;
    }
}

public class Inventory : MonoBehaviour
{
    public int slotCount = 48;
    public InventorySlot[] slots;

    /// <summary>Fired whenever the inventory contents change.</summary>
    public event Action OnInventoryChanged;

    /// <summary>Manually notify listeners that inventory changed (e.g. after external slot modification).</summary>
    public void NotifyChanged() => OnInventoryChanged?.Invoke();

    void Awake()
    {
        slots = new InventorySlot[slotCount];
        for (int i = 0; i < slotCount; i++)
            slots[i] = new InventorySlot();
    }

    /// <summary>
    /// Add an item to the inventory. Stacks onto existing slots first, then uses empty slots.
    /// Returns true if all items were added, false if inventory is full.
    /// </summary>
    public bool AddItem(ItemData item, int amount = 1)
    {
        int remaining = amount;

        // First pass: try to stack onto existing slots with the same item
        if (item.isStackable)
        {
            for (int i = 0; i < slots.Length && remaining > 0; i++)
            {
                if (slots[i].item == item && slots[i].quantity < item.maxStack)
                {
                    int spaceInSlot = item.maxStack - slots[i].quantity;
                    int toAdd = Mathf.Min(remaining, spaceInSlot);
                    slots[i].quantity += toAdd;
                    remaining -= toAdd;
                }
            }
        }

        // Second pass: place remaining into empty slots
        for (int i = 0; i < slots.Length && remaining > 0; i++)
        {
            if (slots[i].IsEmpty)
            {
                slots[i].item = item;
                int toAdd = item.isStackable ? Mathf.Min(remaining, item.maxStack) : 1;
                slots[i].quantity = toAdd;
                remaining -= toAdd;
            }
        }

        if (remaining < amount) // at least some items were added
            OnInventoryChanged?.Invoke();

        return remaining <= 0;
    }

    /// <summary>
    /// Remove an amount of an item. Returns true if enough were found and removed.
    /// </summary>
    public bool RemoveItem(ItemData item, int amount = 1)
    {
        if (GetItemCount(item) < amount)
            return false;

        int remaining = amount;

        for (int i = slots.Length - 1; i >= 0 && remaining > 0; i--)
        {
            if (slots[i].item == item)
            {
                int toRemove = Mathf.Min(remaining, slots[i].quantity);
                slots[i].quantity -= toRemove;
                remaining -= toRemove;

                if (slots[i].quantity <= 0)
                    slots[i].Clear();
            }
        }

        OnInventoryChanged?.Invoke();
        return true;
    }

    /// <summary>Check if the inventory contains at least this many of the item.</summary>
    public bool HasItem(ItemData item, int amount = 1)
    {
        return GetItemCount(item) >= amount;
    }

    /// <summary>Get total quantity of an item across all slots.</summary>
    public int GetItemCount(ItemData item)
    {
        int count = 0;
        for (int i = 0; i < slots.Length; i++)
        {
            if (slots[i].item == item)
                count += slots[i].quantity;
        }
        return count;
    }
}
