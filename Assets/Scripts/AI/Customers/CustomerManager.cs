using UnityEngine;
using System.Collections.Generic;
using Mono.Data.Sqlite;
using System;
using System.Linq;
using TMPro;

public class CustomerManager : MonoBehaviour
{
    [Header("Customer Generation Settings")]

    [SerializeField] private float[] customerDistribution = new float[4] {
        0.40f,  // Budget - Increased from 0.35f
        0.35f,  // Casual - Increased from 0.30f
        0.15f,  // Collector - Decreased from 0.25f
        0.10f   // Wealthy - Unchanged
    };

    [Header("Debug")]
    [SerializeField] private bool showDebugInfo = true;

    private string dbPath;
    private List<Customer> allCustomers = new List<Customer>();

    private void Awake()
    {
        // For Unity, use Application.streamingAssetsPath
        string dbName = "FishDB.db";
        string dbPath = System.IO.Path.Combine(Application.streamingAssetsPath, dbName);
        
        // For Unity Editor and standalone builds
        this.dbPath = $"URI=file:{dbPath}";
        
        Debug.Log($"Database path: {this.dbPath}");
    }

    void Start()
    {
        // ... existing code ...
        CleanupAndInitializeBiases();
        // ... rest of initialization ...
    }

    public void LoadCustomers(TextMeshProUGUI outputText = null)
    {
        try
        {
            string output = "Loading Customers:\n";
            using (var connection = new SqliteConnection(dbPath))
            {
                connection.Open();
                LoadExistingCustomers(connection);
            }

            if (outputText != null)
            {
                outputText.text = output;
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"Error loading customers: {e.Message}");
            if (outputText != null)
            {
                outputText.text = $"Error loading customers: {e.Message}";
            }
        }
    }

    private void LoadExistingCustomers(SqliteConnection connection)
    {
        allCustomers.Clear();

        using (var command = connection.CreateCommand())
        {
            command.CommandText = @"
                SELECT CustomerID, CustomerType, Budget, IsActive 
                FROM Customers 
                WHERE IsActive = 1";

            using (var reader = command.ExecuteReader())
            {
                while (reader.Read())
                {
                    int customerId = reader.GetInt32(0);
                    Customer.CUSTOMERTYPE type = (Customer.CUSTOMERTYPE)reader.GetInt32(1);
                    int budget = reader.GetInt32(2);

                    Customer customer = new Customer(type, dbPath, customerId, budget);
                    LoadCustomerPreferences(connection, customer);
                    LoadCustomerBiases(connection, customer);
                    allCustomers.Add(customer);
                }
            }
        }
    }

    private void LoadCustomerPreferences(SqliteConnection connection, Customer customer)
    {
        using (var command = connection.CreateCommand())
        {
            command.CommandText = @"
                SELECT FishName, PreferenceScore, Rarity, HasPurchased 
                FROM CustomerPreferences 
                WHERE CustomerID = @customerId";
            
            command.Parameters.AddWithValue("@customerId", customer.CustomerID);

            using (var reader = command.ExecuteReader())
            {
                while (reader.Read())
                {
                    customer.FishPreferences.Add(new Customer.FishPreference
                    {
                        FishName = reader.GetString(0),
                        PreferenceScore = reader.GetFloat(1),
                        Rarity = (Customer.FISHRARITY)reader.GetInt32(2),
                        HasPurchased = reader.GetBoolean(3)
                    });
                }
            }
        }
    }

    private void LoadCustomerBiases(SqliteConnection connection, Customer customer)
    {
        using (var command = connection.CreateCommand())
        {
            command.CommandText = @"
                SELECT SellerID, Rarity, BiasValue 
                FROM CustomerBiases 
                WHERE CustomerID = @customerId";
            
            command.Parameters.AddWithValue("@customerId", customer.CustomerID);

            using (var reader = command.ExecuteReader())
            {
                while (reader.Read())
                {
                    int sellerId = reader.GetInt32(0);
                    Customer.FISHRARITY rarity = (Customer.FISHRARITY)Enum.Parse(typeof(Customer.FISHRARITY), reader.GetString(1));
                    float biasValue = reader.GetFloat(2);
                    
                    customer.SetBias(sellerId, rarity, biasValue);
                }
            }
        }
    }

