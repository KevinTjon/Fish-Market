using UnityEngine;

[CreateAssetMenu(fileName = "SpawnLevel", menuName = "Fish/SpawnLevel")]
public class SpawnLevel : ScriptableObject
{
    [System.Serializable]
    public class FishSpawnData
    {
        public GameObject fishPrefab;
        public int initialCount = 5;  // How many fish to spawn when the level starts
        public int maxPopulation = 10;  // Maximum number of this fish type allowed
        [Range(0f, 1f)]
        public float spawnChance = 0.3f;  // Chance to spawn when conditions are met
    }

    [Header("Level Settings")]
    public string levelName = "Shallow Waters";  // e.g., "Shallow Waters", "Mid Waters", etc.
    
    [Header("Spawn Settings")]
    public FishSpawnData[] availableFish;
    
    [Header("Depth Settings")]
    public float minDepth = 0f;  // Minimum depth where fish from this level can spawn
    public float maxDepth = -20f;  // Maximum depth (negative values = deeper)
} 