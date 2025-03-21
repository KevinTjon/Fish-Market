using UnityEngine;
using System.Collections.Generic;

public class SpawnLevelManager : MonoBehaviour
{
    [Header("Level Settings")]
    public SpawnLevel[] spawnLevels;

    private void Start()
    {
        // Debug check of spawn levels
        if (spawnLevels == null || spawnLevels.Length == 0)
        {
            Debug.LogError("No spawn levels assigned to SpawnLevelManager!");
            return;
        }

        Debug.Log($"SpawnLevelManager initialized with {spawnLevels.Length} levels:");
        foreach (var level in spawnLevels)
        {
            if (level == null)
            {
                Debug.LogError("Null spawn level found in array!");
                continue;
            }

            Debug.Log($"Level: {level.levelName}, Depth: {level.minDepth} to {level.maxDepth}, Fish Types: {(level.availableFish != null ? level.availableFish.Length : 0)}");
            if (level.availableFish != null)
            {
                foreach (var fish in level.availableFish)
                {
                    if (fish.fishPrefab != null)
                        Debug.Log($"- Fish: {fish.fishPrefab.name}, Initial: {fish.initialCount}, Max: {fish.maxPopulation}, Chance: {fish.spawnChance}");
                    else
                        Debug.LogError($"Null fish prefab found in level {level.levelName}!");
                }
            }
        }
    }
    
    // Get all spawn levels that can spawn at a given depth
    public List<SpawnLevel> GetSpawnLevelsAtDepth(float depth)
    {
        List<SpawnLevel> validLevels = new List<SpawnLevel>();
        
        if (spawnLevels == null) return validLevels;
        
        foreach (var level in spawnLevels)
        {
            if (level != null && depth <= level.minDepth && depth >= level.maxDepth)
            {
                validLevels.Add(level);
                Debug.Log($"Found valid level {level.levelName} for depth {depth}");
            }
        }
        
        return validLevels;
    }
    
    // Get the effective max population for a fish prefab
    public int GetMaxPopulation(GameObject fishPrefab, float depth)
    {
        int maxPop = 0;
        
        foreach (var level in GetSpawnLevelsAtDepth(depth))
        {
            if (level.availableFish != null)
            {
                foreach (var fishData in level.availableFish)
                {
                    if (fishData.fishPrefab == fishPrefab)
                    {
                        maxPop = Mathf.Max(maxPop, fishData.maxPopulation);
                    }
                }
            }
        }
        
        return maxPop;
    }
    
    // Get spawn chance for a specific fish at a given depth
    public float GetSpawnChance(GameObject fishPrefab, float depth)
    {
        float maxChance = 0f;
        
        foreach (var level in GetSpawnLevelsAtDepth(depth))
        {
            if (level.availableFish != null)
            {
                foreach (var fishData in level.availableFish)
                {
                    if (fishData.fishPrefab == fishPrefab)
                    {
                        maxChance = Mathf.Max(maxChance, fishData.spawnChance);
                    }
                }
            }
        }
        
        return maxChance;
    }
} 