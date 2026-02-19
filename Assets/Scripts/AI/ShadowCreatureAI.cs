using UnityEngine;
using UnityEngine.AI;

public class ShadowCreatureAI : CreatureAI
{
    /// <summary>Static registry of all active shadow creatures for efficient lookups.</summary>
    public static readonly System.Collections.Generic.List<ShadowCreatureAI> ActiveInstances =
        new System.Collections.Generic.List<ShadowCreatureAI>();

    [Header("Shadow Settings")]
    public float patrolRadius = 20f;
    public float attackInterval = 2f;
    [Tooltip("Damage per second taken when the sun rises")]
    public float sunlightDamage = 20f;

    private float attackTimer;
    private DayNightCycle dayNight;
    private PlayerVitals cachedPlayerVitals;

    // Throttle chase path recalculation
    private float pathUpdateTimer;
    private Vector3 lastChaseDestination;

    protected override void Start()
    {
        base.Start();
        dayNight = FindAnyObjectByType<DayNightCycle>();
        cachedPlayerVitals = playerTransform != null ? playerTransform.GetComponent<PlayerVitals>() : null;

        Debug.Log($"[ShadowCreatureAI] Start — playerTransform={playerTransform}, " +
                  $"cachedPlayerVitals={cachedPlayerVitals}, dayNight={dayNight}, " +
                  $"IsNight={(dayNight != null ? dayNight.IsNight.ToString() : "N/A")}, " +
                  $"agent.isOnNavMesh={agent != null && agent.isActiveAndEnabled && agent.isOnNavMesh}");

        currentState = CreatureState.Patrol;
        PickNewPatrolTarget();
        ActiveInstances.Add(this);
    }

    void OnDestroy()
    {
        ActiveInstances.Remove(this);
    }

    protected override void UpdateBehavior(float distToPlayer)
    {
        // Use flat (XZ) distance so terrain height differences don't inflate the range
        float hDist = playerTransform != null
            ? new Vector2(transform.position.x - playerTransform.position.x,
                          transform.position.z - playerTransform.position.z).magnitude
            : float.MaxValue;

        // Dissolve at sunrise
        if (dayNight != null && !dayNight.IsNight)
        {
            currentHealth -= sunlightDamage * Time.deltaTime;
            if (currentHealth <= 0f)
                Die();
            return;
        }

        switch (currentState)
        {
            case CreatureState.Patrol:
                if (!agent.pathPending && agent.remainingDistance < 1.5f)
                    PickNewPatrolTarget();

                if (hDist < data.detectionRange)
                {
                    currentState = CreatureState.Chase;
                    agent.speed = data.runSpeed;
                    Debug.Log($"[ShadowCreatureAI] → Chase (hDist={hDist:F1})");
                }
                break;

            case CreatureState.Chase:
                if (playerTransform != null)
                {
                    pathUpdateTimer -= Time.deltaTime;
                    if (pathUpdateTimer <= 0f ||
                        (playerTransform.position - lastChaseDestination).sqrMagnitude > 4f)
                    {
                        agent.SetDestination(playerTransform.position);
                        lastChaseDestination = playerTransform.position;
                        pathUpdateTimer = 0.25f;
                    }
                }

                if (hDist <= data.attackRange)
                {
                    currentState = CreatureState.Attack;
                    attackTimer = 0f;
                    // Don't stop the agent — keep creeping so distance stays close
                    agent.speed = data.moveSpeed * 0.3f;
                    Debug.Log($"[ShadowCreatureAI] → Attack (hDist={hDist:F1}, attackRange={data.attackRange})");
                }
                else if (hDist > data.detectionRange * 1.5f)
                {
                    currentState = CreatureState.Patrol;
                    agent.speed = data.moveSpeed;
                    PickNewPatrolTarget();
                }
                break;

            case CreatureState.Attack:
                // Keep slowly creeping toward player so we stay in range
                if (playerTransform != null)
                {
                    agent.SetDestination(playerTransform.position);

                    Vector3 lookDir = (playerTransform.position - transform.position).normalized;
                    lookDir.y = 0;
                    if (lookDir.sqrMagnitude > 0.001f)
                        transform.rotation = Quaternion.LookRotation(lookDir);
                }

                attackTimer -= Time.deltaTime;
                if (attackTimer <= 0f)
                {
                    if (cachedPlayerVitals == null && playerTransform != null)
                        cachedPlayerVitals = playerTransform.GetComponent<PlayerVitals>();
                    if (cachedPlayerVitals != null)
                    {
                        Debug.Log($"[ShadowCreatureAI] Dealing {data.damage} damage to player");
                        cachedPlayerVitals.TakeDamage(data.damage);
                    }
                    else
                        Debug.LogWarning("[ShadowCreatureAI] cachedPlayerVitals is null — cannot deal damage!");
                    attackTimer = attackInterval;
                }

                // Player escaped — resume full chase
                if (hDist > data.attackRange * 2f)
                {
                    currentState = CreatureState.Chase;
                    agent.speed = data.runSpeed;
                }
                break;
        }

    }

    protected override void OnDamaged()
    {
        if (currentState == CreatureState.Patrol)
        {
            currentState = CreatureState.Chase;
            agent.speed = data.runSpeed;
        }
    }

    private void PickNewPatrolTarget()
    {
        if (agent == null || !agent.enabled || !agent.isOnNavMesh) return;
        if (TryGetRandomNavMeshPoint(transform.position, patrolRadius, out Vector3 point))
            agent.SetDestination(point);
    }
}
