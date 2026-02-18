using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine.AI;

public class CreaturePrefabBuilder : EditorWindow
{
    private const string DeerFBXPath = "Assets/Models/Animals/Deer/Deer.fbx";
    private const string ShadowFBXPath = "Assets/Models/Animals/ShadowMonster/ShadowMonster.fbx";
    private const string DeerDataPath = "Assets/Models/Animals/Deer.asset";
    private const string ShadowDataPath = "Assets/Models/Animals/ShadowCreature.asset";
    private const string PrefabFolder = "Assets/Prefabs/Creatures";
    private const string LootFolder = "Assets/Items/Loot";
    private const string MatFolder = "Assets/Prefabs/Creatures/Materials";

    [MenuItem("Tools/Build Creature Prefabs")]
    public static void BuildCreaturePrefabs()
    {
        EnsureFolder(PrefabFolder);
        EnsureFolder(LootFolder);
        EnsureFolder(MatFolder);

        // Create loot item assets first
        CreateLootItems();

        // Wire loot tables into CreatureData assets
        AssignLootTables();

        // Build prefabs
        BuildDeerPrefab();
        BuildShadowCreaturePrefab();

        // Try to assign prefabs to CreatureSpawner in the open scene
        AssignToSpawner();

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log("[CreaturePrefabBuilder] Done! Prefabs saved to " + PrefabFolder);
    }

    static void BuildDeerPrefab()
    {
        GameObject modelAsset = AssetDatabase.LoadAssetAtPath<GameObject>(DeerFBXPath);
        if (modelAsset == null)
        {
            Debug.LogError("[CreaturePrefabBuilder] Deer FBX not found at " + DeerFBXPath);
            return;
        }

        CreatureData data = AssetDatabase.LoadAssetAtPath<CreatureData>(DeerDataPath);
        if (data == null)
        {
            Debug.LogError("[CreaturePrefabBuilder] Deer CreatureData not found at " + DeerDataPath);
            return;
        }

        GameObject root = new GameObject("DeerPrefab");

        // Instantiate model as child — rotate 180° so model faces Unity's forward (Z+)
        GameObject model = (GameObject)PrefabUtility.InstantiatePrefab(modelAsset);
        model.transform.SetParent(root.transform, false);
        model.transform.localScale = Vector3.one * 1.0f;
        model.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
        model.name = "DeerModel";

        // Create and apply a proper URP material with textures saved as asset
        CreateAndApplyMaterial(model, "DeerMat",
            "Assets/Models/Animals/Deer/Textures/DeerBase02_0_Diffuse.jpeg",
            "Assets/Models/Animals/Deer/Textures/DeerBase02_0_Normal.jpeg");

        // Build AnimatorController from FBX clips and assign to model's Animator
        BuildAnimatorController(DeerFBXPath, "DeerAnimController",
            new string[] { "DEERALL_Idle", "DEERALL_WalkForward", "DEERALL_Gallop", "DEERALL_Grazing",
                           "DEERALL_Trot", "DEERALL_Backing" });
        Animator anim = model.GetComponentInChildren<Animator>();
        if (anim == null) anim = model.AddComponent<Animator>();
        var ctrl = AssetDatabase.LoadAssetAtPath<AnimatorController>(PrefabFolder + "/DeerAnimController.controller");
        if (ctrl != null) anim.runtimeAnimatorController = ctrl;

        // Add NavMeshAgent
        NavMeshAgent agent = root.AddComponent<NavMeshAgent>();
        agent.speed = data.moveSpeed;
        agent.angularSpeed = 120f;
        agent.acceleration = 8f;
        agent.stoppingDistance = 1f;
        agent.radius = 0.4f;
        agent.height = 1.5f;

        // Add CapsuleCollider
        CapsuleCollider col = root.AddComponent<CapsuleCollider>();
        col.center = new Vector3(0f, 0.75f, 0f);
        col.radius = 0.4f;
        col.height = 1.5f;

        // Add WildlifeAI and assign data
        WildlifeAI ai = root.AddComponent<WildlifeAI>();
        ai.data = data;

        // Save as prefab
        string prefabPath = PrefabFolder + "/DeerPrefab.prefab";
        PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
        Object.DestroyImmediate(root);

        Debug.Log("[CreaturePrefabBuilder] Created Deer prefab at " + prefabPath);
    }

