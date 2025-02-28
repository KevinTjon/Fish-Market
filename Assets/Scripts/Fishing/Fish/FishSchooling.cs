using UnityEngine;
using System.Collections.Generic;

[RequireComponent(typeof(FishMovement))]
public class FishSchooling : MonoBehaviour
{
    [Header("Schooling Settings")]
    public float schoolRadius = 5f;          // Radius to look for school mates
    public float separationRadius = 1f;      // Minimum distance between fish
    public float maxSchoolSize = 10f;        // Maximum number of fish in a school
    public float optimalSchoolDistance = 2f; // Optimal distance between fish in school
    
    [Header("Predator Avoidance")]
    public float predatorDetectionRadius = 2f;    // How far to detect predators
    public float panicSpeed = 2f;                 // Speed multiplier when fleeing
    public float minPredatorSize = 1.2f;          // Minimum size ratio to be considered a predator
    
    [Header("Behavior Weights")]
    [Range(0, 3)] public float cohesionWeight = 1.5f;      // Weight of moving towards center of school
    [Range(0, 3)] public float alignmentWeight = 1.2f;      // Weight of aligning with school direction
    [Range(0, 3)] public float separationWeight = 1.5f;     // Weight of avoiding other fish that are too close
    [Range(0, 5)] public float predatorAvoidWeight = 3f;    // Weight of avoiding predators
    
    private FishMovement fishMovement;
    private FishHookable fishHookable;
    private List<FishSchooling> nearbyFish = new List<FishSchooling>();
    private List<FishHookable> nearbyPredators = new List<FishHookable>();
    private Vector2 schoolingForce;
    private bool isPanicked = false;
    
    private void Start()
    {
        fishMovement = GetComponent<FishMovement>();
        fishHookable = GetComponent<FishHookable>();
        
        // Start with default weights based on fish size
        if (fishHookable?.behavior != null)
        {
            // Smaller fish tend to school more tightly
            float sizeMultiplier = 1f / fishHookable.behavior.size;
            cohesionWeight *= sizeMultiplier;
            alignmentWeight *= sizeMultiplier;
            
            // Adjust detection ranges based on fish size
            schoolRadius *= fishHookable.behavior.size;
            separationRadius *= fishHookable.behavior.size;
            optimalSchoolDistance *= fishHookable.behavior.size;
            predatorDetectionRadius *= (1f + (1f - fishHookable.behavior.size)); // Smaller fish detect predators from further away
        }
    }
    
    public void UpdateNearbyFish(List<FishSchooling> allFish)
    {
        nearbyFish.Clear();
        nearbyPredators.Clear();
        isPanicked = false;
        
        foreach (var otherFish in allFish)
        {
            if (otherFish == this) continue;
            
            Vector2 otherPos = otherFish.transform.position;
            float distance = Vector2.Distance(transform.position, otherPos);
            
            // Check for predators first
            if (IsPredator(otherFish.fishHookable) && distance <= predatorDetectionRadius)
            {
                nearbyPredators.Add(otherFish.fishHookable);
                isPanicked = true;
            }
            
            // Then check for schoolmates
            if (distance <= schoolRadius && IsSameSchoolType(otherFish))
            {
                nearbyFish.Add(otherFish);
                if (nearbyFish.Count >= maxSchoolSize) break;
            }
        }
    }
    
    private bool IsPredator(FishHookable other)
    {
        if (fishHookable == null || other == null || other.behavior == null) return false;
        
        // Consider a fish a predator if it's significantly larger
        return other.behavior.size >= (fishHookable.behavior.size * minPredatorSize);
    }
    
    private bool IsSameSchoolType(FishSchooling other)
    {
        if (fishHookable == null || other.fishHookable == null) return false;
        return fishHookable.behavior.size == other.fishHookable.behavior.size;
    }
    
    private Vector2 CalculatePredatorAvoidance()
    {
        if (nearbyPredators.Count == 0) return Vector2.zero;
        
        Vector2 avoidance = Vector2.zero;
        
        foreach (var predator in nearbyPredators)
        {
            if (predator == null) continue;
            
            Vector2 awayFromPredator = (Vector2)transform.position - (Vector2)predator.transform.position;
            float distance = awayFromPredator.magnitude;
            
            // Stronger avoidance when closer to predator
            float avoidanceStrength = 1f - Mathf.Clamp01(distance / predatorDetectionRadius);
            avoidanceStrength = Mathf.Pow(avoidanceStrength, 2); // Square for stronger close-range avoidance
            
            // Add size difference factor - flee more from bigger predators
            float sizeDifference = predator.behavior.size / fishHookable.behavior.size;
            avoidanceStrength *= Mathf.Clamp(sizeDifference, 1f, 3f);
            
            avoidance += awayFromPredator.normalized * avoidanceStrength;
        }
        
        return avoidance.normalized * predatorAvoidWeight;
    }
    
