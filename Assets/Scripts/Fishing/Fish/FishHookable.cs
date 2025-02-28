using System.Collections;
using System.Collections.Generic;
using UnityEngine;
//us
public class FishHookable : ObjectHookable, IFish
{
    [Header("Fish Configuration")]
    public FishBehavior behavior;
    
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
        
        StartCoroutine(UpdateNearbyEntities());
    }

    private IEnumerator UpdateNearbyEntities()
    {
        while (true)
        {
            if (!isStunned && !isHooked && behavior != null)
            {
                // Update nearby fish
                nearbyFish.Clear();
                Collider2D[] fishColliders = Physics2D.OverlapCircleAll(transform.position, behavior.visionRange, fishLayer);
                foreach (var collider in fishColliders)
                {
                    if (collider.transform != transform)
                    {
                        nearbyFish.Add(collider.transform);
                    }
                }

                // Find nearest bait
                Collider2D[] baitColliders = Physics2D.OverlapCircleAll(transform.position, behavior.baitDetectionRange, baitLayer);
                float nearestBaitDistance = float.MaxValue;
                Transform previousBait = nearestBait;  // Store previous bait for comparison
                nearestBait = null;
                
                if (baitColliders.Length > 0)
                {
                    Debug.Log($"Fish {gameObject.name} detected {baitColliders.Length} bait(s) within range {behavior.baitDetectionRange}");
                    
                    foreach (var collider in baitColliders)
                    {
                        float distance = Vector2.Distance(transform.position, collider.transform.position);
                        BaitObject baitObj = collider.GetComponent<BaitObject>();
                        
                        if (baitObj != null)
                        {
                            bool isCompatible = baitObj.IsAttractedToFish(GetFishSize());
                            Debug.Log($"Checking bait {baitObj.name} - Distance: {distance}, Compatible: {isCompatible}");
                            
                            if (isCompatible && distance < nearestBaitDistance)
                            {
                                nearestBaitDistance = distance;
                                nearestBait = collider.transform;
                            }
                        }
                    }
                }
                else
                {
                    Debug.Log($"Fish {gameObject.name} found no bait within range {behavior.baitDetectionRange}");
                }
                
                // Log if bait target changed
                if (previousBait != nearestBait)
                {
                    if (nearestBait != null)
                        Debug.Log($"Fish {gameObject.name} now targeting bait: {nearestBait.name}");
                    else if (previousBait != null)
                        Debug.Log($"Fish {gameObject.name} stopped targeting bait: {previousBait.name}");
                }
            }
            yield return new WaitForSeconds(0.2f); // Update every 0.2 seconds for performance
        }
    }

    private FishSize GetFishSize()
    {
        if (behavior == null) return FishSize.Medium;
        
        float size = behavior.size;
        if (size <= 0.6f) return FishSize.Tiny;
        else if (size <= 0.8f) return FishSize.Small;
        else if (size <= 1.2f) return FishSize.Medium;
        else if (size <= 1.7f) return FishSize.Large;
        else return FishSize.Huge;
    }

    public new void Hook(Vector2 hookPos)
    {
        base.Hook(hookPos);
        isStunned = true;
        rb.isKinematic = true;
        
        // Clear references to nearby entities when hooked
        nearbyFish.Clear();
        nearestBait = null;
        nearestPredator = null;
        nearestPrey = null;
        
        // Stop the coroutine that updates nearby entities
        StopAllCoroutines();
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
    }

    public Vector2 HookedMovement()
    {
        // Add random struggling movement when hooked
        return new Vector2(Random.Range(-1f, 1f), Random.Range(-1f, 1f)).normalized * behavior.baseSpeed * 0.5f;
    }

    public void Move(Vector2 velocity)
    {
        if (isHooked || isStunned) return;

        // Just store the velocity for reference, don't apply movement
        currentVelocity = velocity;
    }

    private Vector2 CalculateFlockingForce()
    {
        if (nearbyFish.Count == 0) return Vector2.zero;

        Vector2 cohesion = Vector2.zero;
        Vector2 alignment = Vector2.zero;
        Vector2 separation = Vector2.zero;
        int count = 0;

        foreach (Transform fish in nearbyFish)
        {
            float distance = Vector2.Distance(transform.position, fish.position);
            
            // Separation
            if (distance < behavior.personalSpace)
            {
                Vector2 diff = (Vector2)(transform.position - fish.position);
                diff.Normalize();
                diff /= distance;
                separation += diff;
            }
            
            // Cohesion and Alignment
            if (distance < behavior.visionRange)
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
        if (!behavior.isPredator || nearestPrey == null) return Vector2.zero;
        
        return ((Vector2)nearestPrey.position - (Vector2)transform.position).normalized;
    }
}
