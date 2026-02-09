using UnityEngine;

public enum ItemCategory
{
    Resource,
    Tool,
    Weapon,
    Food,
    QuestItem
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
}
