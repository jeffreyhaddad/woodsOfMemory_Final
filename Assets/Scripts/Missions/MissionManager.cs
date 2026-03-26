using System;
using System.Collections.Generic;
using UnityEngine;

public class MissionManager : MonoBehaviour
{
    public static MissionManager Instance { get; private set; }

    [Header("Missions (in order)")]
    [Tooltip("Leave empty to auto-generate the 6 story missions.")]
    public Mission[] missions;

    [Header("Debug")]
    [Tooltip("Skip to this mission index on start (0 = normal, 4 = Into the Dark / dark clearing).")]
    public int debugStartMissionIndex = 0;

    public int CurrentMissionIndex { get; set; } = 0;
    public Mission CurrentMission => (CurrentMissionIndex < missions.Length) ? missions[CurrentMissionIndex] : null;
    public bool AllMissionsComplete => CurrentMissionIndex >= missions.Length;

    public event Action<Mission> OnMissionStarted;
    public event Action<MissionObjective> OnObjectiveProgress;
    public event Action<Mission> OnMissionCompleted;
    public event Action OnAllMissionsCompleted;

    private Inventory inventory;
    private DayNightCycle dayNight;
    private bool wasNight;
    private bool survivedNightTracking;

    // Track items player has ever had (for collect objectives that count cumulative pickups)
    private Dictionary<string, int> itemPickupCounts = new Dictionary<string, int>();
    private Dictionary<string, int> craftCounts = new Dictionary<string, int>();
    private Dictionary<string, int> killCounts = new Dictionary<string, int>();

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
        inventory = FindAnyObjectByType<Inventory>();
        dayNight = FindAnyObjectByType<DayNightCycle>();

        // Auto-generate missions if none assigned
        bool hasValidMissions = false;
        if (missions != null)
        {
            for (int i = 0; i < missions.Length; i++)
            {
                if (missions[i] != null) { hasValidMissions = true; break; }
            }
        }

        if (!hasValidMissions)
            CreateDefaultMissions();

        // Subscribe to events
        if (inventory != null)
            inventory.OnInventoryChanged += OnInventoryChanged;

