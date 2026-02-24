using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Spawns the equipped tool/weapon model in the player's right hand.
/// Auto-created by GameManager. Models loaded from Resources/Weapons/.
/// albedoTexture: if set, a URP Lit material is built at runtime using that texture.
///               Leave empty to use the FBX's own materials (e.g. Cabin assets).
/// </summary>
public class WeaponHolder : MonoBehaviour
{
    [System.Serializable]
    public class ItemConfig
    {
        [Tooltip("Substring of the item name to match (case-insensitive)")]
        public string itemContains;
        public string resourcePath;
        [Tooltip("Resources path to albedo texture for runtime material. Leave empty to use the model's own materials.")]
        public string albedoTexture;
        public Vector3 position;
        public Vector3 rotation;
        public float scale = 1f;
    }

    [Header("Per-Item Configs (adjust offsets here)")]
    public List<ItemConfig> itemConfigs = new List<ItemConfig>
    {
        new ItemConfig { itemContains = "Pickaxe", resourcePath = "Weapons/Pickaxe",  albedoTexture = "",                           position = Vector3.zero, rotation = new Vector3(0,  0, 0), scale = 3f },
        new ItemConfig { itemContains = "Axe",     resourcePath = "Weapons/Axe",      albedoTexture = "",                           position = Vector3.zero, rotation = new Vector3(0,  0, 0), scale = 3f },
        new ItemConfig { itemContains = "Torch",   resourcePath = "Weapons/Torch",    albedoTexture = "Weapons/torchstickColor",       position = Vector3.zero, rotation = new Vector3(90, 0, 0), scale = 50f },
        new ItemConfig { itemContains = "Lantern", resourcePath = "Weapons/Lantern",  albedoTexture = "Weapons/Lantern_AlbedoOpacity", position = Vector3.zero, rotation = new Vector3(0,  0, 0), scale = 50f },
    };

    private Transform rightHandBone;
    private GameObject spawnedModel;
    private string lastToolName   = "";
    private string lastWeaponName = "";

    void Start()
    {
        rightHandBone = FindHandBone();
        if (rightHandBone == null)
            Debug.LogWarning("[WeaponHolder] Could not find right hand bone.");
        else
            Debug.Log($"[WeaponHolder] Hand bone: '{rightHandBone.name}'");
    }

    void Update()
    {
        if (EquipmentManager.Instance == null) return;

        string toolName   = EquipmentManager.Instance.EquippedTool?.itemName   ?? "";
        string weaponName = EquipmentManager.Instance.EquippedWeapon?.itemName ?? "";

        if (toolName != lastToolName || weaponName != lastWeaponName)
        {
            lastToolName   = toolName;
            lastWeaponName = weaponName;
            Refresh(toolName, weaponName);
        }
    }

    Transform FindHandBone()
    {
        foreach (Animator anim in GetComponentsInChildren<Animator>(true))
        {
            if (!anim.isHuman) continue;
            Transform bone = anim.GetBoneTransform(HumanBodyBones.RightHand);
            if (bone != null) return bone;
        }
        foreach (Transform t in GetComponentsInChildren<Transform>(true))
        {
            string lower = t.name.ToLowerInvariant();
            if ((lower.Contains("righthand") || lower.Contains("right_hand") || lower.Contains("r_hand"))
                && !lower.Contains("index") && !lower.Contains("middle")
                && !lower.Contains("ring")  && !lower.Contains("pinky")
                && !lower.Contains("thumb") && !lower.Contains("finger"))
                return t;
        }
        return null;
    }

    void Refresh(string toolName, string weaponName)
    {
        if (spawnedModel != null) { Destroy(spawnedModel); spawnedModel = null; }
        if (rightHandBone == null) return;

        ItemConfig cfg = null;
        string combined = toolName + " " + weaponName;
        foreach (var c in itemConfigs)
        {
            if (combined.IndexOf(c.itemContains, System.StringComparison.OrdinalIgnoreCase) >= 0)
            { cfg = c; break; }
        }
        if (cfg == null) return;

        GameObject prefab = Resources.Load<GameObject>(cfg.resourcePath);
        if (prefab == null)
        {
            Debug.LogError($"[WeaponHolder] Could not load Resources/{cfg.resourcePath}");
            return;
        }

        spawnedModel = Instantiate(prefab, rightHandBone);
        spawnedModel.transform.localPosition    = cfg.position;
        spawnedModel.transform.localEulerAngles = cfg.rotation;
        spawnedModel.transform.localScale       = Vector3.one * cfg.scale;

        foreach (var col in spawnedModel.GetComponentsInChildren<Collider>())
            col.enabled = false;
        foreach (var lt in spawnedModel.GetComponentsInChildren<Light>())
            lt.enabled = false;

        // If an albedoTexture path is given, build a URP material at runtime.
        // Otherwise leave the model's own materials intact (e.g. Cabin Lantern).
        if (!string.IsNullOrEmpty(cfg.albedoTexture))
        {
            Shader urpLit = Shader.Find("Universal Render Pipeline/Lit")
                         ?? Shader.Find("Standard");
            if (urpLit != null)
            {
                Texture2D tex = Resources.Load<Texture2D>(cfg.albedoTexture);
                Material mat  = new Material(urpLit);
                if (tex != null) mat.SetTexture("_BaseMap", tex);
                else             mat.color = new Color(1f, 0.7f, 0.3f);
                foreach (var r in spawnedModel.GetComponentsInChildren<Renderer>(true))
                    r.material = mat;
            }
        }

        var renderers = spawnedModel.GetComponentsInChildren<Renderer>(true);
        Debug.Log($"[WeaponHolder] Spawned '{prefab.name}' — {renderers.Length} renderer(s), scale={cfg.scale}, worldPos={spawnedModel.transform.position}");
        foreach (var r in renderers)
            Debug.Log($"  [{r.name}] bounds={r.bounds.size} enabled={r.enabled}");
    }
}
