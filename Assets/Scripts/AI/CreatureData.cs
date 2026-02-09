using UnityEngine;

[System.Serializable]
public class LootDrop
{
    public ItemData item;
    public int minQuantity = 1;
    public int maxQuantity = 1;
    [Range(0f, 1f)]
    public float dropChance = 1f;
}

[CreateAssetMenu(fileName = "New Creature", menuName = "AI/Creature Data")]
public class CreatureData : ScriptableObject
{
    [Header("Identity")]
    public string creatureName;

    [Header("Stats")]
    public float maxHealth = 50f;
    public float moveSpeed = 3f;
    public float runSpeed = 6f;

    [Header("Detection")]
    public float detectionRange = 15f;
    public float fleeRange = 25f;
    public float attackRange = 2f;

    [Header("Combat")]
    public float damage = 0f;
    public float attackCooldown = 2f;

    [Header("Loot")]
    public LootDrop[] lootTable;
}
