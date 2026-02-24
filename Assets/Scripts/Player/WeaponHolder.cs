using UnityEngine;

/// <summary>
/// Spawns the equipped tool/weapon model in the player's right hand.
/// Auto-created by GameManager — no Inspector setup needed.
/// Models are loaded from Resources/Weapons/.
/// Adjust Position/Rotation Offset in the Inspector to tune the grip.
/// </summary>
public class WeaponHolder : MonoBehaviour
{
    [Header("Hand Offset (tweak after equipping)")]
    public Vector3 positionOffset = Vector3.zero;
    public Vector3 rotationOffset = Vector3.zero;

    private Transform rightHandBone;
    private GameObject spawnedModel;

    void Start()
    {
        rightHandBone = FindHumanoidHandBone();

        if (rightHandBone == null)
            Debug.LogWarning("[WeaponHolder] Could not find right hand bone.");
        else
            Debug.Log($"[WeaponHolder] Hand bone found: {rightHandBone.name}");

        if (EquipmentManager.Instance != null)
            EquipmentManager.Instance.OnEquipmentChanged += Refresh;

        Refresh();
    }

    void OnDestroy()
    {
        if (EquipmentManager.Instance != null)
            EquipmentManager.Instance.OnEquipmentChanged -= Refresh;
    }

    /// <summary>
    /// Finds the right hand bone by iterating all Animator components in the
    /// hierarchy and using the one that has a valid Humanoid avatar.
    /// Falls back to a deep name search if no Humanoid animator is found.
    /// </summary>
    Transform FindHumanoidHandBone()
    {
        // Check every Animator in the hierarchy — find the humanoid one
        foreach (Animator anim in GetComponentsInChildren<Animator>(true))
        {
            if (!anim.isHuman) continue;

            Transform bone = anim.GetBoneTransform(HumanBodyBones.RightHand);
            if (bone != null) return bone;
        }

        // Fallback: deep name search for common Mixamo / Unity bone names
        string[] candidates = {
            "mixamorig:RightHand",
            "RightHand",
            "Hand_R",
            "Bip001 R Hand",
            "mixamorig:RightHandMiddle1"
        };
        foreach (string boneName in candidates)
        {
            Transform found = DeepFind(transform, boneName);
            if (found != null)
            {
                Debug.Log($"[WeaponHolder] Found hand via name search: {boneName}");
                return found;
            }
        }

        return null;
    }

    static Transform DeepFind(Transform root, string name)
    {
        if (root.name == name) return root;
        foreach (Transform child in root)
        {
            Transform result = DeepFind(child, name);
            if (result != null) return result;
        }
        return null;
    }

    void Refresh()
    {
        if (spawnedModel != null)
        {
            Destroy(spawnedModel);
            spawnedModel = null;
        }

        if (rightHandBone == null || EquipmentManager.Instance == null) return;

        string toolName   = EquipmentManager.Instance.EquippedTool?.itemName   ?? "";
        string weaponName = EquipmentManager.Instance.EquippedWeapon?.itemName ?? "";

        string resourcePath = null;
        if (toolName.IndexOf("Pickaxe", System.StringComparison.OrdinalIgnoreCase) >= 0)
            resourcePath = "Weapons/Pickaxe";
        else if (toolName.IndexOf("Axe",   System.StringComparison.OrdinalIgnoreCase) >= 0
              || weaponName.IndexOf("Axe",  System.StringComparison.OrdinalIgnoreCase) >= 0)
            resourcePath = "Weapons/Axe";

        if (resourcePath == null) return;

        GameObject prefab = Resources.Load<GameObject>(resourcePath);
        if (prefab == null)
        {
            Debug.LogWarning($"[WeaponHolder] Missing Resources/{resourcePath}.fbx");
            return;
        }

        spawnedModel = Instantiate(prefab, rightHandBone);
        spawnedModel.transform.localPosition    = positionOffset;
        spawnedModel.transform.localEulerAngles = rotationOffset;

        foreach (var col in spawnedModel.GetComponentsInChildren<Collider>())
            col.enabled = false;

        Debug.Log($"[WeaponHolder] Equipped {prefab.name} in hand.");
    }
}
