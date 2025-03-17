using UnityEngine;

[RequireComponent(typeof(FishHookable))]
public class FishMovement : MonoBehaviour
{
    private FishHookable fishHookable;
    private FishBehavior behavior;
    private Vector2 currentVelocity;
    private Vector2 targetDirection;
    private float currentSpeed;
    private Transform spriteTransform; // Cache the sprite transform
    public FishSpawner spawner { get; private set; } // Reference to spawner for debug settings, accessible but only settable internally
    private FishSchooling schooling; // Reference to schooling component
    private Vector2 currentForce = Vector2.zero; // Add this at class level

    [Header("Movement Bounds")]
    [HideInInspector] public BoxCollider2D boundaryArea; // Now assigned by spawner
    public float boundaryForce = 5f;
    public float boundaryTurnSpeed = 2f;
    public float edgeBuffer = 2f;
    public float hardBoundaryBuffer = 0.5f; // Distance from edge where hard boundary kicks in
    public float returnToCenterForce = 2f;
    public float boundaryExponent = 2f;

    [Header("Movement Settings")]
    public float directionChangeSpeed = 0.1f;
    public float minSpeedMultiplier = 0.5f;
    public float maxSpeedMultiplier = 1.5f;
    public float maxTurnSpeed = 2f; // Maximum turning speed in radians per second
    public float minimumSpeed = 1f; // Minimum speed the fish must maintain
    private float speedMultiplier = 1f;
    private float directionChangeTimer;
    private float directionChangeInterval;
    private Vector2 currentDirection; // Track current movement direction

    // Cached boundary values
    private float minX, maxX, minY, maxY;
    private Vector2 centerPoint;
    private bool hasBoundary;

    private void Start()
    {
        fishHookable = GetComponent<FishHookable>();
        behavior = fishHookable.behavior;
        schooling = GetComponent<FishSchooling>();
        
        // Cache the sprite transform
        spriteTransform = transform.Find("Sprite");
        if (spriteTransform == null)
        {
            Debug.LogWarning("No 'Sprite' child object found on " + gameObject.name);
        }
        
        // Get reference to spawner
        spawner = boundaryArea?.GetComponent<FishSpawner>();
        
        if (boundaryArea == null)
        {
            Debug.LogWarning("No boundary area assigned to " + gameObject.name);
            hasBoundary = false;
        }
        else
        {
            UpdateBoundaryValues();
            hasBoundary = true;
        }
        
        if (behavior != null)
        {
            currentSpeed = behavior.baseSpeed;
            // Initialize with a random direction
            currentDirection = Random.insideUnitCircle.normalized;
            targetDirection = currentDirection;
            
            // Initialize random timers
            directionChangeInterval = Random.Range(3f, 6f);
            speedMultiplier = Random.Range(minSpeedMultiplier, maxSpeedMultiplier);

            // Apply proper scaling to all components
            ApplyFishScale();
        }
    }

    private void UpdateBoundaryValues()
    {
        if (boundaryArea != null)
        {
            // Get the world bounds of the collider
            Bounds bounds = boundaryArea.bounds;
            minX = bounds.min.x;
            maxX = bounds.max.x;
            minY = bounds.min.y;
            maxY = bounds.max.y;
            centerPoint = bounds.center;
        }
    }

    private void OnValidate()
    {
        if (boundaryArea != null)
        {
            UpdateBoundaryValues();
            hasBoundary = true;
        }
    }

    private bool IsNearBoundary(Vector2 position)
    {
        if (!hasBoundary) return false;
        
        return position.x - minX < edgeBuffer ||
               maxX - position.x < edgeBuffer ||
               position.y - minY < edgeBuffer ||
               maxY - position.y < edgeBuffer;
    }

    private bool IsOutsideSafeZone(Vector2 position)
    {
        if (!hasBoundary) return false;
        
        float distanceFromCenter = (position - centerPoint).magnitude;
        float maxAllowedDistance = Mathf.Min(maxX - minX, maxY - minY) * 0.5f;
        return distanceFromCenter > maxAllowedDistance * 0.8f;
    }