    public Vector2 CalculateSchoolingForce()
    {
        // Calculate predator avoidance first
        Vector2 predatorForce = CalculatePredatorAvoidance();
        
        // If strongly avoiding predators, prioritize escape
        if (predatorForce.magnitude > predatorAvoidWeight * 0.8f)
        {
            return predatorForce;
        }
        
        // Normal schooling behavior when no immediate predator threat
        if (nearbyFish.Count == 0) return predatorForce;
        
        Vector2 cohesion = Vector2.zero;
        Vector2 alignment = Vector2.zero;
        Vector2 separation = Vector2.zero;
        Vector2 schoolCenter = Vector2.zero;
        
        int count = 0;
        float totalWeight = 0f;
        
        foreach (var fish in nearbyFish)
        {
            if (fish == null) continue;
            
            Vector2 fishPos = fish.transform.position;
            float distance = Vector2.Distance(transform.position, fishPos);
            
            // Calculate influence weight based on distance
            float distanceWeight = 1f - (distance / schoolRadius);
            distanceWeight = Mathf.Pow(distanceWeight, 2);
            totalWeight += distanceWeight;
            
            // Separation - stronger at close range
            if (distance < separationRadius)
            {
                Vector2 awayFromFish = (Vector2)transform.position - fishPos;
                float separationStrength = 1f - (distance / separationRadius);
                separation += awayFromFish.normalized * separationStrength * 2f;
            }
            
            // Cohesion - pull towards optimal distance
            if (distance > optimalSchoolDistance)
            {
                float cohesionStrength = (distance - optimalSchoolDistance) / (schoolRadius - optimalSchoolDistance);
                cohesionStrength = Mathf.Pow(cohesionStrength, 2);
                cohesion += (fishPos - (Vector2)transform.position).normalized * cohesionStrength;
            }
            
            // Alignment - weighted by distance
            Vector2 fishVelocity = fish.GetComponent<FishMovement>().GetCurrentVelocity();
            alignment += fishVelocity * distanceWeight;
            
            schoolCenter += fishPos;
            count++;
        }
        
        if (count > 0)
        {
            schoolCenter /= count;
            
            if (totalWeight > 0)
            {
                alignment = (alignment / totalWeight).normalized * alignmentWeight;
            }
            
            if (separation.magnitude > 0)
            {
                separation = separation.normalized * separationWeight;
            }
            
            float distanceToCenter = Vector2.Distance(transform.position, schoolCenter);
            float centerCohesionStrength = Mathf.Clamp01(distanceToCenter / schoolRadius);
            Vector2 centerCohesion = ((Vector2)schoolCenter - (Vector2)transform.position).normalized 
                                   * centerCohesionStrength * cohesionWeight;
            
            // Combine forces with priority to separation and predator avoidance
            schoolingForce = separation + predatorForce;
            if (separation.magnitude < 0.1f && predatorForce.magnitude < 0.1f)
            {
                schoolingForce += alignment + centerCohesion + cohesion;
            }
            
            // Adjust force magnitude based on panic state
            float forceMagnitude = schoolingForce.magnitude;
            float maxForce = isPanicked ? 2f * panicSpeed : 2f;
            schoolingForce = schoolingForce.normalized * Mathf.Clamp(forceMagnitude, 0.5f, maxForce);
        }
        
        return schoolingForce;
    }
    
    private void OnDrawGizmosSelected()
    {
        if (!fishMovement?.spawner?.showDebugVisuals ?? false) return;
        
        // Draw school radius
        Gizmos.color = new Color(0, 1, 0, 0.1f);
        Gizmos.DrawWireSphere(transform.position, schoolRadius);
        
        // Draw separation radius
        Gizmos.color = new Color(1, 0, 0, 0.2f);
        Gizmos.DrawWireSphere(transform.position, separationRadius);
        
        // Draw predator detection radius
        Gizmos.color = new Color(1, 0.5f, 0, 0.1f);
        Gizmos.DrawWireSphere(transform.position, predatorDetectionRadius);
        
        // Draw lines to school members
        Gizmos.color = Color.yellow;
        foreach (var fish in nearbyFish)
        {
            if (fish != null)
            {
                Gizmos.DrawLine(transform.position, fish.transform.position);
            }
        }
        
        // Draw lines to predators (in red)
        Gizmos.color = Color.red;
        foreach (var predator in nearbyPredators)
        {
            if (predator != null)
            {
                Gizmos.DrawLine(transform.position, predator.transform.position);
            }
        }
        
        // Draw schooling force
        if (schoolingForce.magnitude > 0.1f)
        {
            Gizmos.color = isPanicked ? Color.red : Color.cyan;
            Gizmos.DrawRay(transform.position, schoolingForce * 2f);
        }
    }
} 