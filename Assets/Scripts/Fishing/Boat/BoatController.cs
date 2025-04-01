using UnityEngine;
using UnityEngine.InputSystem;

public class BoatController : MonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] private float moveSpeed = 5f;
    
    [Header("References")]
    [SerializeField] private LevelZone shallowZone;

    private float minX, maxX;
    private Vector2 movement;
    private Rigidbody2D rb;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.gravityScale = 0; // Disable gravity
        rb.drag = 5; // Add some drag to stop more smoothly
        rb.constraints = RigidbodyConstraints2D.FreezeRotation | RigidbodyConstraints2D.FreezePositionY; // Lock Y position and rotation

        // Find shallow zone if not assigned
        if (!shallowZone)
        {
            LevelZone[] zones = FindObjectsOfType<LevelZone>();
            foreach (LevelZone zone in zones)
            {
                if (zone.level == WaterLevel.Shallow)
                {
                    shallowZone = zone;
                    break;
                }
            }
        }

        // Set movement boundaries from shallow zone
        if (shallowZone)
        {
            BoxCollider2D collider = shallowZone.GetComponent<BoxCollider2D>();
            if (collider)
            {
                minX = collider.bounds.min.x;
                maxX = collider.bounds.max.x;
            }
        }
    }

    private void Update()
    {
        // Get input from keyboard
        Keyboard keyboard = Keyboard.current;
        if (keyboard == null) return;

        // Check for A/D or Left/Right arrow keys
        float moveInput = 0;
        if (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed)
            moveInput = 1;
        else if (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed)
            moveInput = -1;

        // Update movement vector
        movement = new Vector2(moveInput, 0);
    }

    private void FixedUpdate()
    {
        // Move the boat
        Vector2 newPosition = rb.position + movement * moveSpeed * Time.fixedDeltaTime;
        
        // Clamp position within boundaries
        newPosition.x = Mathf.Clamp(newPosition.x, minX, maxX);
        
        // Update position
        rb.MovePosition(newPosition);
    }

    private void OnDrawGizmos()
    {
        if (!shallowZone) return;

        // Draw movement boundaries
        Gizmos.color = Color.green;
        float yPos = transform.position.y;
        Gizmos.DrawLine(new Vector3(minX, yPos - 1, 0), new Vector3(minX, yPos + 1, 0));
        Gizmos.DrawLine(new Vector3(maxX, yPos - 1, 0), new Vector3(maxX, yPos + 1, 0));
    }
}
