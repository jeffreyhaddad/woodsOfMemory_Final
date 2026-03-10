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

    // Maps item names to icon filenames in Resources/Icons/
    private static Dictionary<string, string> iconMap = new Dictionary<string, string>
    {
        // Raw materials
        { "Wood",           "wood" },
        { "Stone",          "stone" },
        { "Fiber",          "fiber" },
        { "Berries",        "berries" },
        { "Herbs",          "herbs" },
        { "Deer Hide",      "deer_hide" },
        { "Venison",        "venison" },
        { "Bone",           "bone" },
        { "Feather",        "feather" },
        { "Iron Ore",       "iron_ore" },
        // Special / quest items
        { "Rusted Key",     "rusted_key" },
        { "Lantern",        "lantern" },
        { "Shadow Essence", "shadow_essence" },
        { "Shadow Ward",    "shadow_ward" },
        // Tools
        { "Stone Axe",      "stone_axe" },
        { "Stone Pickaxe",  "stone_pickaxe" },
        { "Torch",          "torch" },
        { "Fishing Rod",    "fishing_rod" },
        { "Rope",           "rope" },
        { "Campfire",       "campfire" },
        { "Bone Needle",    "bone_needle" },
        // Weapons
        { "Wooden Spear",   "wooden_spear" },
        { "Bow",            "bow" },
        { "Arrow",          "arrow" },
        { "Stone Knife",    "stone_knife" },
        // Food
        { "Cooked Venison", "cooked_venison" },
        { "Berry Stew",     "berry_stew" },
        { "Herbal Tea",     "herbal_tea" },
        // Consumables / wearables
        { "Bandage",        "bandage" },
        { "Leather Armor",  "leather_armor" },
        { "Shelter Kit",    "shelter_kit" },
        { "Fur Bedroll",    "fur_bedroll" },
        { "Water from well","water_bucket" },
    };

    private static Dictionary<string, Sprite> spriteCache = new Dictionary<string, Sprite>();

    /// <summary>Register an ItemData so it can be looked up by name later.</summary>
    public static void Register(ItemData item)
    {
        if (item == null || string.IsNullOrEmpty(item.itemName)) return;

        // Auto-assign icon if not already set
        if (item.icon == null)
            item.icon = LoadIcon(item.itemName);

        // Don't overwrite if already registered (first registration wins)
        if (!items.ContainsKey(item.itemName))
            items[item.itemName] = item;
    }

    static Sprite LoadIcon(string itemName)
    {
        // Check cache first
        if (spriteCache.TryGetValue(itemName, out Sprite cached))
            return cached;

        // Look up the icon filename for this item
        string iconFile;
        if (!iconMap.TryGetValue(itemName, out iconFile))
            return null;

        // Try loading as Sprite first (works if texture import type is Sprite)
        Sprite sprite = Resources.Load<Sprite>("Icons/" + iconFile);
        if (sprite != null)
        {
            spriteCache[itemName] = sprite;
            return sprite;
        }

        // Fallback: load as Texture2D and create Sprite at runtime
        Texture2D tex = Resources.Load<Texture2D>("Icons/" + iconFile);
        if (tex != null)
        {
            sprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height),
                new Vector2(0.5f, 0.5f), 100f);
            spriteCache[itemName] = sprite;
            return sprite;
        }

        return null;
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
