using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;

public class FishPriceRepository
{
    private static FishPriceRepository _instance;
    public static FishPriceRepository Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = new FishPriceRepository();
            }
            return _instance;
        }
    }

    private DatabaseManager DbManager => DatabaseManager.Instance;

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

    public List<FishPriceData> GetFishPriceHistory(string fishName)
    {
        string cacheKey = QueryCache.GenerateKey("FishPriceHistory", fishName);
        
        if (QueryCache.Instance.TryGet(cacheKey, out List<FishPriceData> cachedResult))
        {
            return cachedResult;
        }

        List<FishPriceData> pricesWithDays = new List<FishPriceData>();
        
        try
        {
            string sql = @"
                SELECT Day, Price 
                FROM MarketPrices 
                WHERE FishName = @fishName 
                ORDER BY Day";

            var parameters = new Dictionary<string, object> { { "@fishName", fishName } };

            DbManager.ExecuteReader(sql, reader =>
            {
                while (reader.Read())
                {
                    int day = reader.GetInt32(0);
                    float price = (float)reader.GetDouble(1);
                    pricesWithDays.Add(new FishPriceData(day, price));
                }
            }, parameters);

            QueryCache.Instance.Set(cacheKey, pricesWithDays, TimeSpan.FromMinutes(1)); // Cache for 1 minute since prices change frequently
        }
        catch (Exception e)
        {
            Debug.LogError($"Error loading price history for fish {fishName}: {e.Message}");
        }

        return pricesWithDays;
    }

    public float[] GetPriceArray(string fishName)
    {
        string cacheKey = QueryCache.GenerateKey("FishPriceArray", fishName);
        
        if (QueryCache.Instance.TryGet(cacheKey, out float[] cachedResult))
        {
            return cachedResult;
        }

        var result = GetFishPriceHistory(fishName).Select(data => data.Price).ToArray();
        QueryCache.Instance.Set(cacheKey, result, TimeSpan.FromMinutes(1));
        return result;
    }

    public int[] GetDayArray(string fishName)
    {
        string cacheKey = QueryCache.GenerateKey("FishDayArray", fishName);
        
        if (QueryCache.Instance.TryGet(cacheKey, out int[] cachedResult))
        {
            return cachedResult;
        }

        var result = GetFishPriceHistory(fishName).Select(data => data.Day).ToArray();
        QueryCache.Instance.Set(cacheKey, result, TimeSpan.FromMinutes(1));
        return result;
    }
} 