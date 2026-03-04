using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine.AI;

/// <summary>
/// Builds the Deer prefab from its FBX.
/// Menu: Tools → Build Creature Prefabs
/// For the zombie shadow creature, use: Woods of Memory → Setup Zombie Creature
/// </summary>
public class CreaturePrefabBuilder : EditorWindow
{
    private const string DeerFBXPath   = "Assets/Models/Animals/Deer/Deer.fbx";
    private const string DeerDataPath  = "Assets/Models/Animals/Deer.asset";
    private const string PrefabFolder  = "Assets/Prefabs/Creatures";
    private const string LootFolder    = "Assets/Items/Loot";
    private const string MatFolder     = "Assets/Prefabs/Creatures/Materials";

    [MenuItem("Tools/Build Creature Prefabs")]
    public static void BuildCreaturePrefabs()
    {
        EnsureFolder(PrefabFolder);
        EnsureFolder(LootFolder);
        EnsureFolder(MatFolder);

        CreateDeerLootItems();
        AssignDeerLootTable();
        BuildDeerPrefab();
        AssignToSpawner();

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log("[CreaturePrefabBuilder] Done! Deer prefab saved to " + PrefabFolder);
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

        GameObject model = (GameObject)PrefabUtility.InstantiatePrefab(modelAsset);
        model.transform.SetParent(root.transform, false);
        model.transform.localScale    = Vector3.one;
        model.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
        model.name = "DeerModel";

        CreateAndApplyMaterial(model, "DeerMat",
            "Assets/Models/Animals/Deer/Textures/DeerBase02_0_Diffuse.jpeg",
            "Assets/Models/Animals/Deer/Textures/DeerBase02_0_Normal.jpeg");

        BuildAnimatorController(DeerFBXPath, "DeerAnimController",
            new string[] { "DEERALL_Idle", "DEERALL_WalkForward", "DEERALL_Gallop",
                           "DEERALL_Grazing", "DEERALL_Trot", "DEERALL_Backing" });

        Animator anim = model.GetComponentInChildren<Animator>();
        if (anim == null) anim = model.AddComponent<Animator>();
        var ctrl = AssetDatabase.LoadAssetAtPath<AnimatorController>(PrefabFolder + "/DeerAnimController.controller");
        if (ctrl != null) anim.runtimeAnimatorController = ctrl;

        NavMeshAgent agent = root.AddComponent<NavMeshAgent>();
        agent.speed            = data.moveSpeed;
        agent.angularSpeed     = 120f;
        agent.acceleration     = 8f;
        agent.stoppingDistance = 1f;
        agent.radius           = 0.4f;
        agent.height           = 1.5f;

        CapsuleCollider col = root.AddComponent<CapsuleCollider>();
        col.center = new Vector3(0f, 0.75f, 0f);
        col.radius = 0.4f;
        col.height = 1.5f;

        WildlifeAI ai = root.AddComponent<WildlifeAI>();
        ai.data = data;

        string prefabPath = PrefabFolder + "/DeerPrefab.prefab";
        PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
        Object.DestroyImmediate(root);

        Debug.Log("[CreaturePrefabBuilder] Created Deer prefab at " + prefabPath);
    }

    static void BuildAnimatorController(string fbxPath, string controllerName, string[] clipNames)
    {
        string controllerPath = PrefabFolder + "/" + controllerName + ".controller";
        SetClipsToLoop(fbxPath);

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

        AnimatorController controller = AnimatorController.CreateAnimatorControllerAtPath(controllerPath);
        AnimatorStateMachine rootSM   = controller.layers[0].stateMachine;

        bool firstState = true;
        foreach (AnimationClip clip in clips)
        {
            if (clipNames != null && clipNames.Length > 0)
            {
                bool found = false;
                foreach (string name in clipNames)
                    if (clip.name == name) { found = true; break; }
                if (!found) continue;
            }

            AnimatorState state = rootSM.AddState(clip.name);
            state.motion = clip;
            if (firstState) { rootSM.defaultState = state; firstState = false; }
        }

        EditorUtility.SetDirty(controller);
    }

