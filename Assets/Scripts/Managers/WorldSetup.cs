using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// Auto-generates mission triggers, resource pickups, and note pickups at runtime.
/// Add this to a GameObject in GameScene. Adjust positions in the Inspector.
/// </summary>
public class WorldSetup : MonoBehaviour
{
    [Header("Cabin Trigger Positions")]
    [Tooltip("World positions for abandoned cabin triggers. Need at least 2 for Mission 3.")]
    public Vector3 cabin1Position = new Vector3(80, 0, 120);
    public Vector3 cabin2Position = new Vector3(-60, 0, 180);
    public float cabinTriggerSize = 12f;

    [Header("Dark Clearing (Mission 5)")]
    [Tooltip("World position of the corrupted clearing for Mission 5 — Into the Dark.")]
    public Vector3 darkClearingPosition = new Vector3(110, 0, 155);
    public float darkClearingTriggerSize = 15f;

    [Header("Forest Exit")]
    [Tooltip("World position of the forest exit for Mission 6.")]
    public Vector3 exitPosition = new Vector3(200, 0, 250);
    public float exitTriggerSize = 10f;

    [Header("Resource Spawning")]
    [Tooltip("How far from the player spawn to scatter resources")]
    public float resourceSpawnRadius = 40f;
    public int woodCount = 25;
    public int stoneCount = 20;
    public int fiberCount = 15;
    public int berryCount = 12;
    public int herbCount = 10;

    [Header("Resource Respawning")]
    [Tooltip("Seconds between respawn waves")]
    public float respawnInterval = 60f;
    [Tooltip("How many resources to add each wave")]
    public int respawnBatchSize = 5;

    [Header("Note Pickups")]
    [Tooltip("World positions for the 10 discoverable story notes (entries 1-10 in JournalManager). " +
             "Spread across the map: spawn area → forest → second cabin → exit.")]
    public Vector3 note1Position  = new Vector3( 15,  0.5f,  20);   // Strange Markings     — near spawn
    public Vector3 note2Position  = new Vector3( 30,  0.5f,  50);   // Day Three            — early forest
    public Vector3 note3Position  = new Vector3( 75,  0.5f, 115);   // A Torn Letter        — near cabin 1
    public Vector3 note4Position  = new Vector3( 50,  0.5f,  80);   // Night Whispers       — mid forest
    public Vector3 note5Position  = new Vector3(-30,  0.5f, 100);   // They're Not Animals  — west forest
    public Vector3 note6Position  = new Vector3(-55,  0.5f, 175);   // The Other Cabin      — near cabin 2
    public Vector3 note7Position  = new Vector3(120,  0.5f, 160);   // The Map Fragment     — deep forest
    public Vector3 note8Position  = new Vector3(160,  0.5f, 210);   // Almost Out           — approaching exit
    public Vector3 note9Position  = new Vector3(180,  0.5f, 235);   // Memories Returning   — near exit
    public Vector3 note10Position = new Vector3(195,  0.5f, 245);   // The Truth            — at exit
    public Vector3 note11Position = new Vector3(108,  0.5f, 153);   // Why I Came           — at Dark Clearing

    // Cached ItemData instances
    private ItemData woodItem;
    private ItemData stoneItem;
    private ItemData fiberItem;
    private ItemData berryItem;
    private ItemData herbItem;

    private float respawnTimer;
    private Vector3 spawnCenter;
    private Transform playerTransform;
    private static Shader cachedURPLitShader;

    // Shared material cache — all pickups of the same color reuse one material (enables GPU batching)
    private static readonly Dictionary<Color, Material> sharedMaterials = new Dictionary<Color, Material>();
    private static Material sharedTransparentMarkerMat;

    void OnDestroy()
    {
        // Clear static material caches so they don't accumulate across editor play sessions
        sharedMaterials.Clear();
        sharedTransparentMarkerMat = null;
        cachedURPLitShader = null;
    }

    void Start()
    {
        // Cache the URP Lit shader once to avoid repeated Shader.Find calls
        if (cachedURPLitShader == null)
        {
            cachedURPLitShader = Shader.Find("Universal Render Pipeline/Lit");
            if (cachedURPLitShader == null) cachedURPLitShader = Shader.Find("Standard");
            if (cachedURPLitShader == null) cachedURPLitShader = Shader.Find("Sprites/Default");
        }

        CreateItemData();

        // Find player BEFORE creating triggers so cabin1 can be placed at the
        // actual spawn position rather than the hardcoded inspector default.
        PlayerVitals pv = FindAnyObjectByType<PlayerVitals>();
        if (pv != null) playerTransform = pv.transform;
        spawnCenter = playerTransform != null ? playerTransform.position : Vector3.zero;

        CreateMissionTriggers();
        SpawnResources();
        CreateNotePickups();
        respawnTimer = respawnInterval;
    }

