using UnityEngine;
using Mono.Data.Sqlite;
using System.Collections.Generic;

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
                List<(int fishId, string rarity)> fishList = new List<(int fishId, string rarity)>();
                using (var command = connection.CreateCommand())
                {
                    command.CommandText = "SELECT FishID, Rarity FROM Fish;";
                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            int fishId = reader.GetInt32(0);
                            string rarity = reader.GetString(1);
                            fishList.Add((fishId, rarity));
                        }
                    }
                }

                // Generate and insert prices for each fish
                using (var command = connection.CreateCommand())
                {
                    command.CommandText = "INSERT INTO MarketPrices (FishID, Day, Price) VALUES (@fishId, @day, @price)";
                    var fishIdParam = command.Parameters.Add("@fishId", System.Data.DbType.Int32);
                    var dayParam = command.Parameters.Add("@day", System.Data.DbType.Int32);
                    var priceParam = command.Parameters.Add("@price", System.Data.DbType.Double);

                    foreach (var fish in fishList)
                    {
                        var priceRange = rarityPriceRanges[fish.rarity];
                        float basePrice = Random.Range(priceRange.min, priceRange.max);
                        float variation = basePrice * Random.Range(-0.1f, 0.1f);
                        float finalPrice = Mathf.Round((basePrice + variation) * 100f) / 100f;

                        fishIdParam.Value = fish.fishId;
                        dayParam.Value = day;
                        priceParam.Value = finalPrice;

                        command.ExecuteNonQuery();
                    }
                }
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