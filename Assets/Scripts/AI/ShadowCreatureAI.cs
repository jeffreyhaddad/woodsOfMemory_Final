using UnityEngine;
using UnityEngine.AI;

public class ShadowCreatureAI : CreatureAI
{
    [Header("Shadow Settings")]
    public float patrolRadius = 20f;
    public float attackInterval = 2f;
    [Tooltip("Damage per second taken when the sun rises")]
    public float sunlightDamage = 20f;

    private float attackTimer;
    private DayNightCycle dayNight;

    protected override void Start()
    {
        base.Start();
        dayNight = FindAnyObjectByType<DayNightCycle>();
        currentState = CreatureState.Patrol;
        PickNewPatrolTarget();
    }

    protected override void UpdateBehavior(float distToPlayer)
    {
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

                if (distToPlayer < data.detectionRange)
                {
                    currentState = CreatureState.Chase;
                    agent.speed = data.runSpeed;
                }
                break;

            case CreatureState.Chase:
                if (playerTransform != null)
                    agent.SetDestination(playerTransform.position);

                if (distToPlayer <= data.attackRange)
                {
                    currentState = CreatureState.Attack;
                    agent.isStopped = true;
                    attackTimer = 0f;
                }
                else if (distToPlayer > data.detectionRange * 1.5f)
                {
                    currentState = CreatureState.Patrol;
                    agent.speed = data.moveSpeed;
                    PickNewPatrolTarget();
                }
                break;

            case CreatureState.Attack:
                // Face the player
                if (playerTransform != null)
                {
                    Vector3 lookDir = (playerTransform.position - transform.position).normalized;
                    lookDir.y = 0;
                    if (lookDir.sqrMagnitude > 0.001f)
                        transform.rotation = Quaternion.LookRotation(lookDir);
                }

                attackTimer -= Time.deltaTime;
                if (attackTimer <= 0f)
                {
                    PlayerVitals pv = playerTransform != null
                        ? playerTransform.GetComponent<PlayerVitals>()
                        : null;
                    if (pv != null)
                        pv.TakeDamage(data.damage);

                    attackTimer = attackInterval;
                }

                // Player moved out of range — chase again
                if (distToPlayer > data.attackRange * 1.5f)
                {
                    currentState = CreatureState.Chase;
                    agent.isStopped = false;
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
        if (TryGetRandomNavMeshPoint(transform.position, patrolRadius, out Vector3 point))
            agent.SetDestination(point);
    }
}
