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
                    // Get the 5-day average price for this fish
                    command.CommandText = @"
                        SELECT AVG(Price) 
                        FROM MarketPrices 
                        WHERE FishName = @fishName 
                        AND Day >= (SELECT MAX(Day) - 4 FROM MarketPrices)";

                    command.Parameters.AddWithValue("@fishName", fishName);
                    
                    var result = command.ExecuteScalar();
                    if (result != null && result != DBNull.Value)
                    {
                        basePrice = Convert.ToSingle(result);
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
        int price = (int)basePrice;
        float finalPrice = price;
        
        switch (priceStrategy)
        {
            case PriceStrategy.Aggressive:
                finalPrice = price * 0.80f;   // 20% below base price
                break;
            
            case PriceStrategy.Conservative:
                finalPrice = price * 1.01f;   // 1% above base price
                break;
            
            case PriceStrategy.MarketValue:
                finalPrice = price * 0.90f;   // 10% below base price
                break;
        }

        // Apply rarity-based price caps
        if (finalPrice > GetMaxPriceForRarity(price))
        {
            finalPrice = GetMaxPriceForRarity(price);
        }

        return Mathf.RoundToInt(finalPrice);
    }

    private float GetMaxPriceForRarity(float basePrice)
    {
        // Estimate rarity based on base price
        if (basePrice < 100) // Common
            return 40;
        if (basePrice < 200) // Uncommon
            return 100;
        if (basePrice < 1000) // Rare
            return 500;
        return 1000; // Legendary
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