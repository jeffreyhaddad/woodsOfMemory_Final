using System.Collections;
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
    private Animator playerAnimator;

    // Shared across all pickup instances — FindAnyObjectByType is only called once
    // regardless of how many pickups exist in the scene.
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

        GameObject player = GameObject.FindWithTag("Player");
        if (player != null)
            playerAnimator = player.GetComponentInChildren<Animator>();
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

        PlayerMovement.inputBlocked = true;
        playerAnimator?.SetTrigger("PickupItem");
        StartCoroutine(FinishPickup());
    }

    private IEnumerator FinishPickup()
    {
        // Match this value to your pickup animation clip length
        yield return new WaitForSeconds(1.5f);
        PlayerMovement.inputBlocked = false;
        Debug.Log("Picked up " + quantity + "x " + itemData.itemName);
        SFXManager.PlayPickup();
        Destroy(gameObject);
    }
}
