using System.Collections;
using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// AI for the zombie shadow creature. Drives Mixamo humanoid animations
/// via the Animator states: Idle, Walk, Run, Attack, Death.
/// Spawns Shadow Essence on death for the Shadow Harvest mission.
/// </summary>
public class ShadowCreatureAI : CreatureAI
{
    public static readonly System.Collections.Generic.List<ShadowCreatureAI> ActiveInstances =
        new System.Collections.Generic.List<ShadowCreatureAI>();

    [Header("Shadow Settings")]
    public float patrolRadius   = 20f;
    public float attackInterval = 2f;
    [Tooltip("Damage per second taken when the sun rises")]
    public float sunlightDamage = 20f;

    private float attackTimer;
    private DayNightCycle dayNight;
    private PlayerVitals cachedPlayerVitals;

    // Path recalculation throttle
    private float pathUpdateTimer;
    private Vector3 lastChaseDestination;

    // Animation state tracking
    private CreatureState lastAnimState = (CreatureState)(-1);
    private float animForceRefreshTimer; // periodically re-send CrossFade to fight exitTime auto-transitions

    [Header("Movement Tuning")]
    [Tooltip("Degrees per second the creature rotates to face its target")]
    public float turnSpeed = 200f;

    [Header("Attack Timing")]
    [Tooltip("Seconds after swing starts before damage lands — tune to match the animation hit frame")]
    public float attackHitDelay = 0.55f;
    private bool swingInProgress = false;

    // ── Animator state names (must match ZombieAnimController states) ──
    private const string AnimIdle   = "Idle";
    private const string AnimWalk   = "Walk";
    private const string AnimRun    = "Run";
    private const string AnimAttack = "Attack";
    private const string AnimDeath  = "Death";

    protected override void Start()
    {
        base.Start();
        dayNight         = FindAnyObjectByType<DayNightCycle>();
        cachedPlayerVitals = playerTransform != null
            ? playerTransform.GetComponent<PlayerVitals>() : null;

        // Disable root motion so the Animator doesn't fight the NavMeshAgent (causes jitter)
        if (animator != null)
            animator.applyRootMotion = false;

        // Let us control rotation manually for smooth turning; NavMeshAgent snaps by default.
        // stoppingDistance = attackRange means the agent decelerates naturally and arrives just
        // outside attack range — no overshoot, no need to ever call agent.isStopped.
        if (agent != null && agent.isOnNavMesh)
        {
            agent.updateRotation  = false;
            agent.acceleration    = 10f;
            agent.stoppingDistance = data != null ? data.attackRange : 2f;
        }

        // Prevent SkinnedMeshRenderer from being incorrectly culled when its bounds go stale
        foreach (SkinnedMeshRenderer smr in GetComponentsInChildren<SkinnedMeshRenderer>())
            smr.updateWhenOffscreen = true;

        // Ensure a collider exists so PlayerCombat's OverlapSphere can detect this creature
        if (GetComponent<Collider>() == null)
        {
            CapsuleCollider col = gameObject.AddComponent<CapsuleCollider>();
            col.center = new Vector3(0f, 1f, 0f);
            col.radius = 0.5f;
            col.height = 2f;
        }

        // Kinematic Rigidbody so the CharacterController treats this as a moving obstacle.
        // Interpolate so NavMeshAgent movement (Update) stays smooth at any frame rate.
        if (GetComponent<Rigidbody>() == null)
        {
            Rigidbody rb = gameObject.AddComponent<Rigidbody>();
            rb.isKinematic  = true;
            rb.useGravity   = false;
            rb.interpolation = RigidbodyInterpolation.Interpolate;
        }

        currentState = CreatureState.Patrol;
        PickNewPatrolTarget();
        PlayAnimation(AnimWalk);
        lastAnimState = CreatureState.Patrol;

        ActiveInstances.Add(this);
    }

