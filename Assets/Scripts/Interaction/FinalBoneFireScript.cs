using System.Collections;
using UnityEngine;

/// <summary>
/// Placed bonfire in the scene. Three-stage cooking like a Minecraft furnace:
///   1. Unlit  → [E] with Wood + Stone lights the fire.
///   2. Idle   → [E] with Venison starts cooking; ring drains over cookDuration.
///   3. Ready  → cooked venison spawns as a pickup near the fire.
///               It shifts golden → red → charred over burnDuration seconds.
///               Player must interact with the pickup to collect it.
///               Walk away mid-cook cancels and refunds the raw venison.
/// </summary>
public class FinalBoneFireScript : Interactable
{
    [Header("Required to Light")]
    public int requiredWood = 3;
    public int requiredStone = 2;

    [Header("Cooking")]
    [Tooltip("Seconds to cook one piece of Venison")]
    public float cookDuration = 6f;
    [Tooltip("Seconds the cooked pickup stays before burning away")]
    public float burnDuration = 30f;

    [Header("Burn Damage")]
    [Tooltip("Radius around the fire center that deals burn damage")]
    public float burnDamageRadius = 1.2f;
    [Tooltip("Health lost per hit while standing in the fire")]
    public float burnDamagePerHit = 8f;
    [Tooltip("Seconds between burn damage ticks (after the first immediate hit)")]
    public float burnDamageInterval = 1.5f;

    public bool isLit { get; private set; }

    private enum CookState { Idle, Cooking, Ready }
    private CookState cookState = CookState.Idle;
    private float cookTimer;
    private GameObject readyPickup; // the spawned cooked meat GO

    private Inventory inventory;
    private static Inventory sharedInventory;

    private ParticleSystem fireEffect;
    private ParticleSystem smokeEffect;

    private ItemData woodItem;
    private ItemData stoneItem;
    private ItemData venisonItem;
    private ItemData cookedVenisonItem;

    private PlayerVitals playerVitals;
    private bool wasPlayerInFire;
    private float burnTimer;

    void Start()
    {
        if (sharedInventory == null)
            sharedInventory = FindAnyObjectByType<Inventory>();
        inventory = sharedInventory;

        Transform fireT = transform.Find("FireEffect");
        Transform smokeT = transform.Find("SmokeEffect");
        if (fireT  != null) fireEffect  = fireT.GetComponent<ParticleSystem>();
        if (smokeT != null) smokeEffect = smokeT.GetComponent<ParticleSystem>();

        woodItem        = ItemRegistry.Get("Wood")  ?? MakeStub("Wood",  ItemCategory.Resource);
        stoneItem       = ItemRegistry.Get("Stone") ?? MakeStub("Stone", ItemCategory.Resource);
        venisonItem     = GetOrMakeVenison();
        cookedVenisonItem = GetOrMakeCookedVenison();
        playerVitals    = FindAnyObjectByType<PlayerVitals>();

        UpdatePrompt();
    }

    void Update()
    {
        if (cookState == CookState.Cooking)
        {
            cookTimer += Time.deltaTime;
            if (cookTimer >= cookDuration)
                FinishCooking();
        }
        else if (cookState == CookState.Ready && readyPickup == null)
        {
            // Pickup was collected or burned — return to idle automatically
            cookState = CookState.Idle;
            UpdatePrompt();
        }

        if (isLit)
            UpdateBurnZone();
    }

    // ─── Interactable overrides ───────────────────────────────

    public override void OnFocus() => UpdatePrompt();

    public override void OnLoseFocus()
    {
        if (cookState == CookState.Cooking)
            CancelCooking();
    }

    public override void OnInteract()
    {
        if (!isLit)
            TryLightFire();
        else if (cookState == CookState.Cooking)
            CancelCooking();
        else if (cookState == CookState.Ready)
        {
            // Delegate directly to the pickup so the player doesn't have to hunt for it
            if (readyPickup != null)
            {
                PickupItem pickup = readyPickup.GetComponent<PickupItem>();
                if (pickup != null) pickup.OnInteract();
            }
        }
        else
            TryCookVenison();
    }

    // ─── Fire lighting ────────────────────────────────────────