    static void BuildShadowCreaturePrefab()
    {
        GameObject modelAsset = AssetDatabase.LoadAssetAtPath<GameObject>(ShadowFBXPath);
        if (modelAsset == null)
        {
            Debug.LogError("[CreaturePrefabBuilder] ShadowMonster FBX not found at " + ShadowFBXPath);
            return;
        }

        CreatureData data = AssetDatabase.LoadAssetAtPath<CreatureData>(ShadowDataPath);
        if (data == null)
        {
            Debug.LogError("[CreaturePrefabBuilder] ShadowCreature data not found at " + ShadowDataPath);
            return;
        }

        GameObject root = new GameObject("ShadowCreaturePrefab");

        // Instantiate model as child
        GameObject model = (GameObject)PrefabUtility.InstantiatePrefab(modelAsset);
        model.transform.SetParent(root.transform, false);
        model.transform.localScale = Vector3.one * 0.01f;
        model.name = "ShadowModel";

        // Create and apply material
        CreateAndApplyMaterial(model, "ShadowMat",
            "Assets/Models/Animals/ShadowMonster/Textures/MONSTER SHADOW _ Low _low_poli_BaseColor.png",
            "Assets/Models/Animals/ShadowMonster/Textures/MONSTER SHADOW _ Low _low_poli_Normal.png");

        // Add NavMeshAgent
        NavMeshAgent agent = root.AddComponent<NavMeshAgent>();
        agent.speed = data.moveSpeed;
        agent.angularSpeed = 180f;
        agent.acceleration = 10f;
        agent.stoppingDistance = 1f;
        agent.radius = 0.6f;
        agent.height = 2.0f;

        // Add CapsuleCollider
        CapsuleCollider col = root.AddComponent<CapsuleCollider>();
        col.center = new Vector3(0f, 1.0f, 0f);
        col.radius = 0.6f;
        col.height = 2.0f;

        // Add ShadowCreatureAI and assign data
        ShadowCreatureAI ai = root.AddComponent<ShadowCreatureAI>();
        ai.data = data;
        ai.attackInterval = data.attackCooldown;

        // Save as prefab
        string prefabPath = PrefabFolder + "/ShadowCreaturePrefab.prefab";
        PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
        Object.DestroyImmediate(root);

        Debug.Log("[CreaturePrefabBuilder] Created Shadow Creature prefab at " + prefabPath);
    }

    static void BuildAnimatorController(string fbxPath, string controllerName, string[] clipNames)
    {
        string controllerPath = PrefabFolder + "/" + controllerName + ".controller";

        // Configure FBX importer to set all clips to loop
        SetClipsToLoop(fbxPath);

        // Load all clips from the FBX
        Object[] allAssets = AssetDatabase.LoadAllAssetsAtPath(fbxPath);
        var clips = new System.Collections.Generic.List<AnimationClip>();
        foreach (Object asset in allAssets)
        {
            if (asset is AnimationClip clip && !clip.name.StartsWith("__preview__"))
                clips.Add(clip);
        }

        if (clips.Count == 0)
        {
            Debug.LogWarning("[CreaturePrefabBuilder] No animation clips found in " + fbxPath);
            return;
        }

        // Create or overwrite controller
        AnimatorController controller = AnimatorController.CreateAnimatorControllerAtPath(controllerPath);
        AnimatorStateMachine rootSM = controller.layers[0].stateMachine;

        bool firstState = true;
        foreach (AnimationClip clip in clips)
        {
            // Only add requested clips (or all if none specified)
            if (clipNames != null && clipNames.Length > 0)
            {
                bool found = false;
                foreach (string name in clipNames)
                {
                    if (clip.name == name) { found = true; break; }
                }
                if (!found) continue;
            }

            AnimatorState state = rootSM.AddState(clip.name);
            state.motion = clip;

            // Set first clip as default
            if (firstState)
            {
                rootSM.defaultState = state;
                firstState = false;
            }
        }

        EditorUtility.SetDirty(controller);
        Debug.Log("[CreaturePrefabBuilder] Created AnimatorController with " + clips.Count + " clips: " + controllerPath);
    }

