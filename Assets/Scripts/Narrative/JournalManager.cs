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

        // Add starting entry (player wakes up) — skipped if IntroSequenceManager
        // is present; it will add the entry after the player reads the intro note.
        if (FindAnyObjectByType<IntroSequenceManager>() == null)
        {
            if (startingEntries != null && startingEntries.Length > 0 && startingEntries[0] != null)
                AddEntry(startingEntries[0]);
        }

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
        JournalEntry entry = null;

        switch (mission.missionName)
        {
            case "Survival Basics":
                entry = MakeEntry("First Night",
                    "The fire helped. The hunger less so. But I'm alive, which is more than I expected.\n\n" +
                    "There's something wrong with the dark here. It doesn't just absorb light — " +
                    "it seems to press back against it. Like it knows you're afraid.\n\n" +
                    "I'll worry about that tomorrow.",
                    JournalCategory.Story);
                break;

            case "Hunting for Sustenance":
                entry = MakeEntry("The Cost",
                    "I don't know why I feel guilty. It's a deer. People hunt deer every day.\n\n" +
                    "But the way it looked at me before it went down — not like prey. Not like fear. " +
                    "Like it already knew what was going to happen.\n\n" +
                    "Like it had accepted it.",
                    JournalCategory.Story);
                break;

            case "Exploration":
                entry = MakeEntry("I've Been Here Before",
                    "The second cabin was mine. I know it the same way I know my own handwriting — " +
                    "because it is my handwriting. Scratched into the doorframe, dated three weeks ago.\n\n" +
                    "I wasn't here three weeks ago.\n\n" +
                    "I don't think this is my first time trying to escape.",
                    JournalCategory.Story);
                break;

            case "Crafting Tools":
                entry = MakeEntry("Ready",
                    "Axe. Torch. Bandages. The basics.\n\n" +
                    "It's strange how quickly survival instinct takes over. I stopped wondering why I'm " +
                    "here and started thinking about how long I can stay alive. " +
                    "Maybe that's what the woods want. Keep you busy. Keep you from asking questions.\n\n" +
                    "I'm asking questions.",
                    JournalCategory.Story);
                break;

            case "Forest Threats":
                entry = MakeEntry("What They Take",
                    "I killed three of them tonight. They dissolve when they die — no body, " +
                    "just a faint smell like something old burning.\n\n" +
                    "But I can't remember my mother's face anymore.\n\n" +
                    "That wasn't the woods. That was them. I could feel it — " +
                    "a tugging behind my eyes, right as the third one went down.\n\n" +
                    "I don't have much time.",
                    JournalCategory.Story);
                break;

            case "The Escape":
                entry = MakeEntry("Out",
                    "The trees are behind me.\n\n" +
                    "I don't know how much I've lost. There are gaps — " +
                    "faces I can almost place, names that slip away when I reach for them. " +
                    "The woods kept some of me. Maybe they always do.\n\n" +
                    "But I remember enough. I remember who I am.\n\n" +
                    "That has to be enough.",
                    JournalCategory.Story);
                break;
        }

        if (entry != null)
            AddEntry(entry);
    }

    // ─── Default Story Entries ───────────────────────────────

    void CreateDefaultEntries()
    {
        List<JournalEntry> entries = new List<JournalEntry>();

        // [0] Auto-added on start / by IntroSequenceManager
        entries.Add(MakeEntry("Awakening",
            "I woke up in a cabin I don't recognize. The air is thick with the scent of pine and damp earth. " +
            "My head is pounding, and I can't remember how I got here. Through the window, " +
            "dense forest stretches in every direction. I need to figure out where I am... and how to survive.",
            JournalCategory.Story));

        // [1]-[10] World note pickups — placed as physical notes by WorldSetup.
        // Ordered by rough discovery path: spawn area → forest → cabin 2 → exit.

        entries.Add(MakeEntry("Strange Markings",
            "I found strange symbols carved into the trees near the cabin. They look old, " +
            "but something about them feels familiar. Like I've seen them before.\n\n" +
            "Were they here when I arrived, or did someone — did I — carve them?",
            JournalCategory.Clue));

        entries.Add(MakeEntry("Day Three",
            "Day one: found shelter.\n" +
            "Day two: found fire.\n" +
            "Day three: found tracks. Human tracks. They don't lead anywhere. They just... stop.\n\n" +
            "There's someone else in these woods.\n\n" +
            "Or there was.",
            JournalCategory.Clue));

        entries.Add(MakeEntry("A Torn Letter",
            "Found a torn letter tucked under a rock near the stream:\n\n" +
            "\"...don't go back to the woods. Whatever you think you remember, " +
            "it isn't real. The trees have a way of making you forget. " +
            "If you're reading this, you've already stayed too long...\"\n\n" +
            "The rest is too damaged to read. Who wrote this?",
            JournalCategory.Clue));

        entries.Add(MakeEntry("Night Whispers",
            "The creatures that emerge at night are unlike anything I've seen. " +
            "They're made of shadow and seem to dissolve when the sun rises. " +
            "But the worst part is the whispering. I can almost make out words.\n\n" +
            "My name. They know my name.",
            JournalCategory.Story));

        entries.Add(MakeEntry("They're Not Animals",
            "The deer don't run anymore. They watch. Stand at the tree line and stare " +
            "like they're waiting for something.\n\n" +
            "I caught one watching the cabin for over an hour. When I stepped outside, " +
            "it turned and walked slowly into the dark — looking back the whole time.\n\n" +
            "They're not animals.",
            JournalCategory.Clue));

        entries.Add(MakeEntry("The Other Cabin",
            "I found another cabin deeper in the forest. It looks abandoned, " +
            "but someone lived here recently — a half-eaten meal, an unmade bed, " +
            "a journal with every page torn out.\n\n" +
            "The handwriting carved into the doorframe looks... like mine.",
            JournalCategory.Story));

        entries.Add(MakeEntry("The Map Fragment",
            "Found a fragment of a map hidden in a hollowed-out tree stump. " +
            "It shows a path leading to the edge of the forest, marked with " +
            "EXIT in desperate handwriting. Someone drew this in a hurry.\n\n" +
            "I need to find that path before I forget why I'm looking.",
            JournalCategory.Clue));

        entries.Add(MakeEntry("Almost Out",
            "I can see the edge of the forest from here. Maybe two hundred meters. " +
            "The air smells different — less like pine, more like the world I came from.\n\n" +
            "My legs won't move. Something in me wants to stay. " +
            "I don't know if that's a memory or the woods talking.\n\n" +
            "Move. Just move.",
            JournalCategory.Story));

        entries.Add(MakeEntry("Memories Returning",
            "The more time I spend here, the more fragments come back. " +
            "I chose to come here. Something happened — something I needed to forget. " +
            "The woods were supposed to help.\n\n" +
            "But now I need to remember to find my way out.",
            JournalCategory.Story));

        entries.Add(MakeEntry("The Truth",
            "I understand now. The woods feed on memory. Every night the shadow creatures " +
            "take a little more. The other visitors — the other versions of me — " +
            "they all eventually forgot everything and became part of the forest.\n\n" +
            "That's what the symbols mean. That's what my own handwriting in that cabin means.\n\n" +
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
