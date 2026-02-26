using UnityEngine;

/// <summary>
/// Activates a warm lantern light on the player when a Lantern is equipped.
/// Auto-created by GameManager. The 3D model is handled by WeaponHolder.
/// </summary>
public class LanternLight : MonoBehaviour
{
    [Header("Light Settings")]
    public Color lanternColor   = new Color(1f, 0.85f, 0.5f);
    public float baseIntensity  = 5.0f;
    public float lightRange     = 25f;

    [Header("Flicker")]
    public float flickerSpeed  = 5f;
    public float flickerAmount = 0.15f;

    private GameObject lanternLightObj;
    private Light      lanternLight;
    private bool       lanternActive;

    void Start()
    {
        if (EquipmentManager.Instance != null)
            EquipmentManager.Instance.OnEquipmentChanged += OnEquipmentChanged;
        OnEquipmentChanged();
    }

    void OnDestroy()
    {
        if (EquipmentManager.Instance != null)
            EquipmentManager.Instance.OnEquipmentChanged -= OnEquipmentChanged;
    }

    void Update()
    {
        if (!lanternActive || lanternLight == null) return;

        float noise = Mathf.PerlinNoise(Time.time * flickerSpeed, 1f);
        lanternLight.intensity = baseIntensity + (noise - 0.5f) * flickerAmount * 2f;

        if (lanternLightObj != null)
        {
            lanternLightObj.transform.position = transform.position
                + transform.right   * 0.4f
                + transform.forward * 0.3f
                + Vector3.up        * 1.6f;
        }
    }

    void OnEquipmentChanged()
    {
        if (EquipmentManager.Instance == null) return;

        bool shouldBeActive = EquipmentManager.Instance.EquippedTool != null
            && EquipmentManager.Instance.EquippedTool.itemName
                .IndexOf("Lantern", System.StringComparison.OrdinalIgnoreCase) >= 0;

        if (shouldBeActive && !lanternActive)   EnableLantern();
        else if (!shouldBeActive && lanternActive) DisableLantern();
    }

    void EnableLantern()
    {
        lanternActive = true;

        lanternLightObj = new GameObject("PlayerLanternLight");
        lanternLightObj.transform.position = transform.position + Vector3.up * 1.6f;

        lanternLight               = lanternLightObj.AddComponent<Light>();
        lanternLight.type          = LightType.Point;
        lanternLight.color         = lanternColor;
        lanternLight.intensity     = baseIntensity;
        lanternLight.range         = lightRange;
        lanternLight.shadows       = LightShadows.Soft;
        lanternLight.shadowStrength = 0.5f;
    }

    void DisableLantern()
    {
        lanternActive = false;
        if (lanternLightObj != null) Destroy(lanternLightObj);
        lanternLight = null;
    }
}
