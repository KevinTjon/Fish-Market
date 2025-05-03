using UnityEngine;
using System.Collections;
using Market;

[RequireComponent(typeof(Rigidbody2D))]
public class SimpleCustomerMovement : MonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] private float moveSpeed = 3f;
    [SerializeField] private float stoppingDistance = 0.1f;
    [SerializeField] private float arrivalDistance = 0.5f;
    
    [Header("Animation")]
    [SerializeField] private Animator animator;

    private Rigidbody2D rb;
    private Vector2 targetPosition;
    private bool isMoving = false;
    private System.Action onDestinationReached;
    private Vector2 lastDirection = Vector2.down;

    // Animation parameter names
    private const string IS_WALKING = "IsWalking";
    private const string DIRECTION_X = "DirectionX";
    private const string DIRECTION_Y = "DirectionY";
    private const string WALK_UP = "Walk_Up";
    private const string WALK_DOWN = "Walk_Down";
    private const string WALK_LEFT = "Walk_Left";
    private const string WALK_RIGHT = "Walk_Right";

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.gravityScale = 0;
        rb.drag = 0;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        rb.constraints = RigidbodyConstraints2D.FreezeRotation;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;

        if (animator == null)
        {
            animator = GetComponent<Animator>();
        }

        // Set initial animation direction
        UpdateAnimation(lastDirection);
    }

    private void FixedUpdate()
    {
        if (!isMoving) return;

        Vector2 currentPosition = rb.position;
        Vector2 direction = (targetPosition - currentPosition).normalized;
        float distance = Vector2.Distance(currentPosition, targetPosition);

        if (distance > stoppingDistance)
        {
            // Move towards target
            rb.velocity = direction * moveSpeed;

            // Update animation if there's significant movement
            if (direction.magnitude > 0.01f)
            {
                UpdateAnimation(direction);
                lastDirection = direction;
            }
        }
        else
        {
            // Reached destination
            StopMoving();
            onDestinationReached?.Invoke();
        }
    }

    private void UpdateAnimation(Vector2 direction)
    {
        if (animator == null) return;

        animator.SetBool(IS_WALKING, direction.magnitude > 0.01f);
        
        // Set direction parameters for blend trees
        animator.SetFloat(DIRECTION_X, direction.x);
        animator.SetFloat(DIRECTION_Y, direction.y);

        // Play the appropriate directional animation
        if (direction.y > 0 && direction.y > Mathf.Abs(direction.x))
        {
            animator.Play(WALK_UP);
        }
        else if (direction.y < 0 && -direction.y > Mathf.Abs(direction.x))
        {
            animator.Play(WALK_DOWN);
        }
        else if (direction.x < 0 && -direction.x > Mathf.Abs(direction.y))
        {
            animator.Play(WALK_LEFT);
        }
        else if (direction.x > 0 && direction.x > Mathf.Abs(direction.y))
        {
            animator.Play(WALK_RIGHT);
        }
    }

    public void MoveTo(Vector2 position, System.Action onReached = null)
    {
        targetPosition = position;
        onDestinationReached = onReached;
        isMoving = true;
        
        // Calculate initial direction for animation
        Vector2 direction = ((Vector2)position - rb.position).normalized;
        UpdateAnimation(direction);
    }

    public void StopMoving()
    {
        isMoving = false;
        rb.velocity = Vector2.zero;
        UpdateAnimation(Vector2.zero);
    }

    public bool HasReachedPosition(Vector2 position)
    {
        return Vector2.Distance(rb.position, position) <= arrivalDistance;
    }
} 