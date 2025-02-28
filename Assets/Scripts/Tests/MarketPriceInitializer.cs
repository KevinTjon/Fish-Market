using UnityEngine;
using System;
using System.Collections.Generic;
using Mono.Data.Sqlite;

public class MarketPriceInitializer : MonoBehaviour
{
    private struct PriceRange
    {
        public int min;
        public int max;

        public PriceRange(int min, int max)
        {
            this.min = min;
            this.max = max;
        }
    }

    public void GenerateDayPrices()
    {
        string dbPath = "URI=file:" + Application.dataPath + "/StreamingAssets/FishDB.db";

        try
        {
            using (var connection = new SqliteConnection(dbPath))
            {
                connection.Open();

                // Clear existing market prices
                using (var clearCommand = connection.CreateCommand())
                {
                    clearCommand.CommandText = "DELETE FROM MarketPrices";
                    clearCommand.ExecuteNonQuery();
                }
                
                using (var command = connection.CreateCommand())
                {
                    command.CommandText = "SELECT Name, Rarity FROM Fish";
                    
                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            string fishName = reader.GetString(0);
                            string rarity = reader.GetString(1);
                            PriceRange range = GetPriceRangeForRarity(rarity);
                            
                            // Generate base price for this fish
                            float basePrice = UnityEngine.Random.Range(range.min, range.max + 1);

                            // Generate just one day of prices
                            using (var insertCommand = connection.CreateCommand())
                            {
                                insertCommand.CommandText = @"
                                    INSERT INTO MarketPrices (FishName, Day, Price) 
                                    VALUES (@fishName, @day, @price)";
                                
                                insertCommand.Parameters.AddWithValue("@fishName", fishName);
                                insertCommand.Parameters.AddWithValue("@day", 1);
                                insertCommand.Parameters.AddWithValue("@price", basePrice);
                                
                                insertCommand.ExecuteNonQuery();
                            }
                        }
                    }
                }
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"Error generating prices: {e.Message}");
        }
    }

    private PriceRange GetPriceRangeForRarity(string rarity)
    {
        switch (rarity)
        {
            case "LEGENDARY": return new PriceRange(800, 1200);
            case "EPIC": return new PriceRange(400, 600);
            case "RARE": return new PriceRange(200, 300);
            case "UNCOMMON": return new PriceRange(80, 120);
            case "COMMON": return new PriceRange(40, 60);
            default: return new PriceRange(20, 30);
        }
    }

    public void ShowCurrentMarketPrices()
    {
        string dbPath = "URI=file:" + Application.dataPath + "/StreamingAssets/FishDB.db";
        
        try
        {
            using (var connection = new SqliteConnection(dbPath))
            {
                connection.Open();
                using (var command = connection.CreateCommand())
                {
                    command.CommandText = @"
                        SELECT f.Name, f.Rarity, mp.Price, mp.Day
                        FROM Fish f
                        JOIN MarketPrices mp ON f.Name = mp.FishName
                        WHERE mp.Day = (SELECT MAX(Day) FROM MarketPrices)
                        ORDER BY f.Rarity, f.Name";

                    using (var reader = command.ExecuteReader())
                    {
                        string currentRarity = "";
                        while (reader.Read())
                        {
                            string fishName = reader.GetString(0);
                            string rarity = reader.GetString(1);
                            float price = reader.GetFloat(2);
                            int day = reader.GetInt32(3);

                            if (rarity != currentRarity)
                            {
                                //Debug.Log($"\n=== {rarity} FISH ===");
                                currentRarity = rarity;
                            }

                            //Debug.Log($"{fishName}: {price:F2} gold");
                        }
                    }
                }
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"Error showing market prices: {e.Message}");
        }
    }

    // Update the test menu item name
    [ContextMenu("Generate Day 1 Prices")]
    public void TestInitialization()
    {
        GenerateDayPrices();
        ShowCurrentMarketPrices();
    }
}