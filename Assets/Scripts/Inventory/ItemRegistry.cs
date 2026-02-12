using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Central registry that maps item names to ItemData instances.
/// Both WorldSetup and CraftingUI register their items here so
/// the SaveManager can restore inventory items by name.
/// </summary>
public static class ItemRegistry
{
    private static Dictionary<string, ItemData> items = new Dictionary<string, ItemData>();

    /// <summary>Register an ItemData so it can be looked up by name later.</summary>
    public static void Register(ItemData item)
    {
        if (item == null || string.IsNullOrEmpty(item.itemName)) return;

        // Don't overwrite if already registered (first registration wins)
        if (!items.ContainsKey(item.itemName))
            items[item.itemName] = item;
    }

    /// <summary>Look up an ItemData by its itemName. Returns null if not found.</summary>
    public static ItemData Get(string itemName)
    {
        if (string.IsNullOrEmpty(itemName)) return null;
        items.TryGetValue(itemName, out ItemData item);
        return item;
    }

    /// <summary>Clear the registry (called on scene reload).</summary>
    public static void Clear()
    {
        items.Clear();
    }
}
