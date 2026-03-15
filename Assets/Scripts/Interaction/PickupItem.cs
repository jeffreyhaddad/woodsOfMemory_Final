using UnityEngine;

/// <summary>
/// Interactable that adds an item to the player's inventory when picked up.
/// </summary>
public class PickupItem : Interactable
{
    [Tooltip("The item data asset for this pickup")]
    public ItemData itemData;

    [Tooltip("How many of this item to give")]
    public int quantity = 1;

    private Inventory inventory;

    // Shared across all pickup instances — FindAnyObjectByType is only called once.
    private static Inventory sharedInventory;

    void Awake()
    {
        if (itemData != null && (string.IsNullOrEmpty(promptText) || promptText == "Interact"))
            promptText = "Pick up " + itemData.itemName;
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
            Debug.LogWarning("No Inventory found in scene!");
            return;
        }

        if (itemData == null)
        {
            Debug.LogWarning("PickupItem has no ItemData assigned: " + gameObject.name);
            return;
        }

        if (!inventory.AddItem(itemData, quantity))
        {
            Debug.Log("Inventory full!");
            return;
        }

        SFXManager.PlayPickup();
        Destroy(gameObject);
    }
}
