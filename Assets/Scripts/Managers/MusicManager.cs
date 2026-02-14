using UnityEngine;

/// <summary>
/// Dynamic procedural music system that responds to gameplay tension.
/// Layers: calm exploration drone, night tension, combat intensity, danger pulse.
/// All audio generated procedurally — no audio assets needed.
/// Auto-created by GameManager.
/// </summary>
public class MusicManager : MonoBehaviour
{
    public static MusicManager Instance { get; private set; }

    [Header("Volume")]
    [Range(0f, 1f)] public float masterVolume = 0.25f;

    [Header("Layer Volumes")]
    [Range(0f, 1f)] public float dayLayerVolume = 0.2f;
    [Range(0f, 1f)] public float nightLayerVolume = 0.25f;
    [Range(0f, 1f)] public float combatLayerVolume = 0.3f;
    [Range(0f, 1f)] public float dangerLayerVolume = 0.2f;

    [Header("Crossfade")]
    public float fadeSpeed = 0.5f;

    // Audio sources for each layer
    private AudioSource daySource;
    private AudioSource nightSource;
    private AudioSource combatSource;
    private AudioSource dangerSource;

    // Target volumes (smoothly interpolated)
    private float dayTarget;
    private float nightTarget;
    private float combatTarget;
    private float dangerTarget;

    // Cached references
    private DayNightCycle dayNight;
    private PlayerVitals vitals;

    // Combat detection
    private float combatCheckTimer;
    private float combatCooldown;
    private bool inCombat;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    void Start()
    {
        dayNight = FindAnyObjectByType<DayNightCycle>();
        vitals = FindAnyObjectByType<PlayerVitals>();

        daySource = CreateLoopSource();
        nightSource = CreateLoopSource();
        combatSource = CreateLoopSource();
        dangerSource = CreateLoopSource();

        GenerateAllClips();

        daySource.Play();
        nightSource.Play();
        combatSource.Play();
        dangerSource.Play();
    }

    void Update()
    {
        UpdateTargets();
        FadeVolumes();
    }

    void UpdateTargets()
    {
        bool isNight = dayNight != null && dayNight.IsNight;
        float timeOfDay = dayNight != null ? dayNight.TimeOfDay : 0.5f;
        float healthPct = (vitals != null) ? vitals.Health / vitals.maxHealth : 1f;

        // Day layer: full during day, silent at night
        // Smooth transition during dawn/dusk
        float dayAmount;
        if (timeOfDay >= 0.25f && timeOfDay <= 0.75f)
            dayAmount = 1f; // full day
        else if (timeOfDay > 0.75f && timeOfDay <= 0.85f)
            dayAmount = 1f - (timeOfDay - 0.75f) / 0.1f; // dusk fade
        else if (timeOfDay >= 0.18f && timeOfDay < 0.25f)
            dayAmount = (timeOfDay - 0.18f) / 0.07f; // dawn fade in
        else
            dayAmount = 0f; // night
        dayTarget = dayAmount * dayLayerVolume;

        // Night layer: inverse of day
        float nightAmount = 1f - dayAmount;
        nightTarget = nightAmount * nightLayerVolume;

        // Combat: check for nearby hostile creatures in chase/attack state
        combatCheckTimer -= Time.deltaTime;
        if (combatCheckTimer <= 0f)
        {
            combatCheckTimer = 1f; // check every second
            inCombat = CheckCombatNearby();
        }

        if (inCombat)
        {
            combatTarget = combatLayerVolume;
            combatCooldown = 5f; // stay intense for 5s after combat ends
        }
        else
        {
            combatCooldown -= Time.deltaTime;
            if (combatCooldown <= 0f)
                combatTarget = 0f;
        }

        // Danger layer: low health warning (below 35%)
        if (healthPct < 0.35f)
            dangerTarget = dangerLayerVolume * (1f - healthPct / 0.35f);
        else
            dangerTarget = 0f;
    }

