using UnityEngine;
using System;
using System.Collections.Generic;
using Mono.Data.Sqlite;
using System.Linq;

/// <summary>
/// Centralized database manager for all database operations in the game.
/// Implements the Singleton pattern to ensure only one instance exists.
/// </summary>
public class DatabaseManager : MonoBehaviour
{
    private static DatabaseManager _instance;
    public static DatabaseManager Instance
    {
        get
        {
            if (_instance == null)
            {
                // Find existing instance
                _instance = FindObjectOfType<DatabaseManager>();
                
                // If no instance exists, create one
                if (_instance == null)
                {
                    // Create a new GameObject at the root level
                    GameObject obj = new GameObject("DatabaseManager");
                    _instance = obj.AddComponent<DatabaseManager>();
                }
            }
            return _instance;
        }
    }

    private string dbPath;
    private SqliteConnection persistentConnection;
    private bool isInitialized = false;

    private void Awake()
    {
        // First, ensure this GameObject is at the root level
        if (transform.parent != null)
        {
            transform.SetParent(null);
        }

        // If there's already an instance and it's not this one, destroy this one
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }

        // Make this the singleton instance
        _instance = this;
        
        // Don't destroy on load (only called once we're sure this is the singleton instance)
        DontDestroyOnLoad(gameObject);
        
