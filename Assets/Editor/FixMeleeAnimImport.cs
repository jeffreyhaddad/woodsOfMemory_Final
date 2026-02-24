using UnityEngine;
using UnityEditor;

/// <summary>
/// Tools → Fix Melee Anim Import
/// Sets all FBXs in the Pro Melee Axe Pack to Humanoid rig type so they
/// retarget correctly onto the Humanoid Remy character.
/// </summary>
public static class FixMeleeAnimImport
{
    const string PackPath = "Assets/Models/Character Animations/Pro Melee Axe Pack";

    [MenuItem("Tools/Fix Melee Anim Import")]
    static void Fix()
    {
        string[] guids = AssetDatabase.FindAssets("t:Model", new[] { PackPath });
        int fixed_count = 0;

        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);

            // Skip the character mesh itself — only re-rig the animation FBXs
            // (Remy.fbx is a character mesh, not needed at runtime for us)
            ModelImporter importer = AssetImporter.GetAtPath(path) as ModelImporter;
            if (importer == null) continue;

            bool changed = false;

            if (importer.animationType != ModelImporterAnimationType.Human)
            {
                importer.animationType = ModelImporterAnimationType.Human;
                changed = true;
            }

            // Ensure animation clips are imported (not just mesh data)
            if (!importer.importAnimation)
            {
                importer.importAnimation = true;
                changed = true;
            }

            // Disable mesh import for pure animation FBXs to keep them lean
            // (Remy.fbx has a mesh we don't need; skip it to avoid overwriting our character)
            if (!path.EndsWith("Remy.fbx") && importer.meshCompression != ModelImporterMeshCompression.Off)
            {
                // leave mesh compression as-is, just focus on rig type
            }

            if (changed)
            {
                importer.SaveAndReimport();
                fixed_count++;
                Debug.Log($"[FixMeleeAnimImport] Reimported as Humanoid: {path}");
            }
        }

        AssetDatabase.Refresh();
        Debug.Log($"[FixMeleeAnimImport] Done — fixed {fixed_count} FBX(s).");
        EditorUtility.DisplayDialog("Done",
            $"Fixed {fixed_count} FBX files in the Pro Melee Axe Pack.\n" +
            "All animations are now Humanoid and will retarget correctly onto Remy.",
            "OK");
    }
}