    public void SaveCustomerBias(Customer customer, int sellerId, Customer.FISHRARITY rarity)
    {
        using (var connection = new SqliteConnection(dbPath))
        {
            connection.Open();
            using (var command = connection.CreateCommand())
            {
                command.CommandText = @"
                    UPDATE CustomerBiases 
                    SET BiasValue = @biasValue
                    WHERE CustomerID = @customerId 
                    AND SellerID = @sellerId 
                    AND Rarity = @rarity";
                
                command.Parameters.AddWithValue("@customerId", customer.CustomerID);
                command.Parameters.AddWithValue("@sellerId", sellerId);
                command.Parameters.AddWithValue("@rarity", rarity.ToString());
                command.Parameters.AddWithValue("@biasValue", customer.GetBias(sellerId, rarity));
                
                command.ExecuteNonQuery();
            }
        }
    }

    public List<Customer> GetAllCustomers()
    {
        if (allCustomers.Count == 0)
        {
            using (var connection = new SqliteConnection(dbPath))
            {
                connection.Open();
                LoadExistingCustomers(connection);
            }
        }
        return allCustomers;
    }

    public void TestGenerateInitialCustomers()
    {
        // Clear existing customers
        allCustomers.Clear();

        // Get reference to PurchaseManager
        var purchaseManager = FindObjectOfType<CustomerPurchaseManager>();
        if (purchaseManager != null)
        {
            purchaseManager.ClearCustomers();  // Clear existing customers from purchase manager
        }

        // Generate new customers based on distribution
        using (var connection = new SqliteConnection(dbPath))
        {
            connection.Open();
            using (var command = connection.CreateCommand())
            {
                // First, deactivate all existing customers
                command.CommandText = "UPDATE Customers SET IsActive = 0";
                command.ExecuteNonQuery();

                // Create new CustomerPreferences table
                command.CommandText = @"
                    DROP TABLE IF EXISTS CustomerPreferences;
                    CREATE TABLE CustomerPreferences (
                        CustomerID INTEGER,
                        FishName TEXT,
                        PreferenceScore REAL,
                        Rarity INTEGER,
                        HasPurchased BOOLEAN,
                        PRIMARY KEY (CustomerID, FishName),
                        FOREIGN KEY (CustomerID) REFERENCES Customers(CustomerID)
                    );";
                command.ExecuteNonQuery();

                // Generate new customers with preferences
                for (int i = 0; i < customerDistribution.Length; i++)
                {
                    int count = Mathf.RoundToInt(30 * customerDistribution[i]); // 30 total customers
                    for (int j = 0; j < count; j++)
                    {
                        Customer.CUSTOMERTYPE type = (Customer.CUSTOMERTYPE)i;
                        Customer customer = new Customer(type, dbPath);

                        // Insert customer into database
                        command.CommandText = @"
                            INSERT INTO Customers (CustomerType, Budget, IsActive)
                            VALUES (@type, @budget, 1);
                            SELECT last_insert_rowid();";
                        
                        command.Parameters.Clear();
                        command.Parameters.AddWithValue("@type", (int)type);
                        command.Parameters.AddWithValue("@budget", customer.Budget);

                        // Get the new customer ID
                        customer.CustomerID = Convert.ToInt32(command.ExecuteScalar());

                        // Insert preferences
                        foreach (var preference in customer.FishPreferences)
                        {
                            command.CommandText = @"
                                INSERT INTO CustomerPreferences 
                                (CustomerID, FishName, PreferenceScore, Rarity, HasPurchased)
                                VALUES 
                                (@customerId, @fishName, @preferenceScore, @rarity, @hasPurchased)";
                            
                            command.Parameters.Clear();
                            command.Parameters.AddWithValue("@customerId", customer.CustomerID);
                            command.Parameters.AddWithValue("@fishName", preference.FishName);
                            command.Parameters.AddWithValue("@preferenceScore", preference.PreferenceScore);
                            command.Parameters.AddWithValue("@rarity", (int)preference.Rarity);
                            command.Parameters.AddWithValue("@hasPurchased", preference.HasPurchased);
                            
                            command.ExecuteNonQuery();
                        }

                        // Generate and insert biases for each seller type and fish rarity
                        foreach (Customer.SellerType seller in System.Enum.GetValues(typeof(Customer.SellerType)))
                        {
                            foreach (Customer.FISHRARITY rarity in System.Enum.GetValues(typeof(Customer.FISHRARITY)))
                            {
                                float bias = 0.2f; // Equal initial bias
                                customer.SetBias((int)seller, rarity, bias);

                                command.CommandText = @"
                                    INSERT INTO CustomerBiases 
                                    (CustomerID, SellerID, Rarity, BiasValue)
                                    VALUES 
                                    (@customerId, @sellerId, @rarity, @biasValue)";
                                
                                command.Parameters.Clear();
                                command.Parameters.AddWithValue("@customerId", customer.CustomerID);
                                command.Parameters.AddWithValue("@sellerId", (int)seller);
                                command.Parameters.AddWithValue("@rarity", rarity.ToString());
                                command.Parameters.AddWithValue("@biasValue", bias);
                                
                                command.ExecuteNonQuery();
                            }
                        }

                        allCustomers.Add(customer);
                        
                        // Add customer to purchase manager
                        if (purchaseManager != null)
                        {
                            purchaseManager.AddCustomer(customer);
                        }
                    }
                }
            }
        }

        if (showDebugInfo)
        {
            Debug.Log($"Generated {allCustomers.Count} new customers with preferences and biases");
        }
    }