    static void SetClipsToLoop(string fbxPath)
    {
        ModelImporter importer = AssetImporter.GetAtPath(fbxPath) as ModelImporter;
        if (importer == null) return;

        // Get all clip names from the FBX
        ModelImporterClipAnimation[] existingClips = importer.clipAnimations;

        // If no clips configured yet, use the default clips as a starting point
        if (existingClips == null || existingClips.Length == 0)
            existingClips = importer.defaultClipAnimations;

        if (existingClips == null || existingClips.Length == 0) return;

        bool needsReimport = false;
        for (int i = 0; i < existingClips.Length; i++)
        {
            if (!existingClips[i].loopTime)
            {
                existingClips[i].loopTime = true;
                existingClips[i].loopPose = true;
                needsReimport = true;
            }
        }

        if (needsReimport)
        {
            importer.clipAnimations = existingClips;
            importer.SaveAndReimport();
            Debug.Log("[CreaturePrefabBuilder] Set " + existingClips.Length + " clips to loop in " + fbxPath);
        }
    }

    static void CreateAndApplyMaterial(GameObject model, string matName,
        string diffusePath, string normalPath)
    {
        // Load textures
        Texture2D diffuse = AssetDatabase.LoadAssetAtPath<Texture2D>(diffusePath);
        Texture2D normal = AssetDatabase.LoadAssetAtPath<Texture2D>(normalPath);

        if (diffuse == null)
        {
            Debug.LogWarning("[CreaturePrefabBuilder] Diffuse texture not found: " + diffusePath);
            return;
        }

        // Mark normal map texture type if found
        if (normal != null)
        {
            string normalAssetPath = AssetDatabase.GetAssetPath(normal);
            TextureImporter importer = AssetImporter.GetAtPath(normalAssetPath) as TextureImporter;
            if (importer != null && importer.textureType != TextureImporterType.NormalMap)
            {
                importer.textureType = TextureImporterType.NormalMap;
                importer.SaveAndReimport();
                // Reload after reimport
                normal = AssetDatabase.LoadAssetAtPath<Texture2D>(normalPath);
            }
        }

        // Find URP Lit shader
        Shader urpLit = Shader.Find("Universal Render Pipeline/Lit");
        if (urpLit == null)
        {
            urpLit = Shader.Find("Universal Render Pipeline/Simple Lit");
            if (urpLit == null)
            {
                Debug.LogError("[CreaturePrefabBuilder] Could not find URP Lit shader!");
                return;
            }
        }

        // Create material and save as asset so it persists in the prefab
        string matPath = MatFolder + "/" + matName + ".mat";
        Material mat = AssetDatabase.LoadAssetAtPath<Material>(matPath);
        if (mat == null)
        {
            mat = new Material(urpLit);
            AssetDatabase.CreateAsset(mat, matPath);
        }
        else
        {
            mat.shader = urpLit;
        }

        mat.SetTexture("_BaseMap", diffuse);
        if (normal != null)
        {
            mat.SetTexture("_BumpMap", normal);
            mat.EnableKeyword("_NORMALMAP");
        }

        EditorUtility.SetDirty(mat);

        // Apply to all renderers on the model
        Renderer[] renderers = model.GetComponentsInChildren<Renderer>(true);
        foreach (Renderer rend in renderers)
        {
            Material[] mats = new Material[rend.sharedMaterials.Length];
            for (int i = 0; i < mats.Length; i++)
                mats[i] = mat;
            rend.sharedMaterials = mats;
        }

        Debug.Log("[CreaturePrefabBuilder] Created and applied material: " + matPath);
    }

