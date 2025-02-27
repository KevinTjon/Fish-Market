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

    private new void Awake()
    {
        base.Awake();
        rb.isKinematic = false;
        isHooked = false;
        isStunned = false;
        currentSpeed = behavior.baseSpeed;
        StartCoroutine(UpdateNearbyEntities());
    }

    private IEnumerator UpdateNearbyEntities()
    {
        while (true)
        {
            if (!isStunned && !isHooked)
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
                nearestBait = null;
                foreach (var collider in baitColliders)
                {
                    float distance = Vector2.Distance(transform.position, collider.transform.position);
                    if (distance < nearestBaitDistance)
                    {
                        nearestBaitDistance = distance;
                        nearestBait = collider.transform;
                    }
                }

                // If predator, find nearest prey
                if (behavior.isPredator)
                {
                    Collider2D[] preyColliders = Physics2D.OverlapCircleAll(transform.position, behavior.visionRange, behavior.preyLayer);
                    float nearestPreyDistance = float.MaxValue;
                    nearestPrey = null;
                    foreach (var collider in preyColliders)
                    {
                        float distance = Vector2.Distance(transform.position, collider.transform.position);
                        if (distance < nearestPreyDistance)
                        {
                            nearestPreyDistance = distance;
                            nearestPrey = collider.transform;
                        }
                    }
                }
            }
            yield return new WaitForSeconds(0.2f); // Update every 0.2 seconds for performance
        }
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

        // Store the current velocity for reference
        currentVelocity = velocity;
        
        // We don't need to apply movement here as it's handled by FishMovement
        // This method now just stores the velocity for reference
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
