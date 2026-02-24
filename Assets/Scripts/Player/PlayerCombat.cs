using UnityEngine;

public class PlayerCombat : MonoBehaviour
{
    [Header("Melee Attack")]
    public float attackDamage = 25f;
    public float attackRange = 3.5f;
    public float attackRadius = 2f;
    public float attackCooldown = 0.5f;
    public float attackStaminaCost = 5f;

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
        // Play weapon-appropriate attack animation
        if (animator != null)
        {
            string equipped = EquipmentManager.Instance?.EquippedTool?.itemName ?? "";
            bool hasWeapon  = EquipmentManager.Instance?.EquippedWeapon != null;

            if (equipped.IndexOf("Pickaxe", System.StringComparison.OrdinalIgnoreCase) >= 0)
                animator.CrossFade("PickaxeAttack", 0.1f);
            else if (hasWeapon || equipped.IndexOf("Axe", System.StringComparison.OrdinalIgnoreCase) >= 0)
                animator.CrossFade("AxeAttack", 0.1f);
        }

        // Overlap sphere in front of + at chest height — hits anything in range
        // regardless of exact facing angle, far more reliable than SphereCast
        Vector3 center = transform.position
                       + transform.forward * (attackRange * 0.6f)
                       + Vector3.up * 1.2f;

        float totalDamage = attackDamage;
        if (EquipmentManager.Instance != null)
            totalDamage += EquipmentManager.Instance.WeaponDamageBonus;

        Collider[] hits = Physics.OverlapSphere(center, attackRadius);
        foreach (Collider col in hits)
        {
            CreatureAI creature = col.GetComponentInParent<CreatureAI>();
            if (creature != null)
            {
                creature.TakeDamage(totalDamage);
                hitFlashTimer = 0.15f;
                SFXManager.PlayHit();
                break; // one creature per swing
            }
        }
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
