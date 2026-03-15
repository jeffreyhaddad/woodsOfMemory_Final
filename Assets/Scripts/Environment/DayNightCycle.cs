using UnityEngine;
using UnityEngine.UI;

public class DayNightCycle : MonoBehaviour
{
    [Header("Cycle Settings")]
    [Tooltip("Length of a full day in real-time minutes")]
    public float dayLengthInMinutes = 20f;

    [Range(0f, 1f)]
    [Tooltip("Starting time of day (0 = midnight, 0.25 = 6AM, 0.5 = noon, 0.75 = 6PM)")]
    public float startTimeOfDay = 0f;

    [Tooltip("Automatically advance time each frame")]
    public bool autoAdvanceTime = false;

    [Header("Sun")]
    [Tooltip("Drag the scene's Directional Light here")]
    public Light directionalLight;

    [Tooltip("Sun intensity over the day cycle (x-axis: 0-1 time, y-axis: intensity)")]
    public AnimationCurve sunIntensityCurve;

    [Tooltip("Sun color over the day cycle")]
    public Gradient sunColorGradient;

    [Header("Moon")]
    [Tooltip("Enable moon visual and moonlight")]
    public bool enableMoon = true;

    [Tooltip("Optional second Directional Light for moonlight. Leave empty to auto-create one.")]
    public Light moonLight;

    [Tooltip("Moon light intensity over the night cycle")]
    public AnimationCurve moonIntensityCurve;

    [Tooltip("Moon light color")]
    public Gradient moonColorGradient;

    [Tooltip("Visual size of the moon disc in the sky")]
    [Range(1f, 30f)]
    public float moonSize = 8f;

    [Tooltip("How far the moon billboard is placed from the player")]
    public float moonDistance = 450f;

    [Header("Night")]
    [Range(0f, 1f)]
    [Tooltip("Extra darkness multiplier at night. Scales ambient light down on top of the gradients.")]
    public float nightDarknessBoost = 0.55f;

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
    [Tooltip("Replace scene skybox with a procedural skybox driven by the cycle")]
    public bool enableSkybox = true;
    public Gradient skyTintGradient;
    public AnimationCurve skyExposureCurve;
    public AnimationCurve skyAtmosphereThickness;

    [Header("Clouds")]
    [Tooltip("Enable scrolling cloud texture layer")]
    public bool enableClouds = true;

    [Tooltip("Cloud texture with alpha channel (white clouds, transparent background). Assign in Inspector.")]
    public Texture2D cloudTexture;

    [Range(0f, 1f)]
    [Tooltip("Cloud opacity. Lower = wispier, higher = dense.")]
    public float cloudOpacity = 0.88f;

    [Tooltip("Height above player where the cloud layer sits")]
    public float cloudHeight = 80f;

    [Tooltip("UV scroll speed for each layer. X = horizontal, Y = depth direction.")]
    public Vector2 cloudScrollSpeed = new Vector2(0.003f, 0.001f);

    public Color cloudDayColor = new Color(1f, 1f, 1f, 1f);
    public Color cloudNightColor = new Color(0.12f, 0.14f, 0.22f, 1f);

    [Header("Stars")]
    [Tooltip("Enable star particles at night")]
    public bool enableStars = true;
    private ParticleSystem starParticles;
    private ParticleSystemRenderer starRenderer;
    private Material skyboxMat;

    // Cloud runtime objects (two scrolling planes for depth)
    private GameObject cloudLayer1, cloudLayer2;
    private Material cloudMat1, cloudMat2;
    private static readonly int _BaseMap = Shader.PropertyToID("_BaseMap");

    // Moon runtime objects
    private GameObject moonBillboard;
    private Renderer moonRenderer;
    private Material moonMat;

    // Cached references
    private Transform cachedPlayerTransform;
    private WeatherManager cachedWeather;