    void OnDestroy()
    {
        ActiveInstances.Remove(this);
    }

    protected override void UpdateBehavior(float distToPlayer)
    {
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
                if (agent.enabled && agent.isOnNavMesh &&
                    !agent.pathPending && agent.remainingDistance < 1.5f)
                    PickNewPatrolTarget();

                // Smooth rotation towards direction of travel
                if (agent.velocity.sqrMagnitude > 0.1f)
                    SmoothRotateTowards(transform.position + agent.velocity);

                if (hDist < data.detectionRange)
                {
                    currentState = CreatureState.Chase;
                    agent.speed  = data.runSpeed;
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
                        pathUpdateTimer = 0.2f;
                    }
                }

                // While moving fast, rotate in the direction of travel.
                // When slowing down near the player, face the player directly.
                if (agent.velocity.sqrMagnitude > 2f)
                    SmoothRotateTowards(transform.position + agent.velocity);
                else if (playerTransform != null)
                    SmoothRotateTowards(playerTransform.position);

                // agent.stoppingDistance = attackRange, so the agent decelerates and arrives
                // just outside attack range without overshooting.  We enter Attack once the
                // agent has actually slowed down close to the player.
                if (hDist <= data.attackRange * 1.2f && agent.velocity.sqrMagnitude < 2f)
                {
                    currentState = CreatureState.Attack;
                    attackTimer  = attackInterval; // wait a full interval so first swing isn't instant
                    agent.speed  = data.moveSpeed * 0.3f;
                }
                else if (hDist > data.detectionRange * 1.5f)
                {
                    currentState = CreatureState.Patrol;
                    agent.speed  = data.moveSpeed;
                    PickNewPatrolTarget();
                }
                break;

            case CreatureState.Attack:
                // Refresh destination at a low rate so the agent micro-adjusts to keep pace
                // with a moving player without charging through them.
                if (playerTransform != null)
                {
                    pathUpdateTimer -= Time.deltaTime;
                    if (pathUpdateTimer <= 0f)
                    {
                        agent.SetDestination(playerTransform.position);
                        pathUpdateTimer = 0.35f;
                    }
                    SmoothRotateTowards(playerTransform.position);
                }

                attackTimer -= Time.deltaTime;
                if (attackTimer <= 0f && !swingInProgress)
                {
                    attackTimer = attackInterval;
                    StartCoroutine(SwingAttack());
                }

