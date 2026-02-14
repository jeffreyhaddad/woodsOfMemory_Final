using UnityEngine;

/// <summary>
/// Layered ambient audio system.
/// Base ambience clip (if assigned) + procedural layers: wind, crickets (night), rain integration.
/// Auto-adjusts volumes based on day/night cycle and weather.
/// </summary>
public class AmbientAudio : MonoBehaviour
{
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
    public float cricketNightVolume = 0.15f;

    private AudioSource baseSource;
    private AudioSource windSource;
    private AudioSource cricketSource;
    private DayNightCycle dayNight;
    private float targetBaseVolume;

    void Start()
    {
        dayNight = FindAnyObjectByType<DayNightCycle>();

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

        // Base ambience
        targetBaseVolume = isNight ? nightVolume : dayVolume;
        baseSource.volume = Mathf.MoveTowards(baseSource.volume, targetBaseVolume, volumeTransitionSpeed * Time.deltaTime);

        // Wind — louder during day, softer at night; louder during foggy/rainy weather
        float windTarget = isNight ? windNightVolume : windDayVolume;
        if (WeatherManager.Instance != null)
        {
            if (WeatherManager.Instance.CurrentWeather == WeatherState.Rainy)
                windTarget *= 2.5f;
            else if (WeatherManager.Instance.CurrentWeather == WeatherState.Foggy)
                windTarget *= 1.5f;
        }
        windSource.volume = Mathf.MoveTowards(windSource.volume, windTarget, volumeTransitionSpeed * Time.deltaTime);

        // Crickets — only at night, silent during rain
        float cricketTarget = isNight ? cricketNightVolume : 0f;
        if (WeatherManager.Instance != null && WeatherManager.Instance.IsRaining)
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

        AudioClip clip = AudioClip.Create("Wind", length, 1, sampleRate, false);
        clip.SetData(samples, 0);
        return clip;
    }

    AudioClip GenerateCricketClip()
    {
        int sampleRate = 22050;
        int length = sampleRate * 4; // 4 second loop
        float[] samples = new float[length];

        // Cricket chirps: short bursts of high-frequency oscillation
        float chirpTimer = 0f;
        float chirpDuration = 0f;
        float chirpFreq = 0f;
        bool chirping = false;

        for (int i = 0; i < length; i++)
        {
            float t = (float)i / sampleRate;

            if (!chirping)
            {
                chirpTimer -= 1f / sampleRate;
                if (chirpTimer <= 0f)
                {
                    // Start a chirp burst
                    chirping = true;
                    chirpDuration = Random.Range(0.05f, 0.12f);
                    chirpFreq = Random.Range(3800f, 4500f);
                    chirpTimer = chirpDuration;
                }
                samples[i] = 0f;
            }
            else
            {
                chirpTimer -= 1f / sampleRate;
                // Rapid oscillation with envelope
                float env = Mathf.Sin(Mathf.PI * (1f - chirpTimer / chirpDuration));
                samples[i] = Mathf.Sin(t * chirpFreq * Mathf.PI * 2f) * env * 0.3f;

                if (chirpTimer <= 0f)
                {
                    chirping = false;
                    chirpTimer = Random.Range(0.2f, 0.8f); // Pause between chirps
                }
            }
        }

        AudioClip clip = AudioClip.Create("Crickets", length, 1, sampleRate, false);
        clip.SetData(samples, 0);
        return clip;
    }
}
