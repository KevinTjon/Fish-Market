using UnityEngine;

public enum FishSize
{
    Tiny,
    Small,
    Medium,
    Large,
    Huge
}

[CreateAssetMenu(fileName = "FishType", menuName = "Fish/Type")]
public class FishType : ScriptableObject
{
    [Header("Fish Properties")]
    public string fishName;
    public FishSize size;
    public GameObject prefab; // Reference to the fish prefab
    
    [Header("Value Settings")]
    public float baseValue; // Base monetary value of the fish
    public float minValue; // Minimum value the fish can be worth
    public float maxValue; // Maximum value the fish can be worth
    
    [Header("Behavior Settings")]
    public bool canEat; // Whether this fish can eat other fish
    public FishSize[] preySize; // What sizes of fish this can eat
    public LayerMask preyLayer; // Layer mask for potential prey
    public float preyDetectionRange = 5f; // How far this fish can detect prey
    public float preyChaseSpeed = 1.5f; // Speed multiplier when chasing prey
    
    [Header("Spawning Settings")]
    public int maxPopulation = 10; // Maximum number of this fish type allowed
    public float spawnRate = 1f; // How frequently this fish spawns
    public float spawnChance = 0.5f; // Chance of spawning when conditions are met
    
    [Header("Detection Settings")]
    public float personalSpace = 1f; // Minimum distance to maintain from other fish
    public float visionRange = 5f; // How far this fish can see other fish and bait
    
    // Helper method to check if this fish can eat a specific size
    public bool CanEatSize(FishSize targetSize)
    {
        if (!canEat || preySize == null) return false;
        
        foreach (var size in preySize)
        {
            if (size == targetSize) return true;
        }
        return false;
    }
} 