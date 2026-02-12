using System;
using System.Collections.Generic;
using UnityEngine;

public class MissionManager : MonoBehaviour
{
    public static MissionManager Instance { get; private set; }

    [Header("Missions (in order)")]
    [Tooltip("Leave empty to auto-generate the 6 story missions.")]
    public Mission[] missions;

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

        // Start first mission
        StartMission(0);
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

            if (obj.objectiveType == ObjectiveType.ReachLocation &&
                obj.description.Contains(locationName))
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
            "Gather basic resources to survive your first night in the woods.",
            new MissionObjective[]
            {
                MakeObjective("Collect Wood", ObjectiveType.CollectItem, "Wood", "", 5),
                MakeObjective("Collect Stone", ObjectiveType.CollectItem, "Stone", "", 3)
            }));

        // Mission 2: Hunting for Sustenance
        list.Add(MakeMission("Hunting for Sustenance",
            "Hunt wildlife for food and learn to cook.",
            new MissionObjective[]
            {
                MakeObjective("Kill deer", ObjectiveType.KillCreature, "", "Deer", 2),
                MakeObjective("Collect Venison", ObjectiveType.CollectItem, "Venison", "", 2),
                MakeObjective("Craft Cooked Venison", ObjectiveType.CraftItem, "Cooked Venison", "", 2)
            }));

        // Mission 3: Exploration
        list.Add(MakeMission("Exploration",
            "Explore the forest and discover abandoned cabins for clues.",
            new MissionObjective[]
            {
                MakeObjective("Discover abandoned cabins", ObjectiveType.ReachLocation, "", "", 2)
            }));

        // Mission 4: Crafting Tools
        list.Add(MakeMission("Crafting Tools",
            "Create essential equipment for survival.",
            new MissionObjective[]
            {
                MakeObjective("Craft a Stone Axe", ObjectiveType.CraftItem, "Stone Axe", "", 1),
                MakeObjective("Craft a Torch", ObjectiveType.CraftItem, "Torch", "", 1)
            }));

        // Mission 5: Forest Threats
        list.Add(MakeMission("Forest Threats",
            "The shadow creatures emerge at night. Survive and fight back.",
            new MissionObjective[]
            {
                MakeObjective("Survive a full night", ObjectiveType.SurviveNight, "", "", 1),
                MakeObjective("Kill shadow creatures", ObjectiveType.KillCreature, "", "Shadow Creature", 3)
            }));

        // Mission 6: The Escape
        list.Add(MakeMission("The Escape",
            "You've gathered enough knowledge. Find the way out of the woods.",
            new MissionObjective[]
            {
                MakeObjective("Find the forest exit", ObjectiveType.ReachLocation, "", "", 1)
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