        InitializeDatabase();
    }

    private void OnDestroy()
    {
        CloseConnection();
    }

    private void OnApplicationQuit()
    {
        CloseConnection();
    }

    /// <summary>
    /// Initializes the database connection.
    /// </summary>
    private void InitializeDatabase()
    {
        try
        {
            string dbName = "FishDB.db";
            string dbFilePath = System.IO.Path.Combine(Application.streamingAssetsPath, dbName);
            dbPath = $"URI=file:{dbFilePath}";
            
            // Test the connection
            using (var connection = new SqliteConnection(dbPath))
            {
                connection.Open();
                Debug.Log("Database connection successful: " + dbPath);
            }
            
            isInitialized = true;
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to initialize database: {e.Message}");
            isInitialized = false;
        }
    }

    /// <summary>
    /// Gets a connection to the database. The caller is responsible for closing this connection.
    /// </summary>
    public SqliteConnection GetConnection()
    {
        if (!isInitialized)
        {
            InitializeDatabase();
        }

        var connection = new SqliteConnection(dbPath);
        connection.Open();
        return connection;
    }

    /// <summary>
    /// Gets or creates a persistent connection that stays open until explicitly closed.
    /// Use this for operations that require multiple database calls in sequence.
    /// </summary>
    public SqliteConnection GetPersistentConnection()
    {
        if (persistentConnection == null || persistentConnection.State != System.Data.ConnectionState.Open)
        {
            if (persistentConnection != null)
            {
                try { persistentConnection.Close(); } catch { }
                try { persistentConnection.Dispose(); } catch { }
            }

            persistentConnection = new SqliteConnection(dbPath);
            persistentConnection.Open();
        }
        
        return persistentConnection;
    }

    /// <summary>
    /// Closes the persistent connection if it's open.
    /// </summary>
    public void CloseConnection()
    {
        if (persistentConnection != null && persistentConnection.State == System.Data.ConnectionState.Open)
        {
            try
            {
                persistentConnection.Close();
                persistentConnection.Dispose();
                persistentConnection = null;
            }
            catch (Exception e)
            {
                Debug.LogError($"Error closing database connection: {e.Message}");
            }
        }
    }

    /// <summary>
    /// Executes a non-query SQL command (INSERT, UPDATE, DELETE).
    /// </summary>
    public int ExecuteNonQuery(string sql, Dictionary<string, object> parameters = null)
    {
        try
        {
            using (var connection = GetConnection())
            using (var command = connection.CreateCommand())
            {
                command.CommandText = sql;
                
                if (parameters != null)
                {
                    foreach (var param in parameters)
                    {
                        command.Parameters.AddWithValue(param.Key, param.Value);
                    }
                }
                
                return command.ExecuteNonQuery();
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"Database error in ExecuteNonQuery: {e.Message}\nSQL: {sql}");
            return -1;
        }
    }

    /// <summary>
    /// Executes a SQL query that returns a single value.
    /// </summary>
    public object ExecuteScalar(string sql, Dictionary<string, object> parameters = null)
    {
        try
        {
            using (var connection = GetConnection())
            using (var command = connection.CreateCommand())
            {
                command.CommandText = sql;
                
                if (parameters != null)
                {
                    foreach (var param in parameters)
                    {
                        command.Parameters.AddWithValue(param.Key, param.Value);
                    }
                }
                
                return command.ExecuteScalar();
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"Database error in ExecuteScalar: {e.Message}\nSQL: {sql}");
            return null;
        }
    }

    /// <summary>
    /// Executes a SQL query and processes the results with the provided callback.
    /// </summary>
    public void ExecuteReader(string sql, Action<SqliteDataReader> readerCallback, Dictionary<string, object> parameters = null)
    {
        try
        {
            using (var connection = GetConnection())
            using (var command = connection.CreateCommand())
            {
                command.CommandText = sql;
                
                if (parameters != null)
                {
                    foreach (var param in parameters)
                    {
                        command.Parameters.AddWithValue(param.Key, param.Value);
                    }
                }
                
                using (var reader = command.ExecuteReader())
                {
                    readerCallback(reader);
                }
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"Database error in ExecuteReader: {e.Message}\nSQL: {sql}");
        }
    }

    /// <summary>
    /// Executes a SQL query and returns the results as a list of dictionaries.
    /// </summary>
    public List<Dictionary<string, object>> ExecuteQuery(string sql, Dictionary<string, object> parameters = null)
    {
        var results = new List<Dictionary<string, object>>();
        
        try
        {
            using (var connection = GetConnection())
            using (var command = connection.CreateCommand())
            {
                command.CommandText = sql;
                
                if (parameters != null)
                {
                    foreach (var param in parameters)
                    {
                        command.Parameters.AddWithValue(param.Key, param.Value);
                    }
                }
                
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        var row = new Dictionary<string, object>();
                        for (int i = 0; i < reader.FieldCount; i++)
                        {
                            row[reader.GetName(i)] = reader.GetValue(i);
                        }
                        results.Add(row);
                    }
                }
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"Database error in ExecuteQuery: {e.Message}\nSQL: {sql}");
        }
        
        return results;
    }

    /// <summary>
    /// Begins a transaction for multiple operations.
    /// </summary>
    public SqliteTransaction BeginTransaction()
    {
        try
        {
            var connection = GetPersistentConnection();
            return connection.BeginTransaction();
        }
        catch (Exception e)
        {
            Debug.LogError($"Error beginning transaction: {e.Message}");
            return null;
        }
    }

    /// <summary>
    /// Executes multiple SQL commands within a transaction.
    /// </summary>
    public bool ExecuteInTransaction(Action<SqliteConnection, SqliteTransaction> action)
    {
        SqliteConnection connection = null;
        SqliteTransaction transaction = null;
        
        try
        {
            connection = GetConnection();
            transaction = connection.BeginTransaction();
            
            action(connection, transaction);
            
            transaction.Commit();
            return true;
        }
        catch (Exception e)
        {
            Debug.LogError($"Transaction error: {e.Message}");
            transaction?.Rollback();
            return false;
        }
        finally
        {
            transaction?.Dispose();
            connection?.Close();
            connection?.Dispose();
        }
    }

    // Customer-specific database methods

    /// <summary>
    /// Gets all active customers from the database.
    /// </summary>
    public List<Dictionary<string, object>> GetActiveCustomers()
    {
        string sql = @"
            SELECT CustomerID, CustomerType, Budget, IsActive 
            FROM Customers 
            WHERE IsActive = 1";
            
        return ExecuteQuery(sql);
    }

    /// <summary>
    /// Gets customer preferences for a specific customer.
    /// </summary>
    public List<Dictionary<string, object>> GetCustomerPreferences(int customerId)
    {
        string sql = @"
            SELECT FishName, PreferenceScore, Rarity, HasPurchased 
            FROM CustomerPreferences 
            WHERE CustomerID = @customerId";
            
        var parameters = new Dictionary<string, object>
        {
            { "@customerId", customerId }
        };
            
        return ExecuteQuery(sql, parameters);
    }

    /// <summary>
    /// Gets customer biases for a specific customer.
    /// </summary>
    public List<Dictionary<string, object>> GetCustomerBiases(int customerId)
    {
        string sql = @"
            SELECT SellerID, Rarity, BiasValue 
            FROM CustomerBiases 
            WHERE CustomerID = @customerId";
            
        var parameters = new Dictionary<string, object>
        {
            { "@customerId", customerId }
        };
            
        return ExecuteQuery(sql, parameters);
    }

    /// <summary>
    /// Updates a customer's bias for a specific seller and rarity.
    /// </summary>
    public bool UpdateCustomerBias(int customerId, int sellerId, string rarity, float biasValue)
    {
        string sql = @"
            UPDATE CustomerBiases 
            SET BiasValue = @biasValue
            WHERE CustomerID = @customerId 
            AND SellerID = @sellerId 
            AND Rarity = @rarity";
            
        var parameters = new Dictionary<string, object>
        {
            { "@customerId", customerId },
            { "@sellerId", sellerId },
            { "@rarity", rarity },
            { "@biasValue", biasValue }
        };
            
        return ExecuteNonQuery(sql, parameters) > 0;
    }

    /// <summary>
    /// Gets all unsold market listings for a specific rarity.
    /// </summary>
    public List<Dictionary<string, object>> GetUnsoldListings(string rarity)
    {
        string sql = @"
            SELECT ListingID, FishName, ListedPrice, Rarity, SellerID 
            FROM MarketListings
            WHERE Rarity = @rarity 
            AND IsSold = 0";
            
        var parameters = new Dictionary<string, object>
        {
            { "@rarity", rarity }
        };
            
        return ExecuteQuery(sql, parameters);
    }

    /// <summary>
    /// Marks a listing as sold.
    /// </summary>
    public bool MarkListingAsSold(int listingId)
    {
        string sql = @"
            UPDATE MarketListings 
            SET IsSold = 1 
            WHERE ListingID = @listingId AND IsSold = 0";
            
        var parameters = new Dictionary<string, object>
        {
            { "@listingId", listingId }
        };
            
        return ExecuteNonQuery(sql, parameters) > 0;
    }

    /// <summary>
    /// Records a rejection reason for a listing.
    /// </summary>
    public bool RecordRejectionReason(int listingId, int customerId, string reason)
    {
        string sql = @"
            INSERT INTO ListingRejections (
                ListingID, 
                CustomerID, 
                Reason, 
                RejectionTime
            ) VALUES (
                @listingId, 
                @customerId, 
                @reason,
                DATETIME('now')
            )";
            
        var parameters = new Dictionary<string, object>
        {
            { "@listingId", listingId },
            { "@customerId", customerId },
            { "@reason", reason }
        };
            
        return ExecuteNonQuery(sql, parameters) > 0;
    }

    /// <summary>
    /// Gets historical average prices for fish of a specific rarity.
    /// </summary>
    public Dictionary<string, float> GetHistoricalAveragePrices(string rarity)
    {
        var result = new Dictionary<string, float>();
        
        string sql = @"
            SELECT mp.FishName, AVG(mp.Price) as AvgPrice
            FROM MarketPrices mp
            JOIN Fish f ON mp.FishName = f.Name
            WHERE f.Rarity = @rarity
            AND mp.Day >= (SELECT MAX(Day) - 4 FROM MarketPrices)
            GROUP BY mp.FishName";
            
        var parameters = new Dictionary<string, object>
        {
            { "@rarity", rarity }
        };
        
        ExecuteReader(sql, reader => {
            while (reader.Read())
            {
                string fishName = reader.GetString(0);
                float avgPrice = Convert.ToSingle(reader.GetDouble(1));
                result[fishName] = avgPrice;
            }
        }, parameters);
        
        return result;
    }

    /// <summary>
    /// Updates a customer's preference for a specific fish.
    /// </summary>
    public bool UpdateCustomerPreference(int customerId, string fishName, bool hasPurchased)
    {
        string sql = @"
            UPDATE CustomerPreferences 
            SET HasPurchased = @hasPurchased
            WHERE CustomerID = @customerId 
            AND FishName = @fishName";
            
        var parameters = new Dictionary<string, object>
        {
            { "@customerId", customerId },
            { "@fishName", fishName },
            { "@hasPurchased", hasPurchased }
        };

        Debug.Log($"Executing SQL: {sql}\nParameters: CustomerID={customerId}, FishName={fishName}, HasPurchased={hasPurchased}");
        int rowsAffected = ExecuteNonQuery(sql, parameters);
        Debug.Log($"UpdateCustomerPreference affected {rowsAffected} rows");
            
        return rowsAffected > 0;
    }

    /// <summary>
    /// Updates a customer's preference score for a specific fish.
    /// </summary>
    public bool UpdateCustomerPreferenceScore(int customerId, string fishName, float preferenceScore)
    {
        string sql = @"
            UPDATE CustomerPreferences 
            SET PreferenceScore = @preferenceScore
            WHERE CustomerID = @customerId 
            AND FishName = @fishName";
            
        var parameters = new Dictionary<string, object>
        {
            { "@customerId", customerId },
            { "@fishName", fishName },
            { "@preferenceScore", preferenceScore }
        };
            
        return ExecuteNonQuery(sql, parameters) > 0;
    }

    /// <summary>
    /// Gets all fish names of a specific rarity.
    /// </summary>
    public List<string> GetFishNamesByRarity(string rarity)
    {
        List<string> fishNames = new List<string>();
        
        ExecuteReader(
            "SELECT Name FROM Fish WHERE Rarity = @rarity",
            reader => {
                while (reader.Read())
                {
                    fishNames.Add(reader.GetString(0));
                }
            },
            new Dictionary<string, object> { { "@rarity", rarity } }
        );
        
        return fishNames;
    }
} 