    bool CheckCombatNearby()
    {
        if (vitals == null) return false;

        // Find any shadow creatures in chase or attack state nearby
        CreatureAI[] creatures = FindObjectsByType<CreatureAI>(FindObjectsSortMode.None);
        Vector3 playerPos = vitals.transform.position;

        for (int i = 0; i < creatures.Length; i++)
        {
            if (creatures[i] == null) continue;

            // Use reflection-free approach: check if creature is a shadow creature
            // and is close enough to the player
            ShadowCreatureAI shadow = creatures[i] as ShadowCreatureAI;
            if (shadow == null) continue;

            float dist = Vector3.Distance(creatures[i].transform.position, playerPos);
            if (dist < 25f)
                return true;
        }

        return false;
    }

    void FadeVolumes()
    {
        float dt = Time.deltaTime * fadeSpeed;
        daySource.volume = Mathf.MoveTowards(daySource.volume, dayTarget * masterVolume, dt);
        nightSource.volume = Mathf.MoveTowards(nightSource.volume, nightTarget * masterVolume, dt);
        combatSource.volume = Mathf.MoveTowards(combatSource.volume, combatTarget * masterVolume, dt);
        dangerSource.volume = Mathf.MoveTowards(dangerSource.volume, dangerTarget * masterVolume, dt);
    }

    AudioSource CreateLoopSource()
    {
        AudioSource src = gameObject.AddComponent<AudioSource>();
        src.playOnAwake = false;
        src.loop = true;
        src.spatialBlend = 0f;
        src.volume = 0f;
        return src;
    }

    // ─── Procedural Music Generation ────────────────────────

    void GenerateAllClips()
    {
        int sampleRate = 22050; // lower rate is fine for ambient music

        daySource.clip = GenerateDayClip(sampleRate);
        nightSource.clip = GenerateNightClip(sampleRate);
        combatSource.clip = GenerateCombatClip(sampleRate);
        dangerSource.clip = GenerateDangerClip(sampleRate);
    }

    /// <summary>
    /// Day music: warm, peaceful ambient pad.
    /// Layered sine waves in a major pentatonic scale with slow volume swells.
    /// </summary>
    AudioClip GenerateDayClip(int sampleRate)
    {
        float duration = 16f;
        int length = (int)(sampleRate * duration);
        float[] samples = new float[length];

        // C major pentatonic frequencies (octave 3-4, Hz)
        float[] notes = { 130.81f, 146.83f, 164.81f, 196.00f, 220.00f, 261.63f, 293.66f };

        // Layer warm tones
        for (int i = 0; i < length; i++)
        {
            float t = (float)i / sampleRate;
            float tNorm = (float)i / length;

            // Base drone: C3 with soft overtone
            float drone = Mathf.Sin(2f * Mathf.PI * 130.81f * t) * 0.15f;
            drone += Mathf.Sin(2f * Mathf.PI * 196.00f * t) * 0.08f; // perfect fifth

            // Slow evolving pad: cycle through pentatonic notes
            float padPhase = t * 0.15f; // very slow
            int noteIdx = (int)(padPhase * notes.Length) % notes.Length;
            float nextIdx = (noteIdx + 1) % notes.Length;
            float blend = (padPhase * notes.Length) % 1f;
            float padFreq = Mathf.Lerp(notes[noteIdx], notes[(int)nextIdx], blend * blend);
            float padEnv = 0.5f + 0.5f * Mathf.Sin(2f * Mathf.PI * 0.08f * t); // slow swell
            float pad = Mathf.Sin(2f * Mathf.PI * padFreq * t) * 0.1f * padEnv;

            // High shimmer — gentle octave harmonics
            float shimmer = Mathf.Sin(2f * Mathf.PI * 523.25f * t) * 0.03f
                * (0.5f + 0.5f * Mathf.Sin(2f * Mathf.PI * 0.12f * t));

            // Breathy texture (filtered noise)
            float noise = Mathf.PerlinNoise(t * 3f, 0.5f) * 2f - 1f;
            float breath = noise * 0.02f * (0.5f + 0.5f * Mathf.Sin(2f * Mathf.PI * 0.05f * t));

            // Seamless loop: crossfade last 2 seconds
            float loopFade = 1f;
            float fadeTime = 2f / duration;
            if (tNorm > 1f - fadeTime)
                loopFade = (1f - tNorm) / fadeTime;
            if (tNorm < fadeTime)
                loopFade = Mathf.Min(loopFade, tNorm / fadeTime);

            samples[i] = (drone + pad + shimmer + breath) * loopFade;
        }

        return MakeClip("DayMusic", samples, sampleRate);
    }

