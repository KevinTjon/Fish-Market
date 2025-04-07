using UnityEngine;

public class BoatController : MonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] private float moveSpeed = 1250f;

    [Header("References")]
    [SerializeField] private LevelZone shallowZone;

    private float minX, maxX;


    // Physical Attribute
    public Rigidbody2D rb { get; private set; }
    
    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.gravityScale = 0; // Disable gravity
        rb.drag = 1; // Add some drag to stop more smoothly
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
        rb = GetComponent<Rigidbody2D>();
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
        // Move the boat
        Vector2 movement = new Vector2(input, 0);
        rb.AddForce(movement * moveSpeed);
        
        // Clamp position within boundaries
        
        //newPosition.x = Mathf.Clamp(newPosition.x, minX, maxX);
        
        // Update position
        //rb.MovePosition(newPosition);
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