                if (hDist > data.attackRange * 2.5f)
                {
                    currentState = CreatureState.Chase;
                    agent.speed  = data.runSpeed;
                    if (playerTransform != null)
                    {
                        agent.SetDestination(playerTransform.position);
                        lastChaseDestination = playerTransform.position;
                        pathUpdateTimer = 0f;
                    }
                }
                break;
        }

        TryUpdateAnimation();
    }

    protected override void OnDamaged()
    {
        if (currentState == CreatureState.Patrol)
        {
            currentState = CreatureState.Chase;
            agent.speed  = data.runSpeed;
        }
    }

    // ── Animation ─────────────────────────────────────────────

    void TryUpdateAnimation()
    {
        bool stateChanged = currentState != lastAnimState;

        // The ZombieAnimController Attack state has an exitTime=0.9 auto-transition back to
        // Idle, which fires even while we're still logically in Attack/Chase/Patrol.
        // We re-send the CrossFade every second to overrule it.
        animForceRefreshTimer -= Time.deltaTime;
        bool forceRefresh = animForceRefreshTimer <= 0f;
        if (forceRefresh) animForceRefreshTimer = 1f;

        // Don't interrupt a swing mid-animation
        if (currentState == CreatureState.Attack && swingInProgress) return;

        if (!stateChanged && !forceRefresh) return;
        lastAnimState = currentState;

        switch (currentState)
        {
            case CreatureState.Patrol: PlayAnimation(AnimWalk);   break;
            case CreatureState.Chase:  PlayAnimation(AnimRun);    break;
            case CreatureState.Attack: PlayAnimation(AnimAttack); break;
        }
    }

    IEnumerator SwingAttack()
    {
        swingInProgress = true;
        PlayAnimation(AnimAttack);

        yield return new WaitForSeconds(attackHitDelay);

        if (currentState == CreatureState.Attack && playerTransform != null)
        {
            float dist = Vector3.Distance(transform.position, playerTransform.position);
            if (dist <= data.attackRange * 1.8f)
            {
                if (cachedPlayerVitals == null)
                    cachedPlayerVitals = playerTransform.GetComponent<PlayerVitals>();
                cachedPlayerVitals?.TakeDamage(data.damage);
            }
        }

        swingInProgress = false;
    }

    /// <summary>Suppress the base tip-over effect — the Animator death clip handles it.</summary>
    protected override void OnDeadUpdate() { }

    // ── Death ─────────────────────────────────────────────────

    protected override void Die()
    {
        currentState    = CreatureState.Dead;
        swingInProgress = false;
        StopAllCoroutines();
        if (agent != null) agent.enabled = false;

        PlayAnimation(AnimDeath);
        SpawnShadowEssence();
        DropLoot();
        RaiseCreatureDeath();

        if (MissionManager.Instance != null && data != null)
            MissionManager.Instance.ReportCreatureKill(data.creatureName);

        Destroy(gameObject, 4f);
    }

    // ── Shadow Essence Drop ───────────────────────────────────

    private static ItemData essenceItem;

    void SpawnShadowEssence()
    {
        if (essenceItem == null)
        {
            essenceItem = ItemRegistry.Get("Shadow Essence");
            if (essenceItem == null)
            {
                essenceItem             = ScriptableObject.CreateInstance<ItemData>();
                essenceItem.name        = essenceItem.itemName = "Shadow Essence";
                essenceItem.description = "A dark residue left behind by shadow creatures.";
                essenceItem.category    = ItemCategory.Resource;
                essenceItem.isStackable = true;
                essenceItem.maxStack    = 20;
                ItemRegistry.Register(essenceItem);
            }
        }

        GameObject pickupObj = new GameObject("Shadow Essence Drop");
        pickupObj.transform.position = transform.position + Vector3.up * 0.5f;

        SphereCollider col = pickupObj.AddComponent<SphereCollider>();
        col.radius = 0.5f;

        PickupItem pickup   = pickupObj.AddComponent<PickupItem>();
        pickup.itemData     = essenceItem;
        pickup.quantity     = 1;
        pickup.promptText   = "Pick up Shadow Essence";

        GameObject visual = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        visual.transform.SetParent(pickupObj.transform, false);
        visual.transform.localScale = Vector3.one * 0.3f;
        Destroy(visual.GetComponent<Collider>());

        Shader shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
        Material mat = new Material(shader);
        mat.color = new Color(0.45f, 0f, 0.85f);
        if (mat.HasProperty("_BaseColor"))
            mat.SetColor("_BaseColor", new Color(0.45f, 0f, 0.85f));
        visual.GetComponent<Renderer>().material = mat;

        pickupObj.AddComponent<PickupBob>();
    }

    // ── Helpers ───────────────────────────────────────────────

    private void SmoothRotateTowards(Vector3 worldTarget)
    {
        Vector3 dir = worldTarget - transform.position;
        dir.y = 0f;
        if (dir.sqrMagnitude < 0.01f) return;
        Quaternion target = Quaternion.LookRotation(dir);
        transform.rotation = Quaternion.RotateTowards(transform.rotation, target, turnSpeed * Time.deltaTime);
    }

    private void PickNewPatrolTarget()
    {
        if (agent == null || !agent.enabled || !agent.isOnNavMesh) return;
        if (TryGetRandomNavMeshPoint(transform.position, patrolRadius, out Vector3 point))
            agent.SetDestination(point);
    }
}
