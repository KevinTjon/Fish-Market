using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
[RequireComponent(typeof(CircleCollider2D))]
[RequireComponent(typeof(Rigidbody2D))]
public class Bait : MonoBehaviour
{
    public BaitData baitData;
    private SpriteRenderer spriteRenderer;
    private CircleCollider2D circleCollider;
    private Rigidbody2D rb;

    [Header("Debug")]
    [SerializeField] private bool showDebugGizmos = true;
    [SerializeField] private Color detectionColor = new Color(1f, 0.5f, 0f, 0.3f); // Orange with transparency

    private bool isUsed = false;
    public bool IsUsed => isUsed;

    // Static event to notify all fish when a bait is used
    public static event System.Action<Bait> OnBaitUsed;

    private LevelZone currentZone;
    public LevelZone CurrentZone => currentZone;

    private void Start()
    {
        // Find initial zone
        FindCurrentZone();
    }

    private void Update()
    {
        // Update zone periodically
        if (Time.frameCount % 30 == 0) // Check every 30 frames
        {
            FindCurrentZone();
        }
    }

    private void FindCurrentZone()
    {
        // Find all LevelZone components in the scene
        LevelZone[] zones = FindObjectsOfType<LevelZone>();
        if (zones.Length == 0)
        {
            Debug.LogWarning("No LevelZone found in scene!");
            return;
        }

        // First check if we're still in our current zone
        if (currentZone != null && currentZone.IsInZone(transform.position))
        {
            return; // Still in the same zone
        }

        // Check each zone to see if the bait is inside it
        foreach (LevelZone zone in zones)
        {
            if (zone.IsInZone(transform.position))
            {
                currentZone = zone;
                return;
            }
        }
        
        // If no zone found, try to find the closest zone
        float minDistance = float.MaxValue;
        LevelZone closestZone = null;
        
        foreach (LevelZone zone in zones)
        {
            float distance = Vector2.Distance(transform.position, zone.transform.position);
            if (distance < minDistance)
            {
                minDistance = distance;
                closestZone = zone;
            }
        }
        
        currentZone = closestZone;
        if (currentZone == null)
        {
            Debug.LogWarning("Bait could not find any valid zone!");
        }
    }

    public void MarkAsUsed()
    {
        if (!isUsed)
        {
            isUsed = true;
            OnBaitUsed?.Invoke(this);
        }
    }

    private void Awake()
    {
        // Setup sprite renderer
        spriteRenderer = GetComponent<SpriteRenderer>();
        
        // Create circle sprite if not already assigned
        if (spriteRenderer.sprite == null)
        {
            // Create a circle sprite
            spriteRenderer.sprite = CreateCircleSprite();
            transform.localScale = new Vector3(0.4f, 0.4f, 1f); // Increased from 0.2f to 0.4f
        }

        // Set the color from bait data
        if (baitData != null)
        {
            spriteRenderer.color = baitData.baitColor;
        }

        // Setup collider
        circleCollider = GetComponent<CircleCollider2D>();
        circleCollider.isTrigger = true; // Make it a trigger so it doesn't cause physical collisions
        circleCollider.radius = 0.5f; // Reduced from 1f to 0.5f to match the visual size better

        // Setup rigidbody
        rb = GetComponent<Rigidbody2D>();
        rb.isKinematic = true; // Make it kinematic so it follows the hook perfectly
        rb.gravityScale = 0;
        
        // Set layer to a special bait layer (you'll need to create this in Unity)
        gameObject.layer = LayerMask.NameToLayer("Bait");
    }

    private Sprite CreateCircleSprite()
    {
        // Create a white circle texture
        int size = 32; // Size of the texture
        Texture2D texture = new Texture2D(size, size);
        
        // Calculate center and radius
        Vector2 center = new Vector2(size / 2, size / 2);
        float radius = size / 2;
        
        // Fill the texture with a circle
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float distance = Vector2.Distance(new Vector2(x, y), center);
                if (distance <= radius)
                {
                    texture.SetPixel(x, y, Color.white);
                }
                else
                {
                    texture.SetPixel(x, y, Color.clear);
                }
            }
        }
        
        texture.Apply();
        
        // Create sprite from texture
        return Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
    }

    public bool IsCompatibleWithFish(FishSize fishSize)
    {
        return baitData != null && baitData.targetFishSize == fishSize;
    }

    private void OnDrawGizmos()
    {
        if (!showDebugGizmos) return;

        // Draw the detection radius
        Gizmos.color = detectionColor;
        Gizmos.DrawWireSphere(transform.position, circleCollider.radius);
    }
} 