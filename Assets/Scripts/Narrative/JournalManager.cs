using System;
using System.Collections.Generic;
using UnityEngine;

public class JournalManager : MonoBehaviour
{
    public static JournalManager Instance { get; private set; }

    [Header("Starting Entries")]
    [Tooltip("Leave empty to auto-generate default story entries.")]
    public JournalEntry[] startingEntries;

    private List<JournalEntry> discoveredEntries = new List<JournalEntry>();

    public IReadOnlyList<JournalEntry> DiscoveredEntries => discoveredEntries;

    public event Action<JournalEntry> OnEntryAdded;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    void Start()
    {
        // Auto-generate if none assigned
        bool hasValid = false;
        if (startingEntries != null)
        {
            for (int i = 0; i < startingEntries.Length; i++)
            {
                if (startingEntries[i] != null) { hasValid = true; break; }
            }
        }

        if (!hasValid)
            CreateDefaultEntries();

        // Add starting entry (player wakes up)
        if (startingEntries != null && startingEntries.Length > 0 && startingEntries[0] != null)
            AddEntry(startingEntries[0]);

        // Subscribe to mission events for auto-journal
        MissionManager mm = FindAnyObjectByType<MissionManager>();
        if (mm != null)
        {
            mm.OnMissionCompleted += OnMissionCompleted;
        }
    }

    public void AddEntry(JournalEntry entry)
    {
        if (entry == null) return;
        if (discoveredEntries.Contains(entry)) return;

        discoveredEntries.Add(entry);
        OnEntryAdded?.Invoke(entry);
        Debug.Log("Journal: Added entry - " + entry.title);
    }

    public bool HasEntry(JournalEntry entry)
    {
        return discoveredEntries.Contains(entry);
    }

    void OnMissionCompleted(Mission mission)
    {
        // Auto-add a journal entry for mission completion
        JournalEntry entry = ScriptableObject.CreateInstance<JournalEntry>();
        entry.name = "Mission: " + mission.missionName;
        entry.title = "Mission Complete: " + mission.missionName;
        entry.body = mission.description + "\n\nI've completed this task. The woods reveal more of their secrets.";
        entry.category = JournalCategory.Mission;
        AddEntry(entry);
    }

    // ─── Default Story Entries ───────────────────────────────

    void CreateDefaultEntries()
    {
        List<JournalEntry> entries = new List<JournalEntry>();

        entries.Add(MakeEntry("Awakening",
            "I woke up in a cabin I don't recognize. The air is thick with the scent of pine and damp earth. " +
            "My head is pounding, and I can't remember how I got here. Through the window, " +
            "dense forest stretches in every direction. I need to figure out where I am... and how to survive.",
            JournalCategory.Story));

        entries.Add(MakeEntry("Strange Markings",
            "I found strange symbols carved into the trees near the cabin. They look old, " +
            "but something about them feels familiar. Like I've seen them before. " +
            "Were they here when I arrived, or did someone — did I — carve them?",
            JournalCategory.Clue));

        entries.Add(MakeEntry("A Torn Letter",
            "Found a torn letter tucked under a rock near the stream:\n\n" +
            "\"...don't go back to the woods. Whatever you think you remember, " +
            "it isn't real. The trees have a way of making you forget. " +
            "If you're reading this, you've already stayed too long...\"\n\n" +
            "The rest is too damaged to read. Who wrote this?",
            JournalCategory.Clue));

        entries.Add(MakeEntry("The Other Cabin",
            "I discovered another cabin deeper in the forest. It looks abandoned, " +
            "but there are signs someone lived here recently — a half-eaten meal, " +
            "an unmade bed, and a journal with pages torn out. " +
            "The handwriting in what remains looks... like mine.",
            JournalCategory.Story));

        entries.Add(MakeEntry("Night Whispers",
            "The creatures that emerge at night are unlike anything I've seen. " +
            "They're made of shadow and seem to dissolve when the sun rises. " +
            "But the worst part is the whispering. I can almost make out words. " +
            "My name. They know my name.",
            JournalCategory.Story));

        entries.Add(MakeEntry("The Map Fragment",
            "Found a fragment of a map hidden in a hollowed-out tree stump. " +
            "It shows a path leading to the edge of the forest, marked with " +
            "\"EXIT\" in desperate handwriting. But the path is blocked by " +
            "a gate that requires multiple keys. I need to keep searching.",
            JournalCategory.Clue));

        entries.Add(MakeEntry("Memories Returning",
            "The more time I spend here, the more fragments come back. " +
            "I chose to come here. Something happened — something I needed to forget. " +
            "The woods were supposed to help me forget. But now I need to remember " +
            "to find my way out.",
            JournalCategory.Story));

        entries.Add(MakeEntry("The Truth",
            "I understand now. The woods feed on memory. Every night the shadow creatures " +
            "take a little more. The previous visitors — the other \"me\" — they all " +
            "eventually forgot everything and became part of the forest. " +
            "I have to escape before I lose myself completely.",
            JournalCategory.Story));

        startingEntries = entries.ToArray();
    }

    JournalEntry MakeEntry(string title, string body, JournalCategory cat)
    {
        JournalEntry entry = ScriptableObject.CreateInstance<JournalEntry>();
        entry.name = title;
        entry.title = title;
        entry.body = body;
        entry.category = cat;
        return entry;
    }
}
