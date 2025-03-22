using UnityEngine;
using UnityEngine.InputSystem;

public class MarketPlayerController : MonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] private float moveSpeed = 5f;
    
    [Header("Components")]
    private Rigidbody2D rb;
    private Animator animator;
    private SpriteRenderer spriteRenderer;
    
    // Animation parameter names
    private const string HORIZONTAL = "Horizontal";
    private const string VERTICAL = "Vertical";
    private const string SPEED = "Speed";
    
    private Vector2 movement;
    private PlayerInput playerInput;
    
    private void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        
        // Configure Rigidbody2D for top-down movement
        rb.gravityScale = 0f;
        rb.constraints = RigidbodyConstraints2D.FreezeRotation;
        
        // Debug initial state
        Debug.Log("Player initialized with animator: " + (animator != null));
    }

    public void OnMove(InputValue value)
    {
        movement = value.Get<Vector2>();
        
        // Debug input values
        Debug.Log($"Movement Input - X: {movement.x}, Y: {movement.y}");
        
        // Animation
        animator.SetFloat(HORIZONTAL, movement.x);
        animator.SetFloat(VERTICAL, movement.y);
        animator.SetFloat(SPEED, movement.sqrMagnitude);
        
        // Debug animation parameters
        Debug.Log($"Animation Parameters - H: {animator.GetFloat(HORIZONTAL)}, V: {animator.GetFloat(VERTICAL)}, Speed: {animator.GetFloat(SPEED)}");
        
        // Flip sprite based on movement direction
        if (movement.x != 0)
        {
            spriteRenderer.flipX = movement.x < 0;
        }
    }
    
    private void FixedUpdate()
    {
        // Movement
        rb.MovePosition(rb.position + movement.normalized * moveSpeed * Time.fixedDeltaTime);
    }
} 