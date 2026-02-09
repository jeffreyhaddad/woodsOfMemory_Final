using UnityEngine;

public class DayNightCycle : MonoBehaviour
{
    [Header("Cycle Settings")]
    [Tooltip("Length of a full day in real-time minutes")]
    public float dayLengthInMinutes = 20f;

    [Range(0f, 1f)]
    [Tooltip("Starting time of day (0 = midnight, 0.25 = 6AM, 0.5 = noon, 0.75 = 6PM)")]
    public float startTimeOfDay = 0.3f;

    [Header("Sun")]
    [Tooltip("Drag the scene's Directional Light here")]
    public Light directionalLight;

    [Tooltip("Sun intensity over the day cycle (x-axis: 0-1 time, y-axis: intensity)")]
    public AnimationCurve sunIntensityCurve;

    [Tooltip("Sun color over the day cycle")]
    public Gradient sunColorGradient;

    [Header("Ambient Light")]
    public Gradient ambientSkyGradient;
    public Gradient ambientEquatorGradient;
    public Gradient ambientGroundGradient;

    [Header("Fog")]
    public bool enableFog = true;

    [Tooltip("Fog density over the day cycle")]
    public AnimationCurve fogDensityCurve;

    public Gradient fogColorGradient;

    [Range(0f, 1f)]
    [Tooltip("Current time of day (0 = midnight, 0.5 = noon)")]
    [SerializeField]
    private float timeOfDay;

    /// <summary>Current time of day as 0-1 (0 = midnight, 0.5 = noon).</summary>
    public float TimeOfDay
    {
        get => timeOfDay;
        set => timeOfDay = Mathf.Repeat(value, 1f);
    }

    /// <summary>Current time as a 0-24 hour value.</summary>
    public float CurrentHour => timeOfDay * 24f;

    /// <summary>True when the sun is below the horizon (roughly 8PM to 5AM).</summary>
    public bool IsNight => timeOfDay < 0.21f || timeOfDay > 0.83f;

    void Start()
    {
        timeOfDay = startTimeOfDay;

        if (directionalLight == null)
        {
            Light[] lights = FindObjectsByType<Light>(FindObjectsSortMode.None);
            foreach (Light l in lights)
            {
                if (l.type == LightType.Directional)
                {
                    directionalLight = l;
                    break;
                }
            }
        }
    }

    void Update()
    {
        // Advance time
        timeOfDay += Time.deltaTime / (dayLengthInMinutes * 60f);
        if (timeOfDay >= 1f)
            timeOfDay -= 1f;

        UpdateSun();
        UpdateAmbientLight();
        UpdateFog();
    }

    void UpdateSun()
    {
        if (directionalLight == null) return;

        // Rotate sun: timeOfDay 0 = midnight (sun at -90°), 0.5 = noon (sun at 90°)
        float sunAngle = (timeOfDay * 360f) - 90f;
        directionalLight.transform.rotation = Quaternion.Euler(sunAngle, -30f, 0f);

        // Intensity and color from curves/gradients
        directionalLight.intensity = sunIntensityCurve.Evaluate(timeOfDay);
        directionalLight.color = sunColorGradient.Evaluate(timeOfDay);
    }

    void UpdateAmbientLight()
    {
        RenderSettings.ambientSkyColor = ambientSkyGradient.Evaluate(timeOfDay);
        RenderSettings.ambientEquatorColor = ambientEquatorGradient.Evaluate(timeOfDay);
        RenderSettings.ambientGroundColor = ambientGroundGradient.Evaluate(timeOfDay);
    }

    void UpdateFog()
    {
        RenderSettings.fog = enableFog;

        if (!enableFog) return;

        RenderSettings.fogDensity = fogDensityCurve.Evaluate(timeOfDay);
        RenderSettings.fogColor = fogColorGradient.Evaluate(timeOfDay);
    }