    // Cached shader property IDs (avoid string hash lookups every frame)
    private static readonly int _SkyTint = Shader.PropertyToID("_SkyTint");
    private static readonly int _Exposure = Shader.PropertyToID("_Exposure");
    private static readonly int _AtmosphereThickness = Shader.PropertyToID("_AtmosphereThickness");
    private static readonly int _GroundColor = Shader.PropertyToID("_GroundColor");
    private static readonly int _BaseColor = Shader.PropertyToID("_BaseColor");

    // Throttle counter for expensive updates
    private int updateCounter;

    // Night atmosphere overlay
    private Image nightOverlay;
    private bool  wasNight;

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

        InitSkyboxDefaults();
        InitMoonDefaults();

        PlayerVitals pv = FindAnyObjectByType<PlayerVitals>();
        if (pv != null) cachedPlayerTransform = pv.transform;
        cachedWeather = WeatherManager.Instance;

        if (enableSkybox)
            SetupProceduralSkybox();
        if (enableMoon)
            SetupMoon();
        if (enableClouds)
            SetupClouds();
        if (enableStars)
            SetupStarParticles();

        SetupNightAtmosphere();
    }

    void Update()
    {
        if (autoAdvanceTime)
            timeOfDay = Mathf.Repeat(timeOfDay + Time.deltaTime / (dayLengthInMinutes * 60f), 1f);

        updateCounter++;

        UpdateSun();
        // Throttle slow-changing visuals to every 3rd frame
        if (updateCounter % 3 == 0)
        {
            UpdateAmbientLight();
            UpdateFog();
            if (enableSkybox) UpdateSkybox();
        }
        if (enableMoon) UpdateMoon();
        if (enableClouds) UpdateClouds();
        if (enableStars) UpdateStars();

        UpdateNightAtmosphere();
    }

    // ─── Night Atmosphere ────────────────────────────────────

    void SetupNightAtmosphere()
    {
        GameObject canvasObj = new GameObject("NightAtmosphereCanvas");
        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode   = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 5; // behind all game UI
        CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode         = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);

        // Subtle dark-blue tint that fades in at night
        GameObject overlayObj = new GameObject("NightOverlay");
        overlayObj.transform.SetParent(canvasObj.transform, false);
        nightOverlay = overlayObj.AddComponent<Image>();
        nightOverlay.color = new Color(0.04f, 0.04f, 0.18f, 0f);
        nightOverlay.raycastTarget = false;
        RectTransform ort = nightOverlay.rectTransform;
        ort.anchorMin = Vector2.zero;
        ort.anchorMax = Vector2.one;
        ort.offsetMin = ort.offsetMax = Vector2.zero;

        wasNight = IsNight;
    }

    void UpdateNightAtmosphere()
    {
        if (nightOverlay == null) return;

        // Dark-blue tint scales with how far into night we are
        Color oc = nightOverlay.color;
        oc.a = NightBlend() * 0.20f;
        nightOverlay.color = oc;
    }

    void UpdateSun()
    {
        if (directionalLight == null) return;

        // Rotate sun: timeOfDay 0 = midnight (sun at -90°), 0.5 = noon (sun at 90°)
        float sunAngle = (timeOfDay * 360f) - 90f;
        directionalLight.transform.rotation = Quaternion.Euler(sunAngle, -30f, 0f);

        directionalLight.intensity = sunIntensityCurve.Evaluate(timeOfDay);
        directionalLight.color = sunColorGradient.Evaluate(timeOfDay);
    }

    // Returns 0 during full day, 1 during deep night, smoothly transitioning at dusk/dawn.
    float NightBlend()
    {
        if (timeOfDay < 0.18f || timeOfDay > 0.85f) return 1f;
        if (timeOfDay < 0.25f) return 1f - (timeOfDay - 0.18f) / 0.07f;
        if (timeOfDay > 0.78f) return (timeOfDay - 0.78f) / 0.07f;
        return 0f;
    }

    void UpdateAmbientLight()
    {
        float dimFactor = 1f - NightBlend() * nightDarknessBoost;

        RenderSettings.ambientSkyColor = ambientSkyGradient.Evaluate(timeOfDay) * dimFactor;
        RenderSettings.ambientEquatorColor = ambientEquatorGradient.Evaluate(timeOfDay) * dimFactor;
        RenderSettings.ambientGroundColor = ambientGroundGradient.Evaluate(timeOfDay) * dimFactor;
    }

    void UpdateFog()
    {
        if (RenderSettings.fog != enableFog)
            RenderSettings.fog = enableFog;

        if (!enableFog) return;

        float baseDensity = fogDensityCurve.Evaluate(timeOfDay);

        if (cachedWeather == null) cachedWeather = WeatherManager.Instance;
        if (cachedWeather != null)
            baseDensity *= cachedWeather.FogMultiplier;

        RenderSettings.fogDensity = baseDensity;
        RenderSettings.fogColor = fogColorGradient.Evaluate(timeOfDay);
    }

    // ─── Skybox ─────────────────────────────────────────────

    void SetupProceduralSkybox()
    {
        Material existing = RenderSettings.skybox;

        // Reuse if already procedural — just clone it so we don't modify the asset
        if (existing != null && existing.shader != null && existing.shader.name == "Skybox/Procedural")
        {
            skyboxMat = new Material(existing);
            RenderSettings.skybox = skyboxMat;
            Debug.Log("DayNightCycle: Reusing existing procedural skybox.");
            return;
        }

        // For any other skybox (including image-based ones), replace with procedural.
        // This allows the day/night cycle to fully control sky appearance.
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

        if (existing != null)
            Debug.Log($"DayNightCycle: Replaced '{existing.shader.name}' skybox with procedural skybox.");
        else
            Debug.Log("DayNightCycle: Created new procedural skybox.");
    }

    void UpdateSkybox()
    {
        if (skyboxMat == null) return;

        skyboxMat.SetColor(_SkyTint, skyTintGradient.Evaluate(timeOfDay));
        skyboxMat.SetFloat(_Exposure, skyExposureCurve.Evaluate(timeOfDay));
        skyboxMat.SetFloat(_AtmosphereThickness, skyAtmosphereThickness.Evaluate(timeOfDay));
        skyboxMat.SetColor(_GroundColor, fogColorGradient.Evaluate(timeOfDay) * 0.5f);
    }

    // ─── Moon ────────────────────────────────────────────────

    void InitMoonDefaults()
    {
        if (moonIntensityCurve == null || moonIntensityCurve.length == 0)
        {
            moonIntensityCurve = new AnimationCurve(
                new Keyframe(0.00f, 0.18f),  // midnight
                new Keyframe(0.10f, 0.22f),  // mid-night peak
                new Keyframe(0.20f, 0.08f),  // fading before dawn
                new Keyframe(0.25f, 0.00f),  // dawn — moon off
                new Keyframe(0.75f, 0.00f),  // dusk — moon off
                new Keyframe(0.80f, 0.08f),  // evening rising
                new Keyframe(0.90f, 0.22f),
                new Keyframe(1.00f, 0.18f)
            );
        }

        if (moonColorGradient == null || moonColorGradient.colorKeys.Length <= 1)
        {
            moonColorGradient = new Gradient();
            moonColorGradient.SetKeys(
                new GradientColorKey[]
                {
                    new GradientColorKey(new Color(0.6f, 0.7f, 0.9f), 0.0f),
                    new GradientColorKey(new Color(0.6f, 0.7f, 0.9f), 1.0f),
                },
                new GradientAlphaKey[]
                {
                    new GradientAlphaKey(1f, 0f),
                    new GradientAlphaKey(1f, 1f),
                }
            );
        }
    }

    void SetupMoon()
    {
        // Create moonlight directional light if not assigned
        if (moonLight == null)
        {
            GameObject moonLightObj = new GameObject("MoonLight");
            moonLightObj.transform.SetParent(transform);
            moonLight = moonLightObj.AddComponent<Light>();
            moonLight.type = LightType.Directional;
            moonLight.shadows = LightShadows.None; // no moon shadows for performance
            moonLight.color = new Color(0.6f, 0.7f, 0.9f);
            moonLight.intensity = 0f;
        }

        // Create moon visual as a sphere primitive
        moonBillboard = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        moonBillboard.name = "MoonBillboard";
        Destroy(moonBillboard.GetComponent<Collider>());

        moonRenderer = moonBillboard.GetComponent<Renderer>();

        // Use an unlit shader so the moon glows without needing a light on it
        Shader unlitShader = Shader.Find("Universal Render Pipeline/Unlit");
        if (unlitShader == null) unlitShader = Shader.Find("Unlit/Color");

        if (unlitShader != null)
        {
            moonMat = new Material(unlitShader);
            moonMat.color = new Color(0.92f, 0.95f, 1.0f);
            moonRenderer.material = moonMat;
        }

        moonBillboard.transform.localScale = Vector3.one * moonSize;
    }

    void UpdateMoon()
    {
        if (moonBillboard == null) return;

        // Moon orbits opposite to the sun (180° offset, same axis tilt)
        float moonAngle = (timeOfDay * 360f) - 90f + 180f;
        Quaternion moonRot = Quaternion.Euler(moonAngle, -30f, 0f);
        Vector3 moonDir = moonRot * Vector3.forward;

        // Place moon billboard far away in the moon direction
        Vector3 origin = cachedPlayerTransform != null ? cachedPlayerTransform.position : Vector3.zero;
        moonBillboard.transform.position = origin + moonDir * moonDistance;
        moonBillboard.transform.LookAt(origin); // face player

        // Update moonlight direction and color
        if (moonLight != null)
        {
            moonLight.transform.rotation = moonRot;
            moonLight.intensity = moonIntensityCurve.Evaluate(timeOfDay);
            moonLight.color = moonColorGradient.Evaluate(timeOfDay);
        }

        // Compute moon brightness: full at night, fade during dawn/dusk, off during day
        float brightness;
        if (timeOfDay < 0.18f || timeOfDay > 0.82f)
            brightness = 1f;
        else if (timeOfDay < 0.25f)
            brightness = 1f - (timeOfDay - 0.18f) / 0.07f;
        else if (timeOfDay > 0.75f)
            brightness = (timeOfDay - 0.75f) / 0.07f;
        else
            brightness = 0f;

        brightness = Mathf.Clamp01(brightness);

        // Only render when above horizon and during night
        bool aboveHorizon = moonDir.y > -0.05f;
        bool shouldShow = brightness > 0.01f && aboveHorizon;
        moonBillboard.SetActive(shouldShow);

        if (shouldShow && moonMat != null)
        {
            Color c = new Color(0.92f, 0.95f, 1.0f) * brightness;
            c.a = 1f;
            moonMat.color = c;
        }
    }

    // ─── Clouds ─────────────────────────────────────────────

    void SetupClouds()
    {
        if (cloudTexture == null)
        {
            Debug.LogWarning("DayNightCycle: No cloud texture assigned — drag a cloud PNG into the Cloud Texture slot. Clouds disabled.");
            enableClouds = false;
            return;
        }

        Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
        if (shader == null) shader = Shader.Find("Unlit/Transparent");
        if (shader == null) { enableClouds = false; return; }

        // Layer 1 — primary clouds, closer, full opacity
        cloudLayer1 = CreateCloudPlane("CloudLayer1", shader, cloudHeight, 1200f, cloudOpacity);
        cloudMat1 = cloudLayer1.GetComponent<Renderer>().material;

        // Layer 2 — secondary detail, slightly higher, slower, less opaque
        cloudLayer2 = CreateCloudPlane("CloudLayer2", shader, cloudHeight + 18f, 1500f, cloudOpacity * 0.55f);
        cloudMat2 = cloudLayer2.GetComponent<Renderer>().material;
    }

    GameObject CreateCloudPlane(string objName, Shader shader, float height, float size, float opacity)
    {
        // Unity Plane primitive is 10×10 units — scale up to desired world size
        GameObject plane = GameObject.CreatePrimitive(PrimitiveType.Plane);
        plane.name = objName;
        plane.transform.SetParent(transform);
        Destroy(plane.GetComponent<Collider>());
        plane.transform.localScale = new Vector3(size / 10f, 1f, size / 10f);

        Vector3 spawnPos = cachedPlayerTransform != null ? cachedPlayerTransform.position : Vector3.zero;
        spawnPos.y = height;
        plane.transform.position = spawnPos;

        Material mat = new Material(shader);
        mat.SetTexture(_BaseMap, cloudTexture);

        // Alpha cutout: transparent pixels are discarded, opaque pixels render solid
        // This avoids blending artifacts where multiple layers intersect
        Color col = cloudDayColor;
        col.a = opacity;
        mat.SetColor(_BaseColor, col);
        mat.SetFloat("_Surface", 0f);       // Opaque render pass
        mat.SetFloat("_AlphaClip", 1f);     // Enable cutout
        mat.SetFloat("_Cutoff", 0.12f);     // Discard pixels below 12% alpha
        mat.EnableKeyword("_ALPHATEST_ON");
        mat.SetOverrideTag("RenderType", "TransparentCutout");
        mat.renderQueue = 2450;

        plane.GetComponent<Renderer>().material = mat;
        return plane;
    }

    void UpdateClouds()
    {
        if (cloudLayer1 == null) return;

        // Follow player horizontally so clouds always surround them
        Vector3 playerPos = cachedPlayerTransform != null ? cachedPlayerTransform.position : Vector3.zero;
        cloudLayer1.transform.position = new Vector3(playerPos.x, cloudHeight, playerPos.z);
        if (cloudLayer2 != null)
            cloudLayer2.transform.position = new Vector3(playerPos.x, cloudHeight + 18f, playerPos.z);

        // Scroll UVs — two layers move in slightly different directions for depth illusion
        float t = Time.time;
        cloudMat1.SetTextureOffset(_BaseMap, new Vector2(t * cloudScrollSpeed.x, t * cloudScrollSpeed.y));
        if (cloudMat2 != null)
            cloudMat2.SetTextureOffset(_BaseMap, new Vector2(-t * cloudScrollSpeed.x * 0.6f, t * cloudScrollSpeed.y * 1.4f));

        // Tint for day / night
        Color col = Color.Lerp(cloudDayColor, cloudNightColor, NightBlend());
        Color col2 = col; col2.a = cloudOpacity * 0.55f;
        col.a = cloudOpacity;
        cloudMat1.SetColor(_BaseColor, col);
        if (cloudMat2 != null) cloudMat2.SetColor(_BaseColor, col2);
    }

    // ─── Stars ──────────────────────────────────────────────

    void SetupStarParticles()
    {
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

        ParticleSystem.ShapeModule shape = starParticles.shape;
        shape.shapeType = ParticleSystemShapeType.Hemisphere;
        shape.radius = 300f;
        shape.radiusThickness = 0f;

        ParticleSystem.EmissionModule emission = starParticles.emission;
        emission.rateOverTime = 0;

        starRenderer = starObj.GetComponent<ParticleSystemRenderer>();
        starRenderer.renderMode = ParticleSystemRenderMode.Billboard;
        Material starMat = new Material(particleShader);
        starMat.SetColor(_BaseColor, Color.white);
        starMat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        starMat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.One);
        starMat.SetFloat("_Surface", 1);
        starMat.SetOverrideTag("RenderType", "Transparent");
        starMat.renderQueue = 3000;
        starRenderer.material = starMat;

        EmitStars();
    }

    void EmitStars()
    {
        if (starParticles == null) return;

        Vector3 center = cachedPlayerTransform != null ? cachedPlayerTransform.position : Vector3.zero;
        center.y += 50f;

        starParticles.transform.position = center;
        starParticles.Emit(200);
    }

    void UpdateStars()
    {
        if (starParticles == null) return;

        float starAlpha = 0f;
        if (timeOfDay < 0.2f)
            starAlpha = 1f;
        else if (timeOfDay < 0.28f)
            starAlpha = 1f - ((timeOfDay - 0.2f) / 0.08f);
        else if (timeOfDay > 0.8f)
            starAlpha = (timeOfDay - 0.8f) / 0.08f;

        starAlpha = Mathf.Clamp01(starAlpha);

        starParticles.gameObject.SetActive(starAlpha > 0.01f);
        if (starAlpha <= 0.01f) return;

        if (cachedPlayerTransform != null)
        {
            Vector3 center = cachedPlayerTransform.position;
            center.y += 50f;
            starParticles.transform.position = center;
        }

        if (starParticles.particleCount < 50)
            EmitStars();

        if (starRenderer != null && starRenderer.material != null)
        {
            if (starRenderer.material.HasProperty(_BaseColor))
            {
                Color c = starRenderer.material.GetColor(_BaseColor);
                c.a = starAlpha;
                starRenderer.material.SetColor(_BaseColor, c);
            }
            else
            {
                Color c = starRenderer.material.color;
                c.a = starAlpha;
                starRenderer.material.color = c;
            }
        }
    }

    // ─── Defaults ───────────────────────────────────────────

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

    void Reset()
    {
        dayLengthInMinutes = 20f;
        startTimeOfDay = 0f;
        autoAdvanceTime = false;
        enableFog = true;

        sunIntensityCurve = new AnimationCurve(
            new Keyframe(0.0f, 0f),
            new Keyframe(0.2f, 0f),
            new Keyframe(0.25f, 0.5f),
            new Keyframe(0.35f, 1.5f),
            new Keyframe(0.5f, 2.0f),
            new Keyframe(0.65f, 1.5f),
            new Keyframe(0.75f, 0.5f),
            new Keyframe(0.8f, 0f),
            new Keyframe(1.0f, 0f)
        );

        sunColorGradient = new Gradient();
        sunColorGradient.SetKeys(
            new GradientColorKey[]
            {
                new GradientColorKey(new Color(0.1f, 0.1f, 0.3f), 0.0f),
                new GradientColorKey(new Color(1.0f, 0.55f, 0.2f), 0.25f),
                new GradientColorKey(new Color(1.0f, 0.95f, 0.85f), 0.35f),
                new GradientColorKey(new Color(1.0f, 1.0f, 1.0f), 0.5f),
                new GradientColorKey(new Color(1.0f, 0.95f, 0.85f), 0.65f),
                new GradientColorKey(new Color(1.0f, 0.4f, 0.15f), 0.75f),
                new GradientColorKey(new Color(0.1f, 0.1f, 0.3f), 0.82f),
                new GradientColorKey(new Color(0.1f, 0.1f, 0.3f), 1.0f),
            },
            new GradientAlphaKey[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, 1f) }
        );

        ambientSkyGradient = new Gradient();
        ambientSkyGradient.SetKeys(
            new GradientColorKey[]
            {
                new GradientColorKey(new Color(0.02f, 0.02f, 0.05f), 0.0f),
                new GradientColorKey(new Color(0.02f, 0.02f, 0.05f), 0.2f),
                new GradientColorKey(new Color(0.4f, 0.3f, 0.2f), 0.25f),
                new GradientColorKey(new Color(0.21f, 0.23f, 0.26f), 0.5f),
                new GradientColorKey(new Color(0.4f, 0.25f, 0.15f), 0.75f),
                new GradientColorKey(new Color(0.02f, 0.02f, 0.05f), 0.8f),
                new GradientColorKey(new Color(0.02f, 0.02f, 0.05f), 1.0f),
            },
            new GradientAlphaKey[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, 1f) }
        );

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
            new GradientAlphaKey[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, 1f) }
        );

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
            new GradientAlphaKey[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, 1f) }
        );

        fogDensityCurve = new AnimationCurve(
            new Keyframe(0.0f, 0.03f),
            new Keyframe(0.2f, 0.03f),
            new Keyframe(0.3f, 0.005f),
            new Keyframe(0.5f, 0.002f),
            new Keyframe(0.7f, 0.005f),
            new Keyframe(0.8f, 0.03f),
            new Keyframe(1.0f, 0.03f)
        );

        fogColorGradient = new Gradient();
        fogColorGradient.SetKeys(
            new GradientColorKey[]
            {
                new GradientColorKey(new Color(0.05f, 0.05f, 0.1f), 0.0f),
                new GradientColorKey(new Color(0.05f, 0.05f, 0.1f), 0.2f),
                new GradientColorKey(new Color(0.6f, 0.5f, 0.4f), 0.25f),
                new GradientColorKey(new Color(0.7f, 0.75f, 0.8f), 0.5f),
                new GradientColorKey(new Color(0.5f, 0.35f, 0.25f), 0.75f),
                new GradientColorKey(new Color(0.05f, 0.05f, 0.1f), 0.8f),
                new GradientColorKey(new Color(0.05f, 0.05f, 0.1f), 1.0f),
            },
            new GradientAlphaKey[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, 1f) }
        );

        skyTintGradient = new Gradient();
        skyTintGradient.SetKeys(
            new GradientColorKey[]
            {
                new GradientColorKey(new Color(0.02f, 0.02f, 0.08f), 0.0f),
                new GradientColorKey(new Color(0.02f, 0.02f, 0.08f), 0.2f),
                new GradientColorKey(new Color(0.7f, 0.4f, 0.2f), 0.25f),
                new GradientColorKey(new Color(0.4f, 0.55f, 0.75f), 0.35f),
                new GradientColorKey(new Color(0.5f, 0.65f, 0.85f), 0.5f),
                new GradientColorKey(new Color(0.4f, 0.55f, 0.75f), 0.65f),
                new GradientColorKey(new Color(0.8f, 0.35f, 0.15f), 0.75f),
                new GradientColorKey(new Color(0.15f, 0.05f, 0.2f), 0.82f),
                new GradientColorKey(new Color(0.02f, 0.02f, 0.08f), 0.88f),
                new GradientColorKey(new Color(0.02f, 0.02f, 0.08f), 1.0f),
            },
            new GradientAlphaKey[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, 1f) }
        );

        skyExposureCurve = new AnimationCurve(
            new Keyframe(0.0f, 0.2f),
            new Keyframe(0.2f, 0.2f),
            new Keyframe(0.25f, 0.8f),
            new Keyframe(0.5f, 1.3f),
            new Keyframe(0.75f, 0.8f),
            new Keyframe(0.82f, 0.2f),
            new Keyframe(1.0f, 0.2f)
        );

        skyAtmosphereThickness = new AnimationCurve(
            new Keyframe(0.0f, 0.4f),
            new Keyframe(0.2f, 0.4f),
            new Keyframe(0.25f, 1.5f),
            new Keyframe(0.35f, 0.8f),
            new Keyframe(0.5f, 0.7f),
            new Keyframe(0.7f, 0.8f),
            new Keyframe(0.75f, 1.5f),
            new Keyframe(0.82f, 0.4f),
            new Keyframe(1.0f, 0.4f)
        );

        moonIntensityCurve = new AnimationCurve(
            new Keyframe(0.00f, 0.18f),
            new Keyframe(0.10f, 0.22f),
            new Keyframe(0.20f, 0.08f),
            new Keyframe(0.25f, 0.00f),
            new Keyframe(0.75f, 0.00f),
            new Keyframe(0.80f, 0.08f),
            new Keyframe(0.90f, 0.22f),
            new Keyframe(1.00f, 0.18f)
        );

        moonColorGradient = new Gradient();
        moonColorGradient.SetKeys(
            new GradientColorKey[]
            {
                new GradientColorKey(new Color(0.6f, 0.7f, 0.9f), 0.0f),
                new GradientColorKey(new Color(0.6f, 0.7f, 0.9f), 1.0f),
            },
            new GradientAlphaKey[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, 1f) }
        );
    }
}
