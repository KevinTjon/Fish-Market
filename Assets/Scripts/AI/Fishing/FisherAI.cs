using UnityEngine;
using System;
using System.Collections.Generic;
using Mono.Data.Sqlite;

public abstract class FisherAI
{
    public enum AIType
    {
        CommonFisher,
        RareFisher,
        BalancedFisher,
        ExpertFisher
    }

    public enum PriceStrategy
    {
        Aggressive,     // Sells below market price
        Conservative,   // Sells above market price
        MarketValue    // Sells at market price
    }

    protected string aiName;
    protected AIType aiType;
    protected PriceStrategy priceStrategy;
    protected Dictionary<string, float> rarityWeights;
    protected int minFishCount;
    protected int maxFishCount;
    protected int sellerID;

    // Properties
    public string Name => aiName;
    public AIType Type => aiType;
    public PriceStrategy Strategy => priceStrategy;
    public int SellerID => sellerID;

    protected virtual int DetermineNumberOfFish()
    {
        return UnityEngine.Random.Range(minFishCount, maxFishCount + 1);
    }

    protected virtual string SelectFishRarity()
    {
        float randomValue = UnityEngine.Random.value;
        float currentProbability = 0f;

        foreach (var rarity in rarityWeights)
        {
            currentProbability += rarity.Value;
            if (randomValue <= currentProbability)
            {
                return rarity.Key;
            }
        }

        return "common"; // Default fallback
    }

    public virtual List<string> GenerateFishCatch()
    {
        List<string> caughtFish = new List<string>();
        string dbPath = "URI=file:" + Application.dataPath + "/StreamingAssets/FishDB.db";

        try
        {
            using (var connection = new SqliteConnection(dbPath))
            {
                connection.Open();
                int fishToGenerate = DetermineNumberOfFish();

                for (int i = 0; i < fishToGenerate; i++)
                {
                    string rarity = SelectFishRarity();
                    
                    using (var command = connection.CreateCommand())
                    {
                        command.CommandText = "SELECT Name FROM Fish WHERE Rarity = @rarity ORDER BY RANDOM() LIMIT 1";
                        command.Parameters.AddWithValue("@rarity", rarity);
                        
                        var result = command.ExecuteScalar();
                        if (result != null)
                        {
                            string fishName = result.ToString();
                            caughtFish.Add(fishName);
                        }
                    }
                }
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"Database error: {e.Message}");
        }

        return caughtFish;
    }

    protected virtual float GetBasePrice(string fishName)
    {
        string dbPath = "URI=file:" + Application.dataPath + "/StreamingAssets/FishDB.db";
        float basePrice = 0f;

        try
        {
            using (var connection = new SqliteConnection(dbPath))
            {
                connection.Open();
                using (var command = connection.CreateCommand())
                {
                    // Get the current max day
                    command.CommandText = "SELECT MAX(Day) FROM MarketPrices";
                    var maxDay = Convert.ToInt32(command.ExecuteScalar());
                    
                    // Get average of available days (up to last 5)
                    command.CommandText = @"
                        SELECT AVG(Price) 
                        FROM MarketPrices 
                        WHERE FishName = @fishName 
                        AND Day > @startDay";

                    command.Parameters.AddWithValue("@fishName", fishName);
                    command.Parameters.AddWithValue("@startDay", Math.Max(0, maxDay - 5));  // Get up to 5 days of history
                    
                    var result = command.ExecuteScalar();
                    if (result != null && result != DBNull.Value)
                    {
                        basePrice = Convert.ToSingle(result);
                    }
                    else
                    {
                        // If no prices found, use rarity-based fallback
                        command.CommandText = "SELECT Rarity FROM Fish WHERE Name = @fishName";
                        var rarity = command.ExecuteScalar()?.ToString();
                        
                        basePrice = rarity switch
                        {
                            "COMMON" => 40f,
                            "UNCOMMON" => 100f,
                            "RARE" => 400f,
                            "EPIC" => 800f,
                            "LEGENDARY" => 1500f,
                            _ => 50f
                        };

                        Debug.LogWarning($"No price history found for {fishName}, using fallback price: {basePrice}");
                    }
                }
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"Error getting market price: {e.Message}");
        }

        return basePrice;
    }

    protected virtual float DetermineSellPrice(float basePrice)
    {
        float minPrice;
        float finalPrice;
        
        // Set minimum prices by rarity to prevent underpricing
        if (basePrice < 100) // Common
            minPrice = 30;
        else if (basePrice < 300) // Uncommon
            minPrice = 80;
        else if (basePrice < 1000) // Rare
            minPrice = 300;
        else if (basePrice < 2000) // Epic
            minPrice = 600;
        else // Legendary
            minPrice = 800;
        
        switch (priceStrategy)
        {
            case PriceStrategy.Aggressive:
                finalPrice = Mathf.Max(minPrice, basePrice * 0.80f);   // 20% below base price but not below min
                break;
            
            case PriceStrategy.Conservative:
                finalPrice = Mathf.Max(minPrice, basePrice * 1.01f);   // 1% above base price
                break;
            
            case PriceStrategy.MarketValue:
                finalPrice = Mathf.Max(minPrice, basePrice * 0.90f);   // 10% below base price but not below min
                break;
            
            default:
                finalPrice = basePrice;
                break;
        }

        // Apply rarity-based price caps
        if (finalPrice > GetMaxPriceForRarity(basePrice))
        {
            finalPrice = GetMaxPriceForRarity(basePrice);
        }

        return Mathf.RoundToInt(finalPrice);
    }

    private float GetMaxPriceForRarity(float basePrice)
    {
        // Adjust price ranges to be more distinct
        if (basePrice < 100) // Common
            return 50;
        if (basePrice < 300) // Uncommon
            return 150;
        if (basePrice < 1000) // Rare
            return 500;
        if (basePrice < 2000) // Epic
            return 1000;
        return 2000; // Legendary
    }

    public void CreateMarketListings(List<string> fishNames)
    {
        string dbPath = "URI=file:" + Application.dataPath + "/StreamingAssets/FishDB.db";

        try
        {
            using (var connection = new SqliteConnection(dbPath))
            {
                connection.Open();
                
                foreach (string fishName in fishNames)
                {
                    using (var command = connection.CreateCommand())
                    {
                        // Get the rarity for this fish
                        command.CommandText = "SELECT Rarity FROM Fish WHERE Name = @fishName";
                        command.Parameters.AddWithValue("@fishName", fishName);
                        string rarity = (string)command.ExecuteScalar();

                        float basePrice = GetBasePrice(fishName);
                        float sellPrice = DetermineSellPrice(basePrice);

                        command.CommandText = @"
                            INSERT INTO MarketListings 
                                (FishName, Rarity, ListedPrice, SellerID, IsSold) 
                            VALUES 
                                (@fishName, @rarity, @listedPrice, @sellerID, 0)";

                        command.Parameters.AddWithValue("@fishName", fishName);
                        command.Parameters.AddWithValue("@rarity", rarity);
                        command.Parameters.AddWithValue("@listedPrice", sellPrice);
                        command.Parameters.AddWithValue("@sellerID", sellerID);

                        command.ExecuteNonQuery();
                    }
                }
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"Error creating market listing: {e.Message}");
        }
    }
} 