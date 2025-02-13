using UnityEngine;
using Mono.Data.Sqlite;
using System.Collections.Generic;
using System.IO;

public class MarketPriceGenerator : MonoBehaviour
{
    private string dbPath;
    private System.Random random;
    private SqliteConnection connection;

    private Dictionary<string, (float min, float max)> rarityPriceRanges = new Dictionary<string, (float min, float max)>
    {
        {"COMMON", (15f, 35f)},
        {"UNCOMMON", (70f, 90f)},
        {"RARE", (145f, 210f)},
        {"EPIC", (275f, 320f)},
        {"LEGENDARY", (450f, 560f)}
    };

    void Start()
    {
        dbPath = @"Data Source=" + Application.dataPath + "/StreamingAssets/FishDB.db";
        random = new System.Random();
        
        // Ensure tables are created
        CreateDatabaseTables();
    }

    void Update()
    {
        // Press 1 to generate prices for day 1
        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            GeneratePricesForDay(1);
        }

        // Press 2 to clear all prices
        if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            ClearAllPrices();
        }
    }

    void OnDisable()
    {
        // Ensure connection is closed when script is disabled
        if (connection != null)
        {
            if (connection.State != System.Data.ConnectionState.Closed)
            {
                connection.Close();
            }
            connection.Dispose();
        }
    }

    private void CreateDatabaseTables()
    {
        try
        {
            using (connection = new SqliteConnection(dbPath))
            {
                connection.Open();
                string sql = File.ReadAllText(Path.Combine(Application.dataPath, "StreamingAssets/CreateTables.sql"));
                using (var command = connection.CreateCommand())
                {
                    command.CommandText = sql;
                    command.ExecuteNonQuery();
                }
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Database error during table creation: {e.Message}");
        }
    }

    public void GeneratePricesForDay(int day)
    {
        try
        {
            if (connection != null)
            {
                connection.Close();
                connection.Dispose();
            }

            using (connection = new SqliteConnection(dbPath))
            {
                connection.Open();

                // Get all fish and their rarities
                List<(string Name, string rarity)> fishList = new List<(string Name, string rarity)>();
                using (var command = connection.CreateCommand())
                {
                    command.CommandText = "SELECT Name, Rarity FROM Fish;";
                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            string Name = reader.GetString(0);
                            string rarity = reader.GetString(1);
                            fishList.Add((Name, rarity));
                        }
                    }
                }
                
                // // Generate and insert prices for each fish
                // using (var command = connection.CreateCommand())
                // {
                //     command.CommandText = "INSERT INTO MarketPrices (FishID, Day, Price) VALUES (@fishId, @day, @price)";
                //     var fishIdParam = command.Parameters.Add("@fishId", System.Data.DbType.Int32);
                //     var dayParam = command.Parameters.Add("@day", System.Data.DbType.Int32);
                //     var priceParam = command.Parameters.Add("@price", System.Data.DbType.Double);

                //     foreach (var fish in fishList)
                //     {   
                //         Debug.Log("AAAA");
                //         var priceRange = rarityPriceRanges[fish.rarity];
                //         float basePrice = Random.Range(priceRange.min, priceRange.max);
                //         float variation = basePrice * Random.Range(-0.1f, 0.1f);
                //         float finalPrice = Mathf.Round((basePrice + variation) * 100f) / 100f;

                //         fishIdParam.Value = fish.fishId;
                //         dayParam.Value = day;
                //         priceParam.Value = finalPrice;

                //         command.ExecuteNonQuery();
                //     }
                // }
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Database error: {e.Message}");
        }
    }

    public void ClearAllPrices()
    {
        try
        {
            if (connection != null)
            {
                connection.Close();
                connection.Dispose();
            }

            using (connection = new SqliteConnection(dbPath))
            {
                connection.Open();
                using (var command = connection.CreateCommand())
                {
                    command.CommandText = "DELETE FROM MarketPrices;";
                    command.ExecuteNonQuery();
                }
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Database error: {e.Message}");
        }
    }
}