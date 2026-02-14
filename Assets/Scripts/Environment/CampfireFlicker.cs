using UnityEngine;

/// <summary>
/// Simple light flicker for campfires. Attach to a GameObject with a Light component.
/// </summary>
public class CampfireFlicker : MonoBehaviour
{
    public float baseIntensity = 2.5f;
    public float flickerSpeed = 6f;
    public float flickerAmount = 0.5f;

    private Light cachedLight;
    private float offset;

    void Start()
    {
        cachedLight = GetComponent<Light>();
        offset = Random.Range(0f, 100f);
    }

    void Update()
    {
        if (cachedLight == null) return;
        float noise = Mathf.PerlinNoise(Time.time * flickerSpeed + offset, offset);
        cachedLight.intensity = baseIntensity + (noise - 0.5f) * flickerAmount * 2f;
    }
}
