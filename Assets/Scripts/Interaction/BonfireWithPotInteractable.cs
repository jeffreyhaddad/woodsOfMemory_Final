using System.Collections;
using UnityEngine;

public class BonfireWithPotInteractable : Interactable
{
    [Header("Required to Light")]
    public int requiredWood = 3;
    public int requiredStone = 2;

    [Header("Cooking")]
    public float cookDuration = 10f;

    public bool isLit { get; private set; }

    private enum PotState { Unlit, Lit, HasWater, Cooking }
    private PotState potState = PotState.Unlit;

    private Inventory inventory;
    private static Inventory sharedInventory;

    private ParticleSystem fireEffect;
    private ParticleSystem smokeEffect;

    private ItemData woodItem;
    private ItemData stoneItem;
    private ItemData waterItem;
    private ItemData potWaterItem;
    private ItemData venisonItem;
    private ItemData stewVenisonItem;
    private ItemData venisonStewItem;

    private float cookTimer;

    void Start()
    {
        if (sharedInventory == null)
            sharedInventory = FindAnyObjectByType<Inventory>();
        inventory = sharedInventory;

        Transform fireT  = transform.Find("FireEffect");
        Transform smokeT = transform.Find("SmokeEffect");
        if (fireT  != null) fireEffect  = fireT.GetComponent<ParticleSystem>();
        if (smokeT != null) smokeEffect = smokeT.GetComponent<ParticleSystem>();

        woodItem        = ItemRegistry.Get("Wood")            ?? MakeStub("Wood",            ItemCategory.Resource);
        stoneItem       = ItemRegistry.Get("Stone")           ?? MakeStub("Stone",           ItemCategory.Resource);
        waterItem       = ItemRegistry.Get("Water from well") ?? MakeStub("Water from well", ItemCategory.Resource);
        potWaterItem    = ItemRegistry.Get("Pot Water")       ?? MakeStub("Pot Water",       ItemCategory.Resource);
        venisonItem     = ItemRegistry.Get("Venison")         ?? MakeStub("Venison",         ItemCategory.Food);
        stewVenisonItem = ItemRegistry.Get("Stew vension")    ?? MakeStub("Stew vension",    ItemCategory.Food);
        venisonStewItem = GetOrMakeVenisonStew();

        UpdatePrompt();
    }

    void Update()
    {
        if (potState == PotState.Cooking)
        {
            cookTimer += Time.deltaTime;
            if (cookTimer >= cookDuration)
                FinishCooking();
        }
    }

    public override void OnFocus() => UpdatePrompt();

    public override void OnInteract()
    {
        switch (potState)
        {
            case PotState.Unlit:    TryLightFire();  break;
            case PotState.Lit:      TryAddWater();   break;
            case PotState.HasWater: TryAddVenison(); break;
        }
    }

    // ─── Light fire ──────────────────────────────────────────

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
        potState = PotState.Lit;

        if (fireEffect  != null) { fireEffect.gameObject.SetActive(true);  fireEffect.Play(); }
        if (smokeEffect != null) { smokeEffect.gameObject.SetActive(true); smokeEffect.Play(); }

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

    // ─── Pour water ──────────────────────────────────────────

    void TryAddWater()
    {
        if (inventory == null) return;

        if (!inventory.HasItem(waterItem))
        {
            Debug.Log("[BonfireWithPot] Need 1x Water from well.");
            return;
        }

        inventory.RemoveItem(waterItem);
        inventory.AddItem(potWaterItem);  // triggers mission objective 3 (Pot Water)

        potState = PotState.HasWater;
        SFXManager.PlayCraft();
        UpdatePrompt();
    }

    // ─── Add venison ─────────────────────────────────────────

    void TryAddVenison()
    {
        if (inventory == null) return;

        if (!inventory.HasItem(venisonItem))
        {
            Debug.Log("[BonfireWithPot] Need 1x Venison.");
            return;
        }

        inventory.RemoveItem(venisonItem);
        inventory.RemoveItem(potWaterItem);

        // Add then remove to trigger mission objective 4 (Stew vension)
        inventory.AddItem(stewVenisonItem);
        inventory.RemoveItem(stewVenisonItem);

        potState = PotState.Cooking;
        cookTimer = 0f;

        SFXManager.PlayCraft();
        UpdatePrompt();
    }

    // ─── Finish cooking ──────────────────────────────────────

    void FinishCooking()
    {
        potState = PotState.Unlit;
        SpawnVenisonStew();
        Debug.Log("[BonfireWithPot] Stew is ready!");
        UpdatePrompt();
    }

    void SpawnVenisonStew()
    {
        Vector3 spawnPos = transform.position + transform.forward * 0.85f + Vector3.up * 0.55f;

        GameObject obj = new GameObject("VenisonStew_Pickup");
        obj.transform.position = spawnPos;

        SphereCollider col = obj.AddComponent<SphereCollider>();
        col.radius = 0.4f;

        PickupItem pickup = obj.AddComponent<PickupItem>();
        pickup.itemData   = venisonStewItem;
        pickup.quantity   = 1;
        pickup.promptText = "Pick up Venison Stew";

        GameObject visual = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        visual.transform.SetParent(obj.transform, false);
        visual.transform.localScale = new Vector3(0.28f, 0.18f, 0.24f);
        Destroy(visual.GetComponent<Collider>());

        obj.AddComponent<PickupBob>();
    }

    // ─── Prompt ──────────────────────────────────────────────

    void UpdatePrompt()
    {
        switch (potState)
        {
            case PotState.Unlit:
                promptText = $"Light Bonfire with Pot ({requiredWood} Wood, {requiredStone} Stone)";
                break;
            case PotState.Lit:
                promptText = "Pour water into the pot (1x Water from well)";
                break;
            case PotState.HasWater:
                promptText = "Add Venison to the stew (1x Venison)";
                break;
            case PotState.Cooking:
                promptText = "Stew is cooking...";
                break;
        }
    }

    // ─── Item helpers ────────────────────────────────────────

    static ItemData GetOrMakeVenisonStew()
    {
        ItemData item = ItemRegistry.Get("Vension Stew");
        if (item != null) return item;

        item = ScriptableObject.CreateInstance<ItemData>();
        item.name        = item.itemName = "Vension Stew";
        item.description = "A hearty venison stew cooked over an open flame.";
        item.category    = ItemCategory.Food;
        item.isStackable = true;
        item.maxStack    = 5;
        item.useAction   = ItemUseAction.EatFood;
        item.useValue    = 30f;
        ItemRegistry.Register(item);
        return item;
    }

    static ItemData MakeStub(string itemName, ItemCategory cat)
    {
        ItemData stub = ScriptableObject.CreateInstance<ItemData>();
        stub.name        = stub.itemName = itemName;
        stub.category    = cat;
        stub.isStackable = true;
        stub.maxStack    = 20;
        return stub;
    }
}
