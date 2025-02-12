using UnityEngine;
using UnityEngine.UI;
using Mono.Data.Sqlite;
using System.Collections.Generic;
using System.IO;

public class FishLog : MonoBehaviour
{
    public Transform content; // The parent object that holds the fish slots
    private List<GameObject> fishSlots; // List to hold the fish slots

    private List<FishData> fishDataList; // List to hold fish data

    void Start()
    {
        fishSlots = new List<GameObject>(); // Initialize the fishSlots list
        fishDataList = new List<FishData>(); // Initialize the fishDataList
        LoadFishData(); // Load fish data from the database
        LoadFishSlots(); // Load the fish slots
    }

    void LoadFishSlots()
    {
        fishSlots.Clear(); // Clear the existing list

        // Iterate through each child in the content transform
        foreach (Transform child in content.transform)
        {
            // Add each child (slot) to the fishSlots list
            fishSlots.Add(child.gameObject);
        }

        // Pass fish data to each fish slot
        for (int i = 0; i < fishSlots.Count && i < fishDataList.Count; i++)
        {
            FishSlot fishSlot = fishSlots[i].GetComponent<FishSlot>();
            if (fishSlot != null) // Check if the component exists
            {
                // Set the fish data from the database
                fishSlot.SetFishData(fishDataList[i]);
                //Debug.Log($"Setting data for slot {i}: {fishDataList[i].Name}");

                // Get the Image component from the child GameObject named "Image"
                Image fishImage = fishSlots[i].transform.Find("Image").GetComponent<Image>();

                // Now check if the fish is discovered
                if (fishSlot.isDiscovered == "Yes")
                {
                    // Load the sprite from the asset path
                    Sprite sprite = Resources.Load<Sprite>(fishSlot.assetPath);
                    if (sprite != null)
                    {
                        fishImage.sprite = sprite; // Assign the sprite to the Image component
                        fishImage.enabled = true; // Ensure the image is visible
                    }
                    else
                    {
                        Debug.LogWarning($"Sprite not found at path: {fishSlot.assetPath}");
                        fishImage.enabled = false; // Hide the image if not found
                    }
                }
                else
                {
                    // Load the default image for undiscovered fish
                    Sprite defaultSprite = Resources.Load<Sprite>("Art/Sprites/Fish/UnknownFish1");
                    if (defaultSprite != null)
                    {
                        fishImage.sprite = defaultSprite; // Assign the default sprite
                        fishImage.enabled = true; // Ensure the image is visible
                    }
                    else
                    {
                        Debug.LogWarning("Default sprite not found at path: Art/Sprites/Fish/UnknownFish1");
                        fishImage.enabled = false; // Hide the image if not found
                    }
                }
            }
            else
            {
                Debug.LogWarning($"FishSlot component not found on {fishSlots[i].name}");
            }
        }
    }

     void LoadFishData()
    {
        string dbPath = @"Data Source=" + Application.dataPath + "/StreamingAssets/FishDB.db";

        using (var connection = new SqliteConnection(dbPath))
        {
            connection.Open();
            using (var command = connection.CreateCommand())
            {
                command.CommandText = "SELECT FishID, Name, Description, Rarity, AssetPath, MinWeight, MaxWeight, TopSpeed, HookedFuncNum, IsDiscovered FROM Fish;";
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        int fishId = reader.GetInt32(0);
                        string name = reader.GetString(1);
                        string description = reader.GetString(2);
                        string rarity = reader.GetString(3);
                        string assetPath = reader.IsDBNull(4) ? null : reader.GetString(4);
                        float minWeight = reader.GetFloat(5);
                        float maxWeight = reader.GetFloat(6);
                        float topSpeed = reader.GetFloat(7);
                        int hookedFuncNum = reader.GetInt32(8);
                        int isDiscovered = reader.GetInt32(9);

                        // Create a new FishData object and add it to the list
                        FishData fishData = new FishData(fishId, name, description, rarity, assetPath, minWeight, maxWeight, topSpeed, hookedFuncNum, isDiscovered);
                        fishDataList.Add(fishData);
                    }
                }
            }
        }
    }

    private void LoadSprite(GameObject slot, string path){
        // Load the sprite from the asset path
        Sprite sprite = Resources.Load<Sprite>(path);
        if (sprite != null)
        {
            Image fishImage = slot.transform.Find("Image").GetComponent<Image>();
            if (fishImage != null)
            {
                fishImage.sprite = sprite; // Assign the sprite to the Image component
                fishImage.enabled = true; // Ensure the image is visible
            }
            else
            {
                // Load the default image for undiscovered fish
                Sprite defaultSprite = Resources.Load<Sprite>("Art/Sprites/Fish/Unknown1");
                if (defaultSprite != null)
                {
                    fishImage.sprite = defaultSprite; // Assign the default sprite
                    fishImage.enabled = true; // Ensure the image is visible
                }
            }
        }
        else
        {
            Debug.LogWarning($"Sprite not found at path: {path}");
            Image fishImage = slot.transform.Find("Image").GetComponent<Image>();
        }
    }
}
