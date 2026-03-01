using UnityEngine;

public class WaterWellInteraction : Interactable
{
    private static Inventory sharedInventory;
    public ItemData water;

    private void Start()
    {
        sharedInventory = FindAnyObjectByType<Inventory>();

        if (water == null)
            water = GetOrMakeWater();

        promptText = "Fill Water (Water from well)";
    }

    public override void OnInteract()
    {
        if (sharedInventory == null)
        {
            Debug.LogWarning("Water Well: No Inventory found in scene.");
            return;
        }

        if (sharedInventory.AddItem(water))
        {
            SFXManager.PlayPickup();
            Debug.Log("Filled 1x " + water.itemName);
        }
        else
        {
            Debug.Log("Inventory is full!");
        }
    }

    static ItemData GetOrMakeWater()
    {
        ItemData item = ItemRegistry.Get("Water from well");
        if (item != null) return item;

        item = ScriptableObject.CreateInstance<ItemData>();
        item.name        = item.itemName = "Water from well";
        item.description = "Fresh water drawn from the well.";
        item.category    = ItemCategory.Resource;
        item.isStackable = true;
        item.maxStack    = 5;
        item.useAction   = ItemUseAction.None;
        ItemRegistry.Register(item);
        return item;
    }
}