    private void FixedUpdate()
    {
        // Get rigidbody reference
        var rb = fishHookable.GetComponent<Rigidbody2D>();
        
        // Ensure rigidbody doesn't rotate
        rb.angularVelocity = 0f;
        rb.rotation = 0f;
        rb.freezeRotation = true;

        Vector2 targetVelocity;

        if (fishHookable.IsStunned)
        {
            // When stunned, no movement
            targetVelocity = Vector2.zero;
            currentDirection = Vector2.zero;
        }
        else if (fishHookable.IsHooked)
        {
            // When hooked, calculate struggling movement
            targetVelocity = fishHookable.HookedMovement();
            currentDirection = targetVelocity.normalized;
        }
        else if (behavior != null)
        {
            // Normal movement
            UpdateTimers();
            Vector2 desiredDirection = CalculateMovementForce();
            
            // Smoothly rotate current direction towards desired direction
            float angle = Vector2.SignedAngle(currentDirection, desiredDirection);
            float maxRotation = maxTurnSpeed * Time.fixedDeltaTime;
            float rotation = Mathf.Clamp(angle, -maxRotation, maxRotation);
            
            currentDirection = RotateVector2(currentDirection, rotation);
            
            // Calculate base velocity with schooling influence
            float baseSpeed = behavior.baseSpeed * speedMultiplier;
            if (schooling != null)
            {
                Vector2 schoolingForce = schooling.CalculateSchoolingForce();
                // Adjust speed based on schooling - slower when close to school, faster when catching up
                float schoolingInfluence = Mathf.Lerp(0.8f, 1.2f, schoolingForce.magnitude);
                baseSpeed *= schoolingInfluence;
            }
            
            // Apply minimum speed as a floor
            baseSpeed = Mathf.Max(baseSpeed, minimumSpeed);
            targetVelocity = currentDirection * baseSpeed;
        }
        else
        {
            targetVelocity = Vector2.zero;
            currentDirection = Vector2.zero;
        }

        // Store current velocity for reference (used by other components like schooling)
        currentVelocity = targetVelocity;
        
        // Update FishHookable's state (but don't apply movement there)
        fishHookable.Move(targetVelocity);
        
        // Apply the actual movement through rigidbody
        rb.velocity = targetVelocity;

        // Clamp position to stay within bounds
        if (hasBoundary)
        {
            Vector2 position = transform.position;
            bool wasOutOfBounds = false;

            // Clamp X position
            if (position.x < minX + hardBoundaryBuffer)
            {
                position.x = minX + hardBoundaryBuffer;
                currentDirection.x = Mathf.Abs(currentDirection.x);
                wasOutOfBounds = true;
            }
            else if (position.x > maxX - hardBoundaryBuffer)
            {
                position.x = maxX - hardBoundaryBuffer;
                currentDirection.x = -Mathf.Abs(currentDirection.x);
                wasOutOfBounds = true;
            }

            // Clamp Y position
            if (position.y < minY + hardBoundaryBuffer)
            {
                position.y = minY + hardBoundaryBuffer;
                currentDirection.y = Mathf.Abs(currentDirection.y);
                wasOutOfBounds = true;
            }
            else if (position.y > maxY - hardBoundaryBuffer)
            {
                position.y = maxY - hardBoundaryBuffer;
                currentDirection.y = -Mathf.Abs(currentDirection.y);
                wasOutOfBounds = true;
            }

            // If we were out of bounds, update position and velocity with minimum speed enforcement
            if (wasOutOfBounds)
            {
                transform.position = position;
                float speed = Mathf.Max(behavior.baseSpeed * speedMultiplier, minimumSpeed);
                rb.velocity = currentDirection * speed;
            }
        }
        
        // Update sprite direction based on current direction instead of velocity
        if (spriteTransform != null && Mathf.Abs(currentDirection.x) > 0.01f)
        {
            Vector3 scale = spriteTransform.localScale;
            scale.y = Mathf.Abs(scale.y);
            scale.z = Mathf.Abs(scale.z);
            scale.x = Mathf.Abs(scale.x) * (currentDirection.x > 0 ? 1 : -1);
            spriteTransform.localScale = scale;
        }
    }

