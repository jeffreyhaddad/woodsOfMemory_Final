using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    /// <summary>Set to true to block all player input (e.g. when inventory is open).</summary>
    public static bool inputBlocked = false;

    [Header("Movement Speeds")]
    public float walkSpeed = 2f;
    public float runSpeed = 5f;
    public float crouchSpeed = 1f;

    [Header("Jump Settings")]
    public float jumpForce = 5f;
    public float gravity = -9.81f;

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

    void Start()
    {
        controller = GetComponent<CharacterController>();
        animator = GetComponentInChildren<Animator>();
        vitals = GetComponent<PlayerVitals>();

        standControllerHeight = controller.height;
        standCenterY = controller.center.y;
    }

    void Update()
    {
        if (inputBlocked)
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

        // Reset vertical velocity when grounded
        if (grounded && verticalVelocity < 0f)
            verticalVelocity = -2f;

        // Jump — works from any state (idle, walk, run, crouch)
        bool canJump = vitals == null || vitals.CanJump;
        if (Input.GetKeyDown(KeyCode.Space) && grounded && canJump)
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
            animator.CrossFade("Idle Blend", 0.15f);
        }

        // Crouch toggle — only when grounded and not jumping
        if (Input.GetKeyDown(KeyCode.LeftControl) && grounded)
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
        float horizontal = Input.GetAxis("Horizontal");
        float vertical = Input.GetAxis("Vertical");

        // Create movement direction
        Vector3 move = transform.right * horizontal + transform.forward * vertical;

        // Clamp so diagonal movement doesn't exceed 1
        if (move.magnitude > 1f)
            move.Normalize();

        // Determine speed — no running while crouched or out of stamina
        bool wantsToRun = Input.GetKey(KeyCode.LeftShift) && !isCrouching;
        bool isRunning = wantsToRun && (vitals == null || vitals.CanRun);
        float speed;
        if (isCrouching)
            speed = crouchSpeed;
        else if (isRunning)
            speed = runSpeed;
        else
            speed = walkSpeed;

        // Apply gravity
        verticalVelocity += gravity * Time.deltaTime;

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
            // Check object name/tag for common surface types
            string name = hit.collider.gameObject.name.ToLower();
            if (name.Contains("wood") || name.Contains("floor") || name.Contains("plank") ||
                name.Contains("cabin") || name.Contains("bridge"))
                return GroundType.Wood;
            if (name.Contains("stone") || name.Contains("rock") || name.Contains("cave") ||
                name.Contains("path") || name.Contains("road"))
                return GroundType.Stone;

            // Check if we're on terrain — use texture splatmap
            Terrain terrain = hit.collider.GetComponent<Terrain>();
            if (terrain != null)
                return GetTerrainGroundType(terrain, hit.point);
        }

        return GroundType.Grass; // Default outdoor surface
    }

    GroundType GetTerrainGroundType(Terrain terrain, Vector3 worldPos)
    {
        TerrainData data = terrain.terrainData;
        if (data.alphamapLayers == 0) return GroundType.Grass;

        // Convert world position to terrain-local coordinates
        Vector3 terrainPos = worldPos - terrain.transform.position;
        int mapX = Mathf.Clamp(Mathf.RoundToInt(terrainPos.x / data.size.x * data.alphamapWidth), 0, data.alphamapWidth - 1);
        int mapZ = Mathf.Clamp(Mathf.RoundToInt(terrainPos.z / data.size.z * data.alphamapHeight), 0, data.alphamapHeight - 1);

        float[,,] splatmap = data.GetAlphamaps(mapX, mapZ, 1, 1);

        // Find the dominant texture layer
        int dominantLayer = 0;
        float maxWeight = 0f;
        for (int i = 0; i < splatmap.GetLength(2); i++)
        {
            if (splatmap[0, 0, i] > maxWeight)
            {
                maxWeight = splatmap[0, 0, i];
                dominantLayer = i;
            }
        }

        // Map terrain layer names to ground types
        if (dominantLayer < data.terrainLayers.Length)
        {
            string layerName = data.terrainLayers[dominantLayer].name.ToLower();
            if (layerName.Contains("rock") || layerName.Contains("stone") || layerName.Contains("gravel"))
                return GroundType.Stone;
            if (layerName.Contains("wood") || layerName.Contains("plank"))
                return GroundType.Wood;
            if (layerName.Contains("dirt") || layerName.Contains("mud") || layerName.Contains("sand"))
                return GroundType.Default;
        }

        return GroundType.Grass;
    }
}
