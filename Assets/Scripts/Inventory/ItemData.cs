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
}
