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
                    
                    // First try to get the current day's price
                    command.CommandText = @"
                        SELECT Price 
                        FROM MarketPrices 
                        WHERE FishName = @fishName 
                        AND Day = @currentDay";

                    command.Parameters.AddWithValue("@fishName", fishName);
                    command.Parameters.AddWithValue("@currentDay", maxDay);
                    
                    var result = command.ExecuteScalar();
                    if (result != null && result != DBNull.Value)
                    {
                        basePrice = Convert.ToSingle(result);
                    }
                    else
                    {
                        // If no current day price found, this shouldn't happen if MarketPriceInitializer ran correctly
                        Debug.LogWarning($"No price found for {fishName} on day {maxDay}. This shouldn't happen!");
                        
                        // Get rarity-based price as last resort
                        command.CommandText = "SELECT Rarity FROM Fish WHERE Name = @fishName";
                        var rarity = command.ExecuteScalar()?.ToString();
                        
                        basePrice = rarity switch
                        {
                            "COMMON" => 10f,
                            "UNCOMMON" => 30f,
                            "RARE" => 50f,
                            "EPIC" => 80f,
                            "LEGENDARY" => 150f,
                            _ => 10f
                        };

                        Debug.LogWarning($"Using emergency fallback price for {fishName}: {basePrice}");
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
        if (basePrice < 20) // Common
            minPrice = 5;
        else if (basePrice < 40) // Uncommon
            minPrice = 20;
        else if (basePrice < 60) // Rare
            minPrice = 40;
        else if (basePrice < 100) // Epic
            minPrice = 60;
        else // Legendary
            minPrice = 100;
        
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
        // Match price ranges with MarketPriceInitializer
        if (basePrice < 60) // Common
            return 60;
        if (basePrice < 120) // Uncommon
            return 120;
        if (basePrice < 300) // Rare
            return 300;
        if (basePrice < 600) // Epic
            return 600;
        return 1200; // Legendary
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