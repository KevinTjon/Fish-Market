using UnityEngine;

public class FishSlot : MonoBehaviour
{
    public string Fishname; // Store the fish name
    public string description; // Store the fish description
    public string rarity; // Store the fish rarity
    public string minWeight; // Store the minimum weight
    public string maxWeight; // Store the maximum weight
    public string topSpeed; // Store the top speed
    public string hookedFuncNum; // Store the hooked function number
    public string isDiscovered; // Store the discovery status
    public string assetPath; // Store the asset path location

    public void SetFishData(FishData fishData)
    {
        Fishname = fishData.Name; // Set the name
        description = fishData.Description; // Set the description
        rarity = fishData.Rarity; // Set the rarity
        minWeight = fishData.MinWeight.ToString(); // Set the minimum weight
        maxWeight = fishData.MaxWeight.ToString(); // Set the maximum weight
        topSpeed = fishData.TopSpeed.ToString(); // Set the top speed
        hookedFuncNum = fishData.HookedFuncNum.ToString(); // Set the hooked function number
        isDiscovered = fishData.IsDiscovered == 1 ? "Yes" : "No"; // Set the discovery status
        assetPath = fishData.AssetPath; // Set the asset path
    }
}