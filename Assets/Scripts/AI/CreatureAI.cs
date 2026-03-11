using System;
using UnityEngine;
using UnityEngine.AI;

public enum CreatureState
{
    Idle,
    Patrol,
    Flee,
    Chase,
    Attack,
    Dead
}

public class CreatureAI : MonoBehaviour
{
    /// <summary>All living creatures in the scene. Used by PlayerCombat for reliable hit detection.</summary>
    public static readonly System.Collections.Generic.List<CreatureAI> All =
        new System.Collections.Generic.List<CreatureAI>();

    [Header("Creature Setup")]
    public CreatureData data;

    protected NavMeshAgent agent;
    protected Animator animator;
    protected CreatureState currentState = CreatureState.Idle;
    protected Transform playerTransform;
    protected float currentHealth;

    public float CurrentHealth  => currentHealth;
    public float HealthPercent  => data != null && data.maxHealth > 0f ? currentHealth / data.maxHealth : 0f;
    public CreatureState State  => currentState;
    public event Action<CreatureAI> OnCreatureDeath;
    public event Action<float> OnHealthChanged;   // fires with new 0-1 percent
    protected void RaiseCreatureDeath() => OnCreatureDeath?.Invoke(this);

    protected virtual void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        animator = GetComponentInChildren<Animator>();
        currentHealth = data.maxHealth;
        All.Add(this);

        PlayerVitals playerVitals = FindAnyObjectByType<PlayerVitals>();
        if (playerVitals != null)
            playerTransform = playerVitals.transform;

        // If NavMeshAgent can't find a valid NavMesh, disable it so the creature still exists
        if (agent != null && agent.isOnNavMesh)
        {
            agent.speed = data.moveSpeed;
            agent.stoppingDistance = 1f;
        }
        else if (agent != null)
        {
            Debug.LogWarning($"[CreatureAI] {gameObject.name} not on NavMesh — disabling agent. Bake a NavMesh!");
            agent.enabled = false;
        }
    }

    protected void PlayAnimation(string clipName, float crossFade = 0.25f)
    {
        if (animator == null) return;
        animator.CrossFadeInFixedTime(clipName, crossFade);
    }

    private float deathTimer;
    private Vector3 deathTiltAxis;

    protected virtual void Update()
    {
        if (currentState == CreatureState.Dead)
        {
            OnDeadUpdate();
            return;
        }

        float distToPlayer = playerTransform != null
            ? Vector3.Distance(transform.position, playerTransform.position)
            : float.MaxValue;

        UpdateBehavior(distToPlayer);
    }

    protected virtual void UpdateBehavior(float distToPlayer)
    {
        // Override in subclasses
    }

    /// <summary>
    /// Called every frame while the creature is in the Dead state.
    /// Default: tips over and sinks (for non-humanoid creatures).
    /// Override to suppress this (e.g. when a real death animation is playing).
    /// </summary>
    protected virtual void OnDeadUpdate()
    {
        deathTimer += Time.deltaTime;
        if (deathTimer < 1f)
            transform.Rotate(deathTiltAxis, 90f * Time.deltaTime, Space.World);
        else
            transform.position += Vector3.down * 0.5f * Time.deltaTime;
    }

    public void TakeDamage(float amount)
    {
        if (currentState == CreatureState.Dead) return;

        currentHealth -= amount;
        OnHealthChanged?.Invoke(HealthPercent);

        // Floating damage number above torso
        DamageNumber.Spawn(amount, transform.position + Vector3.up * 2.5f);

        if (currentHealth <= 0f)
            Die();
        else
            OnDamaged();
    }

    protected virtual void OnDamaged() { }

    protected virtual void Die()
    {
        currentState = CreatureState.Dead;
        All.Remove(this);
        agent.enabled = false;
        deathTimer = 0f;
        // Random tilt direction so they don't all fall the same way
        deathTiltAxis = UnityEngine.Random.onUnitSphere;
        deathTiltAxis.y = 0f;
        deathTiltAxis.Normalize();

        DropLoot();
        OnCreatureDeath?.Invoke(this);

        // Report kill to mission system
        if (MissionManager.Instance != null && data != null)
            MissionManager.Instance.ReportCreatureKill(data.creatureName);

        Destroy(gameObject, 4f);
    }

    protected void DropLoot()
    {
        if (data.lootTable == null) return;

        foreach (LootDrop drop in data.lootTable)
        {
            if (drop.item == null) continue;
            if (UnityEngine.Random.value > drop.dropChance) continue;

            int qty = UnityEngine.Random.Range(drop.minQuantity, drop.maxQuantity + 1);
            SpawnPickup(drop.item, qty);
        }
    }

    private void SpawnPickup(ItemData item, int quantity)
    {
        // Ensure the item has its icon loaded (loot table assets bypass ItemRegistry.Register)
        ItemRegistry.Register(item);

        GameObject pickupObj = new GameObject(item.itemName + " Drop");
        pickupObj.transform.position = transform.position + Vector3.up * 0.5f;

        SphereCollider col = pickupObj.AddComponent<SphereCollider>();
        col.radius = 0.5f;

        PickupItem pickup = pickupObj.AddComponent<PickupItem>();
        pickup.itemData = item;
        pickup.quantity = quantity;
        pickup.promptText = "Pick up " + item.itemName;

        // Placeholder visual
        GameObject visual = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        visual.transform.SetParent(pickupObj.transform, false);
        visual.transform.localScale = Vector3.one * 0.3f;
        Destroy(visual.GetComponent<Collider>());
    }

    protected bool TryGetRandomNavMeshPoint(Vector3 center, float range, out Vector3 result)
    {
        for (int i = 0; i < 5; i++)
        {
            Vector3 randomPoint = center + UnityEngine.Random.insideUnitSphere * range;
            if (NavMesh.SamplePosition(randomPoint, out NavMeshHit hit, 2f, NavMesh.AllAreas))
            {
                result = hit.position;
                return true;
            }
        }
        result = center;
        return false;
    }
}
