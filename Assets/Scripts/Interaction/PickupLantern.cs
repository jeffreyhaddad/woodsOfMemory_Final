using UnityEngine;

public class PickupLantern : Interactable
{
    void Awake()
    {
        if (string.IsNullOrEmpty(promptText) || promptText == "Interact")
            promptText = "Pick up Lantern";
    }

    public override void OnInteract()
    {
        Debug.Log("Picked up Lantern");
        SFXManager.PlayPickup();
        Destroy(gameObject);
    }
}
