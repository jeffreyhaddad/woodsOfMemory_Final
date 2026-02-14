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

    [Header("Skybox")]
    [Tooltip("Enable procedural skybox color changes")]
    public bool enableSkybox = true;
    public Gradient skyTintGradient;
    public AnimationCurve skyExposureCurve;
    public AnimationCurve skyAtmosphereThickness;

    [Header("Stars")]
    [Tooltip("Enable star particles at night")]
    public bool enableStars = true;
    private ParticleSystem starParticles;
    private Material skyboxMat;

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

        // Initialize skybox gradients/curves if not set (Reset() only runs in Editor)
        InitSkyboxDefaults();

        if (enableSkybox)
            SetupProceduralSkybox();
        if (enableStars)
            SetupStarParticles();
    }

    void InitSkyboxDefaults()
    {
        if (skyTintGradient == null || skyTintGradient.colorKeys.Length <= 1)
        {
            skyTintGradient = new Gradient();
            skyTintGradient.SetKeys(
                new GradientColorKey[]
                {
                    new GradientColorKey(new Color(0.02f, 0.02f, 0.08f), 0.0f),
                    new GradientColorKey(new Color(0.02f, 0.02f, 0.08f), 0.2f),
                    new GradientColorKey(new Color(0.7f, 0.4f, 0.2f), 0.25f),
                    new GradientColorKey(new Color(0.5f, 0.65f, 0.85f), 0.5f),
                    new GradientColorKey(new Color(0.8f, 0.35f, 0.15f), 0.75f),
                    new GradientColorKey(new Color(0.15f, 0.05f, 0.2f), 0.82f),
                    new GradientColorKey(new Color(0.02f, 0.02f, 0.08f), 0.88f),
                    new GradientColorKey(new Color(0.02f, 0.02f, 0.08f), 1.0f),
                },
                new GradientAlphaKey[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, 1f) }
            );
        }

        if (skyExposureCurve == null || skyExposureCurve.length == 0)
        {
            skyExposureCurve = new AnimationCurve(
                new Keyframe(0.0f, 0.2f), new Keyframe(0.2f, 0.2f),
                new Keyframe(0.25f, 0.8f), new Keyframe(0.5f, 1.3f),
                new Keyframe(0.75f, 0.8f), new Keyframe(0.82f, 0.2f),
                new Keyframe(1.0f, 0.2f)
            );
        }

        if (skyAtmosphereThickness == null || skyAtmosphereThickness.length == 0)
        {
            skyAtmosphereThickness = new AnimationCurve(
                new Keyframe(0.0f, 0.4f), new Keyframe(0.2f, 0.4f),
                new Keyframe(0.25f, 1.5f), new Keyframe(0.35f, 0.8f),
                new Keyframe(0.5f, 0.7f), new Keyframe(0.7f, 0.8f),
                new Keyframe(0.75f, 1.5f), new Keyframe(0.82f, 0.4f),
                new Keyframe(1.0f, 0.4f)
            );
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
        if (enableSkybox) UpdateSkybox();
        if (enableStars) UpdateStars();
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

        float baseDensity = fogDensityCurve.Evaluate(timeOfDay);

        // Weather system multiplies fog density
        if (WeatherManager.Instance != null)
            baseDensity *= WeatherManager.Instance.FogMultiplier;

        RenderSettings.fogDensity = baseDensity;
        RenderSettings.fogColor = fogColorGradient.Evaluate(timeOfDay);
    }

    // ─── Skybox ─────────────────────────────────────────────

    void SetupProceduralSkybox()
    {
        // Try to reuse the existing skybox if it's already procedural
        Material existing = RenderSettings.skybox;
        if (existing != null && existing.shader != null && existing.shader.name == "Skybox/Procedural")
        {
            // Clone it so we don't modify the asset
            skyboxMat = new Material(existing);
            RenderSettings.skybox = skyboxMat;
            Debug.Log("DayNightCycle: Using existing procedural skybox.");
            return;
        }

        // If the scene has any other skybox (6-sided, cubemap, gradient, etc.), leave it alone
        if (existing != null)
        {
            Debug.Log("DayNightCycle: Scene has a non-procedural skybox (" + existing.shader.name + "). Keeping it.");
            enableSkybox = false;
            return;
        }

        // No skybox at all — create a procedural one
        Shader skyShader = Shader.Find("Skybox/Procedural");
        if (skyShader == null)
        {
            Debug.LogWarning("DayNightCycle: Skybox/Procedural shader not found. Skybox updates disabled.");
            enableSkybox = false;
            return;
        }

        skyboxMat = new Material(skyShader);
        skyboxMat.SetFloat("_SunSize", 0.04f);
        skyboxMat.SetFloat("_SunSizeConvergence", 5f);
        RenderSettings.skybox = skyboxMat;
        Debug.Log("DayNightCycle: Created new procedural skybox.");
    }

    void UpdateSkybox()
    {
        if (skyboxMat == null) return;

        Color tint = skyTintGradient.Evaluate(timeOfDay);
        skyboxMat.SetColor("_SkyTint", tint);
        skyboxMat.SetFloat("_Exposure", skyExposureCurve.Evaluate(timeOfDay));
        skyboxMat.SetFloat("_AtmosphereThickness", skyAtmosphereThickness.Evaluate(timeOfDay));

        // Ground color follows the fog color for cohesion
        skyboxMat.SetColor("_GroundColor", fogColorGradient.Evaluate(timeOfDay) * 0.5f);
    }

    // ─── Stars ──────────────────────────────────────────────

    void SetupStarParticles()
    {
        // Find a working particle shader (URP or built-in)
        Shader particleShader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
        if (particleShader == null) particleShader = Shader.Find("Particles/Standard Unlit");
        if (particleShader == null)
        {
            Debug.LogWarning("DayNightCycle: No particle shader found. Stars disabled.");
            enableStars = false;
            return;
        }

        GameObject starObj = new GameObject("Stars");
        starObj.transform.SetParent(transform);
        starObj.transform.localPosition = Vector3.zero;

        starParticles = starObj.AddComponent<ParticleSystem>();
        ParticleSystem.MainModule main = starParticles.main;
        main.maxParticles = 200;
        main.startLifetime = 9999f;
        main.startSpeed = 0f;
        main.startSize = new ParticleSystem.MinMaxCurve(0.4f, 1.0f);
        main.startColor = new Color(1f, 1f, 0.95f, 0.9f);
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.playOnAwake = false;
        main.loop = false;

        // Shape: large hemisphere above the player
        ParticleSystem.ShapeModule shape = starParticles.shape;
        shape.shapeType = ParticleSystemShapeType.Hemisphere;
        shape.radius = 300f;
        shape.radiusThickness = 0f; // Surface only

        // No continuous emission — we burst them in
        ParticleSystem.EmissionModule emission = starParticles.emission;
        emission.rateOverTime = 0;

        // Renderer
        ParticleSystemRenderer rend = starObj.GetComponent<ParticleSystemRenderer>();
        rend.renderMode = ParticleSystemRenderMode.Billboard;
        Material starMat = new Material(particleShader);
        starMat.SetColor("_BaseColor", Color.white);
        // Additive blending for glow
        starMat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        starMat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.One);
        starMat.SetFloat("_Surface", 1); // Transparent
        starMat.SetOverrideTag("RenderType", "Transparent");
        starMat.renderQueue = 3000;
        rend.material = starMat;

        // Emit initial stars
        EmitStars();
    }

    void EmitStars()
    {
        if (starParticles == null) return;

        // Get player position for centering
        PlayerVitals pv = FindAnyObjectByType<PlayerVitals>();
        Vector3 center = pv != null ? pv.transform.position : Vector3.zero;
        center.y += 50f;

        starParticles.transform.position = center;
        starParticles.Emit(200);
    }

    void UpdateStars()
    {
        if (starParticles == null) return;

        // Stars visible at night, fade during dawn/dusk
        float starAlpha = 0f;
        if (timeOfDay < 0.2f) // midnight to pre-dawn
            starAlpha = 1f;
        else if (timeOfDay < 0.28f) // dawn fade out
            starAlpha = 1f - ((timeOfDay - 0.2f) / 0.08f);
        else if (timeOfDay > 0.8f) // dusk fade in
            starAlpha = (timeOfDay - 0.8f) / 0.08f;
        else
            starAlpha = 0f;

        starAlpha = Mathf.Clamp01(starAlpha);

        // Move star dome to follow player
        PlayerVitals pv = FindAnyObjectByType<PlayerVitals>();
        if (pv != null)
        {
            Vector3 center = pv.transform.position;
            center.y += 50f;
            starParticles.transform.position = center;
        }

        // Re-emit if particles died
        if (starAlpha > 0f && starParticles.particleCount < 50)
            EmitStars();

        // Control visibility via renderer
        ParticleSystemRenderer rend = starParticles.GetComponent<ParticleSystemRenderer>();
        if (rend != null && rend.material != null)
        {
            // Try URP property first, fall back to built-in
            if (rend.material.HasProperty("_BaseColor"))
            {
                Color c = rend.material.GetColor("_BaseColor");
                c.a = starAlpha;
                rend.material.SetColor("_BaseColor", c);
            }
            else
            {
                Color c = rend.material.color;
                c.a = starAlpha;
                rend.material.color = c;
            }
        }

        // Hide completely during day for performance
        starParticles.gameObject.SetActive(starAlpha > 0.01f);
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

        // --- Sky Tint Gradient ---
        skyTintGradient = new Gradient();
        skyTintGradient.SetKeys(
            new GradientColorKey[]
            {
                new GradientColorKey(new Color(0.02f, 0.02f, 0.08f), 0.0f),  // deep night
                new GradientColorKey(new Color(0.02f, 0.02f, 0.08f), 0.2f),
                new GradientColorKey(new Color(0.7f, 0.4f, 0.2f), 0.25f),    // sunrise orange
                new GradientColorKey(new Color(0.4f, 0.55f, 0.75f), 0.35f),  // morning blue
                new GradientColorKey(new Color(0.5f, 0.65f, 0.85f), 0.5f),   // noon blue
                new GradientColorKey(new Color(0.4f, 0.55f, 0.75f), 0.65f),  // afternoon
                new GradientColorKey(new Color(0.8f, 0.35f, 0.15f), 0.75f),  // sunset
                new GradientColorKey(new Color(0.15f, 0.05f, 0.2f), 0.82f),  // dusk purple
                new GradientColorKey(new Color(0.02f, 0.02f, 0.08f), 0.88f), // night
                new GradientColorKey(new Color(0.02f, 0.02f, 0.08f), 1.0f),
            },
            new GradientAlphaKey[]
            {
                new GradientAlphaKey(1f, 0f),
                new GradientAlphaKey(1f, 1f),
            }
        );

        // --- Sky Exposure Curve ---
        skyExposureCurve = new AnimationCurve(
            new Keyframe(0.0f, 0.2f),    // dark night
            new Keyframe(0.2f, 0.2f),
            new Keyframe(0.25f, 0.8f),   // sunrise
            new Keyframe(0.5f, 1.3f),    // noon bright
            new Keyframe(0.75f, 0.8f),   // sunset
            new Keyframe(0.82f, 0.2f),   // dusk
            new Keyframe(1.0f, 0.2f)
        );

        // --- Atmosphere Thickness Curve ---
        skyAtmosphereThickness = new AnimationCurve(
            new Keyframe(0.0f, 0.4f),    // thin at night
            new Keyframe(0.2f, 0.4f),
            new Keyframe(0.25f, 1.5f),   // thick at sunrise (warm colors)
            new Keyframe(0.35f, 0.8f),   // normal
            new Keyframe(0.5f, 0.7f),    // noon
            new Keyframe(0.7f, 0.8f),
            new Keyframe(0.75f, 1.5f),   // thick at sunset
            new Keyframe(0.82f, 0.4f),
            new Keyframe(1.0f, 0.4f)
        );
    }
}
