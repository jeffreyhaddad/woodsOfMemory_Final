using System.Collections;
using UnityEngine;

public class PlayerCombat : MonoBehaviour
{
    [Header("Melee Attack")]
    public float attackDamage = 25f;
    public float attackRange = 3.5f;
    public float attackRadius = 2f;
    public float attackCooldown = 0.5f;
    public float attackStaminaCost = 5f;

    [Header("Tool Swing Speed")]
    [Tooltip("Animation speed multiplier for axe/pickaxe swings. 1 = normal, 1.5 = 50% faster.")]
    public float toolSwingSpeed = 1.5f;

    private float lastAttackTime = -999f;
    private PlayerVitals vitals;
    private Animator animator;
    private float hitFlashTimer;
    private static readonly Color impactColor = new Color(1f, 1f, 0.85f); // warm white


    void Start()
    {
        vitals    = GetComponent<PlayerVitals>();
        animator  = GetComponentInChildren<Animator>();
    }

    void Update()
    {
        if (PlayerMovement.inputBlocked) return;

        if (Input.GetMouseButtonDown(0) && Time.time >= lastAttackTime + attackCooldown)
        {
            if (vitals != null && vitals.Stamina < attackStaminaCost)
                return;

            Attack();
            lastAttackTime = Time.time;
            SFXManager.PlaySwing();

            if (vitals != null)
                vitals.DrainStamina(attackStaminaCost);
        }
    }

    void Attack()
    {
        string equipped = EquipmentManager.Instance?.EquippedTool?.itemName ?? "";
        bool hasWeapon  = EquipmentManager.Instance?.EquippedWeapon != null;

        // Play weapon-appropriate attack animation
        if (animator != null)
        {
            bool isTool = equipped.IndexOf("Pickaxe", System.StringComparison.OrdinalIgnoreCase) >= 0
                       || equipped.IndexOf("Axe",     System.StringComparison.OrdinalIgnoreCase) >= 0;

            if (isTool) animator.speed = toolSwingSpeed;

            if (equipped.IndexOf("Pickaxe", System.StringComparison.OrdinalIgnoreCase) >= 0)
                animator.CrossFade("PickaxeAttack", 0.1f);
            else if (hasWeapon || equipped.IndexOf("Axe", System.StringComparison.OrdinalIgnoreCase) >= 0)
                animator.CrossFade("AxeAttack", 0.1f);

            if (isTool) StartCoroutine(ResetAnimSpeed(attackCooldown));
        }

        // Degrade durability on axe/pickaxe swings
        if (!string.IsNullOrEmpty(equipped) && ToolDurabilityManager.Instance != null)
        {
            if (equipped.IndexOf("Axe", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
                equipped.IndexOf("Pickaxe", System.StringComparison.OrdinalIgnoreCase) >= 0)
            {
                ToolDurabilityManager.Instance.UseTool(equipped);
            }
        }

        float totalDamage = attackDamage;
        if (EquipmentManager.Instance != null)
            totalDamage += EquipmentManager.Instance.WeaponDamageBonus
                         + EquipmentManager.Instance.ToolDamageBonus;

        // Small shake on every swing so unarmed attacks have tactile feel
        CameraFollow.Shake(0.08f, 0.03f);

        // Direct distance check against all living creatures via the static registry.
        Vector3 attackCenter = transform.position
                             + transform.forward * (attackRange * 0.6f)
                             + Vector3.up * 1.5f;

        var creatures = CreatureAI.All;

        foreach (CreatureAI creature in creatures)
        {
            if (creature == null || creature.State == CreatureState.Dead) continue;
            float dist = Vector3.Distance(attackCenter,
                             creature.transform.position + Vector3.up * 1.5f);
            if (dist <= attackRadius + 1f)
            {
                creature.TakeDamage(totalDamage);
                hitFlashTimer = 0.15f;
                CameraFollow.Shake(0.15f, 0.07f);
                SFXManager.PlayHit();
                break;
            }
        }
    }

    IEnumerator ResetAnimSpeed(float delay)
    {
        yield return new WaitForSeconds(delay);
        if (animator != null) animator.speed = 1f;
    }

    void OnGUI()
    {
        // Brief warm-white impact flash when you land a hit
        if (hitFlashTimer > 0f)
        {
            hitFlashTimer -= Time.deltaTime;
            // Spike quickly then fade — peaks at full duration, gone by 0
            float t = hitFlashTimer / 0.15f;
            float alpha = Mathf.Sin(t * Mathf.PI) * 0.45f;
            GUI.color = new Color(impactColor.r, impactColor.g, impactColor.b, alpha);
            GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), Texture2D.whiteTexture);
            GUI.color = Color.white;
        }
    }
}
