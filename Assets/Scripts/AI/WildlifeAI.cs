using UnityEngine;
using UnityEngine.AI;

public class WildlifeAI : CreatureAI
{
    [Header("Wildlife Settings")]
    [Tooltip("How long to idle before picking a new patrol point")]
    public float idleDuration = 3f;
    [Tooltip("Patrol radius around spawn point")]
    public float patrolRadius = 15f;

    private Vector3 spawnPoint;
    private float idleTimer;

    protected override void Start()
    {
        base.Start();
        spawnPoint = transform.position;
        currentState = CreatureState.Patrol;
        PickNewPatrolTarget();
    }

    protected override void UpdateBehavior(float distToPlayer)
    {
        switch (currentState)
        {
            case CreatureState.Idle:
                idleTimer -= Time.deltaTime;
                if (idleTimer <= 0f)
                {
                    currentState = CreatureState.Patrol;
                    PickNewPatrolTarget();
                }
                if (distToPlayer < data.detectionRange)
                {
                    currentState = CreatureState.Flee;
                    agent.speed = data.runSpeed;
                }
                break;

            case CreatureState.Patrol:
                if (!agent.pathPending && agent.remainingDistance < 1f)
                {
                    currentState = CreatureState.Idle;
                    idleTimer = idleDuration + Random.Range(-1f, 1f);
                }
                if (distToPlayer < data.detectionRange)
                {
                    currentState = CreatureState.Flee;
                    agent.speed = data.runSpeed;
                }
                break;

            case CreatureState.Flee:
                FleeFromPlayer();
                if (distToPlayer > data.fleeRange)
                {
                    currentState = CreatureState.Patrol;
                    agent.speed = data.moveSpeed;
                    PickNewPatrolTarget();
                }
                break;
        }
    }

    protected override void OnDamaged()
    {
        currentState = CreatureState.Flee;
        agent.speed = data.runSpeed;
    }

    private void PickNewPatrolTarget()
    {
        if (TryGetRandomNavMeshPoint(spawnPoint, patrolRadius, out Vector3 point))
            agent.SetDestination(point);
    }

    private void FleeFromPlayer()
    {
        if (playerTransform == null) return;

        Vector3 fleeDir = (transform.position - playerTransform.position).normalized;
        Vector3 fleeTarget = transform.position + fleeDir * 10f;

        if (NavMesh.SamplePosition(fleeTarget, out NavMeshHit hit, 5f, NavMesh.AllAreas))
            agent.SetDestination(hit.position);
    }
}
