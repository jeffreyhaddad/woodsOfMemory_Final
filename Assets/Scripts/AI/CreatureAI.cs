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
    [Header("Creature Setup")]
    public CreatureData data;

    protected NavMeshAgent agent;
    protected CreatureState currentState = CreatureState.Idle;
    protected Transform playerTransform;
    protected float currentHealth;

    public float CurrentHealth => currentHealth;
    public event Action<CreatureAI> OnCreatureDeath;

    protected virtual void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        agent.speed = data.moveSpeed;
        agent.stoppingDistance = 1f;

        currentHealth = data.maxHealth;

        PlayerVitals playerVitals = FindAnyObjectByType<PlayerVitals>();
        if (playerVitals != null)
            playerTransform = playerVitals.transform;
    }

    private float deathTimer;
    private Vector3 deathTiltAxis;

    protected virtual void Update()
    {
        if (currentState == CreatureState.Dead)
        {
            // Death animation: tip over and sink
            deathTimer += Time.deltaTime;

            // Tip over during first second
            if (deathTimer < 1f)
            {
                transform.Rotate(deathTiltAxis, 90f * Time.deltaTime, Space.World);
            }
            // Sink into ground after tipping
            else
            {
                transform.position += Vector3.down * 0.5f * Time.deltaTime;
            }
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

    public void TakeDamage(float amount)
    {
        if (currentState == CreatureState.Dead) return;

        currentHealth -= amount;

        if (currentHealth <= 0f)
            Die();
        else
            OnDamaged();
    }

    protected virtual void OnDamaged() { }

    protected virtual void Die()
    {
        currentState = CreatureState.Dead;
        agent.enabled = false;
        deathTimer = 0f;
        // Random tilt direction so they don't all fall the same way
        deathTiltAxis = UnityEngine.Random.onUnitSphere;
        deathTiltAxis.y = 0f;
        deathTiltAxis.Normalize();

        DropLoot();
        OnCreatureDeath?.Invoke(this);
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
