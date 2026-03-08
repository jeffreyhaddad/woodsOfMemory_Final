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
                    "I made it through.\n\n" +
                    "The fire helped more than I expected — not just for warmth, but for the dark. " +
                    "There's something wrong with the dark here. It doesn't just sit. It presses. " +
                    "Like it's leaning against the walls trying to find a gap.\n\n" +
                    "I heard something outside for most of the night. Not an animal. " +
                    "Animals make noise when they move. Whatever this was — it was perfectly still, " +
                    "and I could still hear it.\n\n" +
                    "I won't sleep until sunrise.",
                    JournalCategory.Story);
                break;

            case "Crafting Tools":
                entry = MakeEntry("Muscle Memory",
                    "I made an axe today. Stone head, wood shaft, fiber lashing.\n\n" +
                    "My hands knew exactly what to do. I didn't figure it out — " +
                    "I just did it, the way you tie a shoelace without thinking. " +
                    "Like I've made this exact axe before.\n\n" +
                    "I don't know what to do with that.",
                    JournalCategory.Story);
                break;

            case "Exploration":
                entry = MakeEntry("I've Been Here Before",
                    "The second cabin. I found it unlocked.\n\n" +
                    "Inside: a bedroll, cold ash in the fire pit, a tin cup with something " +
                    "dried and brown in the bottom. Someone was living here.\n\n" +
                    "On the doorframe, carved into the wood with something sharp — a date. " +
                    "Three weeks ago. And below it, a name I recognized the way you recognize " +
                    "your reflection — immediate, certain, and somehow wrong.\n\n" +
                    "My name.\n\n" +
                    "I didn't carve it. I've never been here before.\n\n" +
                    "I don't think that's true anymore.",
                    JournalCategory.Story);
                break;

            case "Shadow Harvest":
                entry = MakeEntry("What They Are",
                    "I killed five of them. They don't leave bodies — " +
                    "just a smell like old paper and a faint warmth in the air that fades fast.\n\n" +
                    "After the third one, I noticed it. A gap. Small, like a word on the tip of " +
                    "your tongue that won't come. By the fifth — bigger. " +
                    "I sat by the fire afterward trying to remember my hometown and I got " +
                    "the street, the house, the color of the door. But not the name of the street.\n\n" +
                    "They're not just creatures. They're hungry for something specific.\n\n" +
                    "They're eating me from the inside out.",
                    JournalCategory.Story);
                break;

            case "Into the Dark":
                entry = MakeEntry("Erosion",
                    "I can't remember my mother's face.\n\n" +
                    "I know she exists. I know I love her — I can feel the shape of that, " +
                    "like a word I know the meaning of but can't pronounce. " +
                    "But her face is gone. Just a blur where something important used to be.\n\n" +
                    "I stood in that clearing and finally understood it. " +
                    "The shadows don't attack you — they feed. They're drawn to grief the way " +
                    "insects are drawn to light. I came here carrying something unbearable, " +
                    "and they've been eating it ever since. The problem is they don't stop " +
                    "at the part that hurts.\n\n" +
                    "I'm writing this down so I don't forget that I forgot.\n\n" +
                    "I have to get out. Not tomorrow. Now.",
                    JournalCategory.Story);
                break;

            case "The Escape":
                entry = MakeEntry("Out",
                    "The trees are behind me. I made it.\n\n" +
                    "There are gaps I can feel but not see — spaces where memories used to live, " +
                    "smooth and sealed over like scar tissue. I don't know exactly what I've lost. " +
                    "You can't inventory an absence.\n\n" +
                    "But I remember my name. I remember why I came in. " +
                    "I remember the notes I left for myself — all those previous versions of me " +
                    "who got close but not close enough.\n\n" +
                    "I made it. I'm the one who finally made it.\n\n" +
                    "I'm not going back.",
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
            "I woke up on the floor of a cabin I've never seen before.\n\n" +
            "No bag. No phone. No memory of how I got here — just the smell of pine and cold ash " +
            "and the feeling that something important is missing. Not a thing. A piece of me.\n\n" +
            "Outside: forest in every direction. Dense. Old. Quiet in a way that doesn't feel quiet.\n\n" +
            "The cabin door was open when I woke up. Whatever left it that way — " +
            "I don't think it used a handle.",
            JournalCategory.Story));

        // [1]-[10] World note pickups — placed as physical notes by WorldSetup.
        // Written by a previous version of the player. Discovered in order:
        // spawn area → early forest → near cabin 1 → mid forest →
        // west forest → near cabin 2 → deep forest → approaching exit → near exit → at exit.

        // Note 1 — near spawn. Practical, almost calm. Early days.
        entries.Add(MakeEntry("Note: Day One",
            "Found this journal in the cabin. Writing this down in case I need to leave myself " +
            "a reminder of how things started.\n\n" +
            "Woke up here. No memory of arriving. Head feels like the inside of a bell.\n\n" +
            "Priorities: fire, food, water. Figure out where I am later.\n\n" +
            "Something carved into the wall by the window. Looks like tally marks — " +
            "but too many of them. Way too many.",
            JournalCategory.Clue));

        // Note 2 — early forest. First signs of unease.
        entries.Add(MakeEntry("Note: Tracks",
            "Found footprints in the mud north of the cabin. Human. My size.\n\n" +
            "I followed them for ten minutes before they stopped. Just stopped — " +
            "mid-stride, no sign of a struggle, no change in direction. " +
            "Like whoever made them stepped off the edge of the world.\n\n" +
            "The weird part is the tread pattern. It matches my boots exactly.\n\n" +
            "I'm going to pretend that's a coincidence.",
            JournalCategory.Clue));

        // Note 3 — near cabin 1. A warning.
        entries.Add(MakeEntry("Note: To Whoever Finds This",
            "If you found this, you woke up in the cabin too. You probably feel fine. " +
            "Confused, but fine.\n\n" +
            "You won't feel fine tonight.\n\n" +
            "The things that come out after dark — don't look at them directly. " +
            "Don't listen to what they say. They know things about you that " +
            "you haven't told anyone. That's how you know they're real.\n\n" +
            "Stay near the fire. Leave at dawn. Don't stop moving.\n\n" +
            "— Someone who should have left sooner",
            JournalCategory.Clue));

        // Note 4 — mid forest. About the shadow creatures.
        entries.Add(MakeEntry("Note: What They Whisper",
            "The shadows speak. I don't mean they make sounds — I mean they speak. " +
            "Words. My name. Details.\n\n" +
            "Last night one of them told me the name of my first dog. " +
            "Stood outside the fire's reach and just... said it. " +
            "Clear as anything.\n\n" +
            "I ran inside and didn't come out until sunrise.\n\n" +
            "When I woke up I couldn't remember the dog's name anymore.\n\n" +
            "It took it from me. Payment for saying it out loud.",
            JournalCategory.Clue));

        // Note 5 — west forest. The deer.
        entries.Add(MakeEntry("Note: The Deer",
            "Observation: the deer don't startle anymore.\n\n" +
            "They used to flee when I came close. Now they just turn and watch. " +
            "Big dark eyes, completely still. There was one at the tree line " +
            "this morning that didn't move for two hours.\n\n" +
            "I think they're watching the cabin. Not me specifically. The cabin.\n\n" +
            "Like they're waiting for it to be empty.",
            JournalCategory.Clue));

        // Note 6 — near cabin 2. The connection is dawning.
        entries.Add(MakeEntry("Note: The Second Cabin",
            "There's another cabin east of here. I broke the lock and went inside.\n\n" +
            "Someone lived there. Recently. Bedroll, fire pit, a tin cup. A life, paused.\n\n" +
            "On the doorframe: a name carved in the wood. A date. " +
            "I recognized the handwriting before I recognized the name — " +
            "because it was my handwriting.\n\n" +
            "The date is five weeks ago.\n\n" +
            "I've been in these woods before. I came back.\n\n" +
            "Why did I come back.",
            JournalCategory.Story));

        // Note 7 — deep forest. Desperate directions.
        entries.Add(MakeEntry("Note: The Way Out",
            "There is a way out. I've seen it — the tree line thins to the northeast, " +
            "past the ridge where the old pine split. You'll smell it before you see it: " +
            "less pine, more soil, more world.\n\n" +
            "I drew a map on the back of this note.\n\n" +
            "I don't know why I'm still here. I found the exit three days ago. " +
            "Something keeps making me turn around.\n\n" +
            "If you feel that pull — don't trust it. It isn't you.",
            JournalCategory.Clue));

        // Note 8 — approaching exit. Fragmenting.
        entries.Add(MakeEntry("Note: Almost",
            "I can see the edge.\n\n" +
            "I've been standing here for — I don't know how long. " +
            "My legs work. I tested them. But every time I decide to walk forward " +
            "I find myself sitting down again, like the decision never reached my feet.\n\n" +
            "There are gaps in my memory now. Small ones. " +
            "Names I should know. A street I grew up on.\n\n" +
            "I'm losing myself by degrees and I still can't make myself walk through those trees.\n\n" +
            "Something here doesn't want me to leave.",
            JournalCategory.Story));

        // Note 9 — very near exit. Memory fragments.
        entries.Add(MakeEntry("Note: What I Remember",
            "While I still can:\n\n" +
            "I have a sister. She's younger. She worries.\n" +
            "I came here on purpose — there was something I needed to forget. " +
            "It worked. I can't remember what it was.\n" +
            "I stayed too long.\n\n" +
            "The things in the dark didn't just take the bad memories. " +
            "They took whatever they wanted.\n\n" +
            "I don't know if there's enough of me left to be worth saving. " +
            "I'm going to try anyway.",
            JournalCategory.Story));

        // Note 10 — at the exit. The gut-punch reveal.
        entries.Add(MakeEntry("Note: For Next Time",
            "If you're reading this at the edge of the forest, you almost made it.\n\n" +
            "I know you don't remember writing any of the other notes. " +
            "I know the handwriting looks familiar in a way that frightens you. " +
            "I know you found your name in that second cabin and told yourself " +
            "it was a mistake or a coincidence.\n\n" +
            "It wasn't.\n\n" +
            "This is not your first time here. The woods reset you. " +
            "They'll do it again if you let them.\n\n" +
            "So don't let them.\n\n" +
            "Walk through. Don't look back. Don't let the pull stop you this time.\n\n" +
            "You are closer than you have ever been.\n\n" +
            "— You",
            JournalCategory.Story));

        // Note 11 — at the Dark Clearing. Placed by WorldSetup at darkClearingNotePosition.
        // The truth about why the player came to these woods.
        entries.Add(MakeEntry("Note: Why I Came",
            "I came here voluntarily. I need to write that before I forget it too.\n\n" +
            "There was a loss. Someone. The kind that doesn't leave room for anything else — " +
            "it fills up every hour of the day and every hour of the night and " +
            "you stop being able to remember what you were like before it.\n\n" +
            "I found references. Forums. Threads where desperate people talk in careful language " +
            "about this place, these woods. Go deep enough, stay long enough, " +
            "and the dark things will take what you give them. You can choose what you lose.\n\n" +
            "I chose wrong. I said 'the grief.' But grief doesn't come clean — " +
            "it's tangled into everything. Every good memory has loss woven through it. " +
            "The forest took them all. It took her face. It took her name. " +
            "It took the sound of her voice and the specific weight of her in a room.\n\n" +
            "I'm standing in the clearing now. This is where it happens — " +
            "where they gather the most, where the taking is strongest. " +
            "I thought if I asked clearly enough, it would give something back.\n\n" +
            "It doesn't give things back.\n\n" +
            "Run.",
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
