using UnityEngine;
using System.Collections.Generic;

[System.Serializable]
public class FishSpawnData
{
    public GameObject fishPrefab;
    public WaterLevel level;
    [Min(1)]
    public int amountToSpawn = 3;
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

    private void Start()
    {
        mainCamera = Camera.main;
        if (mainCamera == null)
        {
            Debug.LogError("No main camera found in the scene!");
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

    private void SpawnAllFish()
    {
        foreach (var fishData in fishTypes)
        {
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

            // Spawn the specified amount of fish
            for (int i = 0; i < fishData.amountToSpawn; i++)
            {
                // Get random position within the zone's bounds
                Vector2 spawnPos = GetRandomPositionInZone(zoneCollider);
                
                // Spawn the fish
                Instantiate(fishData.fishPrefab, spawnPos, Quaternion.identity);
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