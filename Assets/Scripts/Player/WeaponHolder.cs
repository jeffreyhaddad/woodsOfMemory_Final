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
        new ItemConfig { itemContains = "Pickaxe", resourcePath = "Weapons/Pickaxe",  albedoTexture = "",                           position = Vector3.zero, rotation = new Vector3(0,  0, 0), scale = 0.02f },
        new ItemConfig { itemContains = "Axe",     resourcePath = "Weapons/Axe",      albedoTexture = "",                           position = Vector3.zero, rotation = new Vector3(0,  0, 0), scale = 0.06f },
        new ItemConfig { itemContains = "Torch",   resourcePath = "Weapons/Torch",    albedoTexture = "", position = new Vector3(0.0f, 0.05f, 0.1f), rotation = new Vector3(-90, 0, 0), scale = 1f },
        new ItemConfig { itemContains = "Lantern", resourcePath = "Weapons/Lantern",  albedoTexture = "", position = new Vector3(0f, 0f,    0.1f), rotation = new Vector3(0, 0, 0), scale = 1f },
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
            Debug.Log($"[WeaponHolder] Hand bone: '{rightHandBone.name}' lossyScale={rightHandBone.lossyScale} right={rightHandBone.right} up={rightHandBone.up} forward={rightHandBone.forward}");
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

        foreach (var col in spawnedModel.GetComponentsInChildren<Collider>())
            col.enabled = false;
        foreach (var lt in spawnedModel.GetComponentsInChildren<Light>())
            lt.enabled = false;

        // Force the entire hierarchy visible — FBX assets can have inactive child nodes
        // or LOD groups that cull the mesh before it even renders.
        foreach (Transform t in spawnedModel.GetComponentsInChildren<Transform>(true))
            t.gameObject.SetActive(true);
        foreach (var r in spawnedModel.GetComponentsInChildren<Renderer>(true))
            r.enabled = true;
        LODGroup lod = spawnedModel.GetComponentInChildren<LODGroup>(true);
        if (lod != null) lod.enabled = false;

        // Auto-size all hand items from real world-space bounds.
        // cfg.scale is ignored for named items — sizes are hard-coded here in metres.
        bool isAxe     = combined.IndexOf("Axe",     System.StringComparison.OrdinalIgnoreCase) >= 0;
        bool isPickaxe = combined.IndexOf("Pickaxe", System.StringComparison.OrdinalIgnoreCase) >= 0;
        bool isTorch   = combined.IndexOf("Torch",   System.StringComparison.OrdinalIgnoreCase) >= 0;
        bool isLantern = combined.IndexOf("Lantern", System.StringComparison.OrdinalIgnoreCase) >= 0;

        float targetMeters = isPickaxe ? 1.2f : isAxe ? 0.9f : isTorch ? 0.8f : isLantern ? 0.6f : -1f;

        if (targetMeters > 0f)
        {
            // Measure mesh bounds at scale = 1 (skip particle renderers — their bounds are unreliable at spawn)
            spawnedModel.transform.localScale = Vector3.one;

            Bounds wb = new Bounds();
            bool first = true;
            foreach (var r in spawnedModel.GetComponentsInChildren<Renderer>(true))
            {
                if (r is ParticleSystemRenderer) continue;
                if (first) { wb = r.bounds; first = false; }
                else wb.Encapsulate(r.bounds);
            }

            float maxExtent = Mathf.Max(wb.size.x, wb.size.y, wb.size.z);
            float autoScale = maxExtent > 0.0001f ? targetMeters / maxExtent : 1f;
            spawnedModel.transform.localScale = Vector3.one * autoScale;
            Debug.Log($"[WeaponHolder] Auto-sized '{prefab.name}': size=({wb.size.x:F4},{wb.size.y:F4},{wb.size.z:F4}) maxExtent={maxExtent:F4}m → localScale={autoScale:F5} (target {targetMeters}m)");
        }
        else
        {
            spawnedModel.transform.localScale = Vector3.one * cfg.scale;
        }

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
        Debug.Log($"[WeaponHolder] Spawned '{prefab.name}' — {renderers.Length} renderer(s), worldPos={spawnedModel.transform.position}");
    }
}
