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
    public float attackInterval = 1.8f;
    [Tooltip("Damage per second taken when the sun rises")]
    public float sunlightDamage = 20f;

    [Header("Movement Tuning")]
    [Tooltip("Degrees per second the creature rotates to face its target")]
    public float turnSpeed = 360f;
    [Tooltip("Speed while shuffling toward player in attack state")]
    public float attackShuffleSpeed = 0.8f;

    [Header("Attack Timing")]
    [Tooltip("Seconds after swing starts before damage lands — tune to match the animation hit frame")]
    public float attackHitDelay = 0.5f;
    [Tooltip("Seconds to wait after damage lands before allowing next swing (rest of animation)")]
    public float attackRecoveryTime = 0.55f;
    [Tooltip("Short delay before first swing when entering attack range")]
    public float firstAttackDelay = 0.15f;

    private float attackTimer;
    private bool  swingInProgress = false;

    private DayNightCycle     dayNight;
    private PlayerVitals      cachedPlayerVitals;
    private ZombieAlertIndicator alertIndicator;

    private float pathUpdateTimer;
    // Periodically re-send the Run CrossFade to fight the Animator's exitTime
    // auto-transition that fires at ~90% of the clip and drops back to Idle.
    private float chaseAnimRefreshTimer;

    // Track last anim state to avoid redundant CrossFades
    private CreatureState lastAnimState = (CreatureState)(-1);

    // ── Animator state names (must match ZombieAnimController states) ──
    private const string AnimIdle   = "Idle";
    private const string AnimWalk   = "Walk";
    private const string AnimRun    = "Run";
    private const string AnimAttack = "Attack";
    private const string AnimDeath  = "Death";

    protected override void Start()
    {
        base.Start();
        dayNight           = FindAnyObjectByType<DayNightCycle>();
        cachedPlayerVitals = playerTransform != null
            ? playerTransform.GetComponent<PlayerVitals>() : null;

        if (animator != null)
            animator.applyRootMotion = false;

        if (agent != null && agent.isOnNavMesh)
        {
            agent.updateRotation   = false;
            agent.acceleration     = 24f;
            agent.angularSpeed     = 0f;
            agent.stoppingDistance = data != null ? data.attackRange : 2f;
            agent.speed            = data != null ? data.moveSpeed : 3f;
        }

        foreach (SkinnedMeshRenderer smr in GetComponentsInChildren<SkinnedMeshRenderer>())
            smr.updateWhenOffscreen = true;

        if (GetComponent<Collider>() == null)
        {
            CapsuleCollider col = gameObject.AddComponent<CapsuleCollider>();
            col.center = new Vector3(0f, 1f, 0f);
            col.radius = 0.5f;
            col.height = 2f;
        }

        if (GetComponent<Rigidbody>() == null)
        {
            Rigidbody rb = gameObject.AddComponent<Rigidbody>();
            rb.isKinematic   = true;
            rb.useGravity    = false;
            rb.interpolation = RigidbodyInterpolation.Interpolate;
        }

        ZombieHealthBar hb = gameObject.AddComponent<ZombieHealthBar>();
        hb.Init(this);
        alertIndicator = gameObject.AddComponent<ZombieAlertIndicator>();

        TransitionToPatrol();
        ActiveInstances.Add(this);
    }

    void OnDestroy()
    {
        ActiveInstances.Remove(this);
    }

    // ── Main behavior loop ────────────────────────────────────

    protected override void UpdateBehavior(float distToPlayer)
    {
        // Horizontal distance only — avoids Y-axis terrain variance throwing off ranges
        float hDist = playerTransform != null
            ? new Vector2(transform.position.x - playerTransform.position.x,
                          transform.position.z - playerTransform.position.z).magnitude
            : float.MaxValue;

        // Dissolve at sunrise
        if (dayNight != null && !dayNight.IsNight)
        {
            currentHealth -= sunlightDamage * Time.deltaTime;
            if (currentHealth <= 0f) Die();
            return;
        }

        switch (currentState)
        {
            case CreatureState.Patrol:  UpdatePatrol(hDist);  break;
            case CreatureState.Chase:   UpdateChase(hDist);   break;
            case CreatureState.Attack:  UpdateAttack(hDist);  break;
        }
    }

    // ── Patrol ────────────────────────────────────────────────

    void UpdatePatrol(float hDist)
    {
        if (agent.enabled && agent.isOnNavMesh &&
            !agent.pathPending && agent.remainingDistance < 1.5f)
            PickNewPatrolTarget();

        if (agent.velocity.sqrMagnitude > 0.1f)
            SmoothRotateTowards(transform.position + agent.velocity);

        if (hDist < data.detectionRange)
            TransitionToChase();
    }

    // ── Chase ─────────────────────────────────────────────────

    void UpdateChase(float hDist)
    {
        if (playerTransform != null)
        {
            // Update path frequently so the creature tracks a moving player tightly
            pathUpdateTimer -= Time.deltaTime;
            if (pathUpdateTimer <= 0f)
            {
                agent.SetDestination(playerTransform.position);
                pathUpdateTimer = 0.12f;
            }

            // Always rotate directly toward the player — never follow velocity curve
            SmoothRotateTowards(playerTransform.position);
        }

        // Fight the Animator's exitTime auto-transition: re-send Run every 0.6s
        // so it never drops back to Idle while chasing.
        chaseAnimRefreshTimer -= Time.deltaTime;
        if (chaseAnimRefreshTimer <= 0f)
        {
            PlayAnimation(AnimRun, 0.1f);
            chaseAnimRefreshTimer = 0.6f;
        }

        if (hDist <= (data != null ? data.attackRange : 2f) + 0.5f)
        {
            TransitionToAttack();
            return;
        }

        // Lost the player
        if (hDist > data.detectionRange * 1.5f)
            TransitionToPatrol();
    }

    // ── Attack ────────────────────────────────────────────────

    void UpdateAttack(float hDist)
    {
        float attackRange = data != null ? data.attackRange : 2f;

        // Only close the gap if the player has backed away — don't nudge while standing still
        if (agent.enabled && agent.isOnNavMesh && playerTransform != null)
        {
            pathUpdateTimer -= Time.deltaTime;
            if (pathUpdateTimer <= 0f)
            {
                if (hDist > attackRange + 0.6f)
                    agent.SetDestination(playerTransform.position);
                else
                    agent.ResetPath(); // player is right there — stay put
                pathUpdateTimer = 0.25f;
            }
        }

        if (playerTransform != null)
            SmoothRotateTowards(playerTransform.position);

        attackTimer -= Time.deltaTime;
        if (attackTimer <= 0f && !swingInProgress)
        {
            attackTimer = attackInterval;
            StartCoroutine(SwingAttack());
        }

        // Player backed far away — resume chase
        if (hDist > attackRange * 2.8f)
            TransitionToChase();
    }

    // ── Transitions (each owns animation + agent setup) ───────

    void TransitionToPatrol()
    {
        currentState  = CreatureState.Patrol;
        if (agent.enabled && agent.isOnNavMesh)
            agent.speed = data != null ? data.moveSpeed : 3f;
        PickNewPatrolTarget();
        SetAnimation(AnimWalk, 0.2f);
    }

    void TransitionToChase()
    {
        currentState = CreatureState.Chase;
        if (agent.enabled && agent.isOnNavMesh)
        {
            agent.speed = data != null ? data.runSpeed : 6f;
            // Cancel any patrol path immediately so the creature beelines for the player
            agent.ResetPath();
            if (playerTransform != null)
                agent.SetDestination(playerTransform.position);
        }
        pathUpdateTimer = 0f;
        alertIndicator?.ShowAlert();
        SFXManager.PlayZombieGrowl();
        SetAnimation(AnimRun, 0.1f);
    }

    void TransitionToAttack()
    {
        currentState = CreatureState.Attack;
        if (agent.enabled && agent.isOnNavMesh)
        {
            // Warp to current position — resets the agent's internal velocity and
            // path state completely, which is the only reliable way to stop
            // NavMeshAgent momentum without a multi-frame coast.
            agent.Warp(transform.position);
            agent.speed = attackShuffleSpeed;
        }
        attackTimer     = firstAttackDelay;
        pathUpdateTimer = 0.25f;        // don't move again for a moment after stopping
        SetAnimation(AnimIdle, 0.15f);
    }

    // ── Damage reaction ───────────────────────────────────────

    protected override void OnDamaged()
    {
        if (currentState == CreatureState.Patrol)
            TransitionToChase();
    }

    // ── Swing coroutine ───────────────────────────────────────

    IEnumerator SwingAttack()
    {
        swingInProgress = true;

        // Snap into the attack animation quickly
        PlayAnimation(AnimAttack, 0.05f);

        // Wait for the hit frame
        yield return new WaitForSeconds(attackHitDelay);

        // Deal damage if still in range and alive
        if (currentState == CreatureState.Attack && playerTransform != null)
        {
            float dist = Vector3.Distance(transform.position, playerTransform.position);
            if (dist <= (data != null ? data.attackRange * 1.8f : 4f))
            {
                if (cachedPlayerVitals == null)
                    cachedPlayerVitals = playerTransform.GetComponent<PlayerVitals>();
                cachedPlayerVitals?.TakeDamage(data != null ? data.damage : 10f);
            }
        }

        // Wait for rest of animation before returning to idle stance
        yield return new WaitForSeconds(attackRecoveryTime);

        // Only return to idle if still in attack state (not dead / chasing)
        if (currentState == CreatureState.Attack)
            PlayAnimation(AnimIdle, 0.1f);

        swingInProgress = false;
    }

    // ── Animation helpers ─────────────────────────────────────

    /// <summary>
    /// Sets the animation only if the state has changed, preventing
    /// redundant CrossFades that chop up running/walking cycles.
    /// </summary>
    void SetAnimation(string clipName, float blendTime = 0.2f)
    {
        if (currentState == lastAnimState) return;
        lastAnimState = currentState;
        PlayAnimation(clipName, blendTime);
    }

    /// <summary>Suppress the base tip-over — the Animator death clip handles it.</summary>
    protected override void OnDeadUpdate() { }

    // ── Death ─────────────────────────────────────────────────

    protected override void Die()
    {
        swingInProgress = false;
        StopAllCoroutines();
        base.Die();
        PlayAnimation(AnimDeath, 0.1f);
        SpawnShadowEssence();
    }

    // ── Helpers ───────────────────────────────────────────────

    void SmoothRotateTowards(Vector3 worldTarget)
    {
        Vector3 dir = worldTarget - transform.position;
        dir.y = 0f;
        if (dir.sqrMagnitude < 0.01f) return;
        Quaternion target = Quaternion.LookRotation(dir);
        transform.rotation = Quaternion.RotateTowards(
            transform.rotation, target, turnSpeed * Time.deltaTime);
    }

    void PickNewPatrolTarget()
    {
        if (agent == null || !agent.enabled || !agent.isOnNavMesh) return;
        if (TryGetRandomNavMeshPoint(transform.position, patrolRadius, out Vector3 point))
            agent.SetDestination(point);
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

        PickupItem pickup = pickupObj.AddComponent<PickupItem>();
        pickup.itemData   = essenceItem;
        pickup.quantity   = 1;
        pickup.promptText = "Pick up Shadow Essence";

        GameObject visual = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        visual.transform.SetParent(pickupObj.transform, false);
        visual.transform.localScale = Vector3.one * 0.3f;
        Destroy(visual.GetComponent<Collider>());

        Shader shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
        Material mat  = new Material(shader);
        mat.color = new Color(0.45f, 0f, 0.85f);
        if (mat.HasProperty("_BaseColor"))
            mat.SetColor("_BaseColor", new Color(0.45f, 0f, 0.85f));
        visual.GetComponent<Renderer>().material = mat;

        pickupObj.AddComponent<PickupBob>();
    }
}
