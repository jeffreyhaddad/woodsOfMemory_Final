using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;

/// <summary>
/// Tools → Setup Melee Animations
/// Adds AxeAttack and PickaxeAttack states to PlayAnimController,
/// loaded from the Pro Melee Axe Pack FBX files.
/// Safe to re-run — skips states that already exist.
/// </summary>
public static class SetupMeleeAnimations
{
    const string ControllerPath = "Assets/Models/Character Animations/Action Adventure Pack/PlayAnimController.controller";
    const string PackPath       = "Assets/Models/Character Animations/Pro Melee Axe Pack/";

    [MenuItem("Tools/Setup Melee Animations")]
    static void Setup()
    {
        var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);
        if (controller == null)
        {
            EditorUtility.DisplayDialog("Error", "Could not find PlayAnimController at:\n" + ControllerPath, "OK");
            return;
        }

        var rootSM = controller.layers[0].stateMachine;

        // --- Find existing Idle Blend state ---
        AnimatorState idleBlend = FindState(rootSM, "Idle Blend");
        if (idleBlend == null)
        {
            EditorUtility.DisplayDialog("Error", "Could not find 'Idle Blend' state in the controller.", "OK");
            return;
        }

        // --- Load animation clips ---
        AnimationClip axeClip      = LoadClip(PackPath + "standing melee attack horizontal.fbx");
        AnimationClip pickaxeClip  = LoadClip(PackPath + "standing melee attack downward.fbx");

        if (axeClip == null || pickaxeClip == null)
        {
            EditorUtility.DisplayDialog("Error",
                "Could not load animation clips. Make sure Unity has imported the Pro Melee Axe Pack FBXs.", "OK");
            return;
        }

        // --- Add AxeAttack state ---
        AnimatorState axeState = FindState(rootSM, "AxeAttack");
        if (axeState == null)
        {
            axeState = rootSM.AddState("AxeAttack");
            Debug.Log("[SetupMeleeAnimations] Added AxeAttack state.");
        }
        axeState.motion = axeClip;
        axeState.speed  = 1.4f; // slightly faster for snappier combat feel
        EnsureExitTransition(axeState, idleBlend);

        // --- Add PickaxeAttack state ---
        AnimatorState pickaxeState = FindState(rootSM, "PickaxeAttack");
        if (pickaxeState == null)
        {
            pickaxeState = rootSM.AddState("PickaxeAttack");
            Debug.Log("[SetupMeleeAnimations] Added PickaxeAttack state.");
        }
        pickaxeState.motion = pickaxeClip;
        pickaxeState.speed  = 1.2f;
        EnsureExitTransition(pickaxeState, idleBlend);

        EditorUtility.SetDirty(controller);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log("[SetupMeleeAnimations] Done — AxeAttack and PickaxeAttack wired into PlayAnimController.");
        EditorUtility.DisplayDialog("Done",
            "AxeAttack and PickaxeAttack states added to PlayAnimController.\n\n" +
            "Axe   → standing melee attack horizontal (speed 1.4x)\n" +
            "Pickaxe → standing melee attack downward (speed 1.2x)\n\n" +
            "Both auto-return to Idle Blend at 85% completion.", "OK");
    }

    // ---------------------------------------------------------------

    static AnimatorState FindState(AnimatorStateMachine sm, string name)
    {
        foreach (var cs in sm.states)
            if (cs.state.name == name) return cs.state;
        return null;
    }

    static AnimationClip LoadClip(string fbxPath)
    {
        // FBX files embed clips as sub-assets; filter out __preview__ copies
        Object[] assets = AssetDatabase.LoadAllAssetsAtPath(fbxPath);
        foreach (Object a in assets)
        {
            if (a is AnimationClip clip && !clip.name.StartsWith("__preview__"))
                return clip;
        }
        return null;
    }

    /// <summary>
    /// Ensures there is exactly one exit-time transition from the attack state back to idleBlend.
    /// Won't duplicate if already present.
    /// </summary>
    static void EnsureExitTransition(AnimatorState from, AnimatorState to)
    {
        foreach (var t in from.transitions)
            if (t.destinationState == to) return; // already exists

        var transition = from.AddTransition(to);
        transition.hasExitTime    = true;
        transition.exitTime       = 0.85f; // blend out before the last frame
        transition.duration       = 0.2f;
        transition.hasFixedDuration = false; // normalized time
    }
}
