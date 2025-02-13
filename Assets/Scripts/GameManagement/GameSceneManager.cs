using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections.Generic;
using Mono.Data.Sqlite;
using System.Data;
using System;
using System.Linq;

public class GameSceneManager : MonoBehaviour
{
    public static GameSceneManager Instance { get; private set; }
    private Dictionary<string, FishInfo> fishDatabase = new Dictionary<string, FishInfo>();

    private struct FishInfo
    {
        public string Name;
        public string Rarity;
        public string AssetPath;
        public float MinWeight;
        public float MaxWeight;
        public float TopSpeed;
        public int HookedFuncNum;
    }

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            LoadFishDatabase();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void LoadFishDatabase()
    {
        try
        {
            string dbPath = "URI=file:" + Application.dataPath + "/StreamingAssets/FishDB.db";
            Debug.Log($"Attempting to load database from: {dbPath}");

            using (IDbConnection connection = new SqliteConnection(dbPath))
            {
                connection.Open();
                using (IDbCommand command = connection.CreateCommand())
                {
                    // First, let's count how many fish are in the database
                    command.CommandText = "SELECT COUNT(*) FROM Fish";
                    int count = Convert.ToInt32(command.ExecuteScalar());
                    Debug.Log($"Found {count} fish in database");

                    // Now load the fish data
                    command.CommandText = "SELECT Name, Rarity, AssetPath, MinWeight, MaxWeight, TopSpeed, HookedFuncNum FROM Fish";
                    using (IDataReader reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            try
                            {
                                string name = reader.GetString(0);
                                string rarity = reader.GetString(1);
                                
                                FishInfo fish = new FishInfo
                                {
                                    Name = name,
                                    Rarity = rarity,
                                    AssetPath = reader.GetString(2),
                                    MinWeight = reader.GetFloat(3),
                                    MaxWeight = reader.GetFloat(4),
                                    TopSpeed = reader.GetFloat(5),
                                    HookedFuncNum = reader.GetInt32(6)
                                };
                                fishDatabase[fish.Name] = fish;
                                Debug.Log($"Successfully loaded fish: {name} with rarity: {rarity}");
                            }
                            catch (Exception e)
                            {
                                Debug.LogError($"Error loading fish data: {e.Message}");
                            }
                        }
                    }
                }
            }

            Debug.Log($"Final fish database count: {fishDatabase.Count}");
            
            // Print all fish in database
            foreach (var fish in fishDatabase)
            {
                Debug.Log($"Fish in database: {fish.Key} - {fish.Value.Rarity}");
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"Database loading error: {e.Message}");
        }
    }

    public void LoadFishingScene()
    {
        SceneManager.LoadScene("FishingScene"); // Replace with your actual scene name
    }

    public void LoadMarketScene()
    {
        SceneManager.LoadScene("MarketUI"); // Replace with your actual scene name
    }

    public void SimulateFishCatch()
    {
        string caughtFish = GetRandomFishByRarity();
        if (string.IsNullOrEmpty(caughtFish))
        {
            Debug.LogError("No fish caught!");
            return;
        }

        FishInfo fish = fishDatabase[caughtFish];
        
        // Generate random weight based on fish's min/max weight
        float weight = UnityEngine.Random.Range(fish.MinWeight, fish.MaxWeight);
        
        CaughtFishData testFish = new CaughtFishData
        {
            Name = fish.Name,
            Weight = weight,
            IsDiscovered = "Yes"  // Newly caught fish are automatically discovered
        };

        SaveFishData(testFish);
    }

    private string GetRandomFishByRarity()
    {
        // Create lists for each rarity
        List<string> legendaryFish = new List<string>();
        List<string> rareFish = new List<string>();
        List<string> uncommonFish = new List<string>();
        List<string> commonFish = new List<string>();

        // Sort all fish into rarity lists
        foreach (var fish in fishDatabase)
        {
            switch (fish.Value.Rarity.ToLower())
            {
                case "legendary":
                    legendaryFish.Add(fish.Key);
                    break;
                case "rare":
                    rareFish.Add(fish.Key);
                    break;
                case "uncommon":
                    uncommonFish.Add(fish.Key);
                    break;
                case "common":
                    commonFish.Add(fish.Key);
                    break;
            }
        }

        // Print lists for debugging
        Debug.Log("=== Fish Lists ===");
        Debug.Log($"Legendary Fish ({legendaryFish.Count}): {string.Join(", ", legendaryFish)}");
        Debug.Log($"Rare Fish ({rareFish.Count}): {string.Join(", ", rareFish)}");
        Debug.Log($"Uncommon Fish ({uncommonFish.Count}): {string.Join(", ", uncommonFish)}");
        Debug.Log($"Common Fish ({commonFish.Count}): {string.Join(", ", commonFish)}");

        // Roll for rarity
        float random = UnityEngine.Random.value * 100f;
        Debug.Log($"Random roll: {random}");

        // Select fish based on roll
        if (random <= 5f && legendaryFish.Count > 0)  // 5% Legendary
        {
            Debug.Log("Rolled Legendary!");
            return legendaryFish[UnityEngine.Random.Range(0, legendaryFish.Count)];
        }
        else if (random <= 20f && rareFish.Count > 0)  // 15% Rare
        {
            Debug.Log("Rolled Rare!");
            return rareFish[UnityEngine.Random.Range(0, rareFish.Count)];
        }
        else if (random <= 50f && uncommonFish.Count > 0)  // 30% Uncommon
        {
            Debug.Log("Rolled Uncommon!");
            return uncommonFish[UnityEngine.Random.Range(0, uncommonFish.Count)];
        }
        else if (commonFish.Count > 0)  // 50% Common
        {
            Debug.Log("Rolled Common!");
            return commonFish[UnityEngine.Random.Range(0, commonFish.Count)];
        }

        // Fallback: if no fish in the rolled rarity, pick from any available list
        var allFish = legendaryFish.Concat(rareFish).Concat(uncommonFish).Concat(commonFish).ToList();
        if (allFish.Count > 0)
        {
            Debug.Log("Falling back to random fish from any rarity");
            return allFish[UnityEngine.Random.Range(0, allFish.Count)];
        }

        Debug.LogError("No fish found in database!");
        return null;
    }

    private void SaveFishData(CaughtFishData fish)
    {
        string dbPath = "URI=file:" + Application.dataPath + "/StreamingAssets/FishDB.db";
        
        using (IDbConnection connection = new SqliteConnection(dbPath))
        {
            connection.Open();
            using (IDbCommand command = connection.CreateCommand())
            {
                // Insert into Inventory table
                command.CommandText = @"
                    INSERT INTO Inventory (Name, Weight, Rarity, AssetPath)
                    SELECT @Name, @Weight, Rarity, AssetPath
                    FROM Fish
                    WHERE Name = @Name";

                // Add parameters
                var nameParam = command.CreateParameter();
                nameParam.ParameterName = "@Name";
                nameParam.Value = fish.Name;
                command.Parameters.Add(nameParam);

                var weightParam = command.CreateParameter();
                weightParam.ParameterName = "@Weight";
                weightParam.Value = fish.Weight.ToString(); // Convert to string as Weight is TEXT in DB

                command.Parameters.Add(weightParam);

                try
                {
                    command.ExecuteNonQuery();
                    Debug.Log($"Saved to inventory: {fish.Name}, Weight: {fish.Weight}");
                }
                catch (Exception e)
                {
                    Debug.LogError($"Error saving fish to inventory: {e.Message}");
                }
            }
        }
    }

    public void ResetInventory()
    {
        string dbPath = "URI=file:" + Application.dataPath + "/StreamingAssets/FishDB.db";
        
        using (IDbConnection connection = new SqliteConnection(dbPath))
        {
            connection.Open();
            using (IDbCommand command = connection.CreateCommand())
            {
                command.CommandText = "DELETE FROM Inventory";
                
                try
                {
                    command.ExecuteNonQuery();
                    Debug.Log("Inventory table has been reset!");
                }
                catch (Exception e)
                {
                    Debug.LogError($"Error resetting inventory: {e.Message}");
                }
            }
        }
    }

    // Add initialization check
    public static void EnsureExists()
    {
        if (Instance == null)
        {
            // Try to find existing instance
            Instance = FindObjectOfType<GameSceneManager>();

            // If still null, create new instance
            if (Instance == null)
            {
                GameObject go = new GameObject("GameSceneManager");
                Instance = go.AddComponent<GameSceneManager>();
            }
        }
    }
}

// Update CaughtFishData class to remove Price
public class CaughtFishData
{
    public string Name { get; set; }
    public float Weight { get; set; }
    public string IsDiscovered { get; set; }
}