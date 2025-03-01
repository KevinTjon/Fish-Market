using UnityEngine;
using System;
using System.Collections.Generic;
using System.Data;
using Mono.Data.Sqlite;

public class FishRepository
{
    private static FishRepository _instance;
    public static FishRepository Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = new FishRepository();
            }
            return _instance;
        }
    }

    private DatabaseManager DbManager => DatabaseManager.Instance;

    // Utility method for safe float conversion
    private float SafeGetFloat(SqliteDataReader reader, int index)
    {
        if (reader.IsDBNull(index)) return 0f;
        
        try
        {
            // First try to get as float directly
            return reader.GetFloat(index);
        }
        catch
        {
            try
            {
                // If that fails, try getting as string and parsing
                string value = reader.GetString(index);
                if (float.TryParse(value, out float result))
                {
                    return result;
                }
            }
            catch
            {
                // If all else fails, try to get the raw value and convert
                object value = reader.GetValue(index);
                if (value != null && float.TryParse(value.ToString(), out float result))
                {
                    return result;
                }
            }
        }
        return 0f;
    }

    // Helper method to map a database reader to a Fish object
    private Fish MapReaderToFish(SqliteDataReader reader, bool isFromInventory = false)
    {
        try
        {
            Fish fish = new Fish();

            if (isFromInventory)
            {
                // Mapping for inventory fish (includes weight)
                fish.Name = reader.GetString(0);
                fish.Weight = SafeGetFloat(reader, 1);
                fish.Rarity = reader.GetString(2);
                fish.AssetPath = reader.IsDBNull(3) ? null : reader.GetString(3);
                fish.Description = reader.IsDBNull(4) ? "" : reader.GetString(4);
                fish.MinWeight = reader.IsDBNull(5) ? 0f : SafeGetFloat(reader, 5);
                fish.MaxWeight = reader.IsDBNull(6) ? 0f : SafeGetFloat(reader, 6);
                fish.TopSpeed = reader.IsDBNull(7) ? 0f : SafeGetFloat(reader, 7);
                fish.HookedFuncNum = reader.IsDBNull(8) ? 0 : reader.GetInt32(8);
                fish.IsDiscovered = reader.IsDBNull(9) ? false : reader.GetInt32(9) == 1;
            }
            else
            {
                // Mapping for fish from Fish table
                fish.Name = reader.GetString(0);
                fish.Description = reader.GetString(1);
                fish.Rarity = reader.GetString(2);
                fish.AssetPath = reader.IsDBNull(3) ? null : reader.GetString(3);
                fish.MinWeight = SafeGetFloat(reader, 4);
                fish.MaxWeight = SafeGetFloat(reader, 5);
                fish.TopSpeed = SafeGetFloat(reader, 6);
                fish.HookedFuncNum = reader.GetInt32(7);
                fish.IsDiscovered = reader.GetInt32(8) == 1;
                fish.GenerateRandomWeight(); // Generate initial random weight for non-inventory fish
            }

            return fish;
        }
        catch (Exception e)
        {
            Debug.LogError($"Error mapping fish data: {e.Message}");
            if (reader != null)
            {
                try
                {
                    Debug.LogError($"Failed to map fish: Name={reader.GetString(0)}");
                }
                catch { }
            }
            return null;
        }
    }

    // Get all fish from the database
    public List<Fish> GetAllFish()
    {
        string cacheKey = QueryCache.GenerateKey("AllFish");
        
        if (QueryCache.Instance.TryGet(cacheKey, out List<Fish> cachedResult))
        {
            return cachedResult;
        }

        List<Fish> fishList = new List<Fish>();
        string sql = @"
            SELECT Name, Description, Rarity, AssetPath, 
                   MinWeight, MaxWeight, TopSpeed, HookedFuncNum, IsDiscovered 
            FROM Fish";

        DbManager.ExecuteReader(sql, reader =>
        {
            while (reader.Read())
            {
                Fish fish = MapReaderToFish(reader);
                if (fish != null)
                {
                    fishList.Add(fish);
                }
            }
        });

        QueryCache.Instance.Set(cacheKey, fishList, TimeSpan.FromMinutes(5));
        return fishList;
    }

    // Get fish by name
    public Fish GetFishByName(string name)
    {
        string cacheKey = QueryCache.GenerateKey("FishByName", name);
        
        if (QueryCache.Instance.TryGet(cacheKey, out Fish cachedResult))
        {
            return cachedResult;
        }

        string sql = @"
            SELECT Name, Description, Rarity, AssetPath, 
                   MinWeight, MaxWeight, TopSpeed, HookedFuncNum, IsDiscovered 
            FROM Fish 
            WHERE Name = @name";

        Fish fish = null;
        var parameters = new Dictionary<string, object> { { "@name", name } };

        DbManager.ExecuteReader(sql, reader =>
        {
            if (reader.Read())
            {
                fish = MapReaderToFish(reader);
            }
        }, parameters);

        if (fish != null)
        {
            QueryCache.Instance.Set(cacheKey, fish, TimeSpan.FromMinutes(5));
        }
        return fish;
    }

    // Get fish by rarity
    public List<Fish> GetFishByRarity(string rarity)
    {
        string cacheKey = QueryCache.GenerateKey("FishByRarity", rarity);
        
        if (QueryCache.Instance.TryGet(cacheKey, out List<Fish> cachedResult))
        {
            return cachedResult;
        }

        List<Fish> fishList = new List<Fish>();
        string sql = @"
            SELECT Name, Description, Rarity, AssetPath, 
                   MinWeight, MaxWeight, TopSpeed, HookedFuncNum, IsDiscovered 
            FROM Fish 
            WHERE Rarity = @rarity";

        var parameters = new Dictionary<string, object> { { "@rarity", rarity } };

        DbManager.ExecuteReader(sql, reader =>
        {
            while (reader.Read())
            {
                Fish fish = MapReaderToFish(reader);
                if (fish != null)
                {
                    fishList.Add(fish);
                }
            }
        }, parameters);

        QueryCache.Instance.Set(cacheKey, fishList, TimeSpan.FromMinutes(5));
        return fishList;
    }

    // Update fish discovery status
    public bool UpdateFishDiscovery(string fishName, bool isDiscovered)
    {
        string sql = "UPDATE Fish SET IsDiscovered = @isDiscovered WHERE Name = @name";
        var parameters = new Dictionary<string, object>
        {
            { "@name", fishName },
            { "@isDiscovered", isDiscovered ? 1 : 0 }
        };

        bool success = DbManager.ExecuteNonQuery(sql, parameters) > 0;
        
        if (success)
        {
            // Invalidate all fish-related caches since discovery status changed
            QueryCache.Instance.Remove("AllFish");
            QueryCache.Instance.Remove($"FishByName:{fishName}");
            // We don't know the rarity, so we can't invalidate specific rarity cache
            // Instead, we'll remove all rarity caches
            foreach (var rarity in new[] { "COMMON", "UNCOMMON", "RARE", "EPIC", "LEGENDARY" })
            {
                QueryCache.Instance.Remove($"FishByRarity:{rarity}");
            }
        }
        
        return success;
    }

    // Add fish to inventory
    public bool AddFishToInventory(Fish fish)
    {
        if (fish == null)
        {
            Debug.LogError("Cannot add null fish to inventory");
            return false;
        }

        string sql = @"
            INSERT INTO Inventory (Name, Weight, Rarity, AssetPath)
            VALUES (@name, @weight, @rarity, @assetPath)";

        var parameters = new Dictionary<string, object>
        {
            { "@name", fish.Name },
            { "@weight", fish.Weight },
            { "@rarity", fish.Rarity },
            { "@assetPath", fish.AssetPath }
        };

        bool success = DbManager.ExecuteNonQuery(sql, parameters) > 0;
        
        if (success)
        {
            // Invalidate inventory cache when adding new fish
            QueryCache.Instance.Remove("InventoryFish");
        }
        
        return success;
    }

    // Get inventory fish
    public List<Fish> GetInventoryFish()
    {
        string cacheKey = QueryCache.GenerateKey("InventoryFish");
        
        if (QueryCache.Instance.TryGet(cacheKey, out List<Fish> cachedResult))
        {
            return cachedResult;
        }

        List<Fish> inventoryFish = new List<Fish>();
        string sql = @"
            SELECT i.Name, i.Weight, i.Rarity, i.AssetPath,
                   f.Description, f.MinWeight, f.MaxWeight, f.TopSpeed, f.HookedFuncNum, f.IsDiscovered
            FROM Inventory i
            LEFT JOIN Fish f ON i.Name = f.Name";

        DbManager.ExecuteReader(sql, reader =>
        {
            while (reader.Read())
            {
                Fish fish = MapReaderToFish(reader, true);
                if (fish != null)
                {
                    inventoryFish.Add(fish);
                }
            }
        });

        QueryCache.Instance.Set(cacheKey, inventoryFish, TimeSpan.FromSeconds(30)); // Short cache for inventory
        return inventoryFish;
    }
} 