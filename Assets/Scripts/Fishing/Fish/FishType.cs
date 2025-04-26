using UnityEngine;
using FishSizeNamespace;

[CreateAssetMenu(fileName = "New Fish Type", menuName = "Fish Market/Fish Type")]
public class FishType : ScriptableObject
{
    [Header("Basic Info")]
    public string fishName;
    public FishSize size;
    public GameObject prefab;
    public float baseValue = 10f;

    [Header("Spawn Settings")]
    public float minDepth = 0f;
    public float maxDepth = 10f;
    public float spawnWeight = 1f;  // Higher weight = more common
    [Range(0f, 1f)]
    public float spawnChance = 1f;

    [Header("Behavior Settings")]
    public float minSpeed = 1f;
    public float maxSpeed = 3f;
    public float preyChaseSpeed = 1.5f;  // Multiplier when chasing prey
    public float fleeSpeed = 2f;         // Multiplier when fleeing
    public float turnSpeed = 3f;

    [Header("Depth Behavior")]
    public float preferredDepth;         // The depth this fish prefers to swim at
    public float depthVariance = 2f;     // How far from preferred depth they'll stray
    
    [Header("Interaction Settings")]
    public bool canEat = false;          // Can this fish eat other fish?
    public FishSize[] preySizes;         // What sizes of fish can this fish eat?
    public float visionRange = 5f;       // How far can this fish see?
    public float personalSpace = 1f;     // Minimum distance from other fish
    
    [Header("Time Settings")]
    public bool isDayActive = true;      // Active during day?
    public bool isNightActive = true;    // Active during night?
} 