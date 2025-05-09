using UnityEngine;
using Mono.Data.Sqlite;
using System;
using System.IO;

public static class GoldDB
{
    private static string dbPath
    {
        get
        {
            string dbName = "FishDB.db";
            string dbFilePath = Path.Combine(Application.streamingAssetsPath, dbName);
            return $"URI=file:{dbFilePath}";
        }
    }

    public static float GetGold(int sellerId)
    {
        using (var connection = new SqliteConnection(dbPath))
        {
            connection.Open();
            using (var command = connection.CreateCommand())
            {
                command.CommandText = "SELECT Gold FROM SellerEarnings WHERE SellerID = @sellerId";
                command.Parameters.AddWithValue("@sellerId", sellerId);
                var result = command.ExecuteScalar();
                return result != null && result != DBNull.Value ? Convert.ToSingle(result) : 0f;
            }
        }
    }

    public static void SetGold(int sellerId, float gold)
    {
        using (var connection = new SqliteConnection(dbPath))
        {
            connection.Open();
            using (var command = connection.CreateCommand())
            {
                command.CommandText = @"
                    INSERT INTO SellerEarnings (SellerID, Gold)
                    VALUES (@sellerId, @gold)
                    ON CONFLICT(SellerID) DO UPDATE SET Gold = @gold";
                command.Parameters.AddWithValue("@sellerId", sellerId);
                command.Parameters.AddWithValue("@gold", gold);
                command.ExecuteNonQuery();
            }
        }
    }

    public static void AddGold(int sellerId, float amount)
    {
        float current = GetGold(sellerId);
        SetGold(sellerId, current + amount);
    }
} 