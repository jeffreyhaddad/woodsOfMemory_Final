using UnityEngine;

/// <summary>
/// Picking up the cabin lantern adds a "Lantern" quest item to the player's
/// inventory. The item persists there for a future mission objective.
/// </summary>
public class PickupLantern : Interactable
{
    // Shared ItemData so every lantern in the scene uses the same asset
    private static ItemData lanternItem;

    // Shared inventory reference — avoids a FindAnyObjectByType per instance
    private static Inventory sharedInventory;

    private Inventory inventory;

    void Awake()
    {
        if (string.IsNullOrEmpty(promptText) || promptText == "Interact")
            promptText = "Pick up Lantern";

        // Build the lantern ItemData once for the whole session
        if (lanternItem == null)
        {
            lanternItem             = ScriptableObject.CreateInstance<ItemData>();
            lanternItem.name        = "Lantern";
            lanternItem.itemName    = "Lantern";
            lanternItem.description = "An old oil lantern found in the cabin. "
                                    + "Its warm glow might be useful in the dark.";
            lanternItem.category    = ItemCategory.QuestItem;
            lanternItem.isStackable = false;
            lanternItem.maxStack    = 1;
            lanternItem.equipSlot   = EquipSlot.Tool;
            ItemRegistry.Register(lanternItem);
        }
    }

    void Start()
    {
        if (sharedInventory == null)
            sharedInventory = FindAnyObjectByType<Inventory>();
        inventory = sharedInventory;
    }

    public override void OnInteract()
    {
        if (inventory == null)
        {
            Debug.LogWarning("PickupLantern: No Inventory found in scene.");
            return;
        }

        if (inventory.AddItem(lanternItem, 1))
        {
            SFXManager.PlayPickup();
            Destroy(gameObject);
        }
        else
        {
            Debug.Log("Inventory full — cannot pick up Lantern.");
        }
    }
}