    void TryLightFire()
    {
        if (inventory == null) return;

        if (!inventory.HasItem(woodItem, requiredWood) || !inventory.HasItem(stoneItem, requiredStone))
        {
            Debug.Log($"[Bonfire] Need {requiredWood} Wood and {requiredStone} Stone.");
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
            MissionManager.Instance.ReportLocationReached("campfire");
    }

    // ─── Cooking ──────────────────────────────────────────────

    void TryCookVenison()
    {
        if (inventory == null) return;
        if (venisonItem == null) venisonItem = GetOrMakeVenison();

        if (!inventory.HasItem(venisonItem))
        {
            Debug.Log("[Bonfire] No Venison in inventory to cook.");
            return;
        }

        // Consume the raw venison — it's going on the fire
        inventory.RemoveItem(venisonItem);

        cookState = CookState.Cooking;
        cookTimer = 0f;

        if (CookingProgressUI.Instance != null)
            CookingProgressUI.Instance.StartCooking(cookDuration);

        UpdatePrompt();
    }

    void FinishCooking()
    {
        cookState = CookState.Ready;

        if (CookingProgressUI.Instance != null)
            CookingProgressUI.Instance.StopCooking();

        readyPickup = SpawnCookedVenisonPickup();
        SFXManager.PlayCraft();
        Debug.Log("[Bonfire] Venison is cooked — pick it up before it burns!");
        UpdatePrompt();
    }

    void CancelCooking()
    {
        cookState = CookState.Idle;
        cookTimer = 0f;

        if (CookingProgressUI.Instance != null)
            CookingProgressUI.Instance.StopCooking();

        // Refund raw venison
        if (venisonItem != null && inventory != null)
            inventory.AddItem(venisonItem);

        Debug.Log("[Bonfire] Cooking cancelled — Venison returned.");
        UpdatePrompt();
    }

    // ─── Pickup spawn ─────────────────────────────────────────

    GameObject SpawnCookedVenisonPickup()
    {
        // Place it slightly in front of the fire so it's clearly visible
        Vector3 spawnPos = transform.position
                         + transform.forward * 0.85f
                         + Vector3.up * 0.55f;

        GameObject obj = new GameObject("CookedVenison_Pickup");
        obj.transform.position = spawnPos;

        SphereCollider col = obj.AddComponent<SphereCollider>();
        col.radius = 0.4f;

        PickupItem pickup = obj.AddComponent<PickupItem>();
        pickup.itemData   = cookedVenisonItem;
        pickup.quantity   = 1;
        pickup.promptText = "Pick up Cooked Venison";

        BurnablePickup burnable = obj.AddComponent<BurnablePickup>();
        burnable.burnDuration = burnDuration;

        // Visual — slightly flattened sphere resembling a piece of meat
        GameObject visual = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        visual.transform.SetParent(obj.transform, false);
        visual.transform.localScale = new Vector3(0.30f, 0.18f, 0.26f);
        Destroy(visual.GetComponent<Collider>());

        obj.AddComponent<PickupBob>();

        return obj;
    }

    // ─── Prompt ───────────────────────────────────────────────

    void UpdatePrompt()
    {
        if (!isLit)
            promptText = $"Light Bonfire ({requiredWood} Wood, {requiredStone} Stone)";
        else if (cookState == CookState.Cooking)
            promptText = "Cooking... [E] to cancel";
        else if (cookState == CookState.Ready)
            promptText = "Venison ready — pick it up nearby!";
        else if (inventory != null && venisonItem != null && inventory.HasItem(venisonItem))
            promptText = "Cook Venison";
        else
            promptText = "Bonfire (burning)";
    }

    // ─── Burn damage ──────────────────────────────────────────

    void UpdateBurnZone()
    {
        if (playerVitals == null) return;

        float dist = Vector3.Distance(transform.position, playerVitals.transform.position);
        bool inFire = dist <= burnDamageRadius;

        if (inFire)
        {
            burnTimer += Time.deltaTime;

            // Immediate hit on first contact, then every interval
            if (!wasPlayerInFire || burnTimer >= burnDamageInterval)
            {
                burnTimer = 0f;
                playerVitals.TakeDamage(burnDamagePerHit);
                SFXManager.PlayBurn();
                CameraFollow.Shake(0.35f, 0.09f);
            }
        }
        else
        {
            burnTimer = 0f;
        }

        wasPlayerInFire = inFire;
    }

    // ─── Item helpers ─────────────────────────────────────────

    static ItemData GetOrMakeVenison()
    {
        ItemData item = ItemRegistry.Get("Venison");
        if (item != null) return item;

        item = ScriptableObject.CreateInstance<ItemData>();
        item.name = item.itemName = "Venison";
        item.description  = "Raw venison from a deer. Cook it over a fire for better nutrition.";
        item.category     = ItemCategory.Food;
        item.isStackable  = true;
        item.maxStack     = 10;
        item.useAction    = ItemUseAction.None; // must be cooked first
        item.useValue     = 0f;
        ItemRegistry.Register(item);
        return item;
    }

    static ItemData GetOrMakeCookedVenison()
    {
        ItemData item = ItemRegistry.Get("Cooked Venison");
        if (item != null) return item;

        item = ScriptableObject.CreateInstance<ItemData>();
        item.name = item.itemName = "Cooked Venison";
        item.description  = "Venison cooked over an open flame. Hearty and restorative.";
        item.category     = ItemCategory.Food;
        item.isStackable  = true;
        item.maxStack     = 10;
        item.useAction    = ItemUseAction.EatFood;
        item.useValue     = 20f;
        ItemRegistry.Register(item);
        return item;
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
