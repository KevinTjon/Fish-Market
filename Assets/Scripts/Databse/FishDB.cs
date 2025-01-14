using UnityEngine;
using Mono.Data.Sqlite;
using System.Data;


public class FishDB : MonoBehaviour
{

    private string dbPath;

    void Start()
    {
        dbPath = "URI=file:" + Application.dataPath + "/StreamingAssets/FishDB.db";
        CreateTable();
        AddFish("clownfish", "small","COMMON","");
        GetFish(1);
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

    public void GetFish(int id){
        using (var connection = new SqliteConnection(dbPath)){
            connection.Open();
            using (var command = connection.CreateCommand()){
                command.CommandText =
                "SELECT * FROM fish;";
                //command.Parameters.AddWithValue("@id",id);
                using (IDataReader reader = command.ExecuteReader()){
                    while (reader.Read()){
                        Debug.Log($"Fish: {reader["Name"]}, Type: {reader["Type"]}, Rarity: {reader["Rarity"]}, AssetPath: {reader["AssetPath"]}");
                        Debug.Log("wssap");
                    }
                }
            }
        }
    }
}
