using UnityEngine;
using UnityEditor;
using Mono.Data.Sqlite;
using FishSizeNamespace;
using System.Data;
using System;
public class CoolerItem
{
    // Private set variables
    // Integer of the fish's entry in the local database
    // This should be instantiated during load time
    // Allows us to easily convert the fish between its various contexts
    private string name;
    private string description;
    private FishSize size;
    private string rarity = "common";
    private Sprite sprite;
    private float weight;
    
    // Public get variables
    public string Name => name;
    public string Description => description;
    public FishSize Size => size;
    public string Rarity => rarity;
    public Sprite Sprite => sprite;
    public float Weight => weight;

    // Creates fish from BasicFish Class
    // TODO: Fix to custom fish class
    public void Initialize(BasicFish fish)
    {
        name = fish.Name;
        size = fish.Size;
        sprite = fish.Sprite;
        weight = fish.Weight > 0 ? fish.Weight : 1f;

        // Query the database for the fish description
        try
        {
            string dbPath = "URI=file:" + Application.dataPath + "/StreamingAssets/FishDB.db";
            using (IDbConnection connection = new SqliteConnection(dbPath))
            {
                connection.Open();
                using (IDbCommand command = connection.CreateCommand())
                {
                    //
                    command.CommandText = $"SELECT Description from FISH " + 
                                          $"WHERE Name = '{fish.Name}'";
                    using (IDataReader reader = command.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            description = reader.GetString(0);
                        }
                        else
                        {
                            Debug.Log($"No description found for fish: {fish.Name}");
                            description = "This is a fish. It is very fishy.";
                        }
                    }
                }
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"Error getting fish description: {e.Message}");
            description = "This is a fish. It is very fishy.";
        }
    }

    // Gets the current data of the fish in the cooler
    public CaughtFishData GetCaughtFishData()
    {
        return new CaughtFishData
        {
            Name = name,
            Weight = weight,
            IsDiscovered = "yes"
        };
    } 

    // This displays the fish data in the console
    // TODO: Change this to display with UI
    public void DisplayFish()
    {
        Debug.Log("Fish Name: " + name);
        Debug.Log("Fish Size: " + size);
        Debug.Log(AssetDatabase.GetAssetPath(sprite));
        Debug.Log("Fish Weight: " + weight);
        Debug.Log("Fish Description: " + description);
    }
    public string GetCoolerListEntry()
    {
        return $"{name} | {rarity} | {weight} lbs.";
    }
}