    private Vector2 RotateVector2(Vector2 vector, float degrees)
    {
        float radians = degrees * Mathf.Deg2Rad;
        float sin = Mathf.Sin(radians);
        float cos = Mathf.Cos(radians);
        
        return new Vector2(
            vector.x * cos - vector.y * sin,
            vector.x * sin + vector.y * cos
        );
    }

    private void UpdateTimers()
    {
        directionChangeTimer += Time.fixedDeltaTime;
        if (directionChangeTimer >= directionChangeInterval)
        {
            // Only update speed multiplier, direction changes are now handled smoothly
            speedMultiplier = Random.Range(minSpeedMultiplier, maxSpeedMultiplier);
            directionChangeInterval = Random.Range(3f, 6f);
            directionChangeTimer = 0f;
        }
    }

    private Vector2 CalculateMovementForce()
    {
        if (fishHookable == null) return Vector2.zero;
        
        // Check boundaries first
        Vector2 boundaryDirection = CalculateBoundaryAvoidance();
        if (boundaryDirection != Vector2.zero)
        {
            // If we're near a boundary, use the corrected direction
            return boundaryDirection;
        }

        // Check for bait
        Transform nearestBait = fishHookable.GetNearestBait();
        if (nearestBait != null)
        {
            Vector2 toBait = (Vector2)nearestBait.position - (Vector2)transform.position;
            float distanceToBait = toBait.magnitude;
            
            if (distanceToBait <= behavior.visionRange)
            {
                // Move towards bait if it's compatible (compatibility already checked in FishHookable)
                return toBait.normalized;
            }
        }

        // Normal movement - combine schooling and wandering
        Vector2 moveDirection = Vector2.zero;
        float totalWeight = 0f;

        // Add schooling force with higher priority
        if (schooling != null)
        {
            Vector2 schoolingForce = schooling.CalculateSchoolingForce();
            if (schoolingForce.magnitude > 0.1f)
            {
                moveDirection += schoolingForce * behavior.flockWeight * 2f; // Doubled schooling weight
                totalWeight += behavior.flockWeight * 2f;
            }
        }

        // Add wander force with lower priority
        if (moveDirection.magnitude < 0.1f) // Only add wander if schooling is weak
        {
            Vector2 wanderForce = CalculateWanderForce();
            moveDirection += wanderForce;
            totalWeight += 1f;
        }

        // Return to center if too far out
        if (IsOutsideSafeZone(transform.position))
        {
            Vector2 returnForce = CalculateReturnForce();
            moveDirection += returnForce * returnToCenterForce;
            totalWeight += returnToCenterForce;
        }

        // Normalize the result
        if (totalWeight > 0)
        {
            moveDirection /= totalWeight;
        }

        return moveDirection.normalized;
    }

    private Vector2 CalculateLevelMaintenanceForce()
    {
        if (fishHookable.IsHooked || fishHookable.IsStunned) return Vector2.zero;

        // Get the fish's spawn position Y level (or current Y if not set)
        float targetY = transform.position.y;
        
        // Calculate force to maintain level
        float verticalDifference = targetY - transform.position.y;
        float forceMagnitude = Mathf.Abs(verticalDifference) * 2f; // Stronger correction for larger deviations
        
        // Create a force that pushes the fish back to its target Y level
        Vector2 levelForce = new Vector2(0, verticalDifference).normalized * forceMagnitude;
        
        // Add slight random vertical movement (much reduced from before)
        levelForce += new Vector2(0, Mathf.Sin(Time.time * 0.5f) * 0.1f);
        
        return levelForce;
    }

