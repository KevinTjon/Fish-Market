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

    [Header("Movement Bounds")]
    [HideInInspector] public BoxCollider2D boundaryArea; // Now assigned by spawner
    public float boundaryForce = 5f;
    public float boundaryTurnSpeed = 2f;
    public float edgeBuffer = 2f;
    public float returnToCenterForce = 2f;
    public float boundaryExponent = 2f;

    [Header("Movement Settings")]
    public float directionChangeSpeed = 0.1f;
    public float minSpeedMultiplier = 0.5f;
    public float maxSpeedMultiplier = 1.5f;
    private float speedMultiplier = 1f;
    private float directionChangeTimer;
    private float directionChangeInterval;

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
            targetDirection = Random.insideUnitCircle.normalized;
            
            // Initialize random timers
            directionChangeInterval = Random.Range(3f, 6f);
            speedMultiplier = Random.Range(minSpeedMultiplier, maxSpeedMultiplier);
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
        if (fishHookable.IsStunned || fishHookable.IsHooked || behavior == null)
            return;

        UpdateTimers();
        
        // Calculate all movement forces
        Vector2 movementForce = CalculateMovementForce();
        
        // Apply movement using the rigidbody
        Vector2 targetVelocity = movementForce * behavior.baseSpeed * speedMultiplier;
        fishHookable.GetComponent<Rigidbody2D>().velocity = targetVelocity;
        
        // Store current velocity for reference
        currentVelocity = targetVelocity;
        
        // Simple left/right sprite flipping
        if (spriteTransform != null && Mathf.Abs(targetVelocity.x) > 0.01f)
        {
            Vector3 scale = spriteTransform.localScale;
            scale.x = Mathf.Abs(scale.x) * (targetVelocity.x > 0 ? 1 : -1);
            spriteTransform.localScale = scale;
        }
        
        // Pass the velocity to FishHookable for consistency
        fishHookable.Move(targetVelocity);
    }

    private void UpdateTimers()
    {
        directionChangeTimer += Time.fixedDeltaTime;
        if (directionChangeTimer >= directionChangeInterval)
        {
            // Change direction and speed
            targetDirection = Random.insideUnitCircle.normalized;
            speedMultiplier = Random.Range(minSpeedMultiplier, maxSpeedMultiplier);
            directionChangeInterval = Random.Range(3f, 6f);
            directionChangeTimer = 0f;
        }
    }

    private Vector2 CalculateMovementForce()
    {
        Vector2 force = Vector2.zero;
        
        // Get the fish hookable component
        if (fishHookable == null) return force;
        
        // If there's nearby bait, move towards it
        Transform nearestBait = fishHookable.GetNearestBait();
        if (nearestBait != null)
        {
            Vector2 toBait = (Vector2)nearestBait.position - (Vector2)transform.position;
            float distanceToBait = toBait.magnitude;
            
            // Calculate bait attraction force
            Vector2 baitForce = toBait.normalized * behavior.baitAttractionWeight;
            
            // Apply stronger attraction when closer to bait
            float baitAttractionMultiplier = 1f - (distanceToBait / behavior.baitDetectionRange);
            baitAttractionMultiplier = Mathf.Pow(baitAttractionMultiplier, 2); // Square for stronger close-range attraction
            baitForce *= baitAttractionMultiplier;
            
            force += baitForce;
            
            Debug.Log($"Fish {gameObject.name} moving towards bait: {nearestBait.name}, Force: {baitForce}, Distance: {distanceToBait}");
        }
        
        // Add schooling force if we have a schooling component
        if (schooling != null)
        {
            Vector2 schoolingForce = schooling.CalculateSchoolingForce();
            force += schoolingForce;
        }
        
        // Add boundary avoidance
        Vector2 boundaryForce = CalculateBoundaryAvoidance();
        force += boundaryForce;
        
        // Add return to center force if outside safe zone
        if (IsOutsideSafeZone(transform.position))
        {
            force += CalculateReturnForce();
        }
        
        // Add random wandering if no other forces are significant
        if (force.magnitude < 0.1f)
        {
            force += CalculateWanderForce();
        }
        
        return force.normalized;
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
        Vector2 avoidanceForce = Vector2.zero;
        Vector2 position = transform.position;

        // Calculate distances from boundaries
        float distanceFromLeft = position.x - minX;
        float distanceFromRight = maxX - position.x;
        float distanceFromBottom = position.y - minY;
        float distanceFromTop = maxY - position.y;

        // Only apply forces when actually near boundaries
        if (distanceFromLeft < edgeBuffer)
        {
            float urgency = Mathf.Pow(1 - (distanceFromLeft / edgeBuffer), boundaryExponent);
            avoidanceForce += Vector2.right * boundaryForce * urgency;
        }
        if (distanceFromRight < edgeBuffer)
        {
            float urgency = Mathf.Pow(1 - (distanceFromRight / edgeBuffer), boundaryExponent);
            avoidanceForce += Vector2.left * boundaryForce * urgency;
        }
        if (distanceFromBottom < edgeBuffer)
        {
            float urgency = Mathf.Pow(1 - (distanceFromBottom / edgeBuffer), boundaryExponent);
            avoidanceForce += Vector2.up * boundaryForce * urgency;
        }
        if (distanceFromTop < edgeBuffer)
        {
            float urgency = Mathf.Pow(1 - (distanceFromTop / edgeBuffer), boundaryExponent);
            avoidanceForce += Vector2.down * boundaryForce * urgency;
        }

        // Only add randomization if we're actually applying forces
        if (avoidanceForce.magnitude > 0)
        {
            avoidanceForce += Random.insideUnitCircle * boundaryForce * 0.1f;
        }

        return avoidanceForce;
    }

    private Vector2 CalculateReturnForce()
    {
        Vector2 position = transform.position;
        Vector2 returnForce = Vector2.zero;

        Vector2 toCenter = centerPoint - position;
        float distanceFromCenter = toCenter.magnitude;
        float maxAllowedDistance = Mathf.Min(maxX - minX, maxY - minY) * 0.5f;
        
        // Only apply return force when actually outside safe zone
        if (distanceFromCenter > maxAllowedDistance * 0.8f)
        {
            float urgency = Mathf.Pow((distanceFromCenter - maxAllowedDistance * 0.8f) / (maxAllowedDistance * 0.2f), boundaryExponent);
            returnForce = toCenter.normalized * returnToCenterForce * urgency;
            
            // Minimal randomization
            returnForce += Random.insideUnitCircle * returnToCenterForce * 0.02f;
        }

        return returnForce;
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
} 