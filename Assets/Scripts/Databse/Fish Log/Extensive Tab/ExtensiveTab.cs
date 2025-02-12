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

    public string isDiscovered;

    public FishImagePanel fishImagePanel;

    private FishName fishNameComponent; // Reference to the FishName component
    private Description descriptionComponent; // Reference to the Description component
    private Rarity rarityComponent; // Reference to the Rarity component



    public void ShowFishDetails(ExtensiveTabData fishData)
    {
        if (fishData == null)
        {
            Debug.LogError("FishData is null!");
            return; // Exit if fishData is null
        }

         // Check if the fish is discovered
        if (fishData.IsDiscovered == "No") // Check if isDiscovered is "No"
        {
            Debug.Log("Fish is not discovered. No action taken.");
            return; // Exit if the fish is not discovered
        }

        // Populate the string variables with fish data
        nameText = fishData.Name; // Set the name
        descriptionText = fishData.Description; // Set the description
        rarityText = fishData.Rarity; // Set the rarity
        minWeightText = fishData.MinWeight.ToString(); // Set the minimum weight
        maxWeightText = fishData.MaxWeight.ToString(); // Set the maximum weight
        topSpeedText = fishData.TopSpeed.ToString(); // Set the top speed
        assetPath = fishData.AssetPath; // Set the asset path
        isDiscovered = fishData.IsDiscovered;
        
        fishImagePanel.SetFishImage(assetPath);
        

        Transform detailPagesTransform = transform.Find("Menu/Pages/DetailsPage");
        if (detailPagesTransform != null)
        {
            fishNameComponent = detailPagesTransform.GetComponentInChildren<FishName>();
            descriptionComponent = detailPagesTransform.GetComponentInChildren<Description>();
            rarityComponent = detailPagesTransform.GetComponentInChildren<Rarity>();
        }
        else
        {
            Debug.LogError("DetailPage GameObject not found in the hierarchy!");
        }

        if (fishNameComponent != null)
        {
            fishNameComponent.SetFishName(nameText);
        }

        if (descriptionComponent != null)
        {
            descriptionComponent.SetDescription(descriptionText);
        }

        if (rarityComponent != null)
        {
            rarityComponent.SetRarity(rarityText);
        }

        // Activate the tab to show the details
        gameObject.SetActive(true);
    }
}
