using UnityEngine;

public class WaterWellInteraction : Interactable
{
    private static Inventory sharedInventory;
    public ItemData water;
    public override void OnInteract()
    {
        Debug.LogWarning("Here Inside the OnInteract Script.");
        if (sharedInventory == null)
        {
            Debug.LogWarning("Water Well: No Inventory found in scene.");
            return;
        }
        if(water == null){
            Debug.LogWarning("Water is null");
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
    private void Start()
    {
        sharedInventory = FindAnyObjectByType<Inventory>();
        promptText = "Fill Water";
    }


}
