using System.IO;
using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine.AI;

/// <summary>
/// One-click setup for the zombie shadow creature.
/// Menu: Woods of Memory → Setup Zombie Creature
/// Creates: ZombieAnimController.controller, ZombieCreatureData.asset, ZombieCreaturePrefab.prefab
/// </summary>
public static class ZombieSetupEditor
{
    const string ModelPath      = "Assets/Models/Animals/ZombieMonster/Ch25_nonPBR.fbx";
    const string AnimFolder     = "Assets/Models/Animals/ZombieMonster/Animations/";
    const string ControllerPath = "Assets/Prefabs/Creatures/ZombieAnimController.controller";
    const string DataPath       = "Assets/Models/Animals/ZombieMonster/ZombieCreatureData.asset";
    const string PrefabPath     = "Assets/Prefabs/Creatures/ZombieCreaturePrefab.prefab";

    [MenuItem("Tools/Setup Zombie Creature")]
    public static void SetupZombieCreature()
    {
        // ── 1. Verify model ───────────────────────────────────────
        GameObject modelAsset = AssetDatabase.LoadAssetAtPath<GameObject>(ModelPath);
        if (modelAsset == null)
        {
            EditorUtility.DisplayDialog("Setup Failed",
                "Model not found:\n" + ModelPath +
                "\n\nMake sure Unity has finished importing Ch25_nonPBR.fbx " +
                "(check the Project window for the ZombieMonster folder).", "OK");
            return;
        }

        // ── 2. Load animation clips ───────────────────────────────
        AnimationClip clipIdle   = LoadClip(AnimFolder + "zombie idle.fbx");
        AnimationClip clipWalk   = LoadClip(AnimFolder + "zombie walk.fbx");
        AnimationClip clipRun    = LoadClip(AnimFolder + "zombie run.fbx");
        AnimationClip clipAttack = LoadClip(AnimFolder + "zombie attack.fbx");
        AnimationClip clipDeath  = LoadClip(AnimFolder + "zombie death.fbx");

        if (clipIdle == null || clipWalk == null || clipRun == null ||
            clipAttack == null || clipDeath == null)
        {
            EditorUtility.DisplayDialog("Setup Failed",
                "One or more animation clips could not be loaded from:\n" + AnimFolder +
                "\n\nExpected FBX files:\n  zombie idle\n  zombie walk\n  zombie run\n  zombie attack\n  zombie death" +
                "\n\nWait for Unity to finish importing them.", "OK");
            return;
        }

        // ── 3. Create Animator Controller ────────────────────────
        if (File.Exists(ControllerPath))
            AssetDatabase.DeleteAsset(ControllerPath);

        AnimatorController ctrl = AnimatorController.CreateAnimatorControllerAtPath(ControllerPath);
        AnimatorStateMachine sm = ctrl.layers[0].stateMachine;

        // States — names must match the constants in ShadowCreatureAI
        AnimatorState stIdle   = AddState(sm, "Idle",   clipIdle,   isDefault: true);
        AnimatorState stWalk   = AddState(sm, "Walk",   clipWalk);
        AnimatorState stRun    = AddState(sm, "Run",    clipRun);
        AnimatorState stAttack = AddState(sm, "Attack", clipAttack);
        AnimatorState stDeath  = AddState(sm, "Death",  clipDeath);

        // Loop settings
        SetLooping(clipIdle,   true);
        SetLooping(clipWalk,   true);
        SetLooping(clipRun,    true);
        SetLooping(clipAttack, true);
        SetLooping(clipDeath,  false);

        // CrossFadeInFixedTime handles all transitions from ShadowCreatureAI,
        // so we only need a minimal exit from Attack back to Idle.
        var t = stAttack.AddTransition(stIdle);
        t.hasExitTime = true;
        t.exitTime    = 0.9f;
        t.duration    = 0.2f;

        // Death has no exit — Destroy(gameObject) fires after 4s anyway.
        stDeath.AddTransition(stIdle).hasExitTime = false; // unreachable but required by Unity

        AssetDatabase.SaveAssets();

        // ── 4. Create or update CreatureData ─────────────────────
        CreatureData data = AssetDatabase.LoadAssetAtPath<CreatureData>(DataPath);
        if (data == null)
        {
            data = ScriptableObject.CreateInstance<CreatureData>();
            AssetDatabase.CreateAsset(data, DataPath);
        }
        data.creatureName   = "Shadow Creature";
        data.maxHealth      = 60f;
        data.moveSpeed      = 2.5f;
        data.runSpeed       = 5.5f;
        data.detectionRange = 18f;
        data.fleeRange      = 0f;
        data.attackRange    = 2.2f;
        data.damage         = 15f;
        data.attackCooldown = 2f;
        data.lootTable      = new LootDrop[0];
        EditorUtility.SetDirty(data);
        AssetDatabase.SaveAssets();

        // ── 5. Build Prefab ───────────────────────────────────────
        if (File.Exists(PrefabPath))
            AssetDatabase.DeleteAsset(PrefabPath);

        GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(modelAsset);
        instance.name = "ZombieCreaturePrefab";

        // scale=2 → waist height; scale=2.5 → slightly taller than player, feels threatening
        instance.transform.localScale = Vector3.one * 2.5f;

        // Animator — already on the model root; just assign the controller
        Animator anim = instance.GetComponent<Animator>();
        if (anim == null) anim = instance.AddComponent<Animator>();
        anim.runtimeAnimatorController = ctrl;
        anim.applyRootMotion           = false;
        anim.updateMode                = AnimatorUpdateMode.Normal;

        // NavMeshAgent
        NavMeshAgent agent = instance.AddComponent<NavMeshAgent>();
        agent.speed            = 3f;
        agent.angularSpeed     = 240f;
        agent.acceleration     = 12f;
        agent.stoppingDistance = 1.5f;
        agent.radius           = 0.4f;
        agent.height           = 1.8f;

        // ShadowCreatureAI
        ShadowCreatureAI ai = instance.AddComponent<ShadowCreatureAI>();
        ai.data = data;

        // Apply URP material with zombie texture
        ApplyZombieMaterial(instance);

        // Save prefab
        PrefabUtility.SaveAsPrefabAsset(instance, PrefabPath);
        Object.DestroyImmediate(instance);

        AssetDatabase.Refresh();

        Debug.Log("[ZombieSetup] Done! Prefab saved to: " + PrefabPath);
        EditorUtility.DisplayDialog("Zombie Setup Complete!",
            "Animator controller, creature data, and prefab created.\n\n" +
            "Prefab: " + PrefabPath + "\n\n" +
            "The CreatureSpawner will auto-load the prefab next Play.", "OK");
    }

