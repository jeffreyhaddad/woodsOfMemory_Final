using UnityEngine;
using UnityEditor;

/// <summary>
/// Woods > Fix Weapon Materials
/// Creates URP/Lit .mat files with correct textures for Torch and Lantern,
/// then remaps the FBX importer so the models use them at runtime.
/// Run this once from the menu after importing new weapon FBX files.
/// </summary>
public class SetupWeaponMaterials
{
    [MenuItem("Woods/Fix Weapon Materials")]
    static void FixWeaponMaterials()
    {
        FixModel(
            fbxPath:       "Assets/Resources/Weapons/Torch.fbx",
            baseColorPath: "Assets/Resources/Weapons/torchstickColor.png",
            normalPath:    "Assets/Resources/Weapons/torchstickNormal.png",
            emissionPath:  "Assets/Resources/Weapons/torchstickemmision.png",
            matName:       "TorchMaterial"
        );

        FixModel(
            fbxPath:       "Assets/Resources/Weapons/LanternMesh.fbx",
            baseColorPath: "Assets/ThirdParty/Cabin/Models/Materials/Lantern_AlbedoOpacity.png",
            normalPath:    "Assets/ThirdParty/Cabin/Models/Materials/Lantern_Normal.png",
            emissionPath:  null,
            matName:       "LanternMeshMaterial"
        );

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[SetupWeaponMaterials] Done! Enter Play Mode to see the changes.");
    }

    static void FixModel(string fbxPath, string baseColorPath, string normalPath, string matName, string emissionPath = null)
    {
        // --- Load textures ---
        Texture2D baseColor = AssetDatabase.LoadAssetAtPath<Texture2D>(baseColorPath);
        Texture2D normal    = AssetDatabase.LoadAssetAtPath<Texture2D>(normalPath);
        Texture2D emission  = emissionPath != null ? AssetDatabase.LoadAssetAtPath<Texture2D>(emissionPath) : null;

        if (baseColor == null)
            Debug.LogWarning($"[SetupWeaponMaterials] Texture not found: {baseColorPath}");

        // --- Create URP Lit material ---
        Shader urpLit = Shader.Find("Universal Render Pipeline/Lit");
        if (urpLit == null)
        {
            Debug.LogError("[SetupWeaponMaterials] URP/Lit shader not found — is URP installed?");
            return;
        }

        Material mat = new Material(urpLit) { name = matName };
        if (baseColor != null) mat.SetTexture("_BaseMap", baseColor);
        if (normal    != null) { mat.SetTexture("_BumpMap", normal); mat.EnableKeyword("_NORMALMAP"); }
        if (emission  != null)
        {
            mat.SetTexture("_EmissionMap", emission);
            mat.SetColor("_EmissionColor", Color.white * 0.8f);
            mat.EnableKeyword("_EMISSION");
        }

        mat.SetFloat("_Smoothness", 0.3f);
        mat.SetFloat("_Metallic",   0.1f);

        // --- Save material asset ---
        string matPath = $"Assets/Resources/Weapons/{matName}.mat";
        AssetDatabase.DeleteAsset(matPath);
        AssetDatabase.CreateAsset(mat, matPath);
        Debug.Log($"[SetupWeaponMaterials] Created material: {matPath}");

        Material savedMat = AssetDatabase.LoadAssetAtPath<Material>(matPath);

        // --- Remap FBX embedded materials → our new .mat ---
        ModelImporter importer = AssetImporter.GetAtPath(fbxPath) as ModelImporter;
        if (importer == null)
        {
            Debug.LogWarning($"[SetupWeaponMaterials] No ModelImporter found at {fbxPath}");
            return;
        }

        bool remapped = false;
        foreach (var asset in AssetDatabase.LoadAllAssetsAtPath(fbxPath))
        {
            if (asset is Material embeddedMat)
            {
                var id = new AssetImporter.SourceAssetIdentifier(embeddedMat);
                importer.AddRemap(id, savedMat);
                Debug.Log($"[SetupWeaponMaterials] Remapped '{embeddedMat.name}' → {matName}");
                remapped = true;
            }
        }

        if (!remapped)
            Debug.LogWarning($"[SetupWeaponMaterials] No embedded materials in {fbxPath}.");

        importer.SaveAndReimport();
    }
}
