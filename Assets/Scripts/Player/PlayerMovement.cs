using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    public float walkSpeed = 2f;
    public float runSpeed = 5f;
    
    private CharacterController controller;
    private Animator animator;
    
    void Start()
    {
        controller = GetComponent<CharacterController>();
        animator = GetComponent<Animator>();
    }
    
    void Update()
    {
        // Get WASD input
        float horizontal = Input.GetAxis("Horizontal");
        float vertical = Input.GetAxis("Vertical");

        // Create movement direction
        Vector3 move = transform.right * horizontal + transform.forward * vertical;

        // Clamp so diagonal movement doesn't exceed 1
        if (move.magnitude > 1f)
            move.Normalize();

        // Check if running
        bool isRunning = Input.GetKey(KeyCode.LeftShift);
        float speed = isRunning ? runSpeed : walkSpeed;

        // Move
        controller.Move(move * speed * Time.deltaTime);

        // Update animator - pass actual speed for proper transitions
        float currentSpeed = move.magnitude * speed;
        animator.SetFloat("Speed", currentSpeed);
    }
}