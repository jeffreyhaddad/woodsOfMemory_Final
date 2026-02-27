using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;

/// <summary>
/// One-click setup: adds the Death trigger + state to PlayAnimController
/// and disables looping on the Standing Death Left 01 clip.
/// Menu: Tools > Setup Death Animation
/// </summary>
public class SetupDeathAnimation
{
    const string ControllerPath = "Assets/Models/Character Animations/Action Adventure Pack/PlayAnimController.controller";
    const string FbxPath        = "Assets/Models/Character Animations/Action Adventure Pack/Standing Death Left 01.fbx";
    const string StateName      = "Standing Death Left 01";
    const string TriggerName    = "Death";

    [MenuItem("Tools/Setup Death Animation")]
    static void Run()
    {
        // ── 1. Load controller ─────────────────────────────────────────────
        var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);
        if (controller == null)
        {
            Debug.LogError("[DeathSetup] Controller not found at: " + ControllerPath);
            return;
        }

        // ── 2. Load clip from FBX ──────────────────────────────────────────
        AnimationClip deathClip = null;
        foreach (var obj in AssetDatabase.LoadAllAssetsAtPath(FbxPath))
        {
            if (obj is AnimationClip c && !c.name.StartsWith("__preview__"))
            {
                deathClip = c;
                break;
            }
        }
        if (deathClip == null)
        {
            Debug.LogError("[DeathSetup] AnimationClip not found inside: " + FbxPath);
            return;
        }

        // ── 3. Disable loop on the imported clip ───────────────────────────
        var importer = AssetImporter.GetAtPath(FbxPath) as ModelImporter;
        if (importer != null)
        {
            // Use override clips if already defined, otherwise start from defaults
            var clips = importer.clipAnimations.Length > 0
                ? importer.clipAnimations
                : importer.defaultClipAnimations;

            bool changed = false;
            for (int i = 0; i < clips.Length; i++)
            {
                if (clips[i].loopTime)
                {
                    clips[i].loopTime = false;
                    changed = true;
                }
            }
            if (changed)
            {
                importer.clipAnimations = clips;
                importer.SaveAndReimport();
                // Reload clip reference after reimport
                foreach (var obj in AssetDatabase.LoadAllAssetsAtPath(FbxPath))
                    if (obj is AnimationClip c && !c.name.StartsWith("__preview__"))
                        { deathClip = c; break; }
            }
        }

        // ── 4. Add Death trigger parameter ────────────────────────────────
        bool hasTrigger = false;
        foreach (var p in controller.parameters)
            if (p.name == TriggerName && p.type == AnimatorControllerParameterType.Trigger)
                { hasTrigger = true; break; }

        if (!hasTrigger)
        {
            controller.AddParameter(TriggerName, AnimatorControllerParameterType.Trigger);
            Debug.Log("[DeathSetup] Added trigger: " + TriggerName);
        }
        else
        {
            Debug.Log("[DeathSetup] Trigger already exists: " + TriggerName);
        }

        // ── 5. Find or create the Death state ─────────────────────────────
        var sm = controller.layers[0].stateMachine;

        AnimatorState deathState = null;
        foreach (var cs in sm.states)
            if (cs.state.name == StateName)
                { deathState = cs.state; break; }

        if (deathState == null)
        {
            deathState = sm.AddState(StateName, new Vector3(400, 200, 0));
            Debug.Log("[DeathSetup] Created state: " + StateName);
        }
        else
        {
            Debug.Log("[DeathSetup] State already exists: " + StateName);
        }

        deathState.motion = deathClip;

        // ── 6. Add Any State → Death transition ───────────────────────────
        bool hasTransition = false;
        foreach (var t in sm.anyStateTransitions)
            if (t.destinationState == deathState)
                { hasTransition = true; break; }

        if (!hasTransition)
        {
            var t = sm.AddAnyStateTransition(deathState);
            t.AddCondition(AnimatorConditionMode.If, 0, TriggerName);
            t.duration             = 0.15f;
            t.hasExitTime          = false;
            t.canTransitionToSelf  = false;
            Debug.Log("[DeathSetup] Added Any State → " + StateName + " transition");
        }
        else
        {
            Debug.Log("[DeathSetup] Transition already exists");
        }

        // ── 7. Save ───────────────────────────────────────────────────────
        EditorUtility.SetDirty(sm);
        EditorUtility.SetDirty(controller);
        AssetDatabase.SaveAssets();

        Debug.Log("[DeathSetup] Done! PlayAnimController is ready.");
        EditorUtility.DisplayDialog(
            "Death Animation Setup",
            "Done!\n\nAdded to PlayAnimController:\n• 'Death' trigger parameter\n• 'Standing Death Left 01' state (no loop)\n• Any State → Death transition",
            "OK");
    }
}