    static void CreateLootItems()
    {
        CreateItemAsset("Venison", "Cooked venison meat. Restores hunger.",
            ItemCategory.Food, ItemUseAction.EatFood, 25f, LootFolder + "/Venison.asset");

        CreateItemAsset("Deer Hide", "Tough animal hide. Useful for crafting.",
            ItemCategory.Resource, ItemUseAction.None, 0f, LootFolder + "/DeerHide.asset");

        CreateItemAsset("Shadow Essence", "A dark, swirling essence from a shadow creature.",
            ItemCategory.QuestItem, ItemUseAction.None, 0f, LootFolder + "/ShadowEssence.asset");
    }

    static void CreateItemAsset(string itemName, string description,
        ItemCategory category, ItemUseAction useAction, float useValue, string path)
    {
        if (AssetDatabase.LoadAssetAtPath<ItemData>(path) != null)
        {
            Debug.Log("[CreaturePrefabBuilder] Item already exists: " + path);
            return;
        }

        ItemData item = ScriptableObject.CreateInstance<ItemData>();
        item.itemName = itemName;
        item.description = description;
        item.category = category;
        item.isStackable = true;
        item.maxStack = 10;
        item.useAction = useAction;
        item.useValue = useValue;

        AssetDatabase.CreateAsset(item, path);
        Debug.Log("[CreaturePrefabBuilder] Created item: " + path);
    }

    static void AssignToSpawner()
    {
        CreatureSpawner spawner = Object.FindAnyObjectByType<CreatureSpawner>();
        if (spawner == null)
        {
            Debug.LogWarning("[CreaturePrefabBuilder] No CreatureSpawner found in scene. " +
                "Add one to your scene and run this again, or assign prefabs manually.");
            return;
        }

        GameObject deerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabFolder + "/DeerPrefab.prefab");
        GameObject shadowPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabFolder + "/ShadowCreaturePrefab.prefab");

        if (deerPrefab != null)
        {
            spawner.wildlifePrefabs = new GameObject[] { deerPrefab };
            Debug.Log("[CreaturePrefabBuilder] Assigned Deer prefab to CreatureSpawner.wildlifePrefabs");
        }

        if (shadowPrefab != null)
        {
            spawner.shadowPrefab = shadowPrefab;
            Debug.Log("[CreaturePrefabBuilder] Assigned Shadow prefab to CreatureSpawner.shadowPrefab");
        }

        EditorUtility.SetDirty(spawner);
    }

    static void AssignLootTables()
    {
        CreatureData deerData = AssetDatabase.LoadAssetAtPath<CreatureData>(DeerDataPath);
        if (deerData != null && (deerData.lootTable == null || deerData.lootTable.Length == 0))
        {
            ItemData venison = AssetDatabase.LoadAssetAtPath<ItemData>(LootFolder + "/Venison.asset");
            ItemData hide = AssetDatabase.LoadAssetAtPath<ItemData>(LootFolder + "/DeerHide.asset");

            deerData.lootTable = new LootDrop[]
            {
                new LootDrop { item = venison, minQuantity = 1, maxQuantity = 2, dropChance = 0.8f },
                new LootDrop { item = hide, minQuantity = 1, maxQuantity = 1, dropChance = 0.6f }
            };
            EditorUtility.SetDirty(deerData);
            Debug.Log("[CreaturePrefabBuilder] Assigned loot table to Deer");
        }

        CreatureData shadowData = AssetDatabase.LoadAssetAtPath<CreatureData>(ShadowDataPath);
        if (shadowData != null && (shadowData.lootTable == null || shadowData.lootTable.Length == 0))
        {
            ItemData essence = AssetDatabase.LoadAssetAtPath<ItemData>(LootFolder + "/ShadowEssence.asset");

            shadowData.lootTable = new LootDrop[]
            {
                new LootDrop { item = essence, minQuantity = 1, maxQuantity = 1, dropChance = 0.3f }
            };
            EditorUtility.SetDirty(shadowData);
            Debug.Log("[CreaturePrefabBuilder] Assigned loot table to Shadow Creature");
        }
    }

    static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path)) return;

        string[] parts = path.Split('/');
        string current = parts[0];
        for (int i = 1; i < parts.Length; i++)
        {
            string next = current + "/" + parts[i];
            if (!AssetDatabase.IsValidFolder(next))
                AssetDatabase.CreateFolder(current, parts[i]);
            current = next;
        }
    }
}