    // Provide sensible defaults when the component is first added in the Editor
    void Reset()
    {
        dayLengthInMinutes = 20f;
        startTimeOfDay = 0.3f;
        enableFog = true;

        // --- Sun Intensity Curve ---
        // Night=0, dawn ramp, day peak at 2.0, dusk ramp, night=0
        sunIntensityCurve = new AnimationCurve(
            new Keyframe(0.0f, 0f),    // midnight
            new Keyframe(0.2f, 0f),    // before dawn
            new Keyframe(0.25f, 0.5f), // sunrise
            new Keyframe(0.35f, 1.5f), // mid-morning
            new Keyframe(0.5f, 2.0f),  // noon peak
            new Keyframe(0.65f, 1.5f), // mid-afternoon
            new Keyframe(0.75f, 0.5f), // sunset
            new Keyframe(0.8f, 0f),    // after dusk
            new Keyframe(1.0f, 0f)     // midnight
        );

        // --- Sun Color Gradient ---
        // midnight blue → dawn orange → daylight white → dusk orange → night blue
        sunColorGradient = new Gradient();
        sunColorGradient.SetKeys(
            new GradientColorKey[]
            {
                new GradientColorKey(new Color(0.1f, 0.1f, 0.3f), 0.0f),   // midnight blue
                new GradientColorKey(new Color(1.0f, 0.55f, 0.2f), 0.25f), // sunrise orange
                new GradientColorKey(new Color(1.0f, 0.95f, 0.85f), 0.35f),// morning warm white
                new GradientColorKey(new Color(1.0f, 1.0f, 1.0f), 0.5f),   // noon white
                new GradientColorKey(new Color(1.0f, 0.95f, 0.85f), 0.65f),// afternoon warm
                new GradientColorKey(new Color(1.0f, 0.4f, 0.15f), 0.75f), // sunset deep orange
                new GradientColorKey(new Color(0.1f, 0.1f, 0.3f), 0.82f),  // dusk blue
                new GradientColorKey(new Color(0.1f, 0.1f, 0.3f), 1.0f),   // midnight blue
            },
            new GradientAlphaKey[]
            {
                new GradientAlphaKey(1f, 0f),
                new GradientAlphaKey(1f, 1f),
            }
        );

        // --- Ambient Sky Gradient ---
        ambientSkyGradient = new Gradient();
        ambientSkyGradient.SetKeys(
            new GradientColorKey[]
            {
                new GradientColorKey(new Color(0.02f, 0.02f, 0.05f), 0.0f), // dark night
                new GradientColorKey(new Color(0.02f, 0.02f, 0.05f), 0.2f),
                new GradientColorKey(new Color(0.4f, 0.3f, 0.2f), 0.25f),   // dawn
                new GradientColorKey(new Color(0.21f, 0.23f, 0.26f), 0.5f), // day
                new GradientColorKey(new Color(0.4f, 0.25f, 0.15f), 0.75f), // dusk
                new GradientColorKey(new Color(0.02f, 0.02f, 0.05f), 0.8f),
                new GradientColorKey(new Color(0.02f, 0.02f, 0.05f), 1.0f),
            },
            new GradientAlphaKey[]
            {
                new GradientAlphaKey(1f, 0f),
                new GradientAlphaKey(1f, 1f),
            }
        );

        // --- Ambient Equator Gradient ---
        ambientEquatorGradient = new Gradient();
        ambientEquatorGradient.SetKeys(
            new GradientColorKey[]
            {
                new GradientColorKey(new Color(0.01f, 0.01f, 0.03f), 0.0f),
                new GradientColorKey(new Color(0.01f, 0.01f, 0.03f), 0.2f),
                new GradientColorKey(new Color(0.3f, 0.2f, 0.15f), 0.25f),
                new GradientColorKey(new Color(0.11f, 0.13f, 0.13f), 0.5f),
                new GradientColorKey(new Color(0.3f, 0.15f, 0.1f), 0.75f),
                new GradientColorKey(new Color(0.01f, 0.01f, 0.03f), 0.8f),
                new GradientColorKey(new Color(0.01f, 0.01f, 0.03f), 1.0f),
            },
            new GradientAlphaKey[]
            {
                new GradientAlphaKey(1f, 0f),
                new GradientAlphaKey(1f, 1f),
            }
        );

        // --- Ambient Ground Gradient ---
        ambientGroundGradient = new Gradient();
        ambientGroundGradient.SetKeys(
            new GradientColorKey[]
            {
                new GradientColorKey(new Color(0.01f, 0.01f, 0.01f), 0.0f),
                new GradientColorKey(new Color(0.01f, 0.01f, 0.01f), 0.2f),
                new GradientColorKey(new Color(0.15f, 0.1f, 0.05f), 0.25f),
                new GradientColorKey(new Color(0.05f, 0.04f, 0.04f), 0.5f),
                new GradientColorKey(new Color(0.15f, 0.08f, 0.05f), 0.75f),
                new GradientColorKey(new Color(0.01f, 0.01f, 0.01f), 0.8f),
                new GradientColorKey(new Color(0.01f, 0.01f, 0.01f), 1.0f),
            },
            new GradientAlphaKey[]
            {
                new GradientAlphaKey(1f, 0f),
                new GradientAlphaKey(1f, 1f),
            }
        );

        // --- Fog Density Curve ---
        // Thicker at night, thin during day
        fogDensityCurve = new AnimationCurve(
            new Keyframe(0.0f, 0.03f),   // midnight fog
            new Keyframe(0.2f, 0.03f),   // pre-dawn fog
            new Keyframe(0.3f, 0.005f),  // morning clears
            new Keyframe(0.5f, 0.002f),  // noon clear
            new Keyframe(0.7f, 0.005f),  // afternoon haze
            new Keyframe(0.8f, 0.03f),   // evening fog
            new Keyframe(1.0f, 0.03f)    // midnight fog
        );

        // --- Fog Color Gradient ---
        fogColorGradient = new Gradient();
        fogColorGradient.SetKeys(
            new GradientColorKey[]
            {
                new GradientColorKey(new Color(0.05f, 0.05f, 0.1f), 0.0f),  // night blue-grey
                new GradientColorKey(new Color(0.05f, 0.05f, 0.1f), 0.2f),
                new GradientColorKey(new Color(0.6f, 0.5f, 0.4f), 0.25f),   // dawn warm
                new GradientColorKey(new Color(0.7f, 0.75f, 0.8f), 0.5f),   // day light grey
                new GradientColorKey(new Color(0.5f, 0.35f, 0.25f), 0.75f), // dusk warm
                new GradientColorKey(new Color(0.05f, 0.05f, 0.1f), 0.8f),
                new GradientColorKey(new Color(0.05f, 0.05f, 0.1f), 1.0f),
            },
            new GradientAlphaKey[]
            {
                new GradientAlphaKey(1f, 0f),
                new GradientAlphaKey(1f, 1f),
            }
        );
    }
}
