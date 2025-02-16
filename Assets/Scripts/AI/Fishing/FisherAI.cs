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
                    // Get the latest market price for this fish
                    command.CommandText = @"
                        SELECT Price 
                        FROM MarketPrices 
                        WHERE FishName = @fishName 
                        ORDER BY Day DESC 
                        LIMIT 1";

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
        int price = (int)basePrice;  // Convert to int first
        
        switch (priceStrategy)
        {
            case PriceStrategy.Aggressive:
                return Mathf.RoundToInt(price * 0.95f);  // 20% below base price
            
            case PriceStrategy.Conservative:
                return Mathf.RoundToInt(price * 1.2f);  // 20% above base price
            
            case PriceStrategy.MarketValue:
                return price;  // Already an int
            
            default:
                return price;
        }
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