    void Update()
    {
        respawnTimer -= Time.deltaTime;
        if (respawnTimer <= 0f)
        {
            RespawnResources();
            respawnTimer = respawnInterval;
        }
    }

    void RespawnResources()
    {
        // Spawn a small batch of each resource type
        ItemData[] types = { woodItem, stoneItem, fiberItem, berryItem, herbItem };
        PrimitiveType[] shapes = { PrimitiveType.Cylinder, PrimitiveType.Cube, PrimitiveType.Cube, PrimitiveType.Sphere, PrimitiveType.Sphere };
        Color[] colors = {
            new Color(0.45f, 0.25f, 0.1f),
            new Color(0.5f, 0.5f, 0.5f),
            new Color(0.3f, 0.5f, 0.15f),
            new Color(0.6f, 0.1f, 0.2f),
            new Color(0.2f, 0.6f, 0.15f)
        };
        Vector3[] scales = {
            new Vector3(0.4f, 0.8f, 0.4f),
            new Vector3(0.6f, 0.45f, 0.6f),
            new Vector3(0.5f, 0.35f, 0.5f),
            new Vector3(0.45f, 0.45f, 0.45f),
            new Vector3(0.45f, 0.45f, 0.45f)
        };

        // Use cached player transform for respawn center
        Vector3 center = playerTransform != null ? playerTransform.position : spawnCenter;

        for (int t = 0; t < types.Length; t++)
        {
            SpawnResourceCluster(types[t], respawnBatchSize, center, resourceSpawnRadius, shapes[t], colors[t], scales[t]);
        }
    }

    void CreateItemData()
    {
        woodItem = MakeItem("Wood", ItemCategory.Resource, true, 20);
        stoneItem = MakeItem("Stone", ItemCategory.Resource, true, 20);
        fiberItem = MakeItem("Fiber", ItemCategory.Resource, true, 20);
        berryItem = MakeItem("Berries", ItemCategory.Food, true, 15, ItemUseAction.EatFood, 5f);
        herbItem = MakeItem("Herbs", ItemCategory.Resource, true, 15);
    }

    // ─── Mission Triggers ────────────────────────────────────

    void CreateMissionTriggers()
    {
        // Cabin 1: center on the Cabin GameObject so the trigger is inside it.
        // Falls back to inspector value if no Cabin found in scene.
        GameObject cabinObj = GameObject.Find("Cabin");
        Vector3 c1 = cabinObj != null ? cabinObj.transform.position : cabin1Position;
        CreateTrigger("CabinTrigger1", c1, cabinTriggerSize, "start_cabin");
        //CreateTrigger("CabinTrigger2", cabin2Position, cabinTriggerSize, "cabin_area");
        // Dark Clearing — oneShot=false so DarkClearingEvent's coroutine isn't cut short
        GameObject clearingObj = CreateTrigger("DarkClearing", darkClearingPosition, darkClearingTriggerSize, "dark_clearing", oneShot: false);
        if (clearingObj != null)
            clearingObj.AddComponent<DarkClearingEvent>();

        CreateTrigger("ForestExit", exitPosition, exitTriggerSize, "exit");

        // Register compass POIs.
        // Main cabin is auto-discovered — player starts here.
        // Second cabin and forest exit are hidden until the player explores to them.
        CompassUI.RegisterPOI("Main Cabin", cabinObj != null ? cabinObj.transform.position : c1,
            startDiscovered: true,
            discoveryRadius: 30f,
            new Color(0.9f, 0.78f, 0.5f),
            CompassUI.POIType.Location);

        CompassUI.RegisterPOI("Second Cabin", cabin2Position,
            startDiscovered: false,
            discoveryRadius: 35f,
            new Color(1f, 0.85f, 0.4f),
            CompassUI.POIType.Location);

        CompassUI.RegisterPOI("Dark Clearing", darkClearingPosition,
            startDiscovered: false,
            discoveryRadius: 30f,
            new Color(0.5f, 0.3f, 0.8f),
            CompassUI.POIType.Location);

        CompassUI.RegisterPOI("Forest Exit", exitPosition,
            startDiscovered: false,
            discoveryRadius: 30f,
            new Color(0.45f, 1f, 0.5f),
            CompassUI.POIType.Exit);

        Debug.Log("WorldSetup: Created 4 mission triggers + compass POIs.");
    }

