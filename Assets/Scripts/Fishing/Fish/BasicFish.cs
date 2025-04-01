using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class BasicFish : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float swimSpeed = 3f;
    [SerializeField] private float turnSpeed = 2f;
    [SerializeField] private float boundaryInfluenceDistance = 1f;
    
    private Rigidbody2D rb;
    private SpriteRenderer spriteRenderer;
    private Transform spriteTransform;
    private LevelZone currentZone;
    private Vector2 currentDirection;

    private void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        
        // Find the Sprite child object
        spriteTransform = transform.Find("Sprite");
        if (spriteTransform != null)
        {
            spriteRenderer = spriteTransform.GetComponent<SpriteRenderer>();
        }
        
        if (spriteRenderer == null)
        {
            Debug.LogError("No SpriteRenderer found in Sprite child object of fish: " + gameObject.name);
            return;
        }

        // Set up physics
        rb.gravityScale = 0;
        rb.constraints = RigidbodyConstraints2D.FreezeRotation | RigidbodyConstraints2D.FreezePositionY;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;
        
        // Set random initial direction
        float randomAngle = Random.Range(-30f, 30f);
        currentDirection = Quaternion.Euler(0, 0, randomAngle) * Vector2.right;
        UpdateSpriteFacing();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        LevelZone zone = other.GetComponent<LevelZone>();
        if (zone != null)
        {
            currentZone = zone;
        }
    }

    private void FixedUpdate()
    {
        if (currentZone == null) return;

        BoxCollider2D zoneCollider = currentZone.GetComponent<BoxCollider2D>();
        if (zoneCollider == null) return;

        Vector2 targetDirection = currentDirection;

        // Calculate zone center and boundaries
        Vector2 zoneCenter = zoneCollider.bounds.center;
        float distanceToLeftBound = transform.position.x - zoneCollider.bounds.min.x;
        float distanceToRightBound = zoneCollider.bounds.max.x - transform.position.x;

        // Check if we're near boundaries
        if (distanceToLeftBound < boundaryInfluenceDistance)
        {
            float influence = 1 - (distanceToLeftBound / boundaryInfluenceDistance);
            targetDirection += Vector2.right * influence;
        }
        else if (distanceToRightBound < boundaryInfluenceDistance)
        {
            float influence = 1 - (distanceToRightBound / boundaryInfluenceDistance);
            targetDirection += Vector2.left * influence;
        }

        // Normalize the target direction
        targetDirection.Normalize();

        // Smoothly rotate current direction towards target direction
        currentDirection = Vector2.Lerp(currentDirection, targetDirection, turnSpeed * Time.fixedDeltaTime);
        currentDirection.Normalize();

        // Update movement and facing
        rb.velocity = currentDirection * swimSpeed;
        UpdateSpriteFacing();
    }

    private void UpdateSpriteFacing()
    {
        if (spriteRenderer != null)
        {
            // Flip sprite based on movement direction
            spriteRenderer.flipX = currentDirection.x < 0;
        }
    }

    private void OnDrawGizmos()
    {
        if (currentZone != null)
        {
            // Draw influence zones
            BoxCollider2D zoneCollider = currentZone.GetComponent<BoxCollider2D>();
            if (zoneCollider != null)
            {
                Gizmos.color = Color.yellow;
                // Left influence zone
                Gizmos.DrawWireCube(
                    new Vector3(zoneCollider.bounds.min.x + boundaryInfluenceDistance/2, zoneCollider.bounds.center.y, 0),
                    new Vector3(boundaryInfluenceDistance, zoneCollider.bounds.size.y, 0)
                );
                // Right influence zone
                Gizmos.DrawWireCube(
                    new Vector3(zoneCollider.bounds.max.x - boundaryInfluenceDistance/2, zoneCollider.bounds.center.y, 0),
                    new Vector3(boundaryInfluenceDistance, zoneCollider.bounds.size.y, 0)
                );
            }
        }
    }
} 