using UnityEngine;
using UnityEngine.AI;

public class WildlifeAI : CreatureAI
{
    [Header("Wildlife Settings")]
    [Tooltip("How long to idle before picking a new patrol point")]
    public float idleDuration = 3f;
    [Tooltip("Patrol radius around spawn point")]
    public float patrolRadius = 15f;

    [Header("Animation Clip Names")]
    public string idleAnim = "DEERALL_Idle";
    public string walkAnim = "DEERALL_WalkForward";
    public string runAnim = "DEERALL_Gallop";
    public string grazeAnim = "DEERALL_Grazing";

    private Vector3 spawnPoint;
    private float idleTimer;
    private float fleePathTimer;
    private CreatureState lastState;

    [Header("Movement Tuning")]
    public float turnSpeed = 180f;

    protected override void Start()
    {
        base.Start();
        spawnPoint = transform.position;
        currentState = CreatureState.Idle;
        PlayAnimation(idleAnim);
        lastState = currentState;

        if (agent != null && agent.isOnNavMesh)
        {
            agent.updateRotation = false;
            agent.acceleration   = 8f;
        }

        // Kinematic Rigidbody so the CharacterController treats this as a moving obstacle
        if (GetComponent<Rigidbody>() == null)
        {
            Rigidbody rb = gameObject.AddComponent<Rigidbody>();
            rb.isKinematic = true;
            rb.useGravity  = false;
        }
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
                    if (agent != null && agent.enabled) agent.speed = data.runSpeed;
                }
                break;

            case CreatureState.Patrol:
                if (agent != null && agent.enabled && !agent.pathPending && agent.remainingDistance < 1f)
                {
                    currentState = CreatureState.Idle;
                    // Randomly graze or just idle
                    idleTimer = idleDuration + Random.Range(-1f, 1f);
                }
                if (distToPlayer < data.detectionRange)
                {
                    currentState = CreatureState.Flee;
                    if (agent != null && agent.enabled) agent.speed = data.runSpeed;
                }
                // Smooth rotation while walking
                if (agent != null && agent.velocity.sqrMagnitude > 0.1f)
                    SmoothRotateTowards(transform.position + agent.velocity);
                break;

            case CreatureState.Flee:
                FleeFromPlayer();
                // Smooth rotation while fleeing
                if (agent != null && agent.velocity.sqrMagnitude > 0.1f)
                    SmoothRotateTowards(transform.position + agent.velocity);
                if (distToPlayer > data.fleeRange)
                {
                    currentState = CreatureState.Patrol;
                    if (agent != null && agent.enabled) agent.speed = data.moveSpeed;
                    PickNewPatrolTarget();
                }
                break;
        }

        // Play animation on state change
        if (currentState != lastState)
        {
            switch (currentState)
            {
                case CreatureState.Idle:
                    PlayAnimation(Random.value > 0.5f ? grazeAnim : idleAnim);
                    break;
                case CreatureState.Patrol:
                    PlayAnimation(walkAnim);
                    break;
                case CreatureState.Flee:
                    PlayAnimation(runAnim);
                    break;
            }
            lastState = currentState;
        }
    }

    protected override void OnDamaged()
    {
        currentState = CreatureState.Flee;
        if (agent != null && agent.enabled) agent.speed = data.runSpeed;
    }

    private void PickNewPatrolTarget()
    {
        if (agent == null || !agent.enabled || !agent.isOnNavMesh) return;
        if (TryGetRandomNavMeshPoint(spawnPoint, patrolRadius, out Vector3 point))
            agent.SetDestination(point);
    }

    private void SmoothRotateTowards(Vector3 worldTarget)
    {
        Vector3 dir = worldTarget - transform.position;
        dir.y = 0f;
        if (dir.sqrMagnitude < 0.01f) return;
        Quaternion target = Quaternion.LookRotation(dir);
        transform.rotation = Quaternion.RotateTowards(transform.rotation, target, turnSpeed * Time.deltaTime);
    }

    private void FleeFromPlayer()
    {
        if (playerTransform == null) return;
        if (agent == null || !agent.enabled || !agent.isOnNavMesh) return;

        fleePathTimer -= Time.deltaTime;
        if (fleePathTimer > 0f) return;
        fleePathTimer = 0.5f;

        Vector3 fleeDir = (transform.position - playerTransform.position).normalized;
        Vector3 fleeTarget = transform.position + fleeDir * 10f;

        if (NavMesh.SamplePosition(fleeTarget, out NavMeshHit hit, 5f, NavMesh.AllAreas))
            agent.SetDestination(hit.position);
    }
}
