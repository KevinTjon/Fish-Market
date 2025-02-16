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
        0.4f,  // Budget
        0.3f,  // Casual
        0.2f,  // Collector
        0.1f   // Wealthy
    };

    [Header("Debug")]
    [SerializeField] private bool showDebugInfo = true;

    private string dbPath;
    private List<Customer> allCustomers = new List<Customer>();

    private void Awake()
    {
        dbPath = "URI=file:" + Application.dataPath + "/StreamingAssets/FishDB.db";
    }

    public void LoadCustomers(TextMeshProUGUI outputText = null)
    {
        string output = "Loading Customers:\n";
        using (var connection = new SqliteConnection(dbPath))
        {
            connection.Open();
            output += "Database opened successfully\n";

            LoadExistingCustomers(connection);
            
            output += $"\nLoaded {allCustomers.Count} customers:\n";
            foreach (var customer in allCustomers)
            {
                output += $"Customer {customer.CustomerID} (Type: {customer.Type})\n";
                output += "Shopping List:\n";
                foreach (var item in customer.ShoppingList)
                {
                    output += $"- Needs {item.Amount} {item.Rarity} fish\n";
                }
                output += "Seller Biases:\n";
                foreach (var bias in customer.GetBiases())
                {
                    output += $"- Seller {bias.sellerId} for {bias.rarity}: {bias.value:F2}\n";
                }
                output += "------------------------\n";
            }
        }

        if (outputText != null)
        {
            outputText.text = output;
        }
        else
        {
            Debug.Log(output);
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
                    int rarityInt = reader.GetInt32(0);
                    Customer.FISHRARITY rarity = (Customer.FISHRARITY)rarityInt;
                    
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
                    int rarityInt = reader.GetInt32(1);
                    Customer.FISHRARITY rarity = (Customer.FISHRARITY)rarityInt;
                    float biasValue = reader.GetFloat(2);
                    
                    customer.SetBias(sellerId, rarity, biasValue);
                }
            }
        }
    }

    private void GenerateInitialCustomers()
    {
        int totalCustomers = 10;
        List<Customer.CUSTOMERTYPE> requiredTypes = new List<Customer.CUSTOMERTYPE> 
        { 
            Customer.CUSTOMERTYPE.WEALTHY,
            Customer.CUSTOMERTYPE.COLLECTOR 
        };

        using (var connection = new SqliteConnection(dbPath))
        {
            connection.Open();
            using (var transaction = connection.BeginTransaction())
            {
                try
                {
                    using (var command = connection.CreateCommand())
                    {
                        // First, generate required customer types
                        foreach (var type in requiredTypes)
                        {
                            Customer customer = new Customer(type);
                            
                            command.CommandText = @"
                                INSERT INTO Customers (CustomerType, Budget, IsActive) 
                                VALUES (@type, @budget, 1)";
                            command.Parameters.Clear();
                            command.Parameters.AddWithValue("@type", (int)type);
                            command.Parameters.AddWithValue("@budget", customer.Budget);
                            command.ExecuteNonQuery();

                            command.CommandText = "SELECT last_insert_rowid()";
                            long customerId = Convert.ToInt64(command.ExecuteScalar());
                            customer.CustomerID = (int)customerId;

                            // Insert shopping list and biases
                            InsertCustomerDetails(command, customer);
                            allCustomers.Add(customer);
                        }

                        // Then generate remaining random customers
                        int remainingCustomers = totalCustomers - requiredTypes.Count;
                        for (int i = 0; i < remainingCustomers; i++)
                        {
                            Customer.CUSTOMERTYPE type = DetermineCustomerType(customerDistribution);
                            Customer customer = new Customer(type);

                            command.CommandText = @"
                                INSERT INTO Customers (CustomerType, Budget, IsActive) 
                                VALUES (@type, @budget, 1)";
                            command.Parameters.Clear();
                            command.Parameters.AddWithValue("@type", (int)type);
                            command.Parameters.AddWithValue("@budget", customer.Budget);
                            command.ExecuteNonQuery();

                            command.CommandText = "SELECT last_insert_rowid()";
                            long customerId = Convert.ToInt64(command.ExecuteScalar());
                            customer.CustomerID = (int)customerId;

                            // Insert shopping list and biases
                            InsertCustomerDetails(command, customer);
                            allCustomers.Add(customer);
                        }
                    }
                    transaction.Commit();
                }
                catch (Exception e)
                {
                    transaction.Rollback();
                    Debug.LogError($"Error generating initial customers: {e.Message}");
                }
            }
        }
    }

    private void InsertCustomerDetails(SqliteCommand command, Customer customer)
    {
        // Insert shopping list
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

        // Add initial biases for all seller and rarity combinations
        foreach (Customer.SellerType seller in System.Enum.GetValues(typeof(Customer.SellerType)))
        {
            foreach (Customer.FISHRARITY rarity in System.Enum.GetValues(typeof(Customer.FISHRARITY)))
            {
                command.CommandText = @"
                    INSERT INTO CustomerBiases (CustomerID, SellerID, Rarity, BiasValue)
                    VALUES (@customerId, @sellerId, @rarity, @biasValue)";
                command.Parameters.Clear();
                command.Parameters.AddWithValue("@customerId", customer.CustomerID);
                command.Parameters.AddWithValue("@sellerId", (int)seller);
                command.Parameters.AddWithValue("@rarity", (int)rarity);
                command.Parameters.AddWithValue("@biasValue", 0.2f);
                command.ExecuteNonQuery();
            }
        }
    }

    private Customer.CUSTOMERTYPE DetermineCustomerType(float[] distribution)
    {
        float roll = UnityEngine.Random.value;
        float cumulative = 0f;
        
        for (int i = 0; i < distribution.Length; i++)
        {
            cumulative += distribution[i];
            if (roll <= cumulative)
                return (Customer.CUSTOMERTYPE)i;
        }
        
        return Customer.CUSTOMERTYPE.BUDGET;
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

    [ContextMenu("Test Generate Initial Customers")]
    public void TestGenerateInitialCustomers()
    {
        Debug.Log("Starting customer generation test...");
        
        try
        {
            using (var connection = new SqliteConnection(dbPath))
            {
                connection.Open();
                
                using (var command = connection.CreateCommand())
                {
                    command.CommandText = "SELECT COUNT(*) FROM Customers WHERE IsActive = 1";
                    int existingCount = Convert.ToInt32(command.ExecuteScalar());
                    Debug.Log($"Current active customers: {existingCount}");

                    if (existingCount > 0)
                    {
                        Debug.Log("Customers already exist. Loading existing customers...");
                        LoadExistingCustomers(connection);
                    }
                    else
                    {
                        Debug.Log("No existing customers found. Generating initial customers...");
                        GenerateInitialCustomers();
                    }
                }
            }

            if (showDebugInfo)
            {
                Debug.Log($"Total customers after operation: {allCustomers.Count}");
                foreach (var customer in allCustomers.Take(5))
                {
                    Debug.Log($"Sample customer: {customer}");
                }
                if (allCustomers.Count > 5)
                {
                    Debug.Log("... (more customers exist)");
                }
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"Error during customer generation test: {e.Message}");
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
}