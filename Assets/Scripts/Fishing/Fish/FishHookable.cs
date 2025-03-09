using System.Collections;
using System.Collections.Generic;
using UnityEngine;
//us
public class FishHookable : ObjectHookable, IFish
{
    [Header("Fish Configuration")]
    public FishType fishType; // Reference to the fish type scriptable object
    public FishBehavior behavior; // Now only used for movement behavior
    
    private bool isStunned;
    public bool IsStunned { get {return isStunned;}}
    
    private Vector2 currentVelocity;
    private Vector2 targetDirection;
    private float currentSpeed;
    
    [Header("Detection")]
    public LayerMask fishLayer;
    public LayerMask baitLayer;
    
    private List<Transform> nearbyFish = new List<Transform>();
    private Transform nearestBait;
    private Transform nearestPredator;
    private Transform nearestPrey;

    // Add public accessor for nearest bait
    public Transform GetNearestBait()
    {
        return nearestBait;
    }

    private new void Awake()
    {
        base.Awake();
        rb.isKinematic = false;
        isHooked = false;
        isStunned = false;
        
        // Only set current speed if behavior is assigned
        if (behavior != null)
        {
            currentSpeed = behavior.baseSpeed;
        }
        else
        {
            Debug.LogWarning($"No behavior assigned to fish: {gameObject.name}");
            currentSpeed = 1f; // Default speed
        }
        
        // Validate fish type
        if (fishType == null)
        {
            Debug.LogError($"No fish type assigned to fish: {gameObject.name}");
        }
        
        StartCoroutine(UpdateNearbyEntities());
    }

    private IEnumerator UpdateNearbyEntities()
    {
        while (true)
        {
            if (!isStunned && !isHooked && fishType != null)
            {
                // Update nearby fish
                nearbyFish.Clear();
                Collider2D[] fishColliders = Physics2D.OverlapCircleAll(transform.position, fishType.visionRange, fishLayer);
                foreach (var collider in fishColliders)
                {
                    if (collider.transform != transform)
                    {
                        nearbyFish.Add(collider.transform);
                        
                        // Check if this is potential prey
                        if (fishType.canEat)
                        {
                            var otherFish = collider.GetComponent<FishHookable>();
                            if (otherFish != null && fishType.CanEatSize(otherFish.GetFishSize()))
                            {
                                float distance = Vector2.Distance(transform.position, collider.transform.position);
                                if (nearestPrey == null || distance < Vector2.Distance(transform.position, nearestPrey.position))
                                {
                                    nearestPrey = collider.transform;
                                }
                            }
                        }
                    }
                }

                // Find nearest bait within vision range
                Collider2D[] baitColliders = Physics2D.OverlapCircleAll(transform.position, fishType.visionRange, baitLayer);
                float nearestBaitDistance = float.MaxValue;
                nearestBait = null;
                
                foreach (var collider in baitColliders)
                {
                    float distance = Vector2.Distance(transform.position, collider.transform.position);
                    if (distance < nearestBaitDistance)
                    {
                        BaitObject baitObj = collider.GetComponent<BaitObject>();
                        if (baitObj != null && baitObj.IsAttractedToFish(GetFishSize()))
                        {
                            nearestBaitDistance = distance;
                            nearestBait = collider.transform;
                        }
                    }
                }
            }
            yield return new WaitForSeconds(0.2f);
        }
    }

    // Get the fish size directly from the FishType
    public FishSize GetFishSize()
    {
        return fishType != null ? fishType.size : FishSize.Medium;
    }

    public override void Hook(Vector2 hookPosition)
    {
        base.Hook(hookPosition);
        isStunned = true;
        rb.isKinematic = false;
        rb.gravityScale = 0f;
        
        // Clear references to nearby entities when hooked
        nearbyFish.Clear();
        nearestBait = null;
        nearestPredator = null;
        nearestPrey = null;
        
        // Stop the coroutine that updates nearby entities
        StopAllCoroutines();
    }

    // New method that takes the hook transform
    public void SetHookPosition(Transform hookTransform)
    {
        if (hookTransform != null)
        {
            // Set position below the hook
            transform.position = hookTransform.position - (Vector3.up * 0.5f);
            
            // Rotate fish to be vertical
            transform.rotation = Quaternion.identity;
        }
    }

    public new void Unhook()
    {
        StartCoroutine(UnhookFishStun());
    }

    private IEnumerator UnhookFishStun()
    {
        isHooked = false;
        yield return new WaitForSeconds(2f);
        isStunned = false;
        rb.isKinematic = false;
        
        // Restart entity detection
        StartCoroutine(UpdateNearbyEntities());
    }

    // Internal method for calculating struggling movement when hooked
    internal Vector2 CalculateHookedMovement()
    {
        if (!isHooked) return Vector2.zero;

        // Calculate struggling movement
        float struggleSpeed = behavior != null ? behavior.baseSpeed * 2f : 2f;
        float time = Time.time; // Use time for smooth oscillation
        
        // Create a more natural struggling pattern
        float horizontalStruggle = Mathf.Sin(time * 5f) * Mathf.Cos(time * 3f);
        float verticalStruggle = Mathf.Cos(time * 4f) * Mathf.Sin(time * 2f);
        
        return new Vector2(horizontalStruggle, verticalStruggle).normalized * struggleSpeed;
    }

    // Implementation of IFish interface
    public Vector2 HookedMovement()
    {
        return CalculateHookedMovement();
    }

    public void Move(Vector2 velocity)
    {
        if (isHooked || isStunned) return;

        // Adjust speed based on whether chasing prey
        if (nearestPrey != null && fishType != null)
        {
            velocity *= fishType.preyChaseSpeed;
        }

        currentVelocity = velocity;
    }

    private Vector2 CalculateFlockingForce()
    {
        if (nearbyFish.Count == 0 || fishType == null) return Vector2.zero;

        Vector2 cohesion = Vector2.zero;
        Vector2 alignment = Vector2.zero;
        Vector2 separation = Vector2.zero;
        int count = 0;

        foreach (Transform fish in nearbyFish)
        {
            float distance = Vector2.Distance(transform.position, fish.position);
            
            // Separation
            if (distance < fishType.personalSpace)
            {
                Vector2 diff = (Vector2)(transform.position - fish.position);
                diff.Normalize();
                diff /= distance;
                separation += diff;
            }
            
            // Cohesion and Alignment
            if (distance < fishType.visionRange)
            {
                cohesion += (Vector2)fish.position;
                alignment += (Vector2)fish.up;
                count++;
            }
        }

        if (count > 0)
        {
            cohesion /= count;
            cohesion = (cohesion - (Vector2)transform.position).normalized;
            
            alignment /= count;
            alignment.Normalize();
        }

        return (cohesion + alignment + separation).normalized;
    }

    private Vector2 CalculateWanderForce()
    {
        // Simple wandering behavior
        targetDirection = Vector2.Lerp(targetDirection, Random.insideUnitCircle, Time.deltaTime * 0.1f);
        return targetDirection.normalized;
    }

    private Vector2 CalculateBaitForce()
    {
        if (nearestBait == null) return Vector2.zero;
        
        return ((Vector2)nearestBait.position - (Vector2)transform.position).normalized;
    }

    private Vector2 CalculatePreyForce()
    {
        if (nearestPrey == null || fishType == null || !fishType.canEat) return Vector2.zero;
        
        return ((Vector2)nearestPrey.position - (Vector2)transform.position).normalized;
    }
}
