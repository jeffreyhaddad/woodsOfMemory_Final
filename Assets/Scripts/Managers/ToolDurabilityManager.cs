using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Tracks current durability for equipped tools (axe, pickaxe).
/// Durability decreases on each swing; when it hits 0 the item is
/// unequipped and removed from inventory (Minecraft-style).
/// Auto-created by GameManager.
/// </summary>
public class ToolDurabilityManager : MonoBehaviour
{
    public static ToolDurabilityManager Instance { get; private set; }

    /// <summary>Fired whenever any durability value changes.</summary>
    public event Action OnDurabilityChanged;

    // Current uses remaining, keyed by itemName
    private Dictionary<string, int> current = new Dictionary<string, int>();

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(this); return; }
        Instance = this;
    }

    // ─── Public API ──────────────────────────────────────────────────────────

    public bool HasDurability(string itemName)
    {
        ItemData item = ItemRegistry.Get(itemName);
        return item != null && item.maxDurability > 0;
    }

    public int GetMax(string itemName)
    {
        ItemData item = ItemRegistry.Get(itemName);
        return item != null ? item.maxDurability : 0;
    }

    public int GetCurrent(string itemName)
    {
        if (!current.TryGetValue(itemName, out int val))
        {
            val = GetMax(itemName);
            current[itemName] = val;
        }
        return val;
    }

    public float GetFraction(string itemName)
    {
        int max = GetMax(itemName);
        if (max <= 0) return 1f;
        return Mathf.Clamp01((float)GetCurrent(itemName) / max);
    }

    /// <summary>
    /// Called by PlayerCombat on each axe/pickaxe swing.
    /// Decrements durability and destroys the item if it reaches 0.
    /// </summary>
    public void UseTool(string itemName)
    {
        if (string.IsNullOrEmpty(itemName) || !HasDurability(itemName)) return;

        int val = GetCurrent(itemName) - 1;
        current[itemName] = Mathf.Max(val, 0);
        OnDurabilityChanged?.Invoke();

        if (val <= 0)
        {
            current.Remove(itemName);
            BreakTool(itemName);
        }
    }

    // ─── Private ─────────────────────────────────────────────────────────────

    void BreakTool(string itemName)
    {
        ItemData item = ItemRegistry.Get(itemName);
        if (item == null) return;

        // Unequip first so the HUD and weapon visual update immediately
        EquipmentManager.Instance?.Unequip(item);

        // Remove from inventory
        Inventory inv = FindAnyObjectByType<Inventory>();
        inv?.RemoveItem(item, 1);

        // Reuse hit SFX as a break sound cue
        SFXManager.PlayHit();

        Debug.Log($"[Durability] {itemName} broke!");
    }
}