    GameObject CreateTrigger(string name, Vector3 position, float size, string locationName, bool oneShot = true)
    {
        // Snap to terrain height
        position.y = GetTerrainHeight(position) + 1f;

        GameObject obj = new GameObject(name);
        obj.transform.position = position;

        BoxCollider col = obj.AddComponent<BoxCollider>();
        col.isTrigger = true;
        col.size = new Vector3(size, 6f, size);

        MissionTrigger trigger = obj.AddComponent<MissionTrigger>();
        trigger.locationName = locationName;
        trigger.oneShot = oneShot;

        // Visual marker (semi-transparent pillar) for debugging — remove for release
        GameObject marker = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        marker.transform.SetParent(obj.transform, false);
        marker.transform.localScale = new Vector3(1f, 3f, 1f);
        Destroy(marker.GetComponent<Collider>());
        Renderer rend = marker.GetComponent<Renderer>();
        // Share one transparent marker material across all triggers
        if (sharedTransparentMarkerMat == null)
        {
            sharedTransparentMarkerMat = new Material(cachedURPLitShader);
            sharedTransparentMarkerMat.color = new Color(1f, 1f, 0f, 0.15f);
            sharedTransparentMarkerMat.SetFloat("_Surface", 1);
            sharedTransparentMarkerMat.SetFloat("_Blend", 0);
            sharedTransparentMarkerMat.SetOverrideTag("RenderType", "Transparent");
            sharedTransparentMarkerMat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            sharedTransparentMarkerMat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            sharedTransparentMarkerMat.SetInt("_ZWrite", 0);
            sharedTransparentMarkerMat.renderQueue = 3000;
        }
        rend.sharedMaterial = sharedTransparentMarkerMat;

        return obj;
    }

    // ─── Resource Spawning ───────────────────────────────────

    void SpawnResources()
    {
        Vector3 center = playerTransform != null ? playerTransform.position : Vector3.zero;

        int total = 0;
        total += SpawnResourceCluster(woodItem, woodCount, center, resourceSpawnRadius, PrimitiveType.Cylinder, new Color(0.45f, 0.25f, 0.1f), new Vector3(0.4f, 0.8f, 0.4f));
        total += SpawnResourceCluster(stoneItem, stoneCount, center, resourceSpawnRadius, PrimitiveType.Cube, new Color(0.5f, 0.5f, 0.5f), new Vector3(0.6f, 0.45f, 0.6f));
        total += SpawnResourceCluster(fiberItem, fiberCount, center, resourceSpawnRadius, PrimitiveType.Cube, new Color(0.3f, 0.5f, 0.15f), new Vector3(0.5f, 0.35f, 0.5f));
        total += SpawnResourceCluster(berryItem, berryCount, center, resourceSpawnRadius * 0.8f, PrimitiveType.Sphere, new Color(0.6f, 0.1f, 0.2f), new Vector3(0.45f, 0.45f, 0.45f));
        total += SpawnResourceCluster(herbItem, herbCount, center, resourceSpawnRadius * 0.8f, PrimitiveType.Sphere, new Color(0.2f, 0.6f, 0.15f), new Vector3(0.45f, 0.45f, 0.45f));

        Debug.Log("WorldSetup: Spawned " + total + " resource pickups.");
    }

    int SpawnResourceCluster(ItemData item, int count, Vector3 center, float radius, PrimitiveType shape, Color color, Vector3 scale)
    {
        int spawned = 0;

        for (int i = 0; i < count; i++)
        {
            Vector2 rnd = Random.insideUnitCircle * radius;
            Vector3 candidate = center + new Vector3(rnd.x, 0, rnd.y);

            // Snap XZ to nearest walkable NavMesh position; always use terrain
            // height for Y so resources never float on top of elevated NavMesh surfaces
            if (NavMesh.SamplePosition(candidate, out NavMeshHit hit, 10f, NavMesh.AllAreas))
            {
                candidate.x = hit.position.x;
                candidate.z = hit.position.z;
            }

            float groundY = GetTerrainHeight(candidate);

            Vector3 pos = new Vector3(candidate.x, groundY + scale.y * 0.5f, candidate.z);
            CreateResourcePickup(item, pos, shape, color, scale);
            spawned++;
        }

        return spawned;
    }

    void CreateResourcePickup(ItemData item, Vector3 position, PrimitiveType shape, Color color, Vector3 scale)
    {
        GameObject obj = new GameObject(item.itemName + "_Pickup");
        obj.transform.position = position;

        // Tall capsule collider so the camera ray can hit it even from above
        CapsuleCollider col = obj.AddComponent<CapsuleCollider>();
        col.radius = 0.5f;
        col.height = 1.5f;
        col.center = new Vector3(0, 0.5f, 0);

        // Pickup component
        PickupItem pickup = obj.AddComponent<PickupItem>();
        pickup.itemData = item;
        pickup.quantity = 1;
        pickup.promptText = "Pick up " + item.itemName;

        // Visual
        GameObject visual = GameObject.CreatePrimitive(shape);
        visual.transform.SetParent(obj.transform, false);
        visual.transform.localScale = scale;
        visual.transform.localRotation = Quaternion.Euler(Random.Range(-15f, 15f), Random.Range(0f, 360f), Random.Range(-15f, 15f));
        Destroy(visual.GetComponent<Collider>());

        ApplyColor(visual.GetComponent<Renderer>(), color);
    }

