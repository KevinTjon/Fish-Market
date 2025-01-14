using UnityEngine;
using Mono.Data.Sqlite;
using System.Data;
using System.Collections.Generic;
using System;


public class FishDB : MonoBehaviour
{
    private string dbPath;
    public class Fish{
        public int Id { get; set; }
        public string Name { get; set; }
        public string Type { get; set; }
        public string Rarity { get; set; }
        public string AssetPath { get; set; }
    }

    void Start()
    {
        dbPath = @"Data Source=" + Application.dataPath + "/StreamingAssets/FishDB.db";
        //dbPath = "URI=file::memory:";
        Debug.Log("Database Path: " + dbPath);
        CreateTable();
        // AddFish("Green Fish", "Medium","UNCOMMON","Prefabs/Fish/GreenFish");
        // AddFish("Pink Fish", "small","COMMON","Prefabs/Fish/PinkFish");
        // AddFish("Red Fish", "small","COMMON","Prefabs/Fish/RedFish");
    }

    void CreateTable(){
        using (var connection = new SqliteConnection(dbPath)){
            connection.Open();
            using (var command = connection.CreateCommand()){
                command.CommandText = 
                    @"CREATE TABLE IF NOT EXISTS Fish
                    (
                    Id INTEGER PRIMARY KEY,
                    Name TEXT,
                    Type TEXT,
                    Rarity TEXT,
                    AssetPath TEXT
                    )";
                command.ExecuteNonQuery();
            }
        }
    }

    public void AddFish(string name, string type, string rarity, string assetPath){
        using (var connection = new SqliteConnection(dbPath)){
            connection.Open();
            using(var command = connection.CreateCommand()){
                command.CommandText =
                @"INSERT INTO Fish (Name, Type, Rarity, AssetPath)
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
        using (var connection = new SqliteConnection(dbPath)){
            connection.Open();
            using (var command = connection.CreateCommand()){
                command.CommandText = 
                "SELECT * FROM Fish;"; 
                using (IDataReader reader = command.ExecuteReader()){
                    while (reader.Read()){
                        Fish fish = new Fish();
                        // Safely read and cast values
                        fish.Id = reader["Id"] is DBNull ? 0 : (int)(long)reader["Id"]; // Cast to long first if it's an INTEGER
                        fish.Name = reader["Name"] is DBNull ? string.Empty : reader["Name"].ToString();
                        fish.Type = reader["Type"] is DBNull ? string.Empty : reader["Type"].ToString();
                        fish.Rarity = reader["Rarity"] is DBNull ? string.Empty : reader["Rarity"].ToString();
                        fish.AssetPath = reader["AssetPath"] is DBNull ? string.Empty : reader["AssetPath"].ToString();

                        fishList.Add(fish); // Add fish to the list
                    }
                }
            }
        }
        Debug.Log("Number of fish retrieved: " + fishList.Count);
        return fishList; // Return the list of fish
    }
}
