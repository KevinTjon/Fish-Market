using UnityEngine;
using UnityEngine.Tilemaps;

public class BoatController : MonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] private float moveSpeed = 1250f;

    [Header("References")]
    [SerializeField] private LevelZone shallowZone;
    [SerializeField] private TilemapCollisionHandler tilemapCollision;

    private float minX, maxX;
    private Rigidbody2D rb;
    private BoxCollider2D boatCollider;
    
    private void Awake()
    {
        // Get or add required components
        rb = GetComponent<Rigidbody2D>();
        if (rb == null)
        {
            rb = gameObject.AddComponent<Rigidbody2D>();
        }

        boatCollider = GetComponent<BoxCollider2D>();
        if (boatCollider == null)
        {
            boatCollider = gameObject.AddComponent<BoxCollider2D>();
            // Set a default size for the collider
            boatCollider.size = new Vector2(1f, 0.5f);
        }

        // Configure Rigidbody2D
        rb.gravityScale = 0; // Disable gravity
        rb.drag = 1; // Add some drag to stop more smoothly
        rb.constraints = RigidbodyConstraints2D.FreezeRotation | RigidbodyConstraints2D.FreezePositionY; // Lock Y position and rotation
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous; // Prevent passing through colliders

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

        // Find tilemap collision handler if not assigned
        if (!tilemapCollision)
        {
            // First try to find it on any tilemap in the scene
            Tilemap[] tilemaps = FindObjectsOfType<Tilemap>();
            foreach (Tilemap tilemap in tilemaps)
            {
                tilemapCollision = tilemap.GetComponent<TilemapCollisionHandler>();
                if (tilemapCollision != null)
                {
                    Debug.Log($"Found TilemapCollisionHandler on {tilemap.gameObject.name}");
                    break;
                }
            }

            if (tilemapCollision == null)
            {
                Debug.LogError("No TilemapCollisionHandler found in scene! Please add the TilemapCollisionHandler component to your tilemap GameObject.");
                Debug.Log("To fix this:");
                Debug.Log("1. Select your tilemap GameObject in the hierarchy");
                Debug.Log("2. Click 'Add Component' in the Inspector");
                Debug.Log("3. Search for and add 'TilemapCollisionHandler'");
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

    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void SetBoatForce(float input)
    {
        if (tilemapCollision == null)
        {
            // Try to find the handler again if it's null
            Tilemap[] tilemaps = FindObjectsOfType<Tilemap>();
            foreach (Tilemap tilemap in tilemaps)
            {
                tilemapCollision = tilemap.GetComponent<TilemapCollisionHandler>();
                if (tilemapCollision != null)
                {
                    Debug.Log($"Found TilemapCollisionHandler on {tilemap.gameObject.name}");
                    break;
                }
            }

            if (tilemapCollision == null)
            {
                Debug.LogError("TilemapCollisionHandler is still null! Movement will not work until this is fixed.");
                return;
            }
        }

        // Debug input value
        Debug.Log($"Boat input: {input}");

        // Calculate movement direction
        Vector2 movement = new Vector2(input, 0);
        
        // Check if we're moving
        if (input != 0)
        {
            // Calculate next position with a small offset in the direction of movement
            Vector2 nextPosition = rb.position + movement.normalized * 0.1f;
            
            // Debug collision check
            bool willCollide = tilemapCollision.IsColliding(nextPosition);
            //Debug.Log($"Next position: {nextPosition}, Will collide: {willCollide}");
            
            // Only apply force if we won't collide
            if (!willCollide)
            {
                // Apply force directly for more responsive movement
                rb.velocity = new Vector2(movement.x * moveSpeed * Time.fixedDeltaTime, rb.velocity.y);
            }
            else
            {
                // If we would collide, stop horizontal movement
                rb.velocity = new Vector2(0, rb.velocity.y);
            }
        }
        else
        {
            // If no input, gradually slow down
            rb.velocity = new Vector2(rb.velocity.x * 0.9f, rb.velocity.y);
        }
    }

    public void Flip()
    {
        var newScale = transform.localScale;
        newScale.x *= -1;
        transform.localScale = newScale;
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