    // ── Helpers ───────────────────────────────────────────────

    static AnimatorState AddState(AnimatorStateMachine sm, string name, AnimationClip clip, bool isDefault = false)
    {
        AnimatorState state = sm.AddState(name);
        state.motion = clip;
        if (isDefault) sm.defaultState = state;
        return state;
    }

    static AnimationClip LoadClip(string fbxPath)
    {
        Object[] assets = AssetDatabase.LoadAllAssetsAtPath(fbxPath);
        foreach (Object a in assets)
        {
            if (a is AnimationClip clip && !clip.name.StartsWith("__preview__"))
                return clip;
        }
        Debug.LogWarning("[ZombieSetup] No animation clip found in: " + fbxPath);
        return null;
    }

    static void SetLooping(AnimationClip clip, bool loop)
    {
        if (clip == null) return;
        AnimationClipSettings settings = AnimationUtility.GetAnimationClipSettings(clip);
        settings.loopTime = loop;
        AnimationUtility.SetAnimationClipSettings(clip, settings);
    }

    static void ApplyZombieMaterial(GameObject instance)
    {
        Shader urpLit = Shader.Find("Universal Render Pipeline/Lit")
                     ?? Shader.Find("Universal Render Pipeline/Simple Lit")
                     ?? Shader.Find("Standard");
        if (urpLit == null) { Debug.LogWarning("[ZombieSetup] URP Lit shader not found."); return; }

        string matFolder = "Assets/Prefabs/Creatures/Materials";
        if (!AssetDatabase.IsValidFolder(matFolder))
            AssetDatabase.CreateFolder("Assets/Prefabs/Creatures", "Materials");

        // Zombie flesh colour: pale sickly grey-green — used when no texture is available
        Color zombieColor = new Color(0.38f, 0.42f, 0.32f);

        var matMap = new System.Collections.Generic.Dictionary<Material, Material>();

        foreach (Renderer rend in instance.GetComponentsInChildren<Renderer>(true))
        {
            Material[] origMats = rend.sharedMaterials;
            Material[] newMats  = new Material[origMats.Length];

            for (int i = 0; i < origMats.Length; i++)
            {
                Material orig = origMats[i];
                if (orig == null) { newMats[i] = null; continue; }

                if (!matMap.TryGetValue(orig, out Material urpMat))
                {
                    string safeName = orig.name.Replace("/", "_").Replace("\\", "_");
                    string matPath  = matFolder + "/Zombie_" + safeName + ".mat";

                    // Always recreate so stale white material from a previous run is replaced
                    if (AssetDatabase.LoadAssetAtPath<Material>(matPath) != null)
                        AssetDatabase.DeleteAsset(matPath);

                    urpMat = new Material(urpLit);
                    AssetDatabase.CreateAsset(urpMat, matPath);

                    // Try to carry over the FBX texture (Standard _MainTex or URP _BaseMap)
                    Texture tex = null;
                    if (orig.HasProperty("_MainTex")) tex = orig.GetTexture("_MainTex");
                    if (tex == null && orig.HasProperty("_BaseMap")) tex = orig.GetTexture("_BaseMap");

                    if (tex != null)
                    {
                        urpMat.SetTexture("_BaseMap", tex);
                        urpMat.SetTexture("_MainTex",  tex);
                    }

                    // Colour: prefer what the FBX material stored; fall back to zombie flesh
                    Color col = zombieColor;
                    if (orig.HasProperty("_Color"))
                    {
                        Color origCol = orig.GetColor("_Color");
                        // Only use FBX colour if it isn't pure white (default placeholder)
                        if (origCol.r < 0.95f || origCol.g < 0.95f || origCol.b < 0.95f)
                            col = origCol;
                    }
                    // URP uses _BaseColor; setting .color on a URP mat writes _BaseColor
                    urpMat.SetColor("_BaseColor", col);

                    EditorUtility.SetDirty(urpMat);
                    matMap[orig] = urpMat;
                }

                newMats[i] = urpMat;
            }

            rend.sharedMaterials = newMats;
        }

        AssetDatabase.SaveAssets();
        Debug.Log($"[ZombieSetup] Created {matMap.Count} URP material(s) for zombie.");
    }
}
