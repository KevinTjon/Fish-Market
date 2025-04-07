using UnityEngine;
using UnityEditor;

public class CoolerItem
{
    // Private set variables
    // Integer of the fish's entry in the local database
    // This should be instantiated during load time
    // Allows us to easily convert the fish between its various contexts
    private string name;
    private FishSize size;
    //private string fishRarity;
    private Sprite sprite;
    private float weight;
    
    // Public get variables
    public string Name => name;
    public FishSize Size => size;
    public Sprite Sprite => sprite;
    public float Weight => weight;

    public void Initialize(BasicFish fish)
    {
        name = fish.Name;
        size = fish.Size;
        sprite = fish.Sprite;
        weight = fish.Weight > 0 ? fish.Weight : 1f;
    }

    public CaughtFishData GetCaughtFishData()
    {
        return new CaughtFishData
        {
            Name = name,
            Weight = weight,
            IsDiscovered = "yes"
        };
    } 

    // Change this to display with UI
    public void DisplayFish()
    {
        Debug.Log("Fish Name: " + name);
        Debug.Log("Fish Size: " + size);
        Debug.Log(AssetDatabase.GetAssetPath(sprite));
        Debug.Log("Fish Weight: " + weight);
    }
    
}
