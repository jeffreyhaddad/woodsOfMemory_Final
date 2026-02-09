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
        if (inputBlocked) return;

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
    }
}
