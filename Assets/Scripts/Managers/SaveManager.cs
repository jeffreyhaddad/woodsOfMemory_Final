using System.IO;
using UnityEngine;

public class SaveManager : MonoBehaviour
{
    public static SaveManager Instance { get; private set; }

    [Header("Auto-Save")]
    public float autoSaveIntervalMinutes = 5f;

    private float autoSaveTimer;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    void Start()
    {
        autoSaveTimer = autoSaveIntervalMinutes * 60f;
    }

    void Update()
    {
        if (GameManager.Instance != null && GameManager.Instance.CurrentState == GameState.Playing)
        {
            autoSaveTimer -= Time.deltaTime;
            if (autoSaveTimer <= 0f)
            {
                Save(0); // Auto-save to slot 0
                autoSaveTimer = autoSaveIntervalMinutes * 60f;
                Debug.Log("Auto-saved to slot 0.");
            }
        }
    }

    public void Save(int slot)
    {
        SaveData data = new SaveData();

        // Player position
        PlayerVitals vitals = GameManager.Instance != null ? GameManager.Instance.PlayerVitals : FindAnyObjectByType<PlayerVitals>();
        if (vitals != null)
        {
            Transform pt = vitals.transform;
            data.playerX = pt.position.x;
            data.playerY = pt.position.y;
            data.playerZ = pt.position.z;
            data.playerRotY = pt.eulerAngles.y;

            data.health = vitals.Health;
            data.hunger = vitals.Hunger;
            data.stamina = vitals.Stamina;
        }

        // Inventory
        Inventory inventory = GameManager.Instance != null ? GameManager.Instance.Inventory : FindAnyObjectByType<Inventory>();
        if (inventory != null)
        {
            for (int i = 0; i < inventory.slots.Length; i++)
            {
                if (!inventory.slots[i].IsEmpty)
                {
                    data.inventoryItems.Add(new SavedItem
                    {
                        itemName = inventory.slots[i].item.itemName,
                        quantity = inventory.slots[i].quantity,
                        slotIndex = i
                    });
                }
            }
        }

        // Missions
        MissionManager mm = MissionManager.Instance;
        if (mm != null)
        {
            data.currentMissionIndex = mm.CurrentMissionIndex;

            if (mm.CurrentMission != null)
            {
                for (int i = 0; i < mm.CurrentMission.objectives.Length; i++)
                    data.objectiveProgress.Add(mm.CurrentMission.objectives[i].currentCount);
            }
        }

        // Day/Night
        DayNightCycle dayNight = GameManager.Instance != null ? GameManager.Instance.DayNight : FindAnyObjectByType<DayNightCycle>();
        if (dayNight != null)
            data.timeOfDay = dayNight.TimeOfDay;

        // Journal
        JournalManager journal = JournalManager.Instance;
        if (journal != null)
        {
            for (int i = 0; i < journal.DiscoveredEntries.Count; i++)
            {
                if (journal.DiscoveredEntries[i] != null)
                    data.discoveredEntryTitles.Add(journal.DiscoveredEntries[i].title);
            }
        }

        // Equipment
        EquipmentManager equip = EquipmentManager.Instance;
        if (equip != null)
        {
            data.equippedWeaponName = equip.EquippedWeapon != null ? equip.EquippedWeapon.itemName : "";
            data.equippedToolName = equip.EquippedTool != null ? equip.EquippedTool.itemName : "";
            data.equippedArmorName = equip.EquippedArmor != null ? equip.EquippedArmor.itemName : "";
        }

        data.saveDate = System.DateTime.Now.ToString("yyyy-MM-dd HH:mm");

        // Write to file
        string json = JsonUtility.ToJson(data, true);
        string path = GetSavePath(slot);
        File.WriteAllText(path, json);
        Debug.Log("Saved to slot " + slot + " at " + path);
    }

