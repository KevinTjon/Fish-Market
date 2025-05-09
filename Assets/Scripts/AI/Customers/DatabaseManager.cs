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
    private int dbInitAttempts = 0;
    private const int MaxDbInitAttempts = 3;

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
        dbInitAttempts++;
        if (dbInitAttempts > MaxDbInitAttempts)
        {
            Debug.LogError($"Database initialization failed more than {MaxDbInitAttempts} times. Aborting further attempts.");
            try { System.IO.File.AppendAllText("C:/temp/unity_debug.txt", $"Database initialization failed more than {MaxDbInitAttempts} times. Aborting.\n"); } catch { }
            return;
        }
        try
        {
            string dbName = "FishDB.db";
            string dbFilePath = System.IO.Path.Combine(Application.streamingAssetsPath, dbName);
            dbPath = $"URI=file:{dbFilePath}";
            
            // Test the connection
            using (var connection = new SqliteConnection(dbPath))
            {
                connection.Open();
                //Debug.Log("Database connection successful: " + dbPath);

                using (var command = connection.CreateCommand())
                {
                    // Check if Fish table exists
                    command.CommandText = @"
                        SELECT name FROM sqlite_master 
                        WHERE type='table' AND name='Fish'";
                    var result = command.ExecuteScalar();
                    
                    if (result == null)
                    {
                        Debug.LogError("Fish table does not exist in the database!");
                    }
                    else
                    {
                        // Check if Fish table has any data
                        command.CommandText = "SELECT COUNT(*) FROM Fish";
                        int count = Convert.ToInt32(command.ExecuteScalar());
                        //Debug.Log($"Fish table exists and contains {count} records");

                        if (count == 0)
                        {
                            Debug.LogWarning("Fish table exists but contains no data!");
                        }
                        else
                        {
                            // Log some sample data
                            command.CommandText = "SELECT Name, Rarity FROM Fish LIMIT 5";
                            using (var reader = command.ExecuteReader())
                            {
                                //Debug.Log("Sample fish in database:");
                                while (reader.Read())
                                {
                                    string name = reader.GetString(0);
                                    string fishRarity = reader.GetString(1);
                                    //Debug.Log($"Fish: {name}, Rarity: {fishRarity}");
                                }
                            }
                        }
                    }
                }

                // Check MarketListings table in a separate command
                using (var command = connection.CreateCommand())
                {
                    // Check if MarketListings table exists
                    command.CommandText = @"
                        SELECT name FROM sqlite_master 
                        WHERE type='table' AND name='MarketListings'";
                    var result = command.ExecuteScalar();
                    
                    if (result == null)
                    {
                        Debug.LogError("MarketListings table does not exist in the database!");
                    }
                    else
                    {
                        // Check if MarketListings table has any data
                        command.CommandText = "SELECT COUNT(*) FROM MarketListings WHERE IsSold = 0";
                        int unsoldCount = Convert.ToInt32(command.ExecuteScalar());
                        //Debug.Log($"MarketListings table exists and contains {unsoldCount} unsold listings");

                        if (unsoldCount == 0)
                        {
                            Debug.LogWarning("MarketListings table exists but contains no unsold listings!");
                        }
                        else
                        {
                            // Log some sample data
                            command.CommandText = @"
                                SELECT ListingID, FishName, ListedPrice, Rarity, SellerID 
                                FROM MarketListings 
                                WHERE IsSold = 0 
                                LIMIT 5";
                            using (var reader = command.ExecuteReader())
                            {
                                //Debug.Log("Sample unsold listings in database:");
                                while (reader.Read())
                                {
                                    int listingId = reader.GetInt32(0);
                                    string fishName = reader.GetString(1);
                                    float price = reader.GetFloat(2);
                                    string rarity = reader.GetString(3);
                                    int sellerId = reader.GetInt32(4);
                                    //Debug.Log($"Listing {listingId}: {fishName} (Rarity: {rarity}) - Price: {price}, Seller: {sellerId}");
                                }
                            }
                        }
                    }
                }

                // Create SellerEarnings table if it doesn't exist
                using (var command = connection.CreateCommand())
                {
                    command.CommandText = @"
                        CREATE TABLE IF NOT EXISTS SellerEarnings (
                            SellerID INTEGER PRIMARY KEY,
                            Gold REAL DEFAULT 0,
                            PreviousGold REAL DEFAULT 0
                        )";
                    command.ExecuteNonQuery();
                }
            }
            
            // Ensure all sellers (player and AI) have a row in SellerEarnings
            for (int sellerId = 0; sellerId <= 4; sellerId++)
            {
                SetSellerGold(sellerId, 0); // Set initial gold to 0
                SetSellerPreviousGold(sellerId, 0); // Always set previous day to 0 on init
            }
            isInitialized = true;
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to initialize database: {e.Message}");
            try { System.IO.File.AppendAllText("C:/temp/unity_debug.txt", $"Failed to initialize database: {e.Message}\n"); } catch { }
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
            if (!isInitialized)
            {
                Debug.LogError("Database is not initialized after retry. Returning null connection.");
                try { System.IO.File.AppendAllText("C:/temp/unity_debug.txt", "Database is not initialized after retry. Returning null connection.\n"); } catch { }
                return null;
            }
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
        // Keep rarity in uppercase to match database
        string upperRarity = rarity.ToUpper();
       // Debug.Log($"Getting unsold listings for rarity: {upperRarity}");

        return ExecuteQuery(@"
            SELECT ListingID, FishName, ListedPrice, Rarity, SellerID
            FROM MarketListings
            WHERE Rarity = @rarity
            AND IsSold = 0",
            new Dictionary<string, object> { { "@rarity", upperRarity } }
        );
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
        // Convert rarity to title case (e.g., "RARE" -> "Rare")
        string titleCaseRarity = System.Globalization.CultureInfo.CurrentCulture.TextInfo.ToTitleCase(rarity.ToLower());
        Debug.Log($"Getting historical prices for rarity: {titleCaseRarity}");

        Dictionary<string, float> prices = new Dictionary<string, float>();

        ExecuteReader(@"
            SELECT f.Name, AVG(mp.Price) as AvgPrice
            FROM Fish f
            LEFT JOIN MarketPrices mp ON f.Name = mp.FishName
            WHERE f.Rarity = @rarity
            GROUP BY f.Name",
            reader =>
            {
                while (reader.Read())
                {
                    string fishName = reader.GetString(0);
                    float avgPrice = reader.IsDBNull(1) ? 0f : (float)reader.GetDouble(1);
                    prices[fishName] = avgPrice;
                }
            },
            new Dictionary<string, object> { { "@rarity", titleCaseRarity } }
        );

        return prices;
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
        //Debug.Log($"Getting fish names for rarity: {rarity}");
        List<string> fishNames = new List<string>();

        // Convert rarity to title case (e.g., "RARE" -> "Rare")
        string titleCaseRarity = System.Globalization.CultureInfo.CurrentCulture.TextInfo.ToTitleCase(rarity.ToLower());
        //Debug.Log($"Converted rarity to title case: {titleCaseRarity}");

        ExecuteReader(
            "SELECT Name FROM Fish WHERE Rarity = @rarity",
            reader =>
            {
                while (reader.Read())
                {
                    fishNames.Add(reader.GetString(0));
                }
            },
            new Dictionary<string, object> { { "@rarity", titleCaseRarity } }
        );

        //Debug.Log($"Found {fishNames.Count} fish for rarity {titleCaseRarity}");
        return fishNames;
    }

    // Seller earnings methods
    public void SetSellerGold(int sellerId, float gold)
    {
        string sql = @"
            INSERT INTO SellerEarnings (SellerID, Gold)
            VALUES (@sellerId, @gold)
            ON CONFLICT(SellerID) DO UPDATE SET Gold = @gold";
        var parameters = new Dictionary<string, object> { { "@sellerId", sellerId }, { "@gold", gold } };
        ExecuteNonQuery(sql, parameters);
    }

    public void SetSellerPreviousGold(int sellerId, float value)
    {
        string sql = "UPDATE SellerEarnings SET PreviousGold = @value WHERE SellerID = @sellerId";
        var parameters = new Dictionary<string, object> { { "@sellerId", sellerId }, { "@value", value } };
        ExecuteNonQuery(sql, parameters);
    }
} 