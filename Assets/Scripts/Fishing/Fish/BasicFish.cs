using UnityEngine;
using System.Collections.Generic;
using FishSizeNamespace;

[RequireComponent(typeof(Rigidbody2D))]
public class BasicFish : MonoBehaviour
{
    //private Size defaultSize = FishSizeNamespace.Size.Small; // Default size for fish
    // TODO: Add to a ScriptableObject
    [Header("Fish Properties")]
    private new string name;
    [SerializeField] private ESize serializedSize = ESize.Small;
    private FishSize size; 
    private Sprite sprite;
    
    public string Name => name;
    public FishSize Size => size;  // Public read-only access to fish size
    public Sprite Sprite => sprite; // Public read-only access to fish asset

    // Should be separate from ScriptableObject
    [SerializeField] private float weight = 1f; // Weight of the fish, used for fishing mechanics
    public float Weight => weight;
    
    [Header("Movement")]
    [SerializeField] private float swimSpeed = 3f;
    [SerializeField] private float chaseSpeed = 5f; // Speed when chasing bait
    [SerializeField] private float turnSpeed = 2f;
    [SerializeField] private float boundaryInfluenceDistance = 1f;
    [SerializeField] private float horizontalBias = 1.5f; // Bias towards horizontal movement
    [SerializeField] private float verticalMovementRange = 2f; // How far up/down fish can move from their current position
    [SerializeField] private float verticalMovementSpeed = 0.5f; // How fast fish move vertically
    
    // Movement constraints struct
    private struct MovementConstraints
    {
        public float minX;
        public float maxX;
        public float minY;
        public float maxY;
    }
    private MovementConstraints movementConstraints;
    
    [Header("Bait Detection")]
    [SerializeField] private float baitDetectionRadius = 5f;
    [SerializeField] private float minBaitChaseDistance = 0.5f; // Minimum distance to keep from bait
    
    [Header("Fish Interaction")]
    [SerializeField] private float interactionRadius = 5f; // Radius to check for other fish
    [SerializeField] private float minSeparationDistance = 1.5f; // Minimum distance to keep from other fish
    [SerializeField] private float separationWeight = 2f; // Weight for separation force
    [SerializeField] private float alignmentWeight = 1f; // Weight for alignment force
    [SerializeField] private float cohesionWeight = 1f; // Weight for cohesion force
    [SerializeField] private float baseForwardWeight = 1.2f;  // Weight for base forward movement
    [SerializeField] private float randomMovementWeight = 0.3f;  // Weight for random movement
    [SerializeField] private float randomMovementInterval = 2f;  // How often to change random direction
    
    [Header("Schooling")]
    [SerializeField] private bool isSchooling = false;
    [SerializeField] private float schoolingRadius = 5f;
    
    [Header("Collision")]
    [SerializeField] private float emergencySeperationForce = 5f;
    [SerializeField] private float emergencySeperationDistance = 0.5f;
    
    [Header("Debug Visualization")]
    [SerializeField] private bool showDebugGizmos = false;  // Default to false to hide gizmos
    
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
        
        // Sets up fish properties
        name = gameObject.name;
        size = serializedSize;
        
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

        sprite = spriteRenderer.sprite;

        // Set up physics
        rb = GetComponent<Rigidbody2D>();
        rb.gravityScale = 0;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        
        // Set up collision layers
        // Fish should only collide with boundaries and bait, not other fish
        int fishLayer = LayerMask.NameToLayer("Fish");
        if (fishLayer == -1)
        {
            Debug.LogWarning("'Fish' layer not found. Using default layer (0) instead.");
            gameObject.layer = 0; // Default layer
        }
        else
        {
            gameObject.layer = fishLayer;
        }

        // Set up collider
        Collider2D collider = GetComponent<Collider2D>();
        if (collider != null)
        {
            // Make sure the collider is set to trigger to prevent physical collisions
            collider.isTrigger = true;
        }
        
        // Only freeze rotation when not chasing
        UpdateMovementConstraints(false);
        
        // Set random initial direction (now using random left/right)
        float randomAngle = Random.Range(-30f, 30f);
        currentDirection = Quaternion.Euler(0, 0, randomAngle) * (Random.value > 0.5f ? Vector2.right : Vector2.left);
        UpdateRandomDirection();
        UpdateSpriteFacing();

