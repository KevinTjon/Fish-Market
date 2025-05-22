using UnityEngine;
using UnityEngine.InputSystem;
using Market;
using System.Linq;

public class FishMarketPlayerController : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float moveSpeed = 5f;
    
    [Header("Interaction")]
    [SerializeField] private string[] interactableTags = { "PlayerInteraction", "Stall", "Customer", "Fish", "Vendor" };
    
    [Header("Components")]
    [SerializeField] private Animator animator;
    [SerializeField] private SpriteRenderer spriteRenderer;

    private PlayerControls playerControls;
    private Vector2 movement;
    private string currentDirection = "Down";
    private Rigidbody2D rb;
    private BoxCollider2D boxCollider;
    private IInteractable currentInteractable;

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
            animator.SetBool("IsWalking", false);
            animator.Play("Idle", 0, 0f);
        };

        playerControls.Player.Interact.performed += ctx => HandleInteraction();
        playerControls.Player.Interact.canceled += ctx => EndInteraction();

        SetupComponents();
    }

    private void SetupComponents()
    {
        rb = GetComponent<Rigidbody2D>();
        if (rb == null)
        {
            rb = gameObject.AddComponent<Rigidbody2D>();
        }
        
        boxCollider = GetComponent<BoxCollider2D>();
        if (boxCollider == null)
        {
            boxCollider = gameObject.AddComponent<BoxCollider2D>();
            boxCollider.size = new Vector2(0.8f, 0.8f);
        }

        rb.gravityScale = 0;
        rb.drag = 0;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        rb.constraints = RigidbodyConstraints2D.FreezeRotation;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;
    }

    private void OnEnable()
    {
        playerControls.Enable();
    }

    private void OnDisable()
    {
        playerControls.Disable();
    }

    private void FixedUpdate()
    {
        rb.velocity = movement * moveSpeed;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (interactableTags.Contains(other.tag))
        {
            var interactable = other.GetComponent<IInteractable>();
            if (interactable != null && interactable.CanInteract)
            {
                currentInteractable = interactable;
                Debug.Log($"Found interactable object: {other.gameObject.name} with tag: {other.tag}");
            }
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (interactableTags.Contains(other.tag))
        {
            var interactable = other.GetComponent<IInteractable>();
            if (interactable == currentInteractable)
            {
                if (currentInteractable != null)
                {
                    currentInteractable.OnInteractionEnd();
                }
                currentInteractable = null;
                Debug.Log($"Left interactable object: {other.gameObject.name} with tag: {other.tag}");
            }
        }
    }

    private void HandleInteraction()
    {
        if (currentInteractable != null && currentInteractable.CanInteract)
        {
            Debug.Log($"Starting interaction with: {currentInteractable.GetTransform().gameObject.name}");
            currentInteractable.OnInteractionStart();
        }
        else
        {
            Debug.Log("Tried to interact but no valid interactable object found" + 
                     (currentInteractable == null ? " (no object nearby)" : " (object cannot be interacted with)"));
        }
    }

    private void EndInteraction()
    {
        if (currentInteractable != null)
        {
            Debug.Log($"Ending interaction with: {currentInteractable.GetTransform().gameObject.name}");
            currentInteractable.OnInteractionEnd();
        }
    }

    private void UpdateAnimation(Vector2 input)
    {
        bool isMoving = input.magnitude > 0;
        
        if (isMoving)
        {
            animator.SetBool("IsWalking", true);
            
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
        animator.SetBool("Up", false);
        animator.SetBool("Down", false);
        animator.SetBool("Left", false);
        animator.SetBool("Right", false);

        animator.SetBool(direction, true);
        currentDirection = direction;
    }
} 