    static void SetClipsToLoop(string fbxPath)
    {
        ModelImporter importer = AssetImporter.GetAtPath(fbxPath) as ModelImporter;
        if (importer == null) return;

        ModelImporterClipAnimation[] clips = importer.clipAnimations;
        if (clips == null || clips.Length == 0)
            clips = importer.defaultClipAnimations;
        if (clips == null || clips.Length == 0) return;

        bool needsReimport = false;
        for (int i = 0; i < clips.Length; i++)
        {
            if (!clips[i].loopTime)
            {
                clips[i].loopTime = true;
                clips[i].loopPose = true;
                needsReimport = true;
            }
        }

        if (needsReimport)
        {
            importer.clipAnimations = clips;
            importer.SaveAndReimport();
        }
    }

    static void CreateAndApplyMaterial(GameObject model, string matName,
        string diffusePath, string normalPath)
    {
        Texture2D diffuse = AssetDatabase.LoadAssetAtPath<Texture2D>(diffusePath);
        Texture2D normal  = AssetDatabase.LoadAssetAtPath<Texture2D>(normalPath);

        if (diffuse == null)
        {
            Debug.LogWarning("[CreaturePrefabBuilder] Diffuse texture not found: " + diffusePath);
            return;
        }

        if (normal != null)
        {
            TextureImporter ti = AssetImporter.GetAtPath(normalPath) as TextureImporter;
            if (ti != null && ti.textureType != TextureImporterType.NormalMap)
            {
                ti.textureType = TextureImporterType.NormalMap;
                ti.SaveAndReimport();
                normal = AssetDatabase.LoadAssetAtPath<Texture2D>(normalPath);
            }
        }

        Shader urpLit = Shader.Find("Universal Render Pipeline/Lit")
                     ?? Shader.Find("Universal Render Pipeline/Simple Lit");
        if (urpLit == null) { Debug.LogError("[CreaturePrefabBuilder] URP Lit shader not found!"); return; }

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
        if (normal != null) { mat.SetTexture("_BumpMap", normal); mat.EnableKeyword("_NORMALMAP"); }
        EditorUtility.SetDirty(mat);

        Renderer[] renderers = model.GetComponentsInChildren<Renderer>(true);
        foreach (Renderer rend in renderers)
        {
            Material[] mats = new Material[rend.sharedMaterials.Length];
            for (int i = 0; i < mats.Length; i++) mats[i] = mat;
            rend.sharedMaterials = mats;
        }
    }

    static void CreateDeerLootItems()
    {
        CreateItemAsset("Venison",    "Cooked venison meat. Restores hunger.",
            ItemCategory.Food,     ItemUseAction.EatFood, 25f, LootFolder + "/Venison.asset");
        CreateItemAsset("Deer Hide",  "Tough animal hide. Useful for crafting.",
            ItemCategory.Resource, ItemUseAction.None,    0f,  LootFolder + "/DeerHide.asset");
    }

    static void CreateItemAsset(string itemName, string description,
        ItemCategory category, ItemUseAction useAction, float useValue, string path)
    {
        if (AssetDatabase.LoadAssetAtPath<ItemData>(path) != null) return;

        ItemData item       = ScriptableObject.CreateInstance<ItemData>();
        item.itemName       = itemName;
        item.description    = description;
        item.category       = category;
        item.isStackable    = true;
        item.maxStack       = 10;
        item.useAction      = useAction;
        item.useValue       = useValue;
        AssetDatabase.CreateAsset(item, path);
    }

    static void AssignDeerLootTable()
    {
        CreatureData deerData = AssetDatabase.LoadAssetAtPath<CreatureData>(DeerDataPath);
        if (deerData == null || (deerData.lootTable != null && deerData.lootTable.Length > 0)) return;

        ItemData venison = AssetDatabase.LoadAssetAtPath<ItemData>(LootFolder + "/Venison.asset");
        ItemData hide    = AssetDatabase.LoadAssetAtPath<ItemData>(LootFolder + "/DeerHide.asset");

        deerData.lootTable = new LootDrop[]
        {
            new LootDrop { item = venison, minQuantity = 1, maxQuantity = 2, dropChance = 0.8f },
            new LootDrop { item = hide,    minQuantity = 1, maxQuantity = 1, dropChance = 0.6f },
        };
        EditorUtility.SetDirty(deerData);
    }

    static void AssignToSpawner()
    {
        CreatureSpawner spawner = Object.FindAnyObjectByType<CreatureSpawner>();
        if (spawner == null) return;

        GameObject deer = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabFolder + "/DeerPrefab.prefab");
        if (deer != null)
        {
            spawner.wildlifePrefabs = new GameObject[] { deer };
            EditorUtility.SetDirty(spawner);
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
