using UnityEngine;
using System.Collections;

[RequireComponent(typeof(Animator))]
[RequireComponent(typeof(SpriteRenderer))]
[RequireComponent(typeof(Rigidbody2D))]
public class CustomerVisual : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float moveSpeed = 3f;
    [SerializeField] private float stoppingDistance = 0.1f;

    [Header("Customer Type Animations")]
    [SerializeField] private RuntimeAnimatorController budgetCustomerAnimator;
    [SerializeField] private RuntimeAnimatorController casualCustomerAnimator;
    [SerializeField] private RuntimeAnimatorController collectorCustomerAnimator;
    [SerializeField] private RuntimeAnimatorController wealthyCustomerAnimator;

    [Header("Visual Effects")]
    [SerializeField] private GameObject thoughtBubble;
    
    private Animator animator;
    private SpriteRenderer spriteRenderer;
    private Rigidbody2D rb;
    private Vector2 targetPosition;
    private bool isMoving = false;
    private Customer customerData;
    private Vector2 lastMovementDirection = Vector2.down; // Default facing down

    // Animation parameter names
    private const string IS_WALKING = "IsWalking";
    private const string DIRECTION_X = "DirectionX";
    private const string DIRECTION_Y = "DirectionY";

    private void Awake()
    {
        animator = GetComponent<Animator>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        rb = GetComponent<Rigidbody2D>();
        
        // Configure Rigidbody2D for top-down movement
        rb.gravityScale = 0;
        rb.drag = 0;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        rb.constraints = RigidbodyConstraints2D.FreezeRotation;

        // Set initial direction parameters
        animator.SetFloat(DIRECTION_X, lastMovementDirection.x);
        animator.SetFloat(DIRECTION_Y, lastMovementDirection.y);
    }

    public void Initialize(Customer customer)
    {
        customerData = customer;
        SetCustomerTypeVisuals(customer.Type);
    }

    private void SetCustomerTypeVisuals(Customer.CUSTOMERTYPE type)
    {
        RuntimeAnimatorController controller = type switch
        {
            Customer.CUSTOMERTYPE.BUDGET => budgetCustomerAnimator,
            Customer.CUSTOMERTYPE.CASUAL => casualCustomerAnimator,
            Customer.CUSTOMERTYPE.COLLECTOR => collectorCustomerAnimator,
            Customer.CUSTOMERTYPE.WEALTHY => wealthyCustomerAnimator,
            _ => budgetCustomerAnimator
        };

        if (controller != null)
        {
            animator.runtimeAnimatorController = controller;
            // Ensure we're showing the last movement direction
            animator.SetFloat(DIRECTION_X, lastMovementDirection.x);
            animator.SetFloat(DIRECTION_Y, lastMovementDirection.y);
        }
        else
        {
            Debug.LogWarning($"No animator controller assigned for customer type: {type}");
        }
    }

    public void MoveTo(Vector2 position)
    {
        targetPosition = position;
        isMoving = true;
        animator.SetBool(IS_WALKING, true);
    }

    private void Update()
    {
        if (!isMoving) return;

        Vector2 currentPosition = rb.position;
        Vector2 direction = (targetPosition - currentPosition).normalized;
        float distance = Vector2.Distance(currentPosition, targetPosition);

        if (distance > stoppingDistance)
        {
            // Move towards target
            rb.MovePosition(currentPosition + direction * moveSpeed * Time.deltaTime);

            // Update animation parameters and store last direction
            if (direction.magnitude > 0.01f) // Only update if there's significant movement
            {
                lastMovementDirection = direction;
                animator.SetFloat(DIRECTION_X, direction.x);
                animator.SetFloat(DIRECTION_Y, direction.y);
            }

            // Flip sprite if moving left
            spriteRenderer.flipX = direction.x < 0;
        }
        else
        {
            // Reached destination - keep the last direction for idle state
            isMoving = false;
            animator.SetBool(IS_WALKING, false);
            rb.velocity = Vector2.zero;
            
            // Keep the last direction parameters to maintain the idle pose
            animator.SetFloat(DIRECTION_X, lastMovementDirection.x);
            animator.SetFloat(DIRECTION_Y, lastMovementDirection.y);
        }
    }

    public void ShowThoughtBubble(bool show)
    {
        if (thoughtBubble != null)
        {
            thoughtBubble.SetActive(show);
        }
    }

    public void PlayEvaluatingAnimation()
    {
        // Store current direction parameters
        Vector2 currentDir = new Vector2(
            animator.GetFloat(DIRECTION_X),
            animator.GetFloat(DIRECTION_Y)
        );
        
        animator.SetTrigger("Evaluate");
        
        // Restore direction parameters after triggering animation
        animator.SetFloat(DIRECTION_X, currentDir.x);
        animator.SetFloat(DIRECTION_Y, currentDir.y);
    }

    public void PlayPurchaseAnimation()
    {
        Vector2 currentDir = new Vector2(
            animator.GetFloat(DIRECTION_X),
            animator.GetFloat(DIRECTION_Y)
        );
        
        animator.SetTrigger("Purchase");
        
        animator.SetFloat(DIRECTION_X, currentDir.x);
        animator.SetFloat(DIRECTION_Y, currentDir.y);
    }

    public void PlayDisappointedAnimation()
    {
        Vector2 currentDir = new Vector2(
            animator.GetFloat(DIRECTION_X),
            animator.GetFloat(DIRECTION_Y)
        );
        
        animator.SetTrigger("Disappointed");
        
        animator.SetFloat(DIRECTION_X, currentDir.x);
        animator.SetFloat(DIRECTION_Y, currentDir.y);
    }
} 