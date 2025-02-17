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

                    Customer customer = new Customer(type, customerId, budget);
                    LoadCustomerShoppingList(connection, customer);
                    LoadCustomerBiases(connection, customer);
                    allCustomers.Add(customer);
                }
            }
        }
    }

    private void LoadCustomerShoppingList(SqliteConnection connection, Customer customer)
    {
        using (var command = connection.CreateCommand())
        {
            command.CommandText = @"
                SELECT Rarity, Amount 
                FROM CustomerShoppingList 
                WHERE CustomerID = @customerId";
            
            command.Parameters.AddWithValue("@customerId", customer.CustomerID);

            using (var reader = command.ExecuteReader())
            {
                while (reader.Read())
                {
                    Customer.FISHRARITY rarity = (Customer.FISHRARITY)reader.GetInt32(0);
                    customer.ShoppingList.Add(new Customer.ShoppingListItem
                    {
                        Rarity = rarity,
                        Amount = reader.GetInt32(1)
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
                    Customer.FISHRARITY rarity = (Customer.FISHRARITY)reader.GetInt32(1);
                    float biasValue = reader.GetFloat(2);
                    
                    customer.SetBias(sellerId, rarity, biasValue);
                }
            }
        }
    }

    public void SaveCustomerBias(Customer customer, int sellerId, Customer.FISHRARITY rarity)
    {
        try
        {
            using (var connection = new SqliteConnection(dbPath))
            {
                connection.Open();
                using (var command = connection.CreateCommand())
                {
                    command.CommandText = @"
                        INSERT OR REPLACE INTO CustomerBiases (CustomerID, SellerID, Rarity, BiasValue)
                        VALUES (@customerId, @sellerId, @rarity, @biasValue)";
                    
                    command.Parameters.AddWithValue("@customerId", customer.CustomerID);
                    command.Parameters.AddWithValue("@sellerId", sellerId);
                    command.Parameters.AddWithValue("@rarity", rarity.ToString());
                    command.Parameters.AddWithValue("@biasValue", customer.GetBias(sellerId, rarity));
                    
                    command.ExecuteNonQuery();
                }
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"Error saving customer bias: {e.Message}");
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

                // Generate new customers with more varied initial biases
                for (int i = 0; i < customerDistribution.Length; i++)
                {
                    int count = Mathf.RoundToInt(30 * customerDistribution[i]); // Increased from 20 to 30 total customers
                    for (int j = 0; j < count; j++)
                    {
                        Customer.CUSTOMERTYPE type = (Customer.CUSTOMERTYPE)i;
                        Customer customer = new Customer(type);

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

                        // Insert shopping list items
                        foreach (var item in customer.ShoppingList)
                        {
                            command.CommandText = @"
                                INSERT INTO CustomerShoppingList (CustomerID, Rarity, Amount)
                                VALUES (@customerId, @rarity, @amount)";
                            
                            command.Parameters.Clear();
                            command.Parameters.AddWithValue("@customerId", customer.CustomerID);
                            command.Parameters.AddWithValue("@rarity", (int)item.Rarity);
                            command.Parameters.AddWithValue("@amount", item.Amount);
                            
                            command.ExecuteNonQuery();
                        }

                        // Generate and insert biases for each seller type and fish rarity
                        foreach (Customer.SellerType seller in System.Enum.GetValues(typeof(Customer.SellerType)))
                        {
                            foreach (Customer.FISHRARITY rarity in System.Enum.GetValues(typeof(Customer.FISHRARITY)))
                            {
                                // Generate a random bias between 0.2 and 0.8
                                float bias = UnityEngine.Random.Range(0.2f, 0.8f);
                                customer.SetBias((int)seller, rarity, bias);

                                command.CommandText = @"
                                    INSERT INTO CustomerBiases (CustomerID, SellerID, Rarity, BiasValue)
                                    VALUES (@customerId, @sellerId, @rarity, @biasValue)";
                                
                                command.Parameters.Clear();
                                command.Parameters.AddWithValue("@customerId", customer.CustomerID);
                                command.Parameters.AddWithValue("@sellerId", (int)seller);
                                command.Parameters.AddWithValue("@rarity", (int)rarity);
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
            Debug.Log($"Generated {allCustomers.Count} new customers with biases");
        }
    }
}