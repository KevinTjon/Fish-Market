using UnityEngine;
using System.Collections.Generic;
using System.Collections;

[RequireComponent(typeof(Rigidbody2D))]
public class BasicFish : MonoBehaviour
{
    [Header("Fish Properties")]
    [SerializeField] private FishSize fishSize = FishSize.Small;
    public FishSize FishSize => fishSize;  // Public read-only access to fish size
    
    [Header("Movement")]
    [SerializeField] private float swimSpeed = 3f;
    [SerializeField] private float chaseSpeed = 5f; // Speed when chasing bait
    [SerializeField] private float turnSpeed = 2f;
    [SerializeField] private float boundaryInfluenceDistance = 1f;
    
    [Header("Bait Detection")]
    [SerializeField] private float baitDetectionRadius = 5f;
    [SerializeField] private float minBaitChaseDistance = 0.5f; // Minimum distance to keep from bait
    
    [Header("Schooling")]
    [SerializeField] private bool isSchooling = false;
    [SerializeField] private float schoolingRadius = 5f;
    [SerializeField] private float separationWeight = 1.5f;
    [SerializeField] private float alignmentWeight = 1f;
    [SerializeField] private float cohesionWeight = 1f;
    [SerializeField] private float minSeparationDistance = 1f;
    [SerializeField] private float baseForwardWeight = 1.2f;  // Weight for base forward movement
    [SerializeField] private float randomMovementWeight = 0.3f;  // Weight for random movement
    [SerializeField] private float randomMovementInterval = 2f;  // How often to change random direction
    
    [Header("Collision")]
    [SerializeField] private float emergencySeperationForce = 5f;
    [SerializeField] private float emergencySeperationDistance = 0.5f;
    
    private Rigidbody2D rb;
    private SpriteRenderer spriteRenderer;
    private Transform spriteTransform;
    private LevelZone currentZone;
    private Vector2 currentDirection;
    private List<BasicFish> nearbyFish = new List<BasicFish>();
    private Vector2 randomDirection;
    private float nextRandomTime;
    private bool isHooked = false;
    private Bait targetBait; // The bait we're currently chasing

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
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        
        // Only freeze rotation and Y position when not chasing
        UpdateMovementConstraints(false);
        
        // Set random initial direction
        float randomAngle = Random.Range(-30f, 30f);
        currentDirection = Quaternion.Euler(0, 0, randomAngle) * Vector2.right;
        UpdateRandomDirection();
        UpdateSpriteFacing();

