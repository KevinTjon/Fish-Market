using UnityEngine;
using UnityEngine.InputSystem;

public class FishMarketPlayerController : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float moveSpeed = 5f;
    
    [Header("Components")]
    [SerializeField] private Animator animator;
    [SerializeField] private SpriteRenderer spriteRenderer;

    private PlayerControls playerControls;
    private Vector2 movement;
    private string currentDirection = "Down"; // Track last direction for idle

    private void Awake()
    {
        playerControls = new PlayerControls();
        
        playerControls.Player.Move.performed += ctx => 
        {
            movement = ctx.ReadValue<Vector2>();
            UpdateAnimation(movement);
        };
        
        playerControls.Player.Move.canceled += ctx => 
        {
            movement = Vector2.zero;
            // Stop walking animation
            animator.SetBool("IsWalking", false);
            animator.Play("Idle", 0, 0f);
        };
    }

    private void UpdateAnimation(Vector2 input)
    {
        bool isMoving = input.magnitude > 0;
        
        if (isMoving)
        {
            animator.SetBool("IsWalking", true);
            
            // Determine primary direction and play the appropriate animation
            if (input.y > 0 && input.y > Mathf.Abs(input.x))
            {
                SetDirection("Up");
                animator.Play("Walk_Up", 0); // Force play the walking animation
            }
            else if (input.y < 0 && -input.y > Mathf.Abs(input.x))
            {
                SetDirection("Down");
                animator.Play("Walk_Down", 0);
            }
            else if (input.x < 0 && -input.x > Mathf.Abs(input.y))
            {
                SetDirection("Left");
                animator.Play("Walk_Left", 0);
            }
            else if (input.x > 0 && input.x > Mathf.Abs(input.y))
            {
                SetDirection("Right");
                animator.Play("Walk_Right", 0);
            }
        }
    }

    private void SetDirection(string direction)
    {
        // Reset all directions
        animator.SetBool("Up", false);
        animator.SetBool("Down", false);
        animator.SetBool("Left", false);
        animator.SetBool("Right", false);

        // Set new direction
        animator.SetBool(direction, true);
        currentDirection = direction;
    }

    private void OnEnable()
    {
        playerControls.Enable();
    }

    private void OnDisable()
    {
        playerControls.Disable();
    }

    private void Start()
    {
        if (animator == null) animator = GetComponent<Animator>();
        if (spriteRenderer == null) spriteRenderer = GetComponent<SpriteRenderer>();
        
        // Set initial idle state
        animator.SetBool("IsWalking", false);
        SetDirection("Down"); // Start facing down
    }

    private void FixedUpdate()
    {
        transform.position += new Vector3(movement.x, movement.y, 0) * moveSpeed * Time.fixedDeltaTime;
    }
} 