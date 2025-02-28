using UnityEngine;
using UnityEngine.UI; // If you're still using standard UI components
using TMPro; // Import the TextMeshPro namespace
using System;  // Add this for Exception
using System.Collections.Generic;
using Mono.Data.Sqlite;

public class FishInventory : MonoBehaviour
{
    public GameObject content; 
    private List<GameObject> fishSlots = new List<GameObject>();     
    private FishDB fishDB;
    [SerializeField] private Sprite defaultFishSprite;  // Add this field

    void Start()
    {
        fishDB = FindObjectOfType<FishDB>(); // Get the FishDB instance
        if (fishDB == null)
        {
            Debug.LogError("FishDB instance not found!"); // Log error if not found
            return; // Exit if FishDB is not found
        }
        LoadFishSlots(); 
        LoadFishInventory();
    }

    void LoadFishSlots()
    {
        fishSlots.Clear();
        foreach (Transform child in content.transform)
        {
            fishSlots.Add(child.gameObject);
        }
    }

    public void LoadFishInventory()
    {
        List<FishDB.Fish> fishList = fishDB.GetFish();
        Dictionary<string, int> fishQuantity = new Dictionary<string, int>();
        Dictionary<string, float> latestPrices = GetLatestMarketPrices();

        // First, clear all slots
        foreach (GameObject slot in fishSlots)
        {
            // Clear the quantity text
            TextMeshProUGUI fishText = slot.GetComponentInChildren<TextMeshProUGUI>();
            if (fishText != null)
            {
                fishText.text = "";
            }

            // Clear the slot data
            FishSlotData slotData = slot.GetComponent<FishSlotData>();
            if (slotData != null)
            {
                slotData.fishName = "";
                slotData.fishRarity = "";
                slotData.quantity = 0;
                slotData.marketPrice = 0f;
                slotData.fishImage = defaultFishSprite;  // Set to default sprite

                // Find the specific Image child component named "FishImage" or similar
                Transform fishImageTransform = slot.transform.Find("FishImage");
                if (fishImageTransform != null)
                {
                    Image fishImage = fishImageTransform.GetComponent<Image>();
                    if (fishImage != null)
                    {
                        fishImage.sprite = defaultFishSprite;  // Set to default sprite
                    }
                }
            }
        }

        // Count the quantities of each fish
        foreach (var fish in fishList)
        {
            if (fishQuantity.ContainsKey(fish.Name))
            {
                fishQuantity[fish.Name]++;
            }
            else
            {
                fishQuantity[fish.Name] = 1;
            }
        }

        // Now load the fish slots
        for (int i = 0; i < fishSlots.Count; i++)
        {
            if (i < fishQuantity.Count)
            {
                string fishName = fishList[i].Name;
                GameObject fishSlot = fishSlots[i];

                // Update quantity text
                TextMeshProUGUI fishText = fishSlot.GetComponentInChildren<TextMeshProUGUI>();
                if (fishText != null)
                {
                    fishText.text = "X" + fishQuantity[fishName];
                }

                // Store the data in the slot
                FishSlotData slotData = fishSlot.GetComponent<FishSlotData>();
                if (slotData == null)
                {
                    slotData = fishSlot.AddComponent<FishSlotData>();
                }
                slotData.fishName = fishName;
                slotData.fishRarity = fishList[i].Rarity;
                slotData.quantity = fishQuantity[fishName];
                slotData.marketPrice = latestPrices.ContainsKey(fishName) ? latestPrices[fishName] : 0f;
                slotData.fishImage = Resources.Load<Sprite>(fishList[i].AssetPath);

                // Find and update the specific Image child component
                Transform fishImageTransform = fishSlot.transform.Find("FishImage");
                if (fishImageTransform != null)
                {
                    Image fishImage = fishImageTransform.GetComponent<Image>();
                    if (fishImage != null)
                    {
                        fishImage.sprite = slotData.fishImage;
                    }
                }
                else
                {
                    Debug.LogError($"FishImage child not found in slot {i}");
                }
            }
        }
    }

    private Dictionary<string, float> GetLatestMarketPrices()
    {
        Dictionary<string, float> prices = new Dictionary<string, float>();
        string dbPath = "URI=file:" + Application.dataPath + "/StreamingAssets/FishDB.db";

        try
        {
            using (var connection = new SqliteConnection(dbPath))
            {
                connection.Open();
                using (var command = connection.CreateCommand())
                {
                    command.CommandText = @"
                        SELECT FishName, Price 
                        FROM MarketPrices 
                        WHERE (FishName, Day) IN (
                            SELECT FishName, MAX(Day) 
                            FROM MarketPrices 
                            GROUP BY FishName
                        )";

                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            string fishName = reader.GetString(0);
                            float price = (float)reader.GetDouble(1);
                            prices[fishName] = price;
                            //Debug.Log($"Loaded market price for {fishName}: {price}");
                        }
                    }
                }
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"Error loading market prices: {e.Message}");
        }

        return prices;
    }
}