        // Start updating nearby fish
        if (isSchooling)
        {
            InvokeRepeating(nameof(UpdateNearbyFish), 0f, 0.5f);
        }
    }

    // New method to update movement constraints based on chasing state
    private void UpdateMovementConstraints(bool isChasing)
    {
        if (isChasing)
        {
            // When chasing, only freeze rotation
            rb.constraints = RigidbodyConstraints2D.FreezeRotation;
        }
        else
        {
            // When not chasing, freeze rotation and Y position
            rb.constraints = RigidbodyConstraints2D.FreezeRotation | RigidbodyConstraints2D.FreezePositionY;
        }
    }

    private void UpdateRandomDirection()
    {
        // Generate a new random direction within a cone in front of the fish
        float angle = Random.Range(-45f, 45f);
        randomDirection = Quaternion.Euler(0, 0, angle) * currentDirection;
        nextRandomTime = Time.time + randomMovementInterval;
    }

    private void UpdateNearbyFish()
    {
        nearbyFish.Clear();
        Collider2D[] colliders = Physics2D.OverlapCircleAll(transform.position, schoolingRadius);
        
        foreach (Collider2D col in colliders)
        {
            BasicFish fish = col.GetComponent<BasicFish>();
            if (fish != null && fish != this && fish.isSchooling)
            {
                nearbyFish.Add(fish);
            }
        }
    }

    private Vector2 CalculateSchoolingBehavior()
    {
        // Always include base forward movement and random movement
        Vector2 baseForward = currentDirection * baseForwardWeight;
        
        // Update random direction periodically
        if (Time.time >= nextRandomTime)
        {
            UpdateRandomDirection();
        }
        
        // Add random movement
        Vector2 randomMovement = randomDirection * randomMovementWeight;

        // If not schooling or no nearby fish, just use base movement with randomization
        if (!isSchooling || nearbyFish.Count == 0)
            return (baseForward + randomMovement).normalized;

        Vector2 separation = Vector2.zero;
        Vector2 alignment = Vector2.zero;
        Vector2 cohesion = Vector2.zero;
        int alignmentCount = 0;
        int cohesionCount = 0;

        foreach (BasicFish fish in nearbyFish)
        {
            if (fish == null) continue; // Skip if fish was destroyed

            Vector2 toFish = fish.transform.position - transform.position;
            float distance = toFish.magnitude;

            // Separation
            if (distance < minSeparationDistance)
            {
                separation -= toFish.normalized / distance;
            }

            // Alignment
            if (distance < schoolingRadius)
            {
                alignment += (Vector2)fish.currentDirection;
                alignmentCount++;
            }

            // Cohesion
            if (distance < schoolingRadius)
            {
                cohesion += (Vector2)fish.transform.position;
                cohesionCount++;
            }
        }

        // Normalize and apply weights
        if (alignmentCount > 0)
        {
            alignment = (alignment / alignmentCount) * alignmentWeight;
        }

        if (cohesionCount > 0)
        {
            cohesion = ((cohesion / cohesionCount) - (Vector2)transform.position).normalized * cohesionWeight;
        }

        separation = separation.normalized * separationWeight;

        // Combine all forces including base forward movement and random movement
        Vector2 combinedForce = baseForward + separation + alignment + cohesion + randomMovement;
        return combinedForce.normalized;
    }

    private void Update()
    {
        if (!isHooked && !isChasing)
        {
            // Look for compatible bait
            FindNearbyBait();
        }
    }

    private bool isChasing => targetBait != null;

    private void FixedUpdate()
    {
        if (isHooked) return;
        if (currentZone == null) return;

        BoxCollider2D zoneCollider = currentZone.GetComponent<BoxCollider2D>();
        if (zoneCollider == null) return;

        Vector2 targetDirection = currentDirection;

        // If we have a target bait, chase it
        if (targetBait != null)
        {
            // Check if bait still exists and is in range
            if (targetBait == null || Vector2.Distance(transform.position, targetBait.transform.position) > baitDetectionRadius)
            {
                targetBait = null;
                UpdateMovementConstraints(false); // Reset constraints when stopping chase
            }
            else
            {
                // Calculate direct path to bait
                Vector2 toBait = (targetBait.transform.position - transform.position);
                float distanceToBait = toBait.magnitude;
                Vector2 directPath = toBait.normalized;

                // If bait is above water, ignore it
                if (targetBait.transform.position.y > zoneCollider.bounds.max.y)
                {
                    targetBait = null;
                    UpdateMovementConstraints(false);
                    return;
                }

                // If we're too close to the bait, back off slightly
                if (distanceToBait < minBaitChaseDistance)
                {
                    targetDirection = -directPath;
                }
                else
                {
                    targetDirection = directPath;
                }

                // Prevent upward movement if near surface
                float distanceToSurface = zoneCollider.bounds.max.y - transform.position.y;
                if (distanceToSurface < 1f)
                {
                    // If we're near the surface, only allow horizontal or downward movement
                    if (targetDirection.y > 0)
                    {
                        targetDirection.y = 0;
                        if (targetDirection.x != 0)
                        {
                            targetDirection = targetDirection.normalized;
                        }
                        else
                        {
                            // If no horizontal movement, stop chasing
                            targetBait = null;
                            UpdateMovementConstraints(false);
                            return;
                        }
                    }
                }

                // Move directly towards target with minimal smoothing
                currentDirection = Vector2.Lerp(currentDirection, targetDirection, turnSpeed * 2f * Time.fixedDeltaTime);
                currentDirection.Normalize();
                
                // Apply velocity directly
                rb.velocity = currentDirection * chaseSpeed;
                UpdateSpriteFacing();

                // Strictly enforce water boundary
                if (transform.position.y >= zoneCollider.bounds.max.y)
                {
                    Vector3 newPos = transform.position;
                    newPos.y = zoneCollider.bounds.max.y - 0.1f;
                    transform.position = newPos;
                    
                    // Stop vertical movement if we hit the surface
                    Vector2 newVelocity = rb.velocity;
                    newVelocity.y = Mathf.Min(newVelocity.y, 0);
                    rb.velocity = newVelocity;
                }
                return;
            }
        }

        // Normal movement behavior when not chasing bait
        // Calculate zone boundaries influence
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

        // Apply schooling behavior if enabled
        if (isSchooling)
        {
            Vector2 schoolingDirection = CalculateSchoolingBehavior();
            targetDirection = Vector2.Lerp(targetDirection, schoolingDirection, 0.5f);
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

    private void OnTriggerEnter2D(Collider2D other)
    {
        // Check for level zone
        LevelZone zone = other.GetComponent<LevelZone>();
        if (zone != null)
        {
            currentZone = zone;
            return;
        }

        // Check for bait
        Bait bait = other.GetComponent<Bait>();
        if (bait != null && !isHooked)
        {
            // Check if the bait is compatible with this fish's size
            if (bait.IsCompatibleWithFish(fishSize))
            {
                // Get the hook controller from the bait's parent
                SimpleHookController hook = bait.GetComponentInParent<SimpleHookController>();
                if (hook != null)
                {
                    // Let the hook handle the catching logic
                    hook.OnFishContact(this);
                }
            }
        }
    }

    // Called by the hook controller when this fish is caught
    public void GetHooked()
    {
        isHooked = true;
        rb.simulated = false; // Disable physics completely
        
        // Disable fish AI behavior while hooked
        StopAllCoroutines();
    }

    // Called when the fish is released from the hook
    public void GetReleased()
    {
        isHooked = false;
        rb.simulated = true; // Re-enable physics
        rb.velocity = Vector2.zero;
        
        // Add a small force to make the fish swim away when released
        rb.AddForce(new Vector2(Random.Range(-1f, 1f), Random.Range(-1f, 1f)).normalized * swimSpeed, ForceMode2D.Impulse);
        
        // Restart schooling behavior if this fish was schooling
        if (isSchooling)
        {
            InvokeRepeating(nameof(UpdateNearbyFish), 0f, 0.5f);
        }
    }

    private void UpdateSpriteFacing()
    {
        if (spriteRenderer != null)
        {
            // Flip sprite based on movement direction (flipped from previous implementation)
            spriteRenderer.flipX = currentDirection.x > 0;
        }
    }

    public void SetSchooling(bool schooling, float radius)
    {
        isSchooling = schooling;
        schoolingRadius = radius;
        
        if (isSchooling && !IsInvoking(nameof(UpdateNearbyFish)))
        {
            InvokeRepeating(nameof(UpdateNearbyFish), 0f, 0.5f);
        }
        else if (!isSchooling)
        {
            CancelInvoke(nameof(UpdateNearbyFish));
            nearbyFish.Clear();
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

        // Draw bait detection radius
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, baitDetectionRadius);

        // Draw line to target bait if exists
        if (targetBait != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawLine(transform.position, targetBait.transform.position);
        }
    }

    private void OnCollisionStay2D(Collision2D collision)
    {
        BasicFish otherFish = collision.gameObject.GetComponent<BasicFish>();
        if (otherFish != null)
        {
            // Calculate separation vector
            Vector2 separationVector = transform.position - collision.transform.position;
            float distance = separationVector.magnitude;
            
            if (distance < emergencySeperationDistance)
            {
                // Apply immediate separation force
                Vector2 separationForce = separationVector.normalized * emergencySeperationForce;
                rb.AddForce(separationForce, ForceMode2D.Impulse);
                
                // Also slightly adjust the current direction
                currentDirection = Vector2.Lerp(currentDirection, separationVector.normalized, 0.5f);
            }
        }
    }

    private void FindNearbyBait()
    {
        if (targetBait != null) return; // Already chasing bait

        Collider2D[] colliders = Physics2D.OverlapCircleAll(transform.position, baitDetectionRadius);
        float closestDistance = float.MaxValue;
        Bait closestBait = null;

        foreach (Collider2D col in colliders)
        {
            Bait bait = col.GetComponent<Bait>();
            if (bait != null && bait.IsCompatibleWithFish(fishSize))
            {
                float distance = Vector2.Distance(transform.position, bait.transform.position);
                if (distance < closestDistance)
                {
                    closestDistance = distance;
                    closestBait = bait;
                }
            }
        }

        if (closestBait != targetBait)
        {
            targetBait = closestBait;
            // Update movement constraints when starting/stopping chase
            UpdateMovementConstraints(targetBait != null);
        }
    }
} 