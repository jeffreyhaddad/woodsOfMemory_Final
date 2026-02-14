using UnityEngine;

public enum WeatherState
{
    Clear,
    Foggy,
    Rainy
}

/// <summary>
/// Dynamic weather system that cycles through clear, foggy, and rainy states.
/// Creates rain particles and adjusts fog density. Integrates with DayNightCycle.
/// Auto-creates everything at runtime — no editor setup needed.
/// </summary>
public class WeatherManager : MonoBehaviour
{
    public static WeatherManager Instance { get; private set; }

    [Header("Weather Timing")]
    [Tooltip("Minimum seconds before weather can change")]
    public float minWeatherDuration = 120f;
    [Tooltip("Maximum seconds before weather changes")]
    public float maxWeatherDuration = 300f;
    [Tooltip("How long the transition between weather states takes")]
    public float transitionDuration = 15f;

    [Header("Probabilities")]
    [Range(0f, 1f)] public float clearChance = 0.5f;
    [Range(0f, 1f)] public float foggyChance = 0.3f;
    [Range(0f, 1f)] public float rainyChance = 0.2f;

    [Header("Fog Multipliers")]
    public float clearFogMultiplier = 1f;
    public float foggyFogMultiplier = 4f;
    public float rainyFogMultiplier = 2.5f;

    [Header("Rain Settings")]
    public int rainParticleCount = 800;
    public float rainAreaSize = 40f;
    public float rainHeight = 25f;
    public float rainSpeed = 15f;

    public WeatherState CurrentWeather { get; private set; } = WeatherState.Clear;
    public float FogMultiplier { get; private set; } = 1f;
    public bool IsRaining => CurrentWeather == WeatherState.Rainy;

    private float weatherTimer;
    private float transitionTimer;
    private WeatherState targetWeather;
    private float startFogMult;
    private float targetFogMult;
    private bool transitioning;

    // Rain
    private ParticleSystem rainParticles;
    private Transform playerTransform;
    private AudioSource rainAudio;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    void Start()
    {
        PlayerVitals pv = FindAnyObjectByType<PlayerVitals>();
        if (pv != null) playerTransform = pv.transform;

        CreateRainSystem();
        CreateRainAudio();

        // Start with clear weather
        CurrentWeather = WeatherState.Clear;
        FogMultiplier = clearFogMultiplier;
        weatherTimer = Random.Range(minWeatherDuration, maxWeatherDuration);
        SetRainActive(false);
    }

    void Update()
    {
        // Weather change timer
        weatherTimer -= Time.deltaTime;
        if (weatherTimer <= 0f && !transitioning)
        {
            WeatherState next = PickNextWeather();
            if (next != CurrentWeather)
                StartTransition(next);
            weatherTimer = Random.Range(minWeatherDuration, maxWeatherDuration);
        }

        // Smooth transition
        if (transitioning)
        {
            transitionTimer -= Time.deltaTime;
            float t = 1f - Mathf.Clamp01(transitionTimer / transitionDuration);
            FogMultiplier = Mathf.Lerp(startFogMult, targetFogMult, t);

            // Fade rain in/out during transition
            if (targetWeather == WeatherState.Rainy && t > 0.3f && !rainParticles.isPlaying)
                SetRainActive(true);
            if (targetWeather != WeatherState.Rainy && CurrentWeather == WeatherState.Rainy && t > 0.5f)
                SetRainActive(false);

            if (transitionTimer <= 0f)
            {
                transitioning = false;
                CurrentWeather = targetWeather;
                FogMultiplier = targetFogMult;

                if (CurrentWeather != WeatherState.Rainy)
                    SetRainActive(false);
            }
        }

        // Follow player with rain
        if (rainParticles != null && playerTransform != null)
        {
            Vector3 pos = playerTransform.position;
            pos.y += rainHeight;
            rainParticles.transform.position = pos;
        }

        // Update rain audio volume
        if (rainAudio != null)
        {
            float targetVol = (CurrentWeather == WeatherState.Rainy || (transitioning && targetWeather == WeatherState.Rainy)) ? 0.3f : 0f;
            rainAudio.volume = Mathf.MoveTowards(rainAudio.volume, targetVol, Time.deltaTime * 0.5f);
        }
    }

