using UnityEngine;

[CreateAssetMenu(fileName = "New Mission", menuName = "Missions/Mission")]
public class Mission : ScriptableObject
{
    public string missionName;
    [TextArea] public string description;
    public MissionObjective[] objectives;

    [HideInInspector] public bool isActive;
    [HideInInspector] public bool isCompleted;

    public bool AreAllObjectivesComplete()
    {
        for (int i = 0; i < objectives.Length; i++)
        {
            if (!objectives[i].IsCompleted)
                return false;
        }
        return true;
    }

    public MissionObjective GetCurrentObjective()
    {
        for (int i = 0; i < objectives.Length; i++)
        {
            if (!objectives[i].IsCompleted)
                return objectives[i];
        }
        return null;
    }

    public void ResetMission()
    {
        isActive = false;
        isCompleted = false;
        for (int i = 0; i < objectives.Length; i++)
            objectives[i].Reset();
    }
}
