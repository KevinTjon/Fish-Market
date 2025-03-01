using UnityEngine;
using System.Collections.Generic;

public class FishSpawner : MonoBehaviour
{
    [System.Serializable]
    public class FishTypeSpawnInfo
    {
        public GameObject fishPrefab;
        public int initialCount = 10;
        public int maxCount = 20;
        [Range(0f, 1f)]
        public float spawnChance = 0.3f;
    }

    [Header("Spawn Settings")]
    public FishTypeSpawnInfo[] fishTypes;
    public float spawnInterval = 5f;
    
    [Header("Boundary Settings")]
    public BoxCollider2D spawnBoundary;
    [Header("Debug Visualization")]
    public bool showDebugVisuals = true;
    public bool showSpawnArea = true;
    public bool showFishForces = true;

    [Header("Schooling Update")]
    public float schoolingUpdateInterval = 0.2f;  // How often to update school memberships
    private float nextSchoolingUpdate;

    private Dictionary<GameObject, List<GameObject>> activeFish;
    private List<FishSchooling> allFishSchooling = new List<FishSchooling>();
    private float nextSpawnTime;

    private void Awake()
    {
        activeFish = new Dictionary<GameObject, List<GameObject>>();
        
        if (spawnBoundary == null)
        {
            // Create a default boundary if none is assigned
            spawnBoundary = gameObject.AddComponent<BoxCollider2D>();
            spawnBoundary.size = new Vector2(20f, 10f);
            spawnBoundary.isTrigger = true;
        }
    }

    private void OnEnable()
    {
        InitializeFishTracking();
    }

    private void OnDisable()
    {
        // Clean up spawned fish when disabled
        if (activeFish != null)
        {
            foreach (var fishList in activeFish.Values)
            {
                foreach (var fish in fishList)
                {
                    if (fish != null)
                    {
                        Destroy(fish);
                    }
                }
            }
            activeFish.Clear();
        }
    }

    private void InitializeFishTracking()
    {
        if (fishTypes == null) return;
        
        // Clear existing tracking
        activeFish.Clear();
        
        // Initialize tracking for each fish type
        foreach (var fishType in fishTypes)
        {
            if (fishType != null && fishType.fishPrefab != null)
            {
                activeFish[fishType.fishPrefab] = new List<GameObject>();
                
                // Spawn initial fish
                for (int i = 0; i < fishType.initialCount; i++)
                {
                    SpawnFish(fishType.fishPrefab);
                }
            }
        }
    }

    private void Update()
    {
        if (fishTypes == null) return;

        // Update schooling
        if (Time.time >= nextSchoolingUpdate)
        {
            UpdateSchooling();
            nextSchoolingUpdate = Time.time + schoolingUpdateInterval;
        }

        // Handle spawning
        if (Time.time >= nextSpawnTime)
        {
            foreach (var fishType in fishTypes)
            {
                if (fishType != null && fishType.fishPrefab != null && activeFish.ContainsKey(fishType.fishPrefab))
                {
                    // Clean up null references before checking count
                    activeFish[fishType.fishPrefab].RemoveAll(fish => fish == null);
                    
                    // Check if we should spawn more of this type
                    if (activeFish[fishType.fishPrefab].Count < fishType.maxCount &&
                        Random.value < fishType.spawnChance)
                    {
                        SpawnFish(fishType.fishPrefab);
                    }
                }
            }
            nextSpawnTime = Time.time + spawnInterval;
        }
    }

    private void UpdateSchooling()
    {
        // Update list of all fish with schooling component
        allFishSchooling.Clear();
        foreach (var fishList in activeFish.Values)
        {
            foreach (var fish in fishList)
            {
                if (fish != null)
                {
                    var schooling = fish.GetComponent<FishSchooling>();
                    if (schooling != null)
                    {
                        allFishSchooling.Add(schooling);
                    }
                }
            }
        }

        // Update nearby fish for each schooling component
        foreach (var schooling in allFishSchooling)
        {
            if (schooling != null)
            {
                schooling.UpdateNearbyFish(allFishSchooling);
            }
        }
    }

    private void SpawnFish(GameObject fishPrefab)
    {
        if (fishPrefab == null || !activeFish.ContainsKey(fishPrefab) || spawnBoundary == null) return;

        // Calculate random position within spawn area
        Bounds bounds = spawnBoundary.bounds;
        float x = Random.Range(bounds.min.x, bounds.max.x);
        float y = Random.Range(bounds.min.y, bounds.max.y);
        Vector3 spawnPosition = new Vector3(x, y, 0f);

        // Spawn the fish with zero rotation
        GameObject fish = Instantiate(fishPrefab, spawnPosition, Quaternion.identity);
        
        // Reset the fish's transform rotation to ensure it's perfectly horizontal
        fish.transform.rotation = Quaternion.identity;
        
        // Get components
        var fishHookable = fish.GetComponent<FishHookable>();
        var fishMovement = fish.GetComponent<FishMovement>();
        
        // Load behavior if not already assigned
        if (fishHookable != null && fishHookable.behavior == null)
        {
            // Try to find behavior based on fish name
            string behaviorName = fish.name.Replace("(Clone)", "").Replace("Fish", "FishBehavior");
            FishBehavior behavior = Resources.Load<FishBehavior>($"FishBehaviors/{behaviorName}");
            if (behavior != null)
            {
                fishHookable.behavior = behavior;
            }
            else
            {
                Debug.LogError($"Could not find behavior for fish: {fish.name}");
                Destroy(fish);
                return;
            }
        }
        
        // Assign the boundary to the fish
        if (fishMovement != null)
        {
            fishMovement.boundaryArea = spawnBoundary;
            
            // Find and setup the sprite transform
            Transform spriteTransform = fish.transform.Find("Sprite");
            if (spriteTransform != null)
            {
                // Reset all rotations to ensure proper alignment
                spriteTransform.localRotation = Quaternion.identity;
                
                // Reset scale to positive values first
                Vector3 scale = spriteTransform.localScale;
                scale.x = Mathf.Abs(scale.x);
                scale.y = Mathf.Abs(scale.y);
                scale.z = Mathf.Abs(scale.z);
                
                // Only flip X scale based on spawn position relative to center
                scale.x *= (x < bounds.center.x ? 1 : -1);
                spriteTransform.localScale = scale;
            }
            else
            {
                Debug.LogWarning($"No 'Sprite' child object found on fish: {fish.name}");
            }
        }
        
        activeFish[fishPrefab].Add(fish);
    }

    private void OnDrawGizmos()
    {
        if (!showDebugVisuals || spawnBoundary == null) return;

        if (showSpawnArea)
        {
            // Draw spawn area
            Gizmos.color = new Color(0f, 1f, 1f, 0.2f);
            Bounds bounds = spawnBoundary.bounds;
            Gizmos.DrawCube(bounds.center, bounds.size);
            
            // Draw wire frame
            Gizmos.color = new Color(0f, 1f, 1f, 0.8f);
            Gizmos.DrawWireCube(bounds.center, bounds.size);
        }
    }

    // Helper method to get all fish of a specific type
    public List<GameObject> GetFishOfType(GameObject prefab)
    {
        if (activeFish != null && activeFish.ContainsKey(prefab))
        {
            return new List<GameObject>(activeFish[prefab]);
        }
        return new List<GameObject>();
    }

    // Helper method to get all active fish
    public List<GameObject> GetAllFish()
    {
        List<GameObject> allFish = new List<GameObject>();
        if (activeFish != null)
        {
            foreach (var fishList in activeFish.Values)
            {
                allFish.AddRange(fishList);
            }
        }
        return allFish;
    }
} 