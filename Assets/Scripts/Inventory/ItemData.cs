using UnityEngine;

public enum ItemCategory
{
    Resource,
    Tool,
    Weapon,
    Food,
    QuestItem
}

public enum ItemUseAction
{
    None,
    EatFood,
    UseBandage,
    PlaceCampfire,
    EquipTorch
}

public enum EquipSlot
{
    None,
    Weapon,
    Tool,
    Armor
}

/// <summary>
/// Defines an item type. Create assets via: right-click → Create → Inventory → Item.
/// </summary>
[CreateAssetMenu(fileName = "New Item", menuName = "Inventory/Item")]
public class ItemData : ScriptableObject
{
    public string itemName;
    public Sprite icon;
    [TextArea] public string description;
    public ItemCategory category;
    public bool isStackable = true;
    public int maxStack = 10;

    [Header("Use Action")]
    public ItemUseAction useAction = ItemUseAction.None;
    [Tooltip("Amount restored/applied when used (hunger for food, health for bandage)")]
    public float useValue = 0f;

    [Header("Equipment")]
    public EquipSlot equipSlot = EquipSlot.None;
    [Tooltip("Extra damage when equipped as weapon (added to base melee damage)")]
    public float damageBonus = 0f;
    [Tooltip("Damage reduction when equipped as armor (flat amount subtracted from incoming damage)")]
    public float defenseBonus = 0f;
}
