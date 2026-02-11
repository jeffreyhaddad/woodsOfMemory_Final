using UnityEngine;

/// <summary>
/// Place on objects in the world (papers, notebooks, etc.).
/// When the player interacts, it adds a journal entry and destroys itself.
/// </summary>
public class NotePickup : Interactable
{
    [Header("Journal")]
    [Tooltip("The journal entry to add when picked up")]
    public JournalEntry entry;

    void Awake()
    {
        if (entry != null && (string.IsNullOrEmpty(promptText) || promptText == "Interact"))
            promptText = "Read " + entry.title;
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
