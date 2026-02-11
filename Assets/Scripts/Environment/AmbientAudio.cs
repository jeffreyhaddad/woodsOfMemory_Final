using UnityEngine;

public class AmbientAudio : MonoBehaviour
{
    [Header("Audio")]
    [Tooltip("Drag the forest ambience AudioClip here, or leave empty to auto-load")]
    public AudioClip ambienceClip;

    [Header("Volume")]
    [Tooltip("Base volume during the day")]
    public float dayVolume = 0.3f;
    [Tooltip("Volume during the night (louder for atmosphere)")]
    public float nightVolume = 0.5f;
    [Tooltip("How quickly volume transitions")]
    public float volumeTransitionSpeed = 1f;

    private AudioSource audioSource;
    private DayNightCycle dayNight;
    private float targetVolume;

    void Start()
    {
        dayNight = FindAnyObjectByType<DayNightCycle>();

        // Auto-load the audio clip if not assigned
        if (ambienceClip == null)
        {
            // Try loading from Resources, then from known path
            ambienceClip = Resources.Load<AudioClip>("forest-ambient-at-day");

            if (ambienceClip == null)
            {
                // Search all AudioClips in the project
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

        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.clip = ambienceClip;
        audioSource.loop = true;
        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 0f; // 2D sound
        audioSource.volume = dayVolume;

        if (ambienceClip != null)
        {
            audioSource.Play();
            Debug.Log("AmbientAudio: Playing " + ambienceClip.name);
        }
        else
            Debug.LogWarning("AmbientAudio: No audio clip found. Drag the forest ambience clip onto the Ambience Clip field in the Inspector.");
    }

    void Update()
    {
        if (dayNight != null)
            targetVolume = dayNight.IsNight ? nightVolume : dayVolume;
        else
            targetVolume = dayVolume;

        audioSource.volume = Mathf.MoveTowards(audioSource.volume, targetVolume, volumeTransitionSpeed * Time.deltaTime);
    }
}
