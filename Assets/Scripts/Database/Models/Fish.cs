using UnityEngine;

public class Fish
{
    // Core Properties
    public int Id { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
    public string Rarity { get; set; }
    public string AssetPath { get; set; }

    // Physical Properties
    public float Weight { get; set; }
    public float MinWeight { get; set; }
    public float MaxWeight { get; set; }
    public float TopSpeed { get; set; }

    // Game State
    public int HookedFuncNum { get; set; }
    public bool IsDiscovered { get; set; }

    // Constructor with all parameters
    public Fish(
        int id,
        string name,
        string description,
        string rarity,
        string assetPath,
        float weight,
        float minWeight,
        float maxWeight,
        float topSpeed,
        int hookedFuncNum,
        bool isDiscovered)
    {
        Id = id;
        Name = name;
        Description = description;
        Rarity = rarity;
        AssetPath = assetPath;
        Weight = weight;
        MinWeight = minWeight;
        MaxWeight = maxWeight;
        TopSpeed = topSpeed;
        HookedFuncNum = hookedFuncNum;
        IsDiscovered = isDiscovered;
    }

    // Default constructor
    public Fish() { }

    // Constructor from FishData
    public Fish(FishData fishData)
    {
        Name = fishData.Name;
        Description = fishData.Description;
        Rarity = fishData.Rarity;
        AssetPath = fishData.AssetPath;
        MinWeight = fishData.MinWeight;
        MaxWeight = fishData.MaxWeight;
        TopSpeed = fishData.TopSpeed;
        HookedFuncNum = fishData.HookedFuncNum;
        IsDiscovered = fishData.IsDiscovered == 1;
        Weight = Random.Range(MinWeight, MaxWeight); // Generate random weight within range
    }

    // Helper method to generate a random weight within the fish's range
    public void GenerateRandomWeight()
    {
        Weight = Random.Range(MinWeight, MaxWeight);
    }
} 