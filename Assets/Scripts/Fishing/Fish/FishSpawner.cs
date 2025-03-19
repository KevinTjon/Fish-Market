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
    
    [Header("Spawn Zones")]
    public SpawnZone[] spawnZones;  // Assign your spawn zone objects here
    
    [Header("Debug Visualization")]
    public bool showDebugVisuals = true;
    
    [Header("Schooling Update")]
    public float schoolingUpdateInterval = 0.2f;  // How often to update school memberships
    private float nextSchoolingUpdate;
    
    private Dictionary<GameObject, List<GameObject>> activeFish;
    private List<FishSchooling> allFishSchooling = new List<FishSchooling>();
    private float nextSpawnTime;

    private void Awake()
    {
        activeFish = new Dictionary<GameObject, List<GameObject>>();
        
        // Find spawn zones if not assigned
        if (spawnZones == null || spawnZones.Length == 0)
        {
            spawnZones = FindObjectsOfType<SpawnZone>();
            if (spawnZones.Length == 0)
            {
                Debug.LogError("No spawn zones found in scene!");
            }
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
        if (fishTypes == null || spawnZones == null || spawnZones.Length == 0) return;
        
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
        if (fishTypes == null || spawnZones == null || spawnZones.Length == 0) return;

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

    private Vector3 GetRandomSpawnPosition()
    {
        if (spawnZones == null || spawnZones.Length == 0) return Vector3.zero;
        
        // Pick a random spawn zone
        SpawnZone zone = spawnZones[Random.Range(0, spawnZones.Length)];
        if (zone == null) return Vector3.zero;
        
        BoxCollider2D collider = zone.GetComponent<BoxCollider2D>();
        if (collider == null) return Vector3.zero;
        
        // Get random position within the zone's bounds
        Bounds bounds = collider.bounds;
        float x = Random.Range(bounds.min.x, bounds.max.x);
        float y = Random.Range(bounds.min.y, bounds.max.y);
        return new Vector3(x, y, 0f);
    }

    private void SpawnFish(GameObject fishPrefab, Vector3? position = null)
    {
        if (fishPrefab == null) return;

        // Use provided position or get random position
        Vector3 spawnPosition = position ?? GetRandomSpawnPosition();

        // Spawn the fish with zero rotation
        GameObject fish = Instantiate(fishPrefab, spawnPosition, Quaternion.identity);
        
        // Reset the fish's transform rotation to ensure it's perfectly horizontal
        fish.transform.rotation = Quaternion.identity;
        
        // Get components
        var fishHookable = fish.GetComponent<FishHookable>();
        var fishMovement = fish.GetComponent<FishMovement>();
        
        // Find and assign the boundary area BEFORE any other initialization
        if (fishMovement != null)
        {
            foreach (var zone in spawnZones)
            {
                if (zone != null)
                {
                    var zoneCollider = zone.GetComponent<BoxCollider2D>();
                    if (zoneCollider != null && zoneCollider.bounds.Contains(spawnPosition))
                    {
                        fishMovement.boundaryArea = zoneCollider;
                        break;
                    }
                }
            }
            
            // If no specific zone was found, use the first available zone as fallback
            if (fishMovement.boundaryArea == null && spawnZones.Length > 0 && spawnZones[0] != null)
            {
                fishMovement.boundaryArea = spawnZones[0].GetComponent<BoxCollider2D>();
                Debug.LogWarning($"No specific zone found for fish at {spawnPosition}, using first available zone as fallback.");
            }
            
            if (fishMovement.boundaryArea == null)
            {
                Debug.LogError($"Could not assign boundary area for fish at {spawnPosition}. Make sure spawn zones are set up correctly.");
                Destroy(fish);
                return;
            }
        }
        
        if (fishHookable != null)
        {
            // Load FishType if not already assigned
            if (fishHookable.fishType == null)
            {
                string typeName = fish.name.Replace("(Clone)", "").Replace("Fish", "FishType");
                FishType fishType = Resources.Load<FishType>($"FishTypes/{typeName}");
                if (fishType != null)
                {
                    fishHookable.fishType = fishType;
                }
                else
                {
                    Debug.LogError($"Could not find FishType for fish: {fish.name}. Looking for: FishTypes/{typeName}");
                    Destroy(fish);
                    return;
                }
            }
            
            // Load behavior if not already assigned
            if (fishHookable.behavior == null)
            {
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
        }
        
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
            if (fishMovement.boundaryArea != null)
            {
                scale.x *= (spawnPosition.x < fishMovement.boundaryArea.bounds.center.x ? 1 : -1);
            }
            spriteTransform.localScale = scale;
        }
        else
        {
            Debug.LogWarning($"No 'Sprite' child object found on fish: {fish.name}");
        }
        
        // Initialize tracking if needed
        if (!activeFish.ContainsKey(fishPrefab))
        {
            activeFish[fishPrefab] = new List<GameObject>();
        }
        activeFish[fishPrefab].Add(fish);
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