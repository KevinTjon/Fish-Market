using UnityEngine;

[RequireComponent(typeof(FishHookable))]
public class FishMovement : MonoBehaviour
{
    private enum FishState
    {
        Normal,
        ChasingBait,
        Hooked,
        Stunned
    }

    private FishHookable fishHookable;
    private FishBehavior behavior;
    private Vector2 currentVelocity;
    private FishSchooling schooling;
    private Transform spriteTransform;
    [HideInInspector] public FishSpawner spawner;
    [HideInInspector] public BoxCollider2D boundaryArea; // Now assigned by spawner
    public float boundaryForce = 5f;
    public float boundaryTurnSpeed = 2f;
    public float edgeBuffer = 2f;
    public float hardBoundaryBuffer = 0.5f; // Distance from edge where hard boundary kicks in
    public float returnToCenterForce = 2f;
    public float boundaryExponent = 2f;

    [Header("State Settings")]
    private FishState currentState = FishState.Normal;
    public float baitChaseSpeedMultiplier = 2f;
    public float baitChaseTurnSpeedMultiplier = 4f;

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
    private Vector2 targetDirection; // Target direction for wandering
    private float currentSpeed; // Current base speed of the fish

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
        var rb = fishHookable.GetComponent<Rigidbody2D>();
        rb.angularVelocity = 0f;
        rb.rotation = 0f;
        rb.freezeRotation = true;

        // Update the current state
        UpdateState();

        Vector2 targetVelocity = Vector2.zero;

        switch (currentState)
        {
            case FishState.Stunned:
                targetVelocity = HandleStunnedState();
                break;

            case FishState.Hooked:
                targetVelocity = HandleHookedState();
                break;

            case FishState.ChasingBait:
                targetVelocity = HandleBaitChaseState();
                break;

            case FishState.Normal:
                targetVelocity = HandleNormalState();
                break;
        }

        // Apply movement
        currentVelocity = targetVelocity;
        fishHookable.Move(targetVelocity);
        rb.velocity = targetVelocity;

        // Handle boundaries
        HandleBoundaries(rb);
        
