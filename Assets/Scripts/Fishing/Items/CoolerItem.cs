using UnityEngine;

public class CoolerItem
{
    // Private set variables
    // Integer of the fish's entry in the local database
    // This should be instantiated during load time
    // Allows us to easily convert the fish between its various contexts
    private string fishName;
    private FishSize fishSize;
    //private string fishRarity;
    //private string fishAssetPath;
    private float fishWeight;
    // Public get variables
    public string Name { get {return fishName;} }
    public FishSize Size { get {return fishSize;} }
    //public string Rarity { get {return fishRarity;} }
    //public string AssetPath { get {return fishAssetPath;} }
    public float Weight { get {return fishWeight;} }

    public void Initialize(FishType fishType)
    {
        fishName = fishType.fishName;
        fishSize = fishType.size;
        //fishAssetPath = fishType.prefab.name;
        fishWeight = 1f;
    }

    public void Initialize(FishType fishType, float weight)
    {
        fishName = fishType.fishName;
        fishSize = fishType.size;
        //fishAssetPath = fishType.prefab.name;
        fishWeight = weight;
    }

    public CaughtFishData GetCaughtFishData()
    {
        return new CaughtFishData
        {
            Name = fishName,
            Weight = fishWeight,
            IsDiscovered = "yes"
        };
    } 

    // Change this to display with UI
    public void DisplayFish()
    {
        Debug.Log("Fish Name: " + fishName);
        Debug.Log("Fish Size: " + fishSize);
        //Debug.Log("Fish Asset Path: " + fishAssetPath);
        Debug.Log("Fish Weight: " + fishWeight);
    }
    
}
