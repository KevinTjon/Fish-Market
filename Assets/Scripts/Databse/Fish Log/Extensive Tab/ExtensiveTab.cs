using System.Collections.Generic;
using System.Data;
using Mono.Data.Sqlite; // Make sure to include this for SQLite
using UnityEngine;
using System.Linq;

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
    //[SerializeField] private FishPriceChart fishPriceChart;




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
        nameText = "Fish: "  + fishData.Name; // Set the name
        descriptionText = fishData.Description; // Set the description
        rarityText = "Rarity: " +fishData.Rarity; // Set the rarity
        minWeightText = fishData.MinWeight.ToString(); // Set the minimum weight
        maxWeightText = fishData.MaxWeight.ToString(); // Set the maximum weight
        topSpeedText = fishData.TopSpeed.ToString(); // Set the top speed
        assetPath = fishData.AssetPath; // Set the asset path
        isDiscovered = fishData.IsDiscovered;
        
        fishImagePanel.SetFishImage(assetPath);
        

        fishNameComponent = GetComponentInChildren<FishName>();
        descriptionComponent = GetComponentInChildren<Description>();
        rarityComponent = GetComponentInChildren<Rarity>();

        if (fishNameComponent == null || descriptionComponent == null || rarityComponent == null)
        {
            Debug.LogError("One or more components not found in children!");
            return;
        }

        fishNameComponent.SetFishName(nameText);
        descriptionComponent.SetDescription(descriptionText);
        rarityComponent.SetRarity(rarityText);

        List<FishPriceData> fishPricesWithDays = GetFishPrices(fishData.Name);
    
        // Convert to arrays for the chart
        float[] prices = fishPricesWithDays.Select(data => data.Price).ToArray();
        
        // Update the chart with the prices
        //fishPriceChart.UpdatePrices(prices);

        // Activate the tab to show the details
        gameObject.SetActive(true);
    }

    public struct FishPriceData
{
    public int Day;
    public float Price;

    public FishPriceData(int day, float price)
    {
        Day = day;
        Price = price;
    }
}


    public List<FishPriceData> GetFishPrices(string fishName)
{
    List<FishPriceData> pricesWithDays = new List<FishPriceData>();
    string connectionString = "URI=file:" + Application.dataPath + "/StreamingAssets/FishDB.db";

    using (IDbConnection dbConnection = new SqliteConnection(connectionString))
    {
        dbConnection.Open();
        using (IDbCommand dbCommand = dbConnection.CreateCommand())
        {
            // Modified query to get both Day and Price
            dbCommand.CommandText = "SELECT Day, Price FROM MarketPrices WHERE FishName = @fishName ORDER BY Day";
            var parameter = dbCommand.CreateParameter();
            parameter.ParameterName = "@fishName";
            parameter.Value = fishName;
            dbCommand.Parameters.Add(parameter);

            using (IDataReader reader = dbCommand.ExecuteReader())
            {
                while (reader.Read())
                {
                    // Add both day and price to the list
                    int day = reader.GetInt32(0);  // Get Day from first column
                    float price = reader.GetFloat(1);  // Get Price from second column
                    pricesWithDays.Add(new FishPriceData(day, price));
                }
            }
        }
    }

    return pricesWithDays;
}

}