    WeatherState PickNextWeather()
    {
        float total = clearChance + foggyChance + rainyChance;
        float roll = Random.Range(0f, total);

        if (roll < clearChance) return WeatherState.Clear;
        if (roll < clearChance + foggyChance) return WeatherState.Foggy;
        return WeatherState.Rainy;
    }

    void StartTransition(WeatherState next)
    {
        targetWeather = next;
        transitioning = true;
        transitionTimer = transitionDuration;
        startFogMult = FogMultiplier;

        switch (next)
        {
            case WeatherState.Clear: targetFogMult = clearFogMultiplier; break;
            case WeatherState.Foggy: targetFogMult = foggyFogMultiplier; break;
            case WeatherState.Rainy: targetFogMult = rainyFogMultiplier; break;
        }

        Debug.Log("Weather changing to: " + next);
    }

    void SetRainActive(bool active)
    {
        if (rainParticles == null) return;

        if (active && !rainParticles.isPlaying)
            rainParticles.Play();
        else if (!active && rainParticles.isPlaying)
            rainParticles.Stop();
    }

    // ─── Rain Particle System ─────────────────────────────────

    void CreateRainSystem()
    {
        GameObject rainObj = new GameObject("RainParticles");
        rainObj.transform.SetParent(transform);

        rainParticles = rainObj.AddComponent<ParticleSystem>();

        ParticleSystem.MainModule main = rainParticles.main;
        main.maxParticles = rainParticleCount;
        main.startLifetime = rainHeight / rainSpeed;
        main.startSpeed = rainSpeed;
        main.startSize = new ParticleSystem.MinMaxCurve(0.02f, 0.04f);
        main.startColor = new Color(0.7f, 0.75f, 0.85f, 0.4f);
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.gravityModifier = 0f; // We set speed directly
        main.playOnAwake = false;

        // Shape: box above the player
        ParticleSystem.ShapeModule shape = rainParticles.shape;
        shape.shapeType = ParticleSystemShapeType.Box;
        shape.scale = new Vector3(rainAreaSize, 0.1f, rainAreaSize);
        shape.rotation = new Vector3(0, 0, 180); // Emit downward

        // Emission
        ParticleSystem.EmissionModule emission = rainParticles.emission;
        emission.rateOverTime = rainParticleCount * 2;

        // Stretch particles to look like rain streaks
        ParticleSystemRenderer rend = rainObj.GetComponent<ParticleSystemRenderer>();
        rend.renderMode = ParticleSystemRenderMode.Stretch;
        rend.lengthScale = 8f;
        rend.velocityScale = 0.05f;

        // URP-compatible particle material
        Shader particleShader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
        if (particleShader == null) particleShader = Shader.Find("Particles/Standard Unlit");
        if (particleShader != null)
        {
            Material rainMat = new Material(particleShader);
            rainMat.SetColor("_BaseColor", new Color(0.7f, 0.75f, 0.85f, 0.3f));
            rainMat.SetFloat("_Surface", 1); // Transparent
            rainMat.SetOverrideTag("RenderType", "Transparent");
            rainMat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            rainMat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            rainMat.renderQueue = 3000;
            rend.material = rainMat;
        }

        rainParticles.Stop();
    }

    void CreateRainAudio()
    {
        rainAudio = gameObject.AddComponent<AudioSource>();
        rainAudio.loop = true;
        rainAudio.playOnAwake = false;
        rainAudio.spatialBlend = 0f;
        rainAudio.volume = 0f;

        // Generate procedural rain noise
        int sampleRate = 22050;
        int length = sampleRate * 4; // 4 second loop
        AudioClip clip = AudioClip.Create("RainNoise", length, 1, sampleRate, false);
        float[] samples = new float[length];

        // Brown noise filtered to sound like rain
        float lastSample = 0f;
        for (int i = 0; i < length; i++)
        {
            float white = Random.Range(-1f, 1f);
            lastSample = (lastSample + 0.02f * white) / 1.02f;
            // Add occasional patter (random spikes)
            float patter = (Random.value > 0.995f) ? Random.Range(0.1f, 0.3f) : 0f;
            samples[i] = Mathf.Clamp(lastSample * 3f + patter, -1f, 1f);
        }

        clip.SetData(samples, 0);
        rainAudio.clip = clip;
        rainAudio.Play();
    }
}
