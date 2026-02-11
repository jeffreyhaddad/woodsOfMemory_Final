using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class SaveData
{
    // Player
    public float playerX, playerY, playerZ;
    public float playerRotY;

    // Vitals
    public float health;
    public float hunger;
    public float stamina;

    // Inventory
    public List<SavedItem> inventoryItems = new List<SavedItem>();

    // Missions
    public int currentMissionIndex;
    public List<int> objectiveProgress = new List<int>();

    // Day/Night
    public float timeOfDay;

    // Journal
    public List<string> discoveredEntryTitles = new List<string>();

    // Metadata
    public string saveDate;
}

[Serializable]
public class SavedItem
{
    public string itemName;
    public int quantity;
    public int slotIndex;
}
