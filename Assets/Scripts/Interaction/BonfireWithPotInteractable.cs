using System.Collections;
using UnityEngine;

public class BonfireWithPotInteractable : Interactable
{
    [Header("Required to Light")]
    public int requiredWood = 3;
    public int requiredStone = 2;

    public bool isLit { get; private set; }

    private Inventory inventory;
    private static Inventory sharedInventory;

    private ParticleSystem fireEffect;
    private ParticleSystem smokeEffect;

    private ItemData woodItem;
    private ItemData stoneItem;

    void Start()
    {
        if (sharedInventory == null)
            sharedInventory = FindAnyObjectByType<Inventory>();
        inventory = sharedInventory;

        Transform fireT  = transform.Find("FireEffect");
        Transform smokeT = transform.Find("SmokeEffect");
        if (fireT  != null) fireEffect  = fireT.GetComponent<ParticleSystem>();
        if (smokeT != null) smokeEffect = smokeT.GetComponent<ParticleSystem>();

        woodItem  = ItemRegistry.Get("Wood")  ?? MakeStub("Wood",  ItemCategory.Resource);
        stoneItem = ItemRegistry.Get("Stone") ?? MakeStub("Stone", ItemCategory.Resource);

        UpdatePrompt();
    }

    public override void OnFocus() => UpdatePrompt();

    public override void OnInteract()
    {
        if (!isLit)
            TryLightFire();
    }

    void TryLightFire()
    {
        if (inventory == null) return;

        if (!inventory.HasItem(woodItem, requiredWood) || !inventory.HasItem(stoneItem, requiredStone))
        {
            Debug.Log($"[BonfireWithPot] Need {requiredWood} Wood and {requiredStone} Stone.");
            return;
        }

        inventory.RemoveItem(woodItem, requiredWood);
        inventory.RemoveItem(stoneItem, requiredStone);

        isLit = true;
        if (fireEffect  != null) fireEffect.Play();
        if (smokeEffect != null) smokeEffect.Play();

        SFXManager.PlayCraft();
        UpdatePrompt();
        StartCoroutine(ReportFireLit());
    }

    IEnumerator ReportFireLit()
    {
        yield return new WaitForSeconds(1.5f);
        if (MissionManager.Instance != null)
            MissionManager.Instance.ReportLocationReached("boneFire with pot");
    }

    void UpdatePrompt()
    {
        if (!isLit)
            promptText = $"Light Bonfire with Pot ({requiredWood} Wood, {requiredStone} Stone)";
        else
            promptText = "Bonfire (burning)";
    }

    static ItemData MakeStub(string itemName, ItemCategory cat)
    {
        ItemData stub = ScriptableObject.CreateInstance<ItemData>();
        stub.name = stub.itemName = itemName;
        stub.category    = cat;
        stub.isStackable = true;
        stub.maxStack    = 20;
        return stub;
    }
}