        // Start first mission — deferred if IntroSequenceManager is present
        if (FindAnyObjectByType<IntroSequenceManager>() == null)
            StartMission(debugStartMissionIndex);
    }

    /// <summary>Called by IntroSequenceManager after the intro completes.</summary>
    public void BeginMissions()
    {
        StartMission(debugStartMissionIndex);
    }

    void OnDestroy()
    {
        if (inventory != null)
            inventory.OnInventoryChanged -= OnInventoryChanged;
    }

    void Update()
    {
        if (AllMissionsComplete) return;

        Mission mission = CurrentMission;
        if (mission == null || !mission.isActive) return;

        // Track survive-night objectives
        if (dayNight != null)
        {
            bool isNight = dayNight.IsNight;

            // Night just started
            if (isNight && !wasNight)
                survivedNightTracking = true;

            // Night just ended — survived!
            if (!isNight && wasNight && survivedNightTracking)
            {
                for (int i = 0; i < mission.objectives.Length; i++)
                {
                    MissionObjective obj = mission.objectives[i];
                    if (obj.objectiveType == ObjectiveType.SurviveNight && !obj.IsCompleted)
                    {
                        obj.currentCount++;
                        OnObjectiveProgress?.Invoke(obj);
                        CheckMissionComplete();
                    }
                }
                survivedNightTracking = false;
            }

            wasNight = isNight;
        }
    }

    void StartMission(int index)
    {
        if (index >= missions.Length) return;

        CurrentMissionIndex = index;
        Mission mission = missions[index];
        mission.isActive = true;
        mission.isCompleted = false;

        // Reset objectives
        for (int i = 0; i < mission.objectives.Length; i++)
            mission.objectives[i].Reset();

        // Pre-fill collect objectives with items already in inventory
        for (int i = 0; i < mission.objectives.Length; i++)
        {
            MissionObjective obj = mission.objectives[i];
            if (obj.objectiveType == ObjectiveType.CollectItem && inventory != null)
            {
                int count = CountItemInInventory(obj.targetItemName);
                obj.currentCount = Mathf.Min(count, obj.targetCount);
            }
        }

        OnMissionStarted?.Invoke(mission);
        Debug.Log("Mission Started: " + mission.missionName);
    }

    void OnInventoryChanged()
    {
        if (AllMissionsComplete) return;

        Mission mission = CurrentMission;
        if (mission == null || !mission.isActive) return;

        for (int i = 0; i < mission.objectives.Length; i++)
        {
            MissionObjective obj = mission.objectives[i];
            if (obj.IsCompleted) continue;

            if (obj.objectiveType == ObjectiveType.CollectItem)
            {
                int count = CountItemInInventory(obj.targetItemName);
                if (count != obj.currentCount)
                {
                    obj.currentCount = Mathf.Min(count, obj.targetCount);
                    OnObjectiveProgress?.Invoke(obj);
                }
            }
            else if (obj.objectiveType == ObjectiveType.CraftItem)
            {
                int count = CountItemInInventory(obj.targetItemName);
                if (count != obj.currentCount)
                {
                    obj.currentCount = Mathf.Min(count, obj.targetCount);
                    OnObjectiveProgress?.Invoke(obj);
                }
            }
        }

        CheckMissionComplete();
    }

    /// <summary>Call this from CreatureAI or combat system when a creature dies.</summary>
    public void ReportCreatureKill(string creatureName)
    {
        if (AllMissionsComplete) return;

        Mission mission = CurrentMission;
        if (mission == null || !mission.isActive) return;

        for (int i = 0; i < mission.objectives.Length; i++)
        {
            MissionObjective obj = mission.objectives[i];
            if (obj.IsCompleted) continue;

            if (obj.objectiveType == ObjectiveType.KillCreature &&
                obj.targetCreatureName.Equals(creatureName, StringComparison.OrdinalIgnoreCase))
            {
                obj.currentCount++;
                OnObjectiveProgress?.Invoke(obj);
            }
        }

        CheckMissionComplete();
    }

    /// <summary>Call this from trigger zones for ReachLocation objectives.</summary>
    public void ReportLocationReached(string locationName)
    {
        if (AllMissionsComplete) return;

        Mission mission = CurrentMission;
        if (mission == null || !mission.isActive) return;

        for (int i = 0; i < mission.objectives.Length; i++)
        {
            MissionObjective obj = mission.objectives[i];
            if (obj.IsCompleted) continue;

            string normalizedLocation = locationName.Replace('_', ' ');
            if (obj.objectiveType == ObjectiveType.ReachLocation &&
                obj.description.IndexOf(normalizedLocation, StringComparison.OrdinalIgnoreCase) >= 0)
            {
                obj.currentCount++;
                OnObjectiveProgress?.Invoke(obj);
            }
        }

        CheckMissionComplete();
    }

    void CheckMissionComplete()
    {
        Mission mission = CurrentMission;
        if (mission == null || !mission.isActive) return;

        if (mission.AreAllObjectivesComplete())
        {
            mission.isCompleted = true;
            mission.isActive = false;
            OnMissionCompleted?.Invoke(mission);
            Debug.Log("Mission Complete: " + mission.missionName);

            int next = CurrentMissionIndex + 1;
            if (next < missions.Length)
                StartMission(next);
            else
            {
                CurrentMissionIndex = missions.Length;
                OnAllMissionsCompleted?.Invoke();
                Debug.Log("All missions completed!");
            }
        }
    }

    int CountItemInInventory(string itemName)
    {
        if (inventory == null) return 0;

        int total = 0;
        for (int i = 0; i < inventory.slots.Length; i++)
        {
            if (!inventory.slots[i].IsEmpty && inventory.slots[i].item.itemName == itemName)
                total += inventory.slots[i].quantity;
        }
        return total;
    }

    // ─── Default Mission Generation ──────────────────────────

    void CreateDefaultMissions()
    {
        List<Mission> list = new List<Mission>();

        // Mission 1: Survival Basics
        list.Add(MakeMission("Survival Basics",
            "Gather basic resources, light the campfire, and hunt for food to survive your first night.",
            new MissionObjective[]
            {
                MakeObjective("Collect Wood",         ObjectiveType.CollectItem,   "Wood",  "",     5),
                MakeObjective("Collect Stone",        ObjectiveType.CollectItem,   "Stone", "",     3),
                MakeObjective("Light a campfire",     ObjectiveType.ReachLocation, "campfire", "", 1),
                MakeObjective("Hunt a Deer",          ObjectiveType.KillCreature,  "",      "Deer", 1),
            }));

        // Mission 2: Crafting Tools
        list.Add(MakeMission("Crafting Tools",
            "Create essential equipment for survival.",
            new MissionObjective[]
            {
                MakeObjective("Craft a Stone Axe", ObjectiveType.CraftItem, "Stone Axe", "", 1),
                MakeObjective("Craft a Torch",     ObjectiveType.CraftItem, "Torch",     "", 1),
            }));

        // Mission 3: Exploration
        list.Add(MakeMission("Exploration",
            "Someone else has been in these woods. Find the second cabin and discover what they left behind.",
            new MissionObjective[]
            {
                MakeObjective("Collect the key inside the wooden box", ObjectiveType.CollectItem,   "Rusted Key",       "", 1),
                MakeObjective("Open the door of the cabin area",         ObjectiveType.ReachLocation, "cabin_area",       "", 1),
                MakeObjective("Collect water from the well",           ObjectiveType.CollectItem,   "Water from well",  "", 1),
            }));

        // Mission 4: Shadow Harvest
        list.Add(MakeMission("Shadow Harvest",
            "Dark creatures stalk these woods at night. Hunt down five of them and collect their essence.",
            new MissionObjective[]
            {
                MakeObjective("Kill shadow creatures",        ObjectiveType.KillCreature, "",               "Shadow Creature", 5),
                MakeObjective("Collect Shadow Essence",       ObjectiveType.CollectItem,  "Shadow Essence", "",                5),
            }));

        // Mission 5: Into the Dark
        list.Add(MakeMission("Into the Dark",
            "The notes speak of a place where the shadows gather. The essence you collected won't protect you — but shaped into a ward, it might slow them down. Find the clearing. Survive the night.",
            new MissionObjective[]
            {
                MakeObjective("Craft a Shadow Ward",          ObjectiveType.CraftItem,    "Shadow Ward",   "",                1),
                MakeObjective("Find the Dark Clearing",       ObjectiveType.ReachLocation, "dark_clearing", "",               1),
                MakeObjective("Survive the shadow assault",   ObjectiveType.KillCreature,  "",             "Shadow Creature", 8),
            }));

        // Mission 6: The Escape
        list.Add(MakeMission("The Escape",
            "The notes warned you — something keeps pulling people back. You can feel it. Find the exit and fight through whatever comes.",
            new MissionObjective[]
            {
                MakeObjective("Reach the forest exit",      ObjectiveType.ReachLocation, "",               "",                1),
                MakeObjective("Survive the final assault",  ObjectiveType.KillCreature,  "",               "Shadow Creature", 6),
            }));

        missions = list.ToArray();
        Debug.Log("MissionManager: Generated " + missions.Length + " default missions.");
    }

    Mission MakeMission(string name, string desc, MissionObjective[] objectives)
    {
        Mission m = ScriptableObject.CreateInstance<Mission>();
        m.name = name;
        m.missionName = name;
        m.description = desc;
        m.objectives = objectives;
        return m;
    }

    MissionObjective MakeObjective(string desc, ObjectiveType type, string itemName, string creatureName, int count)
    {
        return new MissionObjective
        {
            description = desc,
            objectiveType = type,
            targetItemName = itemName,
            targetCreatureName = creatureName,
            targetCount = count,
            currentCount = 0
        };
    }
}
