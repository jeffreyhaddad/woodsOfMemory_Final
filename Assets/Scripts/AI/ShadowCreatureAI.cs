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

    // Animation state tracking (avoids calling CrossFade every frame)
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
        dayNight         = FindAnyObjectByType<DayNightCycle>();
        cachedPlayerVitals = playerTransform != null
            ? playerTransform.GetComponent<PlayerVitals>() : null;

        // Disable root motion so the Animator doesn't fight the NavMeshAgent (causes jitter)
        if (animator != null)
            animator.applyRootMotion = false;

        // Prevent SkinnedMeshRenderer from being incorrectly culled when its bounds go stale
        foreach (SkinnedMeshRenderer smr in GetComponentsInChildren<SkinnedMeshRenderer>())
            smr.updateWhenOffscreen = true;

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
                if (!agent.pathPending && agent.remainingDistance < 1.5f)
                    PickNewPatrolTarget();

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
                        pathUpdateTimer = 0.25f;
                    }
                }

                if (hDist <= data.attackRange)
                {
                    currentState = CreatureState.Attack;
                    attackTimer  = 0f;
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
                        cachedPlayerVitals.TakeDamage(data.damage);
                    attackTimer = attackInterval;
                }

                if (hDist > data.attackRange * 2f)
                {
                    currentState = CreatureState.Chase;
                    agent.speed  = data.runSpeed;
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
        if (currentState == lastAnimState) return;
        lastAnimState = currentState;

        switch (currentState)
        {
            case CreatureState.Patrol: PlayAnimation(AnimWalk);   break;
            case CreatureState.Chase:  PlayAnimation(AnimRun);    break;
            case CreatureState.Attack: PlayAnimation(AnimAttack); break;
        }
    }

    /// <summary>Suppress the base tip-over effect — the Animator death clip handles it.</summary>
    protected override void OnDeadUpdate() { }

    // ── Death ─────────────────────────────────────────────────

    protected override void Die()
    {
        // Handle death manually so we can play the animator death clip
        // and skip the base class tip-over behaviour.
        currentState = CreatureState.Dead;
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

    private void PickNewPatrolTarget()
    {
        if (agent == null || !agent.enabled || !agent.isOnNavMesh) return;
        if (TryGetRandomNavMeshPoint(transform.position, patrolRadius, out Vector3 point))
            agent.SetDestination(point);
    }
}
