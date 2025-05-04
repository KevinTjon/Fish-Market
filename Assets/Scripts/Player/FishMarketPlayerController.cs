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
    private Rigidbody2D rb;
    private BoxCollider2D boxCollider;

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

        // Add interaction handling
        playerControls.Player.Interact.performed += ctx => HandleInteraction();

        // Get or add required components
        rb = GetComponent<Rigidbody2D>();
        if (rb == null)
        {
            rb = gameObject.AddComponent<Rigidbody2D>();
        }
        
        boxCollider = GetComponent<BoxCollider2D>();
        if (boxCollider == null)
        {
            boxCollider = gameObject.AddComponent<BoxCollider2D>();
            // Set a default size for the collider
            boxCollider.size = new Vector2(0.8f, 0.8f);
        }

        // Configure Rigidbody2D for top-down movement
        rb.gravityScale = 0;
        rb.drag = 0; // No drag needed for direct movement
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        rb.constraints = RigidbodyConstraints2D.FreezeRotation;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate; // Smoother movement
    }

    private void HandleInteraction()
    {
        // Find any nearby interactables
        Collider2D[] colliders = Physics2D.OverlapCircleAll(transform.position, 2f);
        foreach (Collider2D collider in colliders)
        {
            MarketStallInteractable interactable = collider.GetComponent<MarketStallInteractable>();
            if (interactable != null)
            {
                interactable.TryInteract();
                break; // Only interact with the first one found
            }
        }
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
                animator.Play("Walk_Up", 0);
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
        // Set velocity directly for responsive movement
        if (movement != Vector2.zero)
        {
            // Normalize for consistent speed in all directions
            rb.velocity = movement.normalized * moveSpeed;
        }
        else
        {
            // Stop immediately when no input
            rb.velocity = Vector2.zero;
        }
    }
} 