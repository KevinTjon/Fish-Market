using UnityEngine;
using Mono.Data.Sqlite;
using System.Data;
using System.Collections.Generic;
using System;


public class FishDB : MonoBehaviour
{
    private string GetDatabasePath()
    {
        string dbPath = "URI=file:" + Application.dataPath + "/StreamingAssets/FishDB.db";
        Debug.Log("Database Path: " + dbPath);  // Add this debug line
        return dbPath;
    }

    public class Fish{
        public int Id { get; set; }
        public string Name { get; set; }
        public string Weight { get; set; }
        public string Rarity { get; set; }
        public string AssetPath { get; set; }
    }

    void Start()
    {
        //dbPath = @"Data Source=" + Application.dataPath + "/StreamingAssets/FishDB.db";
        //dbPath = "URI=file::memory:";
//        Debug.Log("Database Path: " + dbPath);
        CreateTable();

    }

    void CreateTable() {
        // Ensure StreamingAssets directory exists
        string streamingAssetsPath = Application.dataPath + "/StreamingAssets";
        if (!System.IO.Directory.Exists(streamingAssetsPath)) {
            System.IO.Directory.CreateDirectory(streamingAssetsPath);
        }

        string sqlFilePath = streamingAssetsPath + "/CreateTables.sql";
        string sqlCommands = System.IO.File.ReadAllText(sqlFilePath);
        
        using (var connection = new SqliteConnection(GetDatabasePath())) {
            connection.Open();
            using (var command = connection.CreateCommand()) {
                command.CommandText = sqlCommands;
                command.ExecuteNonQuery();
            }
        }
    }

    public void AddFish(string name, string type, string rarity, string assetPath){
        using (var connection = new SqliteConnection(GetDatabasePath())){
            connection.Open();
            using(var command = connection.CreateCommand()){
                command.CommandText =
                @"INSERT INTO Inventory (Name, Type, Rarity, AssetPath)
                VALUES (@name, @type, @rarity, @assetPath)";
                command.Parameters.AddWithValue("@name", name);
                command.Parameters.AddWithValue("@type", type);
                command.Parameters.AddWithValue("@rarity",rarity);
                command.Parameters.AddWithValue("@assetPath",assetPath);
                command.ExecuteNonQuery();
            }
        }
    }

    public List<Fish> GetFish(){
        List<Fish> fishList = new List<Fish>(); 
        string dbPath = GetDatabasePath();

        try
        {
            using (IDbConnection dbConnection = new SqliteConnection(dbPath))
            {
                dbConnection.Open();
                using (var command = dbConnection.CreateCommand())
                {
                    command.CommandText = 
                    "SELECT * FROM Inventory;"; 
                    using (IDataReader reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            Fish fish = new Fish();
                            // Safely read and cast values
                            fish.Id = reader["Id"] is DBNull ? 0 : (int)(long)reader["Id"]; // Cast to long first if it's an INTEGER
                            fish.Name = reader["Name"] is DBNull ? string.Empty : reader["Name"].ToString();
                            fish.Weight = reader["Weight"] is DBNull ? string.Empty : reader["Weight"].ToString();
                            fish.Rarity = reader["Rarity"] is DBNull ? string.Empty : reader["Rarity"].ToString();
                            fish.AssetPath = reader["AssetPath"] is DBNull ? string.Empty : reader["AssetPath"].ToString();

                            fishList.Add(fish); // Add fish to the list
                        }
                    }
                }
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"Database error: {e.Message}");
            Debug.LogError($"Database path: {dbPath}");
        }

        // Debug.Log("Number of fish retrieved: " + fishList.Count);
        return fishList; // Return the list of fish
    }
}
