using UnityEngine;

/// <summary>
/// Layered ambient audio system.
/// Base ambience clip (if assigned) + procedural layers: wind, crickets (night), rain integration.
/// Auto-adjusts volumes based on day/night cycle and weather.
/// </summary>
public class AmbientAudio : MonoBehaviour
{
    public static AmbientAudio Instance { get; private set; }

    void Awake()
    {
        Instance = this;
    }

    [Header("Base Ambience")]
    [Tooltip("Drag the forest ambience AudioClip here, or leave empty to auto-load")]
    public AudioClip ambienceClip;

    [Header("Volume")]
    [Tooltip("Base volume during the day")]
    public float dayVolume = 0.3f;
    [Tooltip("Volume during the night (louder for atmosphere)")]
    public float nightVolume = 0.5f;
    [Tooltip("How quickly volume transitions")]
    public float volumeTransitionSpeed = 1f;

    [Header("Wind")]
    public float windDayVolume = 0.12f;
    public float windNightVolume = 0.06f;

    [Header("Crickets")]
    public float cricketNightVolume = 0.05f;

    private AudioSource baseSource;
    private AudioSource windSource;
    private AudioSource cricketSource;
    private DayNightCycle dayNight;
    private float targetBaseVolume;

    void Start()
    {
        dayNight = FindAnyObjectByType<DayNightCycle>();

        if (SettingsManager.Instance != null)
        {
            float v = SettingsManager.Instance.AmbientVolume;
            dayVolume   = v * 0.6f;
            nightVolume = v;
        }

        // Auto-load the audio clip if not assigned
        if (ambienceClip == null)
        {
            ambienceClip = Resources.Load<AudioClip>("forest-ambient-at-day");

            if (ambienceClip == null)
            {
                AudioClip[] allClips = Resources.FindObjectsOfTypeAll<AudioClip>();
                for (int i = 0; i < allClips.Length; i++)
                {
                    if (allClips[i].name.Contains("forest") || allClips[i].name.Contains("ambient"))
                    {
                        ambienceClip = allClips[i];
                        break;
                    }
                }
            }
        }

        // Base ambience
        baseSource = gameObject.AddComponent<AudioSource>();
        baseSource.clip = ambienceClip;
        baseSource.loop = true;
        baseSource.playOnAwake = false;
        baseSource.spatialBlend = 0f;
        baseSource.volume = dayVolume;
        if (ambienceClip != null)
        {
            baseSource.Play();
            Debug.Log("AmbientAudio: Playing " + ambienceClip.name);
        }

        // Wind layer
        windSource = CreateLayer("Wind", GenerateWindClip(), windDayVolume);

        // Cricket layer
        cricketSource = CreateLayer("Crickets", GenerateCricketClip(), 0f);
    }

    void Update()
    {
        bool isNight = dayNight != null && dayNight.IsNight;
        WeatherManager wm = WeatherManager.Instance; // cache — avoids repeated static property lookup

        // Base ambience
        targetBaseVolume = isNight ? nightVolume : dayVolume;
        baseSource.volume = Mathf.MoveTowards(baseSource.volume, targetBaseVolume, volumeTransitionSpeed * Time.deltaTime);

        // Wind — louder during day, softer at night; louder during foggy/rainy weather
        float windTarget = isNight ? windNightVolume : windDayVolume;
        if (wm != null)
        {
            if (wm.CurrentWeather == WeatherState.Rainy)
                windTarget *= 2.5f;
            else if (wm.CurrentWeather == WeatherState.Foggy)
                windTarget *= 1.5f;
        }
        windSource.volume = Mathf.MoveTowards(windSource.volume, windTarget, volumeTransitionSpeed * Time.deltaTime);

        // Crickets — only at night, silent during rain
        float cricketTarget = isNight ? cricketNightVolume : 0f;
        if (wm != null && wm.IsRaining)
            cricketTarget = 0f;
        cricketSource.volume = Mathf.MoveTowards(cricketSource.volume, cricketTarget, volumeTransitionSpeed * 0.5f * Time.deltaTime);
    }

    AudioSource CreateLayer(string name, AudioClip clip, float startVolume)
    {
        AudioSource src = gameObject.AddComponent<AudioSource>();
        src.clip = clip;
        src.loop = true;
        src.playOnAwake = false;
        src.spatialBlend = 0f;
        src.volume = startVolume;
        src.Play();
        return src;
    }

    // ─── Procedural Audio Generation ──────────────────────────

    AudioClip GenerateWindClip()
    {
        int sampleRate = 22050;
        int length = sampleRate * 6; // 6 second loop
        float[] samples = new float[length];

        // Filtered brown noise with slow modulation = wind
        float lastSample = 0f;
        float modPhase = 0f;

        for (int i = 0; i < length; i++)
        {
            float white = Random.Range(-1f, 1f);
            // Very low-pass brown noise
            lastSample = (lastSample + 0.01f * white) / 1.01f;

            // Slow amplitude modulation (gusts)
            modPhase += 0.3f / sampleRate;
            float gust = 0.6f + 0.4f * Mathf.Sin(modPhase * Mathf.PI * 2f);

            samples[i] = Mathf.Clamp(lastSample * 5f * gust, -1f, 1f);
        }

        // Crossfade loop boundaries to prevent click on repeat
        int xfade = sampleRate / 10; // 100ms
        for (int i = 0; i < length; i++)
        {
            if      (i < xfade)          samples[i] *= (float)i / xfade;
            else if (i > length - xfade) samples[i] *= (float)(length - i) / xfade;
        }

        AudioClip clip = AudioClip.Create("Wind", length, 1, sampleRate, false);
        clip.SetData(samples, 0);
        return clip;
    }

    AudioClip GenerateCricketClip()
    {
        // Sustained band-pass noise centered around cricket frequencies (~3-4 kHz).
        // No chirp state machine → no transients, no clicks, perfectly smooth loop.
        int sampleRate = 22050;
        int length = sampleRate * 8; // 8 second loop
        float[] samples = new float[length];

        // Two-stage low-pass chain to create a band-pass centred near 3.5 kHz.
        // Stage 1: coarse high-pass (cuts sub-bass)
        // Stage 2: low-pass at ~4 kHz to roll off above the cricket band.
        float lp1 = 0f, lp2 = 0f;
        float lpCoeff = 0.45f; // ~4 kHz one-pole low-pass at 22050 Hz

        for (int i = 0; i < length; i++)
        {
            float t = (float)i / sampleRate;

            // White noise → low-pass → gives a soft hiss in the cricket band
            float white = Random.Range(-1f, 1f);
            lp1 = lp1 + lpCoeff * (white - lp1);
            lp2 = lp2 + lpCoeff * (lp1  - lp2);
            // High-pass: subtract a heavily smoothed version to cut DC / sub-bass
            float bandpass = lp1 - lp2;

            // Very slow density swell (insects fade in/out together)
            float swell = 0.6f + 0.4f * Mathf.Sin(2f * Mathf.PI * 0.11f * t);

            samples[i] = bandpass * swell * 0.35f;
        }

        // Crossfade loop boundaries
        int xfade = sampleRate / 4; // 250ms — generous fade, guaranteed clean loop
        for (int i = 0; i < length; i++)
        {
            if      (i < xfade)          samples[i] *= (float)i / xfade;
            else if (i > length - xfade) samples[i] *= (float)(length - i) / xfade;
        }

        AudioClip clip = AudioClip.Create("Crickets", length, 1, sampleRate, false);
        clip.SetData(samples, 0);
        return clip;
    }
}