        // Start updating nearby fish for all fish
        InvokeRepeating(nameof(UpdateNearbyFish), 0f, 0.5f);

        // Subscribe to bait used event
        Bait.OnBaitUsed += HandleBaitUsed;
    }

    private void OnDestroy()
    {
        // Unsubscribe from bait used event
        Bait.OnBaitUsed -= HandleBaitUsed;
    }

    private void HandleBaitUsed(Bait usedBait)
    {
        // If this fish was targeting the used bait, clear its target
        if (targetBait == usedBait)
        {
            targetBait = null;
            UpdateMovementConstraints(false);
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
            // Only freeze rotation, allow Y movement
            rb.constraints = RigidbodyConstraints2D.FreezeRotation;
        }
    }

    private void UpdateRandomDirection()
    {
        // Generate a new random direction with horizontal bias
        float horizontalAngle = Random.Range(-45f, 45f);
        float verticalAngle = Random.Range(-30f, 30f) / horizontalBias; // Reduced vertical angle range
        
        // Randomly choose between left and right as base direction
        Vector2 baseDirection = Random.value > 0.5f ? Vector2.right : Vector2.left;
        Vector2 newDirection = Quaternion.Euler(0, 0, horizontalAngle) * baseDirection;
        newDirection += Vector2.up * Mathf.Sin(verticalAngle * Mathf.Deg2Rad);
        randomDirection = newDirection.normalized;
        nextRandomTime = Time.time + randomMovementInterval;
    }

    private void UpdateNearbyFish()
    {
        nearbyFish.Clear();
        float checkRadius = isSchooling ? schoolingRadius : interactionRadius;
        // Use OverlapCircleAll with a specific layer mask to only detect other fish
        int fishLayer = LayerMask.GetMask("Fish");
        Collider2D[] colliders = Physics2D.OverlapCircleAll(transform.position, checkRadius, fishLayer);
        
        foreach (Collider2D col in colliders)
        {
            BasicFish fish = col.GetComponent<BasicFish>();
            if (fish != null && fish != this)
            {
                nearbyFish.Add(fish);
            }
        }
    }

    private Vector2 CalculateSchoolingBehavior()
    {
        // Always include base forward movement and random movement
        Vector2 baseForward = currentDirection * baseForwardWeight;
        
        // Apply horizontal bias to base forward movement but maintain direction sign
        float xSign = Mathf.Sign(baseForward.x);
        baseForward.x = Mathf.Abs(baseForward.x) * horizontalBias * xSign;
        baseForward = baseForward.normalized;
        
        // Update random direction periodically
        if (Time.time >= nextRandomTime)
        {
            UpdateRandomDirection();
        }
        
        // Add random movement with horizontal bias but maintain direction sign
        Vector2 randomMovement = randomDirection * randomMovementWeight;
        float randomXSign = Mathf.Sign(randomMovement.x);
        randomMovement.x = Mathf.Abs(randomMovement.x) * horizontalBias * randomXSign;
        randomMovement = randomMovement.normalized;

        // If no nearby fish, just use base movement with randomization
        if (nearbyFish.Count == 0)
            return (baseForward + randomMovement).normalized;

        Vector2 separation = Vector2.zero;
        Vector2 alignment = Vector2.zero;
        Vector2 cohesion = Vector2.zero;
        int alignmentCount = 0;
        int cohesionCount = 0;
        float totalSeparationWeight = 0f;

        foreach (BasicFish fish in nearbyFish)
        {
            if (fish == null) continue;

            Vector2 toFish = fish.transform.position - transform.position;
            float distance = toFish.magnitude;

            // Separation - applies to all fish to maintain minimum distance
            if (distance < minSeparationDistance)
            {
                // Calculate separation force based on distance
                float separationForce = 1f - (distance / minSeparationDistance);
                separationForce = Mathf.Pow(separationForce, 2f); // Quadratic falloff
                
                // Add separation force in the opposite direction of the fish
                separation -= toFish.normalized * separationForce;
                totalSeparationWeight += separationForce;
            }

            // Alignment and Cohesion - stronger for schooling fish
            if (isSchooling && fish.isSchooling && distance < schoolingRadius)
            {
                // Only consider alignment if fish are moving in similar directions
                float dotProduct = Vector2.Dot(currentDirection, fish.currentDirection);
                if (dotProduct > 0.5f) // Only align if moving in similar directions
                {
                    alignment += (Vector2)fish.currentDirection;
                    alignmentCount++;
                }
                
                cohesion += (Vector2)fish.transform.position;
                cohesionCount++;
            }
            // Weaker alignment and cohesion for non-schooling fish
            else if (distance < interactionRadius)
            {
                // Only consider alignment if fish are moving in similar directions
                float dotProduct = Vector2.Dot(currentDirection, fish.currentDirection);
                if (dotProduct > 0.5f) // Only align if moving in similar directions
                {
                    alignment += (Vector2)fish.currentDirection * 0.5f;
                    alignmentCount++;
                }
                
                cohesion += (Vector2)fish.transform.position;
                cohesionCount++;
            }
        }

        // Normalize and apply weights
        if (totalSeparationWeight > 0)
        {
            separation = separation.normalized * separationWeight;
        }

        if (alignmentCount > 0)
        {
            alignment = (alignment / alignmentCount) * alignmentWeight;
        }

        if (cohesionCount > 0)
        {
            cohesion = ((cohesion / cohesionCount) - (Vector2)transform.position).normalized * cohesionWeight;
        }

        // Combine all forces with priority to separation
        Vector2 combinedForce = baseForward + randomMovement;
        
        // Add separation with higher priority if there's significant separation force
        if (totalSeparationWeight > 0.5f)
        {
            combinedForce = separation * 2f + combinedForce;
        }
        else
        {
            combinedForce += separation + alignment + cohesion;
        }

        // Normalize and apply horizontal bias to final direction
        combinedForce = combinedForce.normalized;
        float finalXSign = Mathf.Sign(combinedForce.x);
        combinedForce.x = Mathf.Abs(combinedForce.x) * horizontalBias * finalXSign;
        combinedForce = combinedForce.normalized;

        return combinedForce;
    }

    private void Update()
    {
        if (currentZone == null)
        {
            FindCurrentZone();
        }
        else if (!currentZone.IsInZone(transform.position))
        {
            // Check zone every few frames if we're not in our current zone
            if (Time.frameCount % 30 == 0)
            {
                FindCurrentZone();
            }
        }

        if (currentZone == null)
        {
            Debug.LogWarning($"{gameObject.name} has no valid zone!");
            return;
        }

        // Update movement constraints based on current zone
        UpdateMovementConstraints();

        // Check for nearby bait
        if (targetBait == null)
        {
            FindNearbyBait();
        }

        // Update movement based on current state
        if (targetBait != null)
        {
            ChaseBait();
        }
        else
        {
            MoveRandomly();
        }
    }

    private bool isChasing => targetBait != null;

    private void FixedUpdate()
    {
        if (isHooked) return;

        // Get current zone if we don't have one
        if (currentZone == null)
        {
            FindCurrentZone();
        }

        // Restrict vertical movement based on current zone
        if (currentZone != null)
        {
            Bounds zoneBounds = currentZone.GetComponent<BoxCollider2D>().bounds;
            float currentY = transform.position.y;
            float maxY = zoneBounds.max.y;
            
            // If we're above the zone's top boundary, move down
            if (currentY > maxY)
            {
                Vector2 newPosition = transform.position;
                newPosition.y = maxY;
                transform.position = newPosition;
                
                // Reverse vertical component of movement
                if (rb.velocity.y > 0)
                {
                    rb.velocity = new Vector2(rb.velocity.x, -rb.velocity.y * 0.5f);
                }
            }
        }

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
        else
        {
            // Normal movement behavior when not chasing bait
            // Calculate zone boundaries influence
            float distanceToLeftBound = transform.position.x - zoneCollider.bounds.min.x;
            float distanceToRightBound = zoneCollider.bounds.max.x - transform.position.x;
            float distanceToTopBound = zoneCollider.bounds.max.y - transform.position.y;
            float distanceToBottomBound = transform.position.y - zoneCollider.bounds.min.y;

            // Check if we're near horizontal boundaries
            if (distanceToLeftBound < boundaryInfluenceDistance)
            {
                float influence = 1 - (distanceToLeftBound / boundaryInfluenceDistance);
                targetDirection += Vector2.right * influence * horizontalBias;
            }
            else if (distanceToRightBound < boundaryInfluenceDistance)
            {
                float influence = 1 - (distanceToRightBound / boundaryInfluenceDistance);
                targetDirection += Vector2.left * influence * horizontalBias;
            }

            // Check if we're near vertical boundaries
            if (distanceToTopBound < boundaryInfluenceDistance)
            {
                float influence = 1 - (distanceToTopBound / boundaryInfluenceDistance);
                targetDirection += Vector2.down * influence;
            }
            else if (distanceToBottomBound < boundaryInfluenceDistance)
            {
                float influence = 1 - (distanceToBottomBound / boundaryInfluenceDistance);
                targetDirection += Vector2.up * influence;
            }

            // Apply schooling behavior
            Vector2 schoolingDirection = CalculateSchoolingBehavior();
            
            // Apply horizontal bias to schooling direction
            schoolingDirection.x *= horizontalBias;
            schoolingDirection = schoolingDirection.normalized;
            
            targetDirection = Vector2.Lerp(targetDirection, schoolingDirection, 0.5f);
        }

        // Normalize the target direction
        targetDirection.Normalize();

        // Apply horizontal bias to final direction
        targetDirection.x *= horizontalBias;
        targetDirection = targetDirection.normalized;

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
            // Check if the bait is compatible with this fish's size and hasn't been used yet
            if (bait.IsCompatibleWithFish(size) && !bait.IsUsed)
            {
                // Get the hook controller from the bait
                HookController hook = bait.GetComponentInParent<HookController>();
                if (hook != null)
                {
                    // Immediately mark the bait as used and notify all fish
                    bait.MarkAsUsed();
                    
                    // Immediately disable the bait's components to prevent other fish from being hooked
                    if (bait.GetComponent<SpriteRenderer>() != null)
                    {
                        bait.GetComponent<SpriteRenderer>().enabled = false;
                    }
                    if (bait.GetComponent<Collider2D>() != null)
                    {
                        bait.GetComponent<Collider2D>().enabled = false;
                    }
                    if (bait.GetComponent<Rigidbody2D>() != null)
                    {
                        bait.GetComponent<Rigidbody2D>().simulated = false;
                    }
                    
                    // Let the hook handle the catching logic
                    hook.OnFishContact(this);
                    // Mark this fish as hooked
                    isHooked = true;
                    // Stop any current movement
                    if (rb != null)
                    {
                        rb.velocity = Vector2.zero;
                    }
                    // Clear any target bait
                    targetBait = null;
                    
                    // Transform the fish into the bait
                    transform.SetParent(hook.transform);
                    transform.localPosition = Vector3.zero;
                    transform.localRotation = Quaternion.identity;
                    
                    // Disable the fish's components
                    if (rb != null) rb.simulated = false;
                    if (GetComponent<Collider2D>() != null) GetComponent<Collider2D>().enabled = false;
                }
            }
        }
    }

    // Called by the hook controller when this fish is caught
    public void GetHooked()
    {
        isHooked = true;
        rb.simulated = true; // Keep physics enabled for hook interaction
        rb.velocity = Vector2.zero; // Stop current movement
        
        // Disable collider to prevent interference with hook
        GetComponent<Collider2D>().enabled = false;
        
        // Cancel any ongoing movement updates
        CancelInvoke(nameof(UpdateNearbyFish));
        nearbyFish.Clear();
        
        // Clear any target bait
        targetBait = null;
    }

    // Called when the fish is released from the hook
    public void GetReleased()
    {
        isHooked = false;
        rb.velocity = Vector2.zero;
        
        // Re-enable collider
        GetComponent<Collider2D>().enabled = true;
        
        // Add a small force to make the fish swim away when released
        Vector2 releaseDirection = new Vector2(Random.Range(-1f, 1f), Random.Range(-1f, 1f)).normalized;
        rb.AddForce(releaseDirection * swimSpeed, ForceMode2D.Impulse);
        
        // Set initial direction based on release direction
        currentDirection = releaseDirection;
        UpdateSpriteFacing();
        
        // Restart fish behavior
        InvokeRepeating(nameof(UpdateNearbyFish), 0f, 0.5f);
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
        if (!showDebugGizmos) return;  // Early return if gizmos are disabled

        // Draw detection radius
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, baitDetectionRadius);

        // Draw schooling radius if schooling is enabled
        if (isSchooling)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(transform.position, schoolingRadius);
        }

        // Draw current direction
        Gizmos.color = Color.green;
        Vector3 direction = currentDirection.normalized;
        Gizmos.DrawLine(transform.position, transform.position + direction);
    }

    private void OnCollisionStay2D(Collision2D collision)
    {
        // Only handle collisions with boundaries or other non-fish objects
        if (isHooked) return;
        
        // You can add boundary collision handling here if needed
    }

    private void FindNearbyBait()
    {
        if (targetBait != null || isHooked) return; // Don't look for bait if already chasing one or if hooked

        // Create layer mask for bait layer
        int baitLayer = LayerMask.GetMask("Bait");
        if (baitLayer == 0)
        {
            Debug.LogWarning("'Bait' layer not found! Please create a 'Bait' layer in Unity.");
            return;
        }

        // Only check colliders on the bait layer
        Collider2D[] colliders = Physics2D.OverlapCircleAll(transform.position, baitDetectionRadius, baitLayer);
        float closestDistance = float.MaxValue;
        Bait closestBait = null;

        foreach (Collider2D col in colliders)
        {
            Bait bait = col.GetComponent<Bait>();
            // Check if bait is compatible, not used, and in the same level zone
            if (bait != null && bait.IsCompatibleWithFish(size) && !bait.IsUsed && bait.CurrentZone == currentZone)
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

        // Check each zone to see if the fish is inside it
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
            Debug.LogWarning("Fish could not find any valid zone!");
        }
    }

    private void UpdateMovementConstraints()
    {
        if (currentZone == null) return;

        // Get the zone's bounds from its BoxCollider2D
        BoxCollider2D zoneCollider = currentZone.GetComponent<BoxCollider2D>();
        if (zoneCollider == null) return;

        Bounds bounds = zoneCollider.bounds;
        Vector2 zoneMin = bounds.min;
        Vector2 zoneMax = bounds.max;

        // Add a small buffer to prevent fish from getting stuck at edges
        float buffer = 0.5f;
        zoneMin += new Vector2(buffer, buffer);
        zoneMax -= new Vector2(buffer, buffer);

        // Update the movement constraints
        movementConstraints.minX = zoneMin.x;
        movementConstraints.maxX = zoneMax.x;
        movementConstraints.minY = zoneMin.y;
        movementConstraints.maxY = zoneMax.y;

        // If fish is outside bounds, move it back in
        Vector2 currentPos = transform.position;
        bool wasOutOfBounds = false;

        if (currentPos.x < movementConstraints.minX)
        {
            currentPos.x = movementConstraints.minX;
            wasOutOfBounds = true;
        }
        else if (currentPos.x > movementConstraints.maxX)
        {
            currentPos.x = movementConstraints.maxX;
            wasOutOfBounds = true;
        }

        if (currentPos.y < movementConstraints.minY)
        {
            currentPos.y = movementConstraints.minY;
            wasOutOfBounds = true;
        }
        else if (currentPos.y > movementConstraints.maxY)
        {
            currentPos.y = movementConstraints.maxY;
            wasOutOfBounds = true;
        }

        if (wasOutOfBounds)
        {
            transform.position = currentPos;
            // Reset velocity when hitting bounds
            if (rb != null)
            {
                rb.velocity = Vector2.zero;
            }
        }
    }

    private void ChaseBait()
    {
        if (targetBait == null) return;

        // Calculate direction to bait
        Vector2 directionToBait = (targetBait.transform.position - transform.position).normalized;
        
        // Apply chase speed
        if (rb != null)
        {
            rb.velocity = directionToBait * chaseSpeed;
        }

        // Update sprite facing based on movement direction
        UpdateSpriteFacing();

        // Check if we've reached the bait
        float distanceToBait = Vector2.Distance(transform.position, targetBait.transform.position);
        if (distanceToBait < 0.1f)
        {
            // We've reached the bait, stop chasing
            targetBait = null;
            if (rb != null)
            {
                rb.velocity = Vector2.zero;
            }
        }
    }

    private void MoveRandomly()
    {
        if (rb == null) return;

        // Generate a random direction
        float randomAngle = Random.Range(0f, 360f);
        Vector2 randomDirection = new Vector2(Mathf.Cos(randomAngle * Mathf.Deg2Rad), Mathf.Sin(randomAngle * Mathf.Deg2Rad));

        // Apply swim speed
        rb.velocity = randomDirection * swimSpeed;

        // Update sprite facing based on movement direction
        UpdateSpriteFacing();
    }
} 