using UnityEngine;

public enum GroundType
{
    Default,
    Grass,
    Wood,
    Stone
}

/// <summary>
/// Centralized sound effects manager. Generates all sounds procedurally via AudioClip.Create
/// so no audio assets are required. Call static methods from anywhere.
/// </summary>
public class SFXManager : MonoBehaviour
{
    public static SFXManager Instance { get; private set; }

    [Header("Volume")]
    [Range(0f, 1f)] public float masterVolume = 0.5f;

    private AudioSource sfxSource;
    private AudioSource loopSource;

    // Cached clips (generated once)
    private AudioClip hitClip;
    private AudioClip swingClip;
    private AudioClip pickupClip;
    private AudioClip craftClip;
    private AudioClip equipClip;
    private AudioClip hurtClip;
    private AudioClip footstepClip;
    private AudioClip footstepGrassClip;
    private AudioClip footstepWoodClip;
    private AudioClip footstepStoneClip;
    private AudioClip menuClickClip;
    private AudioClip heartbeatClip;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(this); return; }
        Instance = this;

        sfxSource = gameObject.AddComponent<AudioSource>();
        sfxSource.playOnAwake = false;
        sfxSource.spatialBlend = 0f; // 2D sound

        loopSource = gameObject.AddComponent<AudioSource>();
        loopSource.playOnAwake = false;
        loopSource.spatialBlend = 0f;
        loopSource.loop = true;

        GenerateClips();
    }

    // ─── Public API ──────────────────────────────────────────

    public static void PlayHit()
    {
        if (Instance != null) Instance.Play(Instance.hitClip, 0.6f);
    }

    public static void PlaySwing()
    {
        if (Instance != null) Instance.Play(Instance.swingClip, 0.35f);
    }

    public static void PlayPickup()
    {
        if (Instance != null) Instance.Play(Instance.pickupClip, 0.5f);
    }

    public static void PlayCraft()
    {
        if (Instance != null) Instance.Play(Instance.craftClip, 0.55f);
    }

    public static void PlayEquip()
    {
        if (Instance != null) Instance.Play(Instance.equipClip, 0.45f);
    }

    public static void PlayHurt()
    {
        if (Instance != null) Instance.Play(Instance.hurtClip, 0.5f);
    }

    public static void PlayFootstep()
    {
        if (Instance != null) Instance.Play(Instance.footstepClip, 0.2f);
    }

    /// <summary>Play a terrain-specific footstep sound.</summary>
    public static void PlayFootstep(GroundType ground)
    {
        if (Instance == null) return;
        switch (ground)
        {
            case GroundType.Wood:
                Instance.Play(Instance.footstepWoodClip, 0.25f);
                break;
            case GroundType.Stone:
                Instance.Play(Instance.footstepStoneClip, 0.22f);
                break;
            case GroundType.Grass:
                Instance.Play(Instance.footstepGrassClip, 0.18f);
                break;
            default:
                Instance.Play(Instance.footstepClip, 0.2f);
                break;
        }
    }

    public static void PlayMenuClick()
    {
        if (Instance != null) Instance.Play(Instance.menuClickClip, 0.4f);
    }

    public static void StartHeartbeat()
    {
        if (Instance == null || Instance.loopSource.isPlaying) return;
        Instance.loopSource.clip = Instance.heartbeatClip;
        Instance.loopSource.volume = Instance.masterVolume * 0.4f;
        Instance.loopSource.Play();
    }

    public static void StopHeartbeat()
    {
        if (Instance != null) Instance.loopSource.Stop();
    }

    // ─── Internal ────────────────────────────────────────────

    void Play(AudioClip clip, float volume)
    {
        if (clip != null)
            sfxSource.PlayOneShot(clip, volume * masterVolume);
    }

    // ─── Procedural Audio Generation ─────────────────────────

    void GenerateClips()
    {
        int sampleRate = 44100;

        hitClip = CreateClip("Hit", sampleRate, 0.15f, (i, len) =>
        {
            float t = (float)i / len;
            float noise = (Random.value * 2f - 1f);
            float env = Mathf.Exp(-t * 20f);
            float thud = Mathf.Sin(2f * Mathf.PI * 120f * t) * Mathf.Exp(-t * 30f);
            return (noise * 0.6f + thud * 0.4f) * env;
        });

        swingClip = CreateClip("Swing", sampleRate, 0.18f, (i, len) =>
        {
            float t = (float)i / len;
            // Whoosh = filtered noise with volume swell then fade
            float env = Mathf.Sin(t * Mathf.PI) * Mathf.Exp(-t * 3f);
            float noise = Random.value * 2f - 1f;
            return noise * env * 0.5f;
        });

        pickupClip = CreateClip("Pickup", sampleRate, 0.15f, (i, len) =>
        {
            float t = (float)i / len;
            float env = (1f - t) * (1f - t);
            float f1 = Mathf.Sin(2f * Mathf.PI * 600f * t);
            float f2 = Mathf.Sin(2f * Mathf.PI * 900f * t);
            return (f1 * 0.5f + f2 * 0.5f) * env;
        });

        craftClip = CreateClip("Craft", sampleRate, 0.35f, (i, len) =>
        {
            float t = (float)i / len;
            float env = Mathf.Exp(-t * 5f);
            float clang = Mathf.Sin(2f * Mathf.PI * 400f * t) * Mathf.Exp(-t * 12f);
            float shimmer = Mathf.Sin(2f * Mathf.PI * 1200f * t) * Mathf.Exp(-t * 8f) * 0.3f;
            float rise = Mathf.Sin(2f * Mathf.PI * Mathf.Lerp(500f, 1000f, t) * t) * (t > 0.15f ? 1f : 0f) * 0.4f * Mathf.Exp(-(t - 0.15f) * 6f);
            return (clang + shimmer + rise) * env;
        });

        equipClip = CreateClip("Equip", sampleRate, 0.12f, (i, len) =>
        {
            float t = (float)i / len;
            float env = Mathf.Exp(-t * 15f);
            return Mathf.Sin(2f * Mathf.PI * 300f * t) * env;
        });

        hurtClip = CreateClip("Hurt", sampleRate, 0.2f, (i, len) =>
        {
            float t = (float)i / len;
            float env = Mathf.Exp(-t * 10f);
            float noise = (Random.value * 2f - 1f) * 0.5f;
            float tone = Mathf.Sin(2f * Mathf.PI * 180f * t);
            return (noise + tone) * env;
        });

        footstepClip = CreateClip("Footstep", sampleRate, 0.08f, (i, len) =>
        {
            float t = (float)i / len;
            float env = Mathf.Exp(-t * 30f);
            float noise = (Random.value * 2f - 1f);
            float thump = Mathf.Sin(2f * Mathf.PI * 80f * t);
            return (noise * 0.4f + thump * 0.6f) * env;
        });

        // Grass: soft rustling, higher frequency noise, lighter
        footstepGrassClip = CreateClip("FootstepGrass", sampleRate, 0.1f, (i, len) =>
        {
            float t = (float)i / len;
            float env = Mathf.Exp(-t * 20f);
            float noise = (Random.value * 2f - 1f);
            float rustle = Mathf.Sin(2f * Mathf.PI * 2000f * t) * 0.1f;
            return (noise * 0.7f + rustle) * env * 0.6f;
        });

        // Wood: hollow thud, mid-frequency resonance
        footstepWoodClip = CreateClip("FootstepWood", sampleRate, 0.1f, (i, len) =>
        {
            float t = (float)i / len;
            float env = Mathf.Exp(-t * 25f);
            float thud = Mathf.Sin(2f * Mathf.PI * 150f * t);
            float knock = Mathf.Sin(2f * Mathf.PI * 400f * t) * Mathf.Exp(-t * 40f);
            float noise = (Random.value * 2f - 1f) * 0.15f;
            return (thud * 0.5f + knock * 0.35f + noise) * env;
        });

        // Stone: sharp clack, high attack, less bass
        footstepStoneClip = CreateClip("FootstepStone", sampleRate, 0.06f, (i, len) =>
        {
            float t = (float)i / len;
            float env = Mathf.Exp(-t * 40f);
            float click = Mathf.Sin(2f * Mathf.PI * 600f * t) * Mathf.Exp(-t * 50f);
            float scrape = (Random.value * 2f - 1f) * 0.3f * Mathf.Exp(-t * 35f);
            float tap = Mathf.Sin(2f * Mathf.PI * 250f * t) * 0.4f;
            return (click + scrape + tap) * env;
        });

        menuClickClip = CreateClip("Click", sampleRate, 0.05f, (i, len) =>
        {
            float t = (float)i / len;
            return Mathf.Sin(2f * Mathf.PI * 1000f * t) * (1f - t);
        });

        heartbeatClip = CreateClip("Heartbeat", sampleRate, 1.0f, (i, len) =>
        {
            float t = (float)i / len;
            // Two thumps per beat (lub-dub)
            float beat1 = Mathf.Sin(2f * Mathf.PI * 50f * t) * Mathf.Exp(-Mathf.Pow((t - 0.1f) * 20f, 2f));
            float beat2 = Mathf.Sin(2f * Mathf.PI * 40f * t) * Mathf.Exp(-Mathf.Pow((t - 0.25f) * 20f, 2f)) * 0.7f;
            return (beat1 + beat2);
        });
    }

    delegate float SampleGenerator(int sampleIndex, int totalSamples);

    AudioClip CreateClip(string name, int sampleRate, float duration, SampleGenerator generator)
    {
        int sampleCount = Mathf.CeilToInt(sampleRate * duration);
        float[] samples = new float[sampleCount];

        // Save and restore Random state so we get consistent results
        Random.State prevState = Random.state;
        Random.InitState(name.GetHashCode());

        for (int i = 0; i < sampleCount; i++)
            samples[i] = Mathf.Clamp(generator(i, sampleCount), -1f, 1f);

        Random.state = prevState;

        AudioClip clip = AudioClip.Create(name, sampleCount, 1, sampleRate, false);
        clip.SetData(samples, 0);
        return clip;
    }
}
