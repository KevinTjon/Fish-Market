using UnityEngine;

public class ExtensiveTab : MonoBehaviour
{
    // Variables to store fish details as strings
    public string nameText; // Store the fish name
    public string descriptionText; // Store the fish description
    public string rarityText; // Store the fish rarity
    public string minWeightText; // Store the minimum weight
    public string maxWeightText; // Store the maximum weight
    public string topSpeedText; // Store the top speed
    public string assetPath; // Store the asset path location


    public void ShowFishDetails(ExtensiveTabData fishData)
    {
        if (fishData == null)
        {
            Debug.LogError("FishData is null!");
            return; // Exit if fishData is null
        }

        // Populate the string variables with fish data
        nameText = fishData.Name; // Set the name
        descriptionText = fishData.Description; // Set the description
        rarityText = fishData.Rarity; // Set the rarity
        minWeightText = fishData.MinWeight.ToString(); // Set the minimum weight
        maxWeightText = fishData.MaxWeight.ToString(); // Set the maximum weight
        topSpeedText = fishData.TopSpeed.ToString(); // Set the top speed
        assetPath = fishData.AssetPath; // Set the asset path

        // Activate the tab to show the details
        gameObject.SetActive(true);
    }
}