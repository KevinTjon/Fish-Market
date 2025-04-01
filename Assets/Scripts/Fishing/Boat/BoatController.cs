using UnityEngine;
using UnityEngine.InputSystem;

public class BoatController : MonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float moveBoundaryLeft = -8f;
    [SerializeField] private float moveBoundaryRight = 8f;

    private Vector2 movement;
    private Rigidbody2D rb;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.gravityScale = 0; // Disable gravity
        rb.drag = 5; // Add some drag to stop more smoothly
        rb.constraints = RigidbodyConstraints2D.FreezeRotation | RigidbodyConstraints2D.FreezePositionY; // Lock Y position and rotation
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
        newPosition.x = Mathf.Clamp(newPosition.x, moveBoundaryLeft, moveBoundaryRight);
        
        // Update position
        rb.MovePosition(newPosition);
    }

    private void OnDrawGizmos()
    {
        // Draw movement boundaries in the editor
        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(new Vector3(moveBoundaryLeft, transform.position.y - 1, 0), 
                       new Vector3(moveBoundaryLeft, transform.position.y + 1, 0));
        Gizmos.DrawLine(new Vector3(moveBoundaryRight, transform.position.y - 1, 0), 
                       new Vector3(moveBoundaryRight, transform.position.y + 1, 0));
    }
}