    /// <summary>
    /// Night music: dark, eerie ambient drone.
    /// Minor key, low frequencies, unsettling detuned intervals.
    /// </summary>
    AudioClip GenerateNightClip(int sampleRate)
    {
        float duration = 20f;
        int length = (int)(sampleRate * duration);
        float[] samples = new float[length];

        for (int i = 0; i < length; i++)
        {
            float t = (float)i / sampleRate;
            float tNorm = (float)i / length;

            // Deep bass drone: C2 with minor third
            float bass = Mathf.Sin(2f * Mathf.PI * 65.41f * t) * 0.18f;
            float minor3 = Mathf.Sin(2f * Mathf.PI * 77.78f * t) * 0.08f; // Eb2

            // Slowly detuning dissonant tone — creates unease
            float detune = Mathf.Sin(2f * Mathf.PI * 0.03f * t) * 2f; // +/- 2Hz wobble
            float eerieFreq = 155.56f + detune; // Eb3 with wobble
            float eerie = Mathf.Sin(2f * Mathf.PI * eerieFreq * t) * 0.06f;

            // Dark pad: minor 7th chord tones fading in and out
            float padSwell = 0.5f + 0.5f * Mathf.Sin(2f * Mathf.PI * 0.04f * t);
            float pad = Mathf.Sin(2f * Mathf.PI * 116.54f * t) * 0.05f * padSwell; // Bb2
            pad += Mathf.Sin(2f * Mathf.PI * 92.50f * t) * 0.04f * (1f - padSwell); // Gb2-ish

            // Wind-like noise texture
            float windNoise = Mathf.PerlinNoise(t * 1.5f, 10f) * 2f - 1f;
            float wind = windNoise * 0.04f * (0.3f + 0.7f * Mathf.Sin(2f * Mathf.PI * 0.02f * t));

            // Occasional eerie high pitch (like distant whistle)
            float whistleEnv = Mathf.Exp(-Mathf.Pow(Mathf.Sin(2f * Mathf.PI * 0.07f * t) - 0.95f, 2f) * 50f);
            float whistle = Mathf.Sin(2f * Mathf.PI * 880f * t) * 0.02f * whistleEnv;

            // Seamless loop fade
            float loopFade = 1f;
            float fadeTime = 2.5f / duration;
            if (tNorm > 1f - fadeTime)
                loopFade = (1f - tNorm) / fadeTime;
            if (tNorm < fadeTime)
                loopFade = Mathf.Min(loopFade, tNorm / fadeTime);

            samples[i] = (bass + minor3 + eerie + pad + wind + whistle) * loopFade;
        }

        return MakeClip("NightMusic", samples, sampleRate);
    }

