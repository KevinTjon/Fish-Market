using UnityEngine;
using System;
using System.Collections.Generic;
using Mono.Data.Sqlite;

public class MarketRepository
{
    private static MarketRepository _instance;
    public static MarketRepository Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = new MarketRepository();
            }
            return _instance;
        }
    }

    private DatabaseManager DbManager => DatabaseManager.Instance;

    public Dictionary<string, float> GetLatestMarketPrices()
    {
        string cacheKey = QueryCache.GenerateKey("LatestMarketPrices");
        
        if (QueryCache.Instance.TryGet(cacheKey, out Dictionary<string, float> cachedResult))
        {
            return cachedResult;
        }

        Dictionary<string, float> prices = new Dictionary<string, float>();
        
        try
        {
            string sql = @"
                SELECT FishName, Price 
                FROM MarketPrices 
                WHERE (FishName, Day) IN (
                    SELECT FishName, MAX(Day) 
                    FROM MarketPrices 
                    GROUP BY FishName
                )";

            DbManager.ExecuteReader(sql, reader =>
            {
                while (reader.Read())
                {
                    string fishName = reader.GetString(0);
                    float price = (float)reader.GetDouble(1);
                    prices[fishName] = price;
                }
            });

            QueryCache.Instance.Set(cacheKey, prices, TimeSpan.FromMinutes(1)); // Cache for 1 minute since prices change frequently
        }
        catch (Exception e)
        {
            Debug.LogError($"Error loading market prices: {e.Message}");
        }

        return prices;
    }

    public float GetLatestPriceForFish(string fishName)
    {
        string cacheKey = QueryCache.GenerateKey("LatestFishPrice", fishName);
        
        if (QueryCache.Instance.TryGet(cacheKey, out float cachedPrice))
        {
            return cachedPrice;
        }

        float price = 0f;
        
        try
        {
            string sql = @"
                SELECT Price 
                FROM MarketPrices 
                WHERE FishName = @fishName 
                ORDER BY Day DESC 
                LIMIT 1";

            var parameters = new Dictionary<string, object> { { "@fishName", fishName } };

            var result = DbManager.ExecuteScalar(sql, parameters);
            if (result != null && result != DBNull.Value)
            {
                price = Convert.ToSingle(result);
                QueryCache.Instance.Set(cacheKey, price, TimeSpan.FromMinutes(1)); // Cache for 1 minute
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"Error loading price for fish {fishName}: {e.Message}");
        }

        return price;
    }
} 