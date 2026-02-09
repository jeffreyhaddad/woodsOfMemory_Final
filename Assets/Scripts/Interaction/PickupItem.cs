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

    void Awake()
    {
        if (itemData != null && (string.IsNullOrEmpty(promptText) || promptText == "Interact"))
            promptText = "Pick up " + itemData.itemName;
    }

    public override void OnInteract()
    {
        Inventory inventory = FindAnyObjectByType<Inventory>();

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

        if (inventory.AddItem(itemData, quantity))
        {
            Debug.Log("Picked up " + quantity + "x " + itemData.itemName);
            Destroy(gameObject);
        }
        else
        {
            Debug.Log("Inventory full!");
        }
    }
}
