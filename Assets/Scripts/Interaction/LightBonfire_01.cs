using UnityEngine;

/// <summary>
/// Controls the bonfire_01 model: enables fire, smoke, and light effects on start
/// and applies a natural flickering effect to the fire light.
/// Children expected: "Fire Effect", "SmokeEffect", "FireLight"
/// </summary>
public class LightBonfire_01 : MonoBehaviour
{
    [Header("Flicker Settings")]
    public float baseIntensity = 3f;
    public float flickerSpeed = 8f;
    public float flickerAmount = 0.6f;

    private GameObject fireEffect;
    private GameObject smokeEffect;
    private GameObject fireLightObj;
    private Light fireLight;
    private float flickerOffset;

    void Awake()
    {
        fireEffect  = transform.Find("Fire Effect")?.gameObject;
        smokeEffect = transform.Find("SmokeEffect")?.gameObject;
        fireLightObj = transform.Find("FireLight")?.gameObject;

        if (fireLightObj != null)
            fireLight = fireLightObj.GetComponent<Light>();

        flickerOffset = Random.Range(0f, 100f);
    }

    void Start()
    {
        if (fireEffect != null)  fireEffect.SetActive(true);
        if (smokeEffect != null) smokeEffect.SetActive(true);
        if (fireLightObj != null) fireLightObj.SetActive(true);
    }

    void Update()
    {
        if (fireLight == null) return;

        float noise = Mathf.PerlinNoise(Time.time * flickerSpeed + flickerOffset, flickerOffset);
        fireLight.intensity = baseIntensity + (noise - 0.5f) * flickerAmount * 2f;
    }
}