    // ─── Note Pickups ────────────────────────────────────────

    void CreateNotePickups()
    {
        JournalManager journal = JournalManager.Instance;
        if (journal == null)
            journal = FindAnyObjectByType<JournalManager>();

        if (journal == null || journal.startingEntries == null)
        {
            Debug.LogWarning("WorldSetup: No JournalManager found. Skipping note pickups.");
            return;
        }

        // Place notes at specified positions, using entries index 1-10
        // (index 0 is "Awakening" which auto-adds on start / via IntroSequenceManager)
        Vector3[] notePositions = {
            note1Position, note2Position, note3Position, note4Position, note5Position,
            note6Position, note7Position, note8Position, note9Position, note10Position,
            note11Position  // "Note: Why I Came" — at the Dark Clearing
        };

        int placed = 0;
        for (int i = 0; i < notePositions.Length; i++)
        {
            int entryIndex = i + 1; // skip index 0 (Awakening)
            if (entryIndex >= journal.startingEntries.Length) break;
            if (journal.startingEntries[entryIndex] == null) continue;

            Vector3 pos = notePositions[i];
            pos.y = GetTerrainHeight(pos) + 0.8f;

            CreateNoteObject(journal.startingEntries[entryIndex], pos);
            placed++;
        }

        Debug.Log("WorldSetup: Placed " + placed + " note pickups.");
    }

    void CreateNoteObject(JournalEntry entry, Vector3 position)
    {
        GameObject obj = new GameObject("Note_" + entry.title);
        obj.transform.position = position;

        // Collider
        BoxCollider col = obj.AddComponent<BoxCollider>();
        col.size = new Vector3(0.4f, 0.5f, 0.1f);

        // Note pickup component
        NotePickup note = obj.AddComponent<NotePickup>();
        note.entry = entry;
        note.promptText = "Read: " + entry.title;

        // Visual — flat white quad that looks like paper
        GameObject visual = GameObject.CreatePrimitive(PrimitiveType.Quad);
        visual.transform.SetParent(obj.transform, false);
        visual.transform.localScale = new Vector3(0.3f, 0.4f, 1f);
        visual.transform.localRotation = Quaternion.Euler(0, Random.Range(0f, 360f), 0);
        Destroy(visual.GetComponent<Collider>());

        ApplyColor(visual.GetComponent<Renderer>(), new Color(0.9f, 0.85f, 0.7f));

        // Slight hover/bob effect
        obj.AddComponent<PickupBob>();
    }

    // ─── Helpers ─────────────────────────────────────────────

    Material GetSharedMaterial(Color color)
    {
        if (sharedMaterials.TryGetValue(color, out Material existing))
            return existing;

        Material mat = new Material(cachedURPLitShader);
        mat.color = color;
        if (mat.HasProperty("_BaseColor"))
            mat.SetColor("_BaseColor", color);
        mat.enableInstancing = true;
        sharedMaterials[color] = mat;
        return mat;
    }

    void ApplyColor(Renderer rend, Color color)
    {
        if (rend == null) return;
        // Use sharedMaterial (not .material) to avoid creating per-renderer copies
        rend.sharedMaterial = GetSharedMaterial(color);
    }

    float GetTerrainHeight(Vector3 position)
    {
        Terrain terrain = Terrain.activeTerrain;
        if (terrain != null)
            return terrain.SampleHeight(position) + terrain.transform.position.y;

        // Fallback: raycast down
        if (Physics.Raycast(position + Vector3.up * 500f, Vector3.down, out RaycastHit hit, 1000f))
            return hit.point.y;

        return 0f;
    }

    ItemData MakeItem(string itemName, ItemCategory cat, bool stackable, int maxStack,
        ItemUseAction useAction = ItemUseAction.None, float useValue = 0f)
    {
        ItemData item = ScriptableObject.CreateInstance<ItemData>();
        item.name = itemName;
        item.itemName = itemName;
        item.category = cat;
        item.isStackable = stackable;
        item.maxStack = maxStack;
        item.useAction = useAction;
        item.useValue = useValue;
        ItemRegistry.Register(item);
        return item;
    }
}