    /// <summary>
    /// Combat music: intense, rhythmic, percussive.
    /// Pounding bass hits, aggressive tones, driving pulse.
    /// </summary>
    AudioClip GenerateCombatClip(int sampleRate)
    {
        float duration = 8f;
        int length = (int)(sampleRate * duration);
        float[] samples = new float[length];

        float bpm = 130f;
        float beatLen = 60f / bpm;

        for (int i = 0; i < length; i++)
        {
            float t = (float)i / sampleRate;
            float tNorm = (float)i / length;

            float beatPos = (t % beatLen) / beatLen; // 0-1 within each beat

            // Pounding kick drum on every beat
            float kickEnv = Mathf.Exp(-beatPos * 20f);
            float kickFreq = Mathf.Lerp(150f, 50f, beatPos); // pitch drop
            float kick = Mathf.Sin(2f * Mathf.PI * kickFreq * t) * kickEnv * 0.3f;

            // Sub bass following the kick
            float subBass = Mathf.Sin(2f * Mathf.PI * 55f * t) * 0.12f
                * (0.6f + 0.4f * kickEnv);

            // Aggressive saw-ish tone on offbeats
            float halfBeatPos = (t % (beatLen * 2f)) / (beatLen * 2f);
            float offbeatEnv = 0f;
            if (halfBeatPos > 0.45f && halfBeatPos < 0.55f)
                offbeatEnv = 1f - Mathf.Abs(halfBeatPos - 0.5f) / 0.05f;
            // Approximate sawtooth with harmonics
            float saw = 0f;
            float aggFreq = 110f; // A2
            for (int h = 1; h <= 5; h++)
                saw += Mathf.Sin(2f * Mathf.PI * aggFreq * h * t) / h;
            saw *= 0.08f * offbeatEnv;

            // Tension drone: tritone interval (devil's interval)
            float droneSwell = 0.3f + 0.7f * (0.5f + 0.5f * Mathf.Sin(2f * Mathf.PI * 0.25f * t));
            float tensionDrone = Mathf.Sin(2f * Mathf.PI * 82.41f * t) * 0.06f * droneSwell; // E2
            tensionDrone += Mathf.Sin(2f * Mathf.PI * 116.54f * t) * 0.04f * droneSwell; // Bb2

            // Hi-hat on every half beat
            float hihatPos = (t % (beatLen * 0.5f)) / (beatLen * 0.5f);
            float hihat = (Mathf.PerlinNoise(t * 500f, 0f) * 2f - 1f) * Mathf.Exp(-hihatPos * 30f) * 0.06f;

            // Seamless loop
            float loopFade = 1f;
            float fadeTime = 0.5f / duration;
            if (tNorm > 1f - fadeTime)
                loopFade = (1f - tNorm) / fadeTime;
            if (tNorm < fadeTime)
                loopFade = Mathf.Min(loopFade, tNorm / fadeTime);

            samples[i] = (kick + subBass + saw + tensionDrone + hihat) * loopFade;
        }

        return MakeClip("CombatMusic", samples, sampleRate);
    }

    /// <summary>
    /// Danger/low health: slow pulsing drone, oppressive.
    /// Heartbeat-timed bass thuds with dissonant tones.
    /// </summary>
    AudioClip GenerateDangerClip(int sampleRate)
    {
        float duration = 4f;
        int length = (int)(sampleRate * duration);
        float[] samples = new float[length];

        for (int i = 0; i < length; i++)
        {
            float t = (float)i / sampleRate;
            float tNorm = (float)i / length;

            // Heartbeat rhythm: lub-dub every ~0.8 seconds
            float beatCycle = t % 0.85f;
            float lub = Mathf.Sin(2f * Mathf.PI * 45f * t)
                * Mathf.Exp(-Mathf.Pow((beatCycle - 0.05f) * 15f, 2f)) * 0.25f;
            float dub = Mathf.Sin(2f * Mathf.PI * 38f * t)
                * Mathf.Exp(-Mathf.Pow((beatCycle - 0.2f) * 15f, 2f)) * 0.18f;

            // Oppressive low drone
            float drone = Mathf.Sin(2f * Mathf.PI * 55f * t) * 0.1f;
            drone += Mathf.Sin(2f * Mathf.PI * 58f * t) * 0.06f; // slight detune for discomfort

            // Breathing texture
            float breathRate = 0.4f; // slow breath
            float breathEnv = 0.5f + 0.5f * Mathf.Sin(2f * Mathf.PI * breathRate * t);
            float breathNoise = Mathf.PerlinNoise(t * 5f, 20f) * 2f - 1f;
            float breath = breathNoise * 0.03f * breathEnv;

            // Seamless loop
            float loopFade = 1f;
            float fadeTime = 0.5f / duration;
            if (tNorm > 1f - fadeTime)
                loopFade = (1f - tNorm) / fadeTime;
            if (tNorm < fadeTime)
                loopFade = Mathf.Min(loopFade, tNorm / fadeTime);

            samples[i] = (lub + dub + drone + breath) * loopFade;
        }

        return MakeClip("DangerMusic", samples, sampleRate);
    }

    AudioClip MakeClip(string name, float[] samples, int sampleRate)
    {
        // Normalize to prevent clipping
        float peak = 0f;
        for (int i = 0; i < samples.Length; i++)
        {
            float abs = Mathf.Abs(samples[i]);
            if (abs > peak) peak = abs;
        }
        if (peak > 0.9f)
        {
            float scale = 0.9f / peak;
            for (int i = 0; i < samples.Length; i++)
                samples[i] *= scale;
        }

        AudioClip clip = AudioClip.Create(name, samples.Length, 1, sampleRate, false);
        clip.SetData(samples, 0);
        return clip;
    }
}