    private Vector2 CalculateBoundaryAvoidance()
    {
        if (!hasBoundary) return Vector2.zero;

        Vector2 position = transform.position;
        Vector2 direction = currentDirection;

        // Check if we're too close to any boundary and reverse the appropriate direction
        if (position.x - minX < edgeBuffer)
        {
            direction.x = Mathf.Abs(direction.x); // Force movement to the right
        }
        else if (maxX - position.x < edgeBuffer)
        {
            direction.x = -Mathf.Abs(direction.x); // Force movement to the left
        }

        if (position.y - minY < edgeBuffer)
        {
            direction.y = Mathf.Abs(direction.y); // Force movement upward
        }
        else if (maxY - position.y < edgeBuffer)
        {
            direction.y = -Mathf.Abs(direction.y); // Force movement downward
        }

        return direction.normalized * boundaryForce;
    }

    private Vector2 CalculateReturnForce()
    {
        if (!hasBoundary) return Vector2.zero;

        Vector2 position = transform.position;
        Vector2 toCenter = centerPoint - position;
        float distanceFromCenter = toCenter.magnitude;
        float maxAllowedDistance = Mathf.Min(maxX - minX, maxY - minY) * 0.5f;
        
        // Only apply return force when significantly outside safe zone
        if (distanceFromCenter > maxAllowedDistance * 0.8f)
        {
            float t = (distanceFromCenter - maxAllowedDistance * 0.8f) / (maxAllowedDistance * 0.2f);
            return toCenter.normalized * t;
        }

        return Vector2.zero;
    }

    private Vector2 CalculateWanderForce()
    {
        // Simple wandering behavior
        targetDirection = Vector2.Lerp(targetDirection, Random.insideUnitCircle, Time.deltaTime * directionChangeSpeed);
        return targetDirection.normalized * behavior.wanderWeight;
    }

    // Expose current velocity for schooling behavior
    public Vector2 GetCurrentVelocity()
    {
        return currentVelocity;
    }

    private void OnDrawGizmos()
    {
        if (boundaryArea == null || !Application.isPlaying || behavior == null || spawner == null || !spawner.showDebugVisuals) return;
        
        UpdateBoundaryValues();
        Vector3 center = centerPoint;

        // Only draw force vectors for this fish
        Vector3 pos = transform.position;
        
        // Draw movement direction
        Gizmos.color = Color.green;
        Gizmos.DrawLine(pos, pos + (Vector3)currentVelocity);
        
        // Draw dot at target position
        Gizmos.color = Color.blue;
        Vector3 targetPos = pos + (Vector3)currentVelocity;
        Gizmos.DrawSphere(targetPos, 0.1f);
        
        // If near boundary or outside safe zone, draw force vectors
        if (IsNearBoundary(pos) || IsOutsideSafeZone(pos))
        {
            // Draw boundary avoidance force
            if (IsNearBoundary(pos))
            {
                Gizmos.color = Color.red;
                Vector3 boundaryForce = (Vector3)CalculateBoundaryAvoidance();
                Gizmos.DrawLine(pos, pos + boundaryForce);
            }
            
            // Draw return force
            if (IsOutsideSafeZone(pos))
            {
                Gizmos.color = new Color(1f, 0.5f, 0f);
                Vector3 returnForce = (Vector3)CalculateReturnForce();
                Gizmos.DrawLine(pos, pos + returnForce);
            }
        }
    }

    private void ApplyFishScale()
    {
        if (behavior == null) return;

        // Get the base size from behavior
        float size = behavior.size;

        // Scale the collider
        CircleCollider2D fishCollider = GetComponent<CircleCollider2D>();
        if (fishCollider != null)
        {
            fishCollider.radius = 0.5f * size; // Base radius is 0.5, scale with size
        }

        // Scale the sprite
        if (spriteTransform != null)
        {
            // Preserve the current X direction (for fish facing)
            float currentXDirection = spriteTransform.localScale.x > 0 ? 1 : -1;
            
            // Apply the new scale while preserving direction
            Vector3 newScale = Vector3.one * size;
            newScale.x *= currentXDirection;
            spriteTransform.localScale = newScale;
        }

        // Adjust schooling parameters if present
        if (schooling != null)
        {
            schooling.separationRadius *= size;
            schooling.optimalSchoolDistance *= size;
        }
    }
} 