    public void CleanupAndInitializeBiases()
    {
        using (var connection = new SqliteConnection(dbPath))
        {
            connection.Open();
            using (var command = connection.CreateCommand())
            {
                // Drop and recreate the table
                command.CommandText = @"
                    DROP TABLE IF EXISTS CustomerBiases;
                    CREATE TABLE CustomerBiases (
                        CustomerID INTEGER,
                        SellerID INTEGER,
                        Rarity TEXT NOT NULL,  -- Make it NOT NULL to prevent empty entries
                        BiasValue REAL,
                        PRIMARY KEY (CustomerID, SellerID, Rarity)
                    );";
                command.ExecuteNonQuery();
            }
        }
    }

    private void InitializeCustomerBiases(Customer customer)
    {
        using (var connection = new SqliteConnection(dbPath))
        {
            connection.Open();
            using (var command = connection.CreateCommand())
            {
                // Initialize biases for each seller and rarity combination
                foreach (Customer.SellerType seller in Enum.GetValues(typeof(Customer.SellerType)))
                {
                    foreach (Customer.FISHRARITY rarity in Enum.GetValues(typeof(Customer.FISHRARITY)))
                    {
                        command.CommandText = @"
                            INSERT INTO CustomerBiases 
                            (CustomerID, SellerID, Rarity, BiasValue)
                            VALUES 
                            (@customerId, @sellerId, @rarity, @biasValue)";
                        
                        command.Parameters.Clear();
                        command.Parameters.AddWithValue("@customerId", customer.CustomerID);
                        command.Parameters.AddWithValue("@sellerId", (int)seller);
                        command.Parameters.AddWithValue("@rarity", rarity.ToString());  // Store as string
                        command.Parameters.AddWithValue("@biasValue", 0.2f);  // Initial equal bias
                        
                        command.ExecuteNonQuery();
                    }
                }
            }
        }
    }
}