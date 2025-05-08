using UnityEngine;
using UnityEngine.UI;
using Mono.Data.Sqlite;
using System.Collections.Generic;
using System.IO;
using System.Data;

public class FishLog : MonoBehaviour
{
    public Transform content; // The parent object that holds the fish slots
    private List<GameObject> fishSlots; // List to hold the fish slots
    public GameObject fishSlotPrefab; // Assign this in the Inspector

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
        // Clear existing slots from the UI
        foreach (Transform child in content)
        {
            Destroy(child.gameObject);
        }
        fishSlots.Clear();

        for (int i = 0; i < fishDataList.Count; i++)
        {
            // Instantiate a new slot
            GameObject slotObj = Instantiate(fishSlotPrefab, content);
            fishSlots.Add(slotObj);

            // Enable all images and buttons in the slot
            foreach (var img in slotObj.GetComponentsInChildren<Image>(true))
                img.enabled = true;
            foreach (var btn in slotObj.GetComponentsInChildren<Button>(true))
                btn.enabled = true;

            FishSlot fishSlot = slotObj.GetComponent<FishSlot>();
            if (fishSlot != null)
            {
                fishSlot.SetFishData(fishDataList[i]);
                Image fishImage = slotObj.transform.Find("FishImage").GetComponent<Image>();

                if (fishSlot.isDiscovered == "Yes")
                {
                    Sprite sprite = Resources.Load<Sprite>(fishSlot.assetPath);
                    if (sprite != null)
                    {
                        fishImage.sprite = sprite;
                        fishImage.enabled = true;
                    }
                    else
                    {
                        Debug.LogWarning($"Sprite not found at path: {fishSlot.assetPath}, skipping fish: {fishSlot.Fishname}");
                        Destroy(slotObj); // Remove this slot if sprite is missing
                        fishSlots.RemoveAt(fishSlots.Count - 1);
                        continue;
                    }
                }
                else
                {
                    Sprite defaultSprite = Resources.Load<Sprite>("Art/Sprites/Fish/UnknownFish1");
                    if (defaultSprite != null)
                    {
                        fishImage.sprite = defaultSprite;
                        fishImage.enabled = true;
                    }
                    else
                    {
                        Debug.LogWarning("Default sprite not found at path: Art/Sprites/Fish/UnknownFish1");
                        fishImage.enabled = false;
                    }
                }
            }
            else
            {
                Debug.LogWarning("FishSlot component not found on instantiated slot.");
                Destroy(slotObj);
                fishSlots.RemoveAt(fishSlots.Count - 1);
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
                command.CommandText = "SELECT Name, Description, Rarity, AssetPath, MinWeight, MaxWeight, TopSpeed, HookedFuncNum, IsDiscovered FROM Fish;";
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        string name = reader.GetString(0);
                        string description = reader.GetString(1);
                        string rarity = reader.GetString(2);
                        string assetPath = reader.IsDBNull(3) ? null : reader.GetString(3);
                        float minWeight = reader.GetFloat(4);
                        float maxWeight = reader.GetFloat(5);
                        float topSpeed = reader.GetFloat(6);
                        int hookedFuncNum = reader.GetInt32(7);
                        int isDiscovered = reader.GetInt32(8);

                        // Create a new FishData object and add it to the list
                        FishData fishData = new FishData( name, description, rarity, assetPath, minWeight, maxWeight, topSpeed, hookedFuncNum, isDiscovered);
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

    private float FetchLatestMarketPrice(string fishName)
    {
        float currentMarketPrice = 0f;
        string dbPath = "URI=file:" + Application.dataPath + "/StreamingAssets/FishDB.db";
        using (IDbConnection dbConnection = new SqliteConnection(dbPath))
        {
            dbConnection.Open();
            using (IDbCommand cmd = dbConnection.CreateCommand())
            {
                cmd.CommandText = @"
                    SELECT Price 
                    FROM MarketPrices 
                    WHERE FishName = @fishName 
                    AND Day = (SELECT MAX(Day) FROM MarketPrices)
                    LIMIT 1";

                var parameter = cmd.CreateParameter();
                parameter.ParameterName = "@fishName";
                parameter.Value = fishName;
                cmd.Parameters.Add(parameter);

                try
                {
                    var result = cmd.ExecuteScalar();
                    if (result != null && result != System.DBNull.Value)
                    {
                        currentMarketPrice = float.Parse(result.ToString());
                    }
                    else
                    {
                        currentMarketPrice = 0;
                        Debug.LogWarning($"No price found for fish: {fishName}");
                    }
                }
                catch (System.Exception e)
                {
                    Debug.LogError($"Error fetching price: {e.Message}");
                    currentMarketPrice = 0;
                }
            }
        }
        return currentMarketPrice;
    }
}
