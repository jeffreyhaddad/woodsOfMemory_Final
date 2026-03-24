using UnityEngine;

public class GetKeyFromBoxScript : MonoBehaviour
{
    GameObject woodenBox;
    bool isPlayerNear = false;
    bool hasBeenOpened = false;

    void Start()
    {
        woodenBox = GameObject.FindGameObjectWithTag("WoodenBox");

        if (MissionManager.Instance != null)
        {
            // Subscribe BEFORE any SetActive call — C# delegates fire even on
            // inactive GameObjects, so the subscription survives being hidden.
            MissionManager.Instance.OnMissionStarted += OnMissionStarted;

            if (MissionManager.Instance.CurrentMissionIndex < 2)
            {
                // Hide everything until the designated mission arrives.
                // woodenBox covers the visual mesh and any child pickups (rusted key).
                // Disabling this GameObject kills the trigger collider and all callbacks.
                SetVisible(false);
            }
            else if (MissionManager.Instance.CurrentMissionIndex == 2)
            {
                // Loaded from a mid-game save with mission 2 already active.
                RegisterBoxPOI();
            }
            // Mission already past 2 means box was already found — leave it destroyed.
        }
    }

    void OnDestroy()
    {
        if (MissionManager.Instance != null)
            MissionManager.Instance.OnMissionStarted -= OnMissionStarted;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
            isPlayerNear = true;
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
            isPlayerNear = false;
    }

    private void OnGUI()
    {
        if (isPlayerNear && !hasBeenOpened && MissionManager.Instance.CurrentMissionIndex == 2)
            GUI.Label(new Rect(Screen.width / 2 - 150, Screen.height - 100, 300, 30), "Press E to open the box");
    }

    void Update()
    {
        if (MissionManager.Instance.CurrentMissionIndex == 2 && isPlayerNear && Input.GetKeyDown(KeyCode.E) && !hasBeenOpened)
        {
            hasBeenOpened = true;
            GiveKeyToPlayer();
            CompassUI.RemovePOI("Wooden Box");
            Destroy(woodenBox);
            Destroy(gameObject);
        }
    }

    void GiveKeyToPlayer()
    {
        Inventory inv = FindAnyObjectByType<Inventory>();
        if (inv == null) return;

        // Try to grab ItemData from a PickupItem inside the box
        ItemData keyItem = null;
        if (woodenBox != null)
        {
            PickupItem pickup = woodenBox.GetComponentInChildren<PickupItem>();
            if (pickup != null) keyItem = pickup.itemData;
        }

        // Fallback: registry, then create a minimal one
        if (keyItem == null) keyItem = ItemRegistry.Get("Rusted Key");
        if (keyItem == null)
        {
            keyItem = ScriptableObject.CreateInstance<ItemData>();
            keyItem.itemName = "Rusted Key";
            keyItem.isStackable = false;
            keyItem.maxStack = 1;
            keyItem.category = ItemCategory.QuestItem;
            ItemRegistry.Register(keyItem);
        }

        inv.AddItem(keyItem, 1);
        SFXManager.PlayPickup();
    }

    // ─── Helpers ──────────────────────────────────────────────

    void OnMissionStarted(Mission mission)
    {
        if (MissionManager.Instance.CurrentMissionIndex == 2)
        {
            SetVisible(true);
            RegisterBoxPOI();
        }
    }

    void SetVisible(bool visible)
    {
        if (woodenBox != null) woodenBox.SetActive(visible);
        gameObject.SetActive(visible);
    }

    void RegisterBoxPOI()
    {
        Vector3 pos = woodenBox != null ? woodenBox.transform.position : transform.position;
        CompassUI.RegisterPOI(
            "Wooden Box",
            pos,
            startDiscovered: true,
            discoveryRadius: 20f,
            new Color(0.85f, 0.68f, 0.32f));
    }
}
