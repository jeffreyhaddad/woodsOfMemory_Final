using UnityEngine;

/// <summary>
/// Campfire interaction: player needs 4 Wood and 3 Stone, presses E to light the fire.
/// Auto-finds the child objects "Fire Effect", "SmokeEffect", and "FireLight" by name.
/// Reports "campfire" location to MissionManager for mission objectives.
/// </summary>
public class CampfireInteractable : Interactable
{
    [Header("Required Items")]
    public int requiredWood = 4;
    public int requiredStone = 3;

    private bool isLit = false;
    private Inventory inventory;

    private GameObject fireEffect;
    private GameObject smokeEffect;
    private GameObject fireLight;

    void Awake()
    {
        promptText = "Light Campfire (" + requiredWood + " Wood, " + requiredStone + " Stone)";

        // Find children by name
        fireEffect = transform.Find("Fire Effect")?.gameObject;
        smokeEffect = transform.Find("SmokeEffect")?.gameObject;
        fireLight = transform.Find("FireLight")?.gameObject;

        // Make sure all effects are off at start
        if (fireEffect != null) fireEffect.SetActive(false);
        if (smokeEffect != null) smokeEffect.SetActive(false);
        if (fireLight != null) fireLight.SetActive(false);
    }

    public override void OnInteract()
    {
        if (isLit)
            return;

        if (inventory == null)
            inventory = FindAnyObjectByType<Inventory>();

        if (inventory == null)
        {
            Debug.LogWarning("CampfireInteractable: No Inventory found in scene!");
            return;
        }

        ItemData wood = ItemRegistry.Get("Wood");
        ItemData stone = ItemRegistry.Get("Stone");

        if (wood == null || stone == null)
        {
            Debug.LogWarning("CampfireInteractable: Wood or Stone item not found in ItemRegistry!");
            return;
        }

        if (!inventory.HasItem(wood, requiredWood) || !inventory.HasItem(stone, requiredStone))
        {
            Debug.Log("Not enough materials! Need " + requiredWood + " Wood and " + requiredStone + " Stone.");
            return;
        }

        // Consume items
        inventory.RemoveItem(wood, requiredWood);
        inventory.RemoveItem(stone, requiredStone);

        // Light the fire
        LightFire();

        // Play craft sound
        SFXManager.PlayCraft();

        // Report to mission system
        if (MissionManager.Instance != null)
            MissionManager.Instance.ReportLocationReached("campfire");

        Debug.Log("Campfire lit!");
    }

    public override void OnFocus()
    {
        if (isLit)
            promptText = "Campfire (burning)";
        else
            promptText = "Light Campfire (" + requiredWood + " Wood, " + requiredStone + " Stone)";
    }

    void LightFire()
    {
        isLit = true;

        if (fireEffect != null) fireEffect.SetActive(true);
        if (smokeEffect != null) smokeEffect.SetActive(true);
        if (fireLight != null) fireLight.SetActive(true);
    }
}
