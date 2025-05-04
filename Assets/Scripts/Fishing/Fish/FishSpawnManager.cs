using UnityEngine;
using System.Collections.Generic;

[System.Serializable]
public class FishSpawnData
{
    public GameObject fishPrefab;
    public WaterLevel level;
    [Min(1)]
    public int amountToSpawn = 3;
    [Tooltip("If true, fish of this type will school together")]
    public bool isSchooling = false;
    [Tooltip("The radius within which fish will look for schoolmates")]
    [Min(0)]
    public float schoolingRadius = 5f;
}

public class FishSpawnManager : MonoBehaviour
{
    [Header("Level Zones")]
    [SerializeField] private LevelZone shallowZone;
    [SerializeField] private LevelZone middleZone;
    [SerializeField] private LevelZone deepZone;

    [Header("Fish Types")]
    [SerializeField] private List<FishSpawnData> fishTypes = new List<FishSpawnData>();
    
    private Camera mainCamera;
    private const float MIN_SPAWN_DISTANCE = 1f; // Minimum distance between spawned fish
    private List<Vector2> spawnedPositions = new List<Vector2>();

    private FishRepository fishRepository;

    private void Start()
    {
        mainCamera = Camera.main;
        if (mainCamera == null)
        {
            Debug.LogError("No main camera found in the scene!");
            return;
        }

        // Initialize fish repository
        fishRepository = FishRepository.Instance;

        // Debug log: Print all available fish in database
        //var allFish = fishRepository.GetAllFish();
        //Debug.Log("Available fish in database:");
        // foreach (var fish in allFish)
        // {
        //     //Debug.Log($"- {fish.Name}");
        // }

        // Validate fish spawn data
        bool hasErrors = false;
        //Debug.Log($"Number of fish types to spawn: {fishTypes.Count}");
        for (int i = 0; i < fishTypes.Count; i++)
        {
            var fishData = fishTypes[i];

            // Check if prefab exists
            if (fishData.fishPrefab == null)
            {
                //Debug.LogError($"Fish #{i + 1} is missing its prefab! Please assign a prefab in the Unity Inspector.");
                hasErrors = true;
                continue;
            }

            // Get the BasicFish component and validate name
            BasicFish basicFish = fishData.fishPrefab.GetComponent<BasicFish>();
            if (basicFish == null)
            {
                //Debug.LogError($"Fish #{i + 1}'s prefab is missing the BasicFish component!");
                hasErrors = true;
                continue;
            }

            if (string.IsNullOrEmpty(basicFish.Name))
            {
                //Debug.LogError($"Fish #{i + 1}'s prefab has no name set! Please set the 'Display Name' in the prefab's BasicFish component.");
                hasErrors = true;
            }
            else
            {
                //Debug.Log($"Fish #{i + 1} will spawn: '{basicFish.Name}'");
            }
        }

        if (hasErrors)
        {
            Debug.LogError("Please fix the above errors in the fish prefabs.");
            return;
        }

        // Verify zones are assigned
        if (shallowZone == null || middleZone == null || deepZone == null)
        {
            Debug.LogError("Please assign all level zones in the inspector!");
            return;
        }

        // Spawn all fish
        SpawnAllFish();
    }

    private LevelZone GetZoneForLevel(WaterLevel level)
    {
        switch (level)
        {
            case WaterLevel.Shallow:
                return shallowZone;
            case WaterLevel.Middle:
                return middleZone;
            case WaterLevel.Deep:
                return deepZone;
            default:
                return null;
        }
    }

    private Vector2 GetRandomPositionInZone(BoxCollider2D collider)
    {
        // Get the collider's bounds in world space
        Bounds bounds = collider.bounds;
        
        // Calculate a random position within these bounds
        float randomX = Random.Range(bounds.min.x, bounds.max.x);
        float randomY = Random.Range(bounds.min.y, bounds.max.y);
        
        return new Vector2(randomX, randomY);
    }

    private Vector2 GetRandomPositionNearPoint(Vector2 centerPoint, float maxDistance)
    {
        float randomAngle = Random.Range(0f, 360f);
        float randomDistance = Random.Range(0f, maxDistance);
        Vector2 offset = Quaternion.Euler(0, 0, randomAngle) * Vector2.right * randomDistance;
        return centerPoint + offset;
    }

