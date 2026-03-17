using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    /// <summary>Set to true to block all player input (camera, attacks, actions).</summary>
    public static bool inputBlocked = false;

    /// <summary>Set to true to also freeze movement (pause, death, cutscenes).
    /// Inventory/journal/crafting only set inputBlocked, not this.</summary>
    public static bool movementBlocked = false;
    public  static bool isInwater = false;

    float[] underWaterMovementAndGravity = { 1f, 2.5f, 2.5f, 6f, -1f };

    [Header("Movement Speeds")]
    public float walkSpeed = 4f;
    public float runSpeed = 10f;
    public float crouchSpeed = 5f;

    [Header("Jump Settings")]
    public float jumpForce = 12f;
    public float gravity = -20f;
    [Tooltip("Extra gravity multiplier when falling for snappier landings")]
    public float fallMultiplier = 1.5f;

    [Header("Crouch Settings")]
    public float crouchControllerHeight = 1.2f;
    public float crouchCenterY = 0.6f;

    private CharacterController controller;
    private Animator animator;
    private PlayerVitals vitals;
    private bool isCrouching = false;
    private bool isJumping = false;
    private bool hasLeftGround = false;
    private float standControllerHeight;
    private float standCenterY;
    private float verticalVelocity;
    private float footstepTimer;

    // Cached terrain data for footstep ground detection
    private Terrain cachedTerrain;
    private TerrainData cachedTerrainData;

    void Start()
    {
        controller = GetComponent<CharacterController>();
        animator = GetComponentInChildren<Animator>();
        vitals = GetComponent<PlayerVitals>();

        standControllerHeight = controller.height;
        standCenterY = controller.center.y;

        // Ensure root motion never physically moves the CharacterController
        if (animator != null)
            animator.applyRootMotion = false;

        cachedTerrain = Terrain.activeTerrain;
        if (cachedTerrain != null)
            cachedTerrainData = cachedTerrain.terrainData;
    }

    void Update()
    {
        if (isInwater)
        {
            walkSpeed = underWaterMovementAndGravity[0];
            runSpeed = underWaterMovementAndGravity[1];
            crouchSpeed = underWaterMovementAndGravity[2];
            jumpForce = underWaterMovementAndGravity[3];
            gravity = underWaterMovementAndGravity[4];
        }
        if (!isInwater)
        {
            walkSpeed = 6f;
            runSpeed = 12f;
            crouchSpeed = 10f;
            jumpForce = 12f;
            gravity = -20f;

        }
        if (movementBlocked)
        {
            // Zero out animator so player stops walking/running visually
            animator.SetFloat("Speed", 0f);
            animator.SetFloat("Direction", 0f);
            animator.SetFloat("Vertical", 0f);

            // Still apply gravity so the player doesn't float
            if (controller.isGrounded && verticalVelocity < 0f)
                verticalVelocity = -2f;
            verticalVelocity += gravity * Time.deltaTime;
            controller.Move(Vector3.up * verticalVelocity * Time.deltaTime);
            return;
        }

        bool grounded = controller.isGrounded;
        animator.SetBool("isGrounded", grounded);

        // Stick to ground when grounded
        if (grounded && verticalVelocity < 0f)
            verticalVelocity = -2f;

        // Jump — works from any state (idle, walk, run, crouch)
        bool canJump = vitals == null || vitals.CanJump;
        if (Input.GetKeyDown(SettingsManager.GetKey(GameAction.Jump)) && grounded && canJump && !movementBlocked)
        {
            if (vitals != null) vitals.UseStaminaForJump();
            verticalVelocity = jumpForce;
            isJumping = true;
            hasLeftGround = false;
            animator.SetBool("isJumping", true);
            animator.CrossFade("Jump", 0.1f);

            // Uncrouch when jumping
            if (isCrouching)
            {
                isCrouching = false;
                animator.SetBool("isCrouching", false);
                controller.height = standControllerHeight;
                controller.center = new Vector3(controller.center.x, standCenterY, controller.center.z);
            }
        }

        // Track when character actually leaves the ground after jumping
        if (isJumping && !grounded)
            hasLeftGround = true;

        // Clear jump flag only after character has left the ground and landed again
        if (isJumping && hasLeftGround && grounded)
        {
            isJumping = false;
            hasLeftGround = false;
            animator.SetBool("isJumping", false);
            // CrossFade back to the movement blend tree (handles idle/walk/run via Speed param)
            animator.CrossFade("Idle Blend", 0.2f);
        }

        // Crouch toggle — only when grounded and not jumping
        if (Input.GetKeyDown(SettingsManager.GetKey(GameAction.Crouch)) && grounded && !movementBlocked)
        {
            isCrouching = !isCrouching;
            animator.SetBool("isCrouching", isCrouching);

            if (isCrouching)
            {
                controller.height = crouchControllerHeight;
                controller.center = new Vector3(controller.center.x, crouchCenterY, controller.center.z);
            }
            else
            {
                controller.height = standControllerHeight;
                controller.center = new Vector3(controller.center.x, standCenterY, controller.center.z);
            }
        }

        // Get WASD input
        float horizontal = movementBlocked ? 0f : Input.GetAxis("Horizontal");
        float vertical   = movementBlocked ? 0f : Input.GetAxis("Vertical");

        // Create movement direction
        Vector3 move = transform.right * horizontal + transform.forward * vertical;

        // Clamp so diagonal movement doesn't exceed 1
        if (move.magnitude > 1f)
            move.Normalize();

        // Determine speed — no running while crouched or out of stamina
        bool wantsToRun = Input.GetKey(SettingsManager.GetKey(GameAction.Sprint)) && !isCrouching;
        bool isRunning = wantsToRun && (vitals == null || vitals.CanRun);
        float speed;
        if (isCrouching)
            speed = crouchSpeed;
        else if (isRunning)
            speed = runSpeed;
        else
            speed = walkSpeed;

        // Apply gravity — stronger when falling for a snappier, less floaty jump
        float gravityScale = (verticalVelocity < 0f) ? fallMultiplier : 1f;
        verticalVelocity += gravity * gravityScale * Time.deltaTime;

        // Move (horizontal + vertical combined)
        Vector3 finalMove = move * speed + Vector3.up * verticalVelocity;
        controller.Move(finalMove * Time.deltaTime);

        // Stamina drain/regen
        if (vitals != null)
        {
            if (isRunning && move.magnitude > 0.01f)
                vitals.DrainStamina(vitals.staminaDrainRate * Time.deltaTime);
            else
                vitals.RegenStamina(vitals.staminaRegenRate * Time.deltaTime);
        }

        // Normalized speed for animator (0 = idle, 1 = walk, 2 = run)
        float normalizedSpeed;
        if (move.magnitude < 0.01f)
            normalizedSpeed = 0f;
        else if (isRunning)
            normalizedSpeed = 2f;
        else
            normalizedSpeed = 1f;

        // Update animator with damping for smooth blending
        animator.SetFloat("Speed", normalizedSpeed, 0.1f, Time.deltaTime);
        animator.SetFloat("Direction", horizontal, 0.1f, Time.deltaTime);
        animator.SetFloat("Vertical", vertical, 0.1f, Time.deltaTime);

        // Footstep sounds while moving on ground
        if (grounded && move.magnitude > 0.01f)
        {
            float stepInterval = isRunning ? 0.32f : 0.5f;
            footstepTimer -= Time.deltaTime;
            if (footstepTimer <= 0f)
            {
                SFXManager.PlayFootstep(DetectGroundType());
                footstepTimer = stepInterval;
            }
        }
        else
        {
            footstepTimer = 0f;
        }
    }

    /// <summary>Detect what surface the player is standing on.</summary>
    GroundType DetectGroundType()
    {
        // Raycast down to find ground object
        if (Physics.Raycast(transform.position + Vector3.up * 0.2f, Vector3.down, out RaycastHit hit, 2f))
        {
            // Check object name using case-insensitive comparison (avoids ToLower() string alloc)
            string objName = hit.collider.gameObject.name;
            if (ContainsAnyCI(objName, "wood", "floor", "plank", "cabin", "bridge"))
                return GroundType.Wood;
            if (ContainsAnyCI(objName, "stone", "rock", "cave", "path", "road"))
                return GroundType.Stone;

            // Check if we're on terrain — use cached reference
            if (cachedTerrain != null && hit.collider.gameObject == cachedTerrain.gameObject)
                return GetTerrainGroundType(hit.point);
        }

        return GroundType.Grass;
    }

    static bool ContainsAnyCI(string source, params string[] terms)
    {
        for (int i = 0; i < terms.Length; i++)
        {
            if (source.IndexOf(terms[i], System.StringComparison.OrdinalIgnoreCase) >= 0)
                return true;
        }
        return false;
    }

    // Cache last splatmap query to avoid allocating a new float[,,] on every footstep
    private int lastSplatMapX = -1;
    private int lastSplatMapZ = -1;
    private int lastDominantLayer = -1;

    GroundType GetTerrainGroundType(Vector3 worldPos)
    {
        if (cachedTerrainData == null || cachedTerrainData.alphamapLayers == 0)
            return GroundType.Grass;

        Vector3 terrainPos = worldPos - cachedTerrain.transform.position;
        int mapX = Mathf.Clamp(Mathf.RoundToInt(terrainPos.x / cachedTerrainData.size.x * cachedTerrainData.alphamapWidth), 0, cachedTerrainData.alphamapWidth - 1);
        int mapZ = Mathf.Clamp(Mathf.RoundToInt(terrainPos.z / cachedTerrainData.size.z * cachedTerrainData.alphamapHeight), 0, cachedTerrainData.alphamapHeight - 1);

        // Re-query only when the grid cell changes — avoids GetAlphamaps allocation on every footstep
        int dominantLayer;
        if (mapX == lastSplatMapX && mapZ == lastSplatMapZ && lastDominantLayer >= 0)
        {
            dominantLayer = lastDominantLayer;
        }
        else
        {
            float[,,] splatmap = cachedTerrainData.GetAlphamaps(mapX, mapZ, 1, 1);
            dominantLayer = 0;
            float maxWeight = 0f;
            for (int i = 0; i < splatmap.GetLength(2); i++)
            {
                if (splatmap[0, 0, i] > maxWeight)
                {
                    maxWeight = splatmap[0, 0, i];
                    dominantLayer = i;
                }
            }
            lastSplatMapX = mapX;
            lastSplatMapZ = mapZ;
            lastDominantLayer = dominantLayer;
        }

        // Map terrain layer names to ground types (case-insensitive, no alloc)
        if (dominantLayer < cachedTerrainData.terrainLayers.Length)
        {
            string layerName = cachedTerrainData.terrainLayers[dominantLayer].name;
            if (ContainsAnyCI(layerName, "rock", "stone", "gravel"))
                return GroundType.Stone;
            if (ContainsAnyCI(layerName, "wood", "plank"))
                return GroundType.Wood;
            if (ContainsAnyCI(layerName, "dirt", "mud", "sand"))
                return GroundType.Default;
        }

        return GroundType.Grass;
    }
}
