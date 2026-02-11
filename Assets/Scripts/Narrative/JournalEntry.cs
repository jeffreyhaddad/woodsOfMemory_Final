using UnityEngine;

public enum JournalCategory
{
    Story,
    Mission,
    Clue
}

[CreateAssetMenu(fileName = "New Journal Entry", menuName = "Narrative/Journal Entry")]
public class JournalEntry : ScriptableObject
{
    public string title;
    [TextArea(3, 10)] public string body;
    public JournalCategory category;
}