    public bool Load(int slot)
    {
        string path = GetSavePath(slot);
        if (!File.Exists(path))
        {
            Debug.LogWarning("No save file found at slot " + slot);
            return false;
        }

        string json = File.ReadAllText(path);
        SaveData data = JsonUtility.FromJson<SaveData>(json);

        // Player position
        PlayerVitals vitals = GameManager.Instance != null ? GameManager.Instance.PlayerVitals : FindAnyObjectByType<PlayerVitals>();
        if (vitals != null)
        {
            CharacterController cc = vitals.GetComponent<CharacterController>();
            if (cc != null) cc.enabled = false;

            vitals.transform.position = new Vector3(data.playerX, data.playerY, data.playerZ);
            vitals.transform.eulerAngles = new Vector3(0, data.playerRotY, 0);

            if (cc != null) cc.enabled = true;

            vitals.Health = data.health;
            vitals.Hunger = data.hunger;
            vitals.Stamina = data.stamina;
            vitals.enabled = true;
        }

        // Inventory - clear and restore using ItemRegistry
        Inventory inventory = GameManager.Instance != null ? GameManager.Instance.Inventory : FindAnyObjectByType<Inventory>();
        if (inventory != null)
        {
            for (int i = 0; i < inventory.slots.Length; i++)
                inventory.slots[i].Clear();

            int restored = 0;
            for (int i = 0; i < data.inventoryItems.Count; i++)
            {
                SavedItem saved = data.inventoryItems[i];
                ItemData item = ItemRegistry.Get(saved.itemName);
                if (item != null && saved.slotIndex >= 0 && saved.slotIndex < inventory.slots.Length)
                {
                    inventory.slots[saved.slotIndex].item = item;
                    inventory.slots[saved.slotIndex].quantity = saved.quantity;
                    restored++;
                }
            }

            inventory.NotifyChanged();
            Debug.Log("Restored " + restored + "/" + data.inventoryItems.Count + " inventory items.");
        }

        // Missions - restore progress
        MissionManager mm = MissionManager.Instance;
        if (mm != null && mm.missions != null)
        {
            // Advance to the saved mission
            for (int i = 0; i < data.currentMissionIndex && i < mm.missions.Length; i++)
            {
                mm.missions[i].isCompleted = true;
                mm.missions[i].isActive = false;
            }

            mm.CurrentMissionIndex = data.currentMissionIndex;

            if (data.currentMissionIndex < mm.missions.Length)
            {
                Mission current = mm.missions[data.currentMissionIndex];
                current.isActive = true;
                current.isCompleted = false;

                for (int i = 0; i < current.objectives.Length && i < data.objectiveProgress.Count; i++)
                    current.objectives[i].currentCount = data.objectiveProgress[i];
            }
        }

        // Journal - restore discovered entries
        JournalManager journal = JournalManager.Instance;
        if (journal != null && journal.startingEntries != null)
        {
            for (int i = 0; i < data.discoveredEntryTitles.Count; i++)
            {
                string title = data.discoveredEntryTitles[i];
                for (int j = 0; j < journal.startingEntries.Length; j++)
                {
                    if (journal.startingEntries[j] != null && journal.startingEntries[j].title == title)
                    {
                        journal.AddEntry(journal.startingEntries[j]);
                        break;
                    }
                }
            }
        }

        // Equipment - restore equipped items
        EquipmentManager equip = EquipmentManager.Instance;
        if (equip != null)
        {
            equip.UnequipAll();
            if (!string.IsNullOrEmpty(data.equippedWeaponName))
            {
                ItemData weapon = ItemRegistry.Get(data.equippedWeaponName);
                if (weapon != null) equip.Equip(weapon);
            }
            if (!string.IsNullOrEmpty(data.equippedToolName))
            {
                ItemData tool = ItemRegistry.Get(data.equippedToolName);
                if (tool != null) equip.Equip(tool);
            }
            if (!string.IsNullOrEmpty(data.equippedArmorName))
            {
                ItemData armor = ItemRegistry.Get(data.equippedArmorName);
                if (armor != null) equip.Equip(armor);
            }
        }

        // Day/Night
        DayNightCycle dayNight = GameManager.Instance != null ? GameManager.Instance.DayNight : FindAnyObjectByType<DayNightCycle>();
        if (dayNight != null)
            dayNight.TimeOfDay = data.timeOfDay;

        // Resume play
        if (GameManager.Instance != null)
            GameManager.Instance.SetState(GameState.Playing);

        Debug.Log("Loaded save from slot " + slot + " (saved " + data.saveDate + ")");
        return true;
    }

    public bool HasSave(int slot)
    {
        return File.Exists(GetSavePath(slot));
    }

    public string GetSaveInfo(int slot)
    {
        string path = GetSavePath(slot);
        if (!File.Exists(path))
            return "Empty";

        string json = File.ReadAllText(path);
        SaveData data = JsonUtility.FromJson<SaveData>(json);
        return "Saved: " + data.saveDate;
    }

    string GetSavePath(int slot)
    {
        return Path.Combine(Application.persistentDataPath, "save_slot_" + slot + ".json");
    }
}
