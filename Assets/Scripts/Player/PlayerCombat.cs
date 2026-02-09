using UnityEngine;

public class PlayerCombat : MonoBehaviour
{
    [Header("Melee Attack")]
    public float attackDamage = 25f;
    public float attackRange = 3f;
    public float attackRadius = 1f;
    public float attackCooldown = 0.5f;
    public float attackStaminaCost = 5f;

    private float lastAttackTime = -999f;
    private PlayerVitals vitals;
    private float hitFlashTimer;

    void Start()
    {
        vitals = GetComponent<PlayerVitals>();
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

            if (vitals != null)
                vitals.DrainStamina(attackStaminaCost);
        }
    }

    void Attack()
    {
        Vector3 origin = transform.position + Vector3.up * 1.5f;
        Vector3 direction = transform.forward;

        // SphereCast — wider hit area, much easier to land melee hits
        if (Physics.SphereCast(origin, attackRadius, direction, out RaycastHit hit, attackRange))
        {
            CreatureAI creature = hit.collider.GetComponentInParent<CreatureAI>();
            if (creature != null)
            {
                creature.TakeDamage(attackDamage);
                hitFlashTimer = 0.3f;
                Debug.Log("Hit " + creature.data.creatureName + " for " + attackDamage +
                    " (HP: " + creature.CurrentHealth + "/" + creature.data.maxHealth + ")");
            }
        }
    }

    void OnGUI()
    {
        // Brief red flash when you land a hit
        if (hitFlashTimer > 0f)
        {
            hitFlashTimer -= Time.deltaTime;
            float alpha = hitFlashTimer / 0.3f * 0.3f;
            GUI.color = new Color(1f, 0f, 0f, alpha);
            GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), Texture2D.whiteTexture);
            GUI.color = Color.white;
        }
    }
}
