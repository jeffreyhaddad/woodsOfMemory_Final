using UnityEngine;

/// <summary>
/// Place on objects in the world (papers, notebooks, etc.).
/// When the player interacts, it adds a journal entry and destroys itself.
/// Creates a pulsing glow light so the note is visible from a distance.
/// </summary>
public class NotePickup : Interactable
{
    [Header("Journal")]
    [Tooltip("The journal entry to add when picked up")]
    public JournalEntry entry;

    [Header("Glow")]
    public Color glowColor = new Color(1f, 0.9f, 0.5f);
    public float glowRange = 6f;
    public float glowIntensity = 1.5f;
    public float pulseSpeed = 2f;

    private Light glowLight;

    void Awake()
    {
        if (entry != null && (string.IsNullOrEmpty(promptText) || promptText == "Interact"))
            promptText = "Read " + entry.title;

        CreateGlow();
    }

    void Update()
    {
        // Pulse the light
        if (glowLight != null)
        {
            float pulse = 0.7f + 0.3f * Mathf.Sin(Time.time * pulseSpeed);
            glowLight.intensity = glowIntensity * pulse;
        }
    }

    void CreateGlow()
    {
        // Point light for warm glow
        GameObject lightObj = new GameObject("NoteGlow");
        lightObj.transform.SetParent(transform, false);
        lightObj.transform.localPosition = Vector3.up * 0.3f;

        glowLight = lightObj.AddComponent<Light>();
        glowLight.type = LightType.Point;
        glowLight.color = glowColor;
        glowLight.range = glowRange;
        glowLight.intensity = glowIntensity;
        glowLight.shadows = LightShadows.None;
    }

    public override void OnInteract()
    {
        if (entry == null)
        {
            Debug.LogWarning("NotePickup has no JournalEntry assigned: " + gameObject.name);
            return;
        }

        JournalManager journal = JournalManager.Instance;
        if (journal == null)
            journal = FindAnyObjectByType<JournalManager>();

        if (journal != null)
        {
            if (journal.HasEntry(entry))
            {
                Debug.Log("Already have this journal entry: " + entry.title);
                return;
            }

            journal.AddEntry(entry);
        }

        Destroy(gameObject);
    }
}
