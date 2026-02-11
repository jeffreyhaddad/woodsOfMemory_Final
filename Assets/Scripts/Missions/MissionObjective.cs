using UnityEngine;

public enum ObjectiveType
{
    CollectItem,
    CraftItem,
    KillCreature,
    ReachLocation,
    SurviveNight
}

[System.Serializable]
public class MissionObjective
{
    public string description;
    public ObjectiveType objectiveType;

    [Tooltip("Item required for CollectItem/CraftItem objectives")]
    public string targetItemName;

    [Tooltip("Creature name for KillCreature objectives")]
    public string targetCreatureName;

    [Tooltip("How many needed to complete this objective")]
    public int targetCount = 1;

    [HideInInspector]
    public int currentCount = 0;

    public bool IsCompleted => currentCount >= targetCount;

    public string GetProgressText()
    {
        if (objectiveType == ObjectiveType.SurviveNight)
            return description + (IsCompleted ? " (Done)" : "");

        return description + " (" + currentCount + "/" + targetCount + ")";
    }

    public void Reset()
    {
        currentCount = 0;
    }
}