    private Vector2 GetValidSpawnPosition(BoxCollider2D zoneCollider, Vector2? nearPoint = null, float? maxDistance = null)
    {
        const int MAX_ATTEMPTS = 10;
        Vector2 spawnPos = nearPoint.HasValue && maxDistance.HasValue 
            ? GetRandomPositionNearPoint(nearPoint.Value, maxDistance.Value) 
            : GetRandomPositionInZone(zoneCollider);
        
        for (int attempt = 0; attempt < MAX_ATTEMPTS; attempt++)
        {
            // Get a potential spawn position
            if (nearPoint.HasValue && maxDistance.HasValue)
            {
                spawnPos = GetRandomPositionNearPoint(nearPoint.Value, maxDistance.Value);
                // Clamp to zone bounds
                spawnPos.x = Mathf.Clamp(spawnPos.x, zoneCollider.bounds.min.x, zoneCollider.bounds.max.x);
                spawnPos.y = Mathf.Clamp(spawnPos.y, zoneCollider.bounds.min.y, zoneCollider.bounds.max.y);
            }
            else
            {
                spawnPos = GetRandomPositionInZone(zoneCollider);
            }

            // Check if this position is far enough from all other spawned fish
            bool isTooClose = false;
            foreach (Vector2 existingPos in spawnedPositions)
            {
                if (Vector2.Distance(spawnPos, existingPos) < MIN_SPAWN_DISTANCE)
                {
                    isTooClose = true;
                    break;
                }
            }

            // If position is valid, use it
            if (!isTooClose)
            {
                spawnedPositions.Add(spawnPos);
                return spawnPos;
            }
        }

        // If we couldn't find a valid position after MAX_ATTEMPTS, just use the last attempted position
        spawnedPositions.Add(spawnPos);
        return spawnPos;
    }

    private void SpawnAllFish()
    {
        spawnedPositions.Clear(); // Clear the list at the start of spawning

        foreach (var fishData in fishTypes)
        {
            // Get fish name from prefab
            BasicFish prefabFish = fishData.fishPrefab.GetComponent<BasicFish>();
            string fishName = prefabFish.Name;

            // Get fish data from database
            Fish dbFish = fishRepository.GetFishByName(fishName);
            if (dbFish == null)
            {
                Debug.LogError($"Fish '{fishName}' not found in database! Check that the name in the prefab matches exactly with one in the database.");
                continue;
            }

            // Get the corresponding level zone
            LevelZone targetZone = GetZoneForLevel(fishData.level);
            if (targetZone == null)
            {
                Debug.LogError($"No zone found for level {fishData.level}!");
                continue;
            }

            BoxCollider2D zoneCollider = targetZone.GetComponent<BoxCollider2D>();
            if (zoneCollider == null)
            {
                Debug.LogError($"No BoxCollider2D found on zone {targetZone.name}!");
                continue;
            }

            if (fishData.isSchooling)
            {
                // For schooling fish, first get a center point for the school
                Vector2 schoolCenter = GetValidSpawnPosition(zoneCollider);
                float schoolSpawnRadius = fishData.schoolingRadius * 0.5f; // Spawn within half the schooling radius

                // Spawn fish around the center point
                for (int i = 0; i < fishData.amountToSpawn; i++)
                {
                    // Get position near the school center with minimum distance check
                    Vector2 spawnPos = GetValidSpawnPosition(zoneCollider, schoolCenter, schoolSpawnRadius);
                    
                    // Spawn the fish and apply database properties
                    GameObject fishObject = Instantiate(fishData.fishPrefab, spawnPos, Quaternion.identity);
                    BasicFish fishComponent = fishObject.GetComponent<BasicFish>();
                    if (fishComponent != null)
                    {
                        // Apply database properties
                        fishComponent.SetDatabaseProperties(dbFish);
                        fishComponent.SetSchooling(true, fishData.schoolingRadius);
                    }
                }
            }
            else
            {
                // For non-schooling fish, spawn them randomly across the zone
                for (int i = 0; i < fishData.amountToSpawn; i++)
                {
                    Vector2 spawnPos = GetValidSpawnPosition(zoneCollider);
                    GameObject fishObject = Instantiate(fishData.fishPrefab, spawnPos, Quaternion.identity);
                    
                    // Apply database properties
                    BasicFish fishComponent = fishObject.GetComponent<BasicFish>();
                    if (fishComponent != null)
                    {
                        fishComponent.SetDatabaseProperties(dbFish);
                    }
                }
            }
        }
    }
}

// Helper component to notify when a fish is destroyed
public class DestroyNotifier : MonoBehaviour
{
    public System.Action OnDestroyed;

    private void OnDestroy()
    {
        OnDestroyed?.Invoke();
    }
} 