        // Update sprite direction
        UpdateSpriteDirection();
    }

    private Vector2 HandleStunnedState()
    {
        currentDirection = Vector2.zero;
        return Vector2.zero;
    }

    private Vector2 HandleHookedState()
    {
        Vector2 hookedMovement = fishHookable.HookedMovement();
        currentDirection = hookedMovement.normalized;
        return hookedMovement;
    }

    private Vector2 HandleBaitChaseState()
    {
        Transform bait = fishHookable.GetNearestBait();
        if (bait == null) return HandleNormalState();

        // Calculate direct path to bait
        Vector2 toBait = (Vector2)bait.position - (Vector2)transform.position;
        
        // Directly set the direction to the bait without smooth rotation
        currentDirection = toBait.normalized;

        // Use increased speed for bait chasing
        float chaseSpeed = behavior.baseSpeed * baitChaseSpeedMultiplier;
        
        Debug.Log($"Fish {gameObject.name} chasing bait - Speed: {chaseSpeed}, Direction: {currentDirection}");
        return currentDirection * chaseSpeed;
    }

    private Vector2 HandleNormalState()
    {
        UpdateTimers();
        Vector2 desiredDirection = CalculateMovementForce();
        
        // Normal smooth rotation
        float angle = Vector2.SignedAngle(currentDirection, desiredDirection);
        float maxRotation = maxTurnSpeed * Time.fixedDeltaTime;
        float rotation = Mathf.Clamp(angle, -maxRotation, maxRotation);
        
        currentDirection = RotateVector2(currentDirection, rotation);
        
        // Calculate normal movement speed
        float baseSpeed = behavior.baseSpeed * speedMultiplier;
        if (schooling != null)
        {
            Vector2 schoolingForce = schooling.CalculateSchoolingForce();
            float schoolingInfluence = Mathf.Lerp(0.8f, 1.2f, schoolingForce.magnitude);
            baseSpeed *= schoolingInfluence;
        }
        
        baseSpeed = Mathf.Max(baseSpeed, minimumSpeed);
        return currentDirection * baseSpeed;
    }

    private void HandleBoundaries(Rigidbody2D rb)
    {
        if (!hasBoundary) return;

        Vector2 position = transform.position;
        bool wasOutOfBounds = false;

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

        if (wasOutOfBounds)
        {
            transform.position = position;
            float speed = Mathf.Max(behavior.baseSpeed * speedMultiplier, minimumSpeed);
            rb.velocity = currentDirection * speed;
        }
    }

    private void UpdateSpriteDirection()
    {
        if (spriteTransform != null && Mathf.Abs(currentDirection.x) > 0.01f)
        {
            Vector3 scale = spriteTransform.localScale;
            scale.y = Mathf.Abs(scale.y);
            scale.z = Mathf.Abs(scale.z);
            scale.x = Mathf.Abs(scale.x) * (currentDirection.x > 0 ? 1 : -1);
            spriteTransform.localScale = scale;
        }
    }

    private void UpdateState()
    {
        if (fishHookable.IsStunned)
        {
            currentState = FishState.Stunned;
        }
        else if (fishHookable.IsHooked)
        {
            currentState = FishState.Hooked;
        }
        else
        {
            // Check for bait
            Transform nearestBait = fishHookable.GetNearestBait();
            if (nearestBait != null)
            {
                float distanceToBait = Vector2.Distance(transform.position, nearestBait.position);
                if (distanceToBait <= behavior.baitDetectionRange)
                {
                    currentState = FishState.ChasingBait;
                    Debug.Log($"Fish {gameObject.name} entering bait chase state, distance: {distanceToBait}");
                }
                else
                {
                    currentState = FishState.Normal;
                }
            }
            else
            {
                currentState = FishState.Normal;
            }
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
            Debug.Log($"Fish {gameObject.name} avoiding boundary");
            return boundaryDirection;
        }

        Vector2 moveDirection = Vector2.zero;
        float totalWeight = 0f;

        // Check for bait first (highest priority)
        Transform nearestBait = fishHookable.GetNearestBait();
        if (nearestBait != null)
        {
            Vector2 toBait = (Vector2)nearestBait.position - (Vector2)transform.position;
            float distanceToBait = toBait.magnitude;
            
            Debug.Log($"Fish {gameObject.name} detected bait at distance {distanceToBait}, detection range: {behavior.baitDetectionRange}");
            
            if (distanceToBait <= behavior.baitDetectionRange)
            {
                // Move towards bait with highest priority
                Vector2 baitForce = toBait.normalized * behavior.baitAttractionWeight * 5f; // Increased multiplier
                moveDirection += baitForce;
                totalWeight += behavior.baitAttractionWeight * 5f;
                
                Debug.Log($"Fish {gameObject.name} moving towards bait with force: {baitForce}, weight: {behavior.baitAttractionWeight * 5f}");
                
                // If bait is detected, greatly reduce other behaviors
                if (moveDirection.magnitude > 0.1f)
                {
                    // Add minimal schooling and wandering to maintain some natural movement
                    if (schooling != null)
                    {
                        Vector2 schoolForce = schooling.CalculateSchoolingForce() * 0.1f; // Reduced schooling influence
                        moveDirection += schoolForce;
                        totalWeight += 0.1f;
                    }
                    
                    // Return normalized result with bait as primary influence
                    Vector2 finalDirection = (moveDirection / totalWeight).normalized;
                    Debug.Log($"Fish {gameObject.name} final bait movement direction: {finalDirection}");
                    return finalDirection;
                }
            }
        }
        else
        {
            Debug.Log($"Fish {gameObject.name} no bait detected");
        }

        // If no bait or bait influence is weak, calculate normal movement
        if (schooling != null)
        {
            Vector2 schoolingForce = schooling.CalculateSchoolingForce();
            if (schoolingForce.magnitude > 0.1f)
            {
                moveDirection += schoolingForce * behavior.flockWeight;
                totalWeight += behavior.flockWeight;
            }
        }

        // Add wander force with lower priority
        Vector2 wanderForce = CalculateWanderForce();
        moveDirection += wanderForce * behavior.wanderWeight;
        totalWeight += behavior.wanderWeight;

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
        if (!Application.isPlaying || behavior == null) return;
        
        Vector3 pos = transform.position;
        
        // Draw movement direction
        Gizmos.color = Color.green;
        Gizmos.DrawLine(pos, pos + (Vector3)currentVelocity);
        
        // Draw bait attraction force if bait is detected
        Transform nearestBait = fishHookable?.GetNearestBait();
        if (nearestBait != null)
        {
            Vector2 toBait = (Vector2)nearestBait.position - (Vector2)pos;
            if (toBait.magnitude <= behavior.baitDetectionRange)
            {
                Gizmos.color = Color.yellow;
                Vector3 baitForce = (Vector3)(toBait.normalized * behavior.baitAttractionWeight * 5f);
                Gizmos.DrawLine(pos, pos + baitForce);
            }
        }
        
        // Draw boundary and return forces if applicable
        if (boundaryArea != null && spawner != null && spawner.showDebugVisuals)
        {
            if (IsNearBoundary(pos))
            {
                Gizmos.color = Color.red;
                Vector3 boundaryForce = (Vector3)CalculateBoundaryAvoidance();
                Gizmos.DrawLine(pos, pos + boundaryForce);
            }
            
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
    }
} 