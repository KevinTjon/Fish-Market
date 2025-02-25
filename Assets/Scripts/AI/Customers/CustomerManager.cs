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

    private List<Customer> allCustomers = new List<Customer>();

    private void Awake()
    {
        // Ensure DatabaseManager is initialized
        if (DatabaseManager.Instance == null)
        {
            Debug.LogError("DatabaseManager not found!");
        }
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
            LoadExistingCustomers();

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

    private void LoadExistingCustomers()
    {
        allCustomers.Clear();

        var customerResults = DatabaseManager.Instance.GetActiveCustomers();

        foreach (var row in customerResults)
        {
            int customerId = Convert.ToInt32(row["CustomerID"]);
            Customer.CUSTOMERTYPE type = (Customer.CUSTOMERTYPE)Convert.ToInt32(row["CustomerType"]);
            int budget = Convert.ToInt32(row["Budget"]);

            Customer customer = new Customer(type, customerId, budget);
            LoadCustomerPreferences(customer);
            LoadCustomerBiases(customer);
                    allCustomers.Add(customer);
        }
    }

    private void LoadCustomerPreferences(Customer customer)
    {
        var preferences = DatabaseManager.Instance.GetCustomerPreferences(customer.CustomerID);
        
        foreach (var pref in preferences)
        {
            customer.FishPreferences.Add(new Customer.FishPreference
            {
                FishName = pref["FishName"].ToString(),
                PreferenceScore = Convert.ToSingle(pref["PreferenceScore"]),
                Rarity = (Customer.FISHRARITY)Convert.ToInt32(pref["Rarity"]),
                HasPurchased = Convert.ToBoolean(pref["HasPurchased"])
            });
        }
    }

    private void LoadCustomerBiases(Customer customer)
    {
        var biases = DatabaseManager.Instance.GetCustomerBiases(customer.CustomerID);
        
        foreach (var bias in biases)
        {
            int sellerId = Convert.ToInt32(bias["SellerID"]);
            Customer.FISHRARITY rarity = (Customer.FISHRARITY)Enum.Parse(typeof(Customer.FISHRARITY), bias["Rarity"].ToString());
            float biasValue = Convert.ToSingle(bias["BiasValue"]);
                    
                    customer.SetBias(sellerId, rarity, biasValue);
        }
    }

    public void SaveCustomerBias(Customer customer, int sellerId, Customer.FISHRARITY rarity)
    {
        DatabaseManager.Instance.UpdateCustomerBias(
            customer.CustomerID,
            sellerId,
            rarity.ToString(),
            customer.GetBias(sellerId, rarity)
        );
    }

    public void UpdateCustomerPreference(Customer customer, string fishName, bool hasPurchased)
    {
        DatabaseManager.Instance.UpdateCustomerPreference(
            customer.CustomerID,
            fishName,
            hasPurchased
        );
    }

    public List<Customer> GetAllCustomers()
    {
        if (allCustomers.Count == 0)
        {
            LoadExistingCustomers();
        }
        return allCustomers;
    }

    /// <summary>
    /// DEPRECATED: Use GenerateInitialCustomers() instead.
    /// This method generates 30 customers based on distribution, which is too many.
    /// </summary>
    [System.Obsolete("Use GenerateInitialCustomers() instead. This method generates too many customers.")]
    public void TestGenerateInitialCustomers()
    {
        Debug.LogWarning("TestGenerateInitialCustomers is deprecated. Using GenerateInitialCustomers(5) instead.");
        GenerateInitialCustomers(5);
    }

    public void GenerateCustomersForCurrentDay(int count, Dictionary<Customer.FISHRARITY, float> rarityWeights)
    {
        if (count <= 0) return;
        
        Debug.Log($"Generating {count} new customers with weighted preferences");
        
        // Determine customer type distribution based on rarity weights
        Dictionary<Customer.CUSTOMERTYPE, float> typeWeights;
        
        // If we have rarity weights, use them to influence customer types
        if (rarityWeights != null && rarityWeights.Count > 0)
        {
            typeWeights = DetermineCustomerTypeWeights(rarityWeights);
            Debug.Log("Using dynamic customer type weights based on market conditions");
        }
        else
        {
            // Otherwise use the predefined distribution
            typeWeights = new Dictionary<Customer.CUSTOMERTYPE, float>
            {
                { Customer.CUSTOMERTYPE.BUDGET, customerDistribution[0] },
                { Customer.CUSTOMERTYPE.CASUAL, customerDistribution[1] },
                { Customer.CUSTOMERTYPE.COLLECTOR, customerDistribution[2] },
                { Customer.CUSTOMERTYPE.WEALTHY, customerDistribution[3] }
            };
            Debug.Log("Using predefined customer type distribution");
        }
        
        // Log the distribution we're using
        string distributionLog = string.Join(", ", typeWeights.Select(kv => $"{kv.Key}: {kv.Value:P0}"));
        Debug.Log($"Customer type distribution: {distributionLog}");

        // Get reference to PurchaseManager
        var purchaseManager = FindObjectOfType<CustomerPurchaseManager>();
        
        // Generate customers
        DatabaseManager.Instance.ExecuteInTransaction((connection, transaction) => {
            using (var command = connection.CreateCommand())
            {
                command.Transaction = transaction;
                
                for (int i = 0; i < count; i++)
                {
                    // Select customer type based on weights
                    Customer.CUSTOMERTYPE type = SelectWeightedCustomerType(typeWeights);
                    
                    // Create new customer
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

                    // Initialize biases for this customer
                    InitializeCustomerBiases(connection, transaction, customer);
                    
                    // Add to purchase manager
                        if (purchaseManager != null)
                        {
                            purchaseManager.AddCustomer(customer);
                    }
                    
                    // Add to our local list
                    allCustomers.Add(customer);
                }
            }
        });
        
        Debug.Log($"Successfully generated {count} new customers");
    }
    
    private Dictionary<Customer.CUSTOMERTYPE, float> DetermineCustomerTypeWeights(Dictionary<Customer.FISHRARITY, float> rarityWeights)
    {
        Dictionary<Customer.CUSTOMERTYPE, float> typeWeights = new Dictionary<Customer.CUSTOMERTYPE, float>();
        
        // Initialize with predefined weights instead of equal weights
        typeWeights[Customer.CUSTOMERTYPE.BUDGET] = customerDistribution[0];
        typeWeights[Customer.CUSTOMERTYPE.CASUAL] = customerDistribution[1];
        typeWeights[Customer.CUSTOMERTYPE.COLLECTOR] = customerDistribution[2];
        typeWeights[Customer.CUSTOMERTYPE.WEALTHY] = customerDistribution[3];
        
        // Adjust weights based on rarity distribution
        if (rarityWeights.TryGetValue(Customer.FISHRARITY.COMMON, out float commonWeight))
        {
            // Budget customers prefer common fish
            typeWeights[Customer.CUSTOMERTYPE.BUDGET] += commonWeight * 0.3f;
            typeWeights[Customer.CUSTOMERTYPE.CASUAL] += commonWeight * 0.1f;
        }
        
        if (rarityWeights.TryGetValue(Customer.FISHRARITY.UNCOMMON, out float uncommonWeight))
        {
            // Budget and Casual customers prefer uncommon fish
            typeWeights[Customer.CUSTOMERTYPE.BUDGET] += uncommonWeight * 0.1f;
            typeWeights[Customer.CUSTOMERTYPE.CASUAL] += uncommonWeight * 0.3f;
        }
        
        if (rarityWeights.TryGetValue(Customer.FISHRARITY.RARE, out float rareWeight))
        {
            // Casual and Collector customers prefer rare fish
            typeWeights[Customer.CUSTOMERTYPE.CASUAL] += rareWeight * 0.1f;
            typeWeights[Customer.CUSTOMERTYPE.COLLECTOR] += rareWeight * 0.3f;
        }
        
        if (rarityWeights.TryGetValue(Customer.FISHRARITY.EPIC, out float epicWeight))
        {
            // Collector customers prefer epic fish
            typeWeights[Customer.CUSTOMERTYPE.COLLECTOR] += epicWeight * 0.3f;
            typeWeights[Customer.CUSTOMERTYPE.WEALTHY] += epicWeight * 0.1f;
        }
        
        if (rarityWeights.TryGetValue(Customer.FISHRARITY.LEGENDARY, out float legendaryWeight))
        {
            // Wealthy customers prefer legendary fish
            typeWeights[Customer.CUSTOMERTYPE.COLLECTOR] += legendaryWeight * 0.1f;
            typeWeights[Customer.CUSTOMERTYPE.WEALTHY] += legendaryWeight * 0.3f;
        }
        
        // Normalize weights
        float totalWeight = typeWeights.Values.Sum();
        if (totalWeight > 0)
        {
            foreach (var type in typeWeights.Keys.ToList())
            {
                typeWeights[type] /= totalWeight;
            }
        }
        
        return typeWeights;
    }
    
    private Customer.CUSTOMERTYPE SelectWeightedCustomerType(Dictionary<Customer.CUSTOMERTYPE, float> typeWeights)
    {
        float roll = UnityEngine.Random.Range(0f, 1f);
        float cumulativeWeight = 0f;
        
        foreach (var kvp in typeWeights)
        {
            cumulativeWeight += kvp.Value;
            if (roll <= cumulativeWeight)
            {
                return kvp.Key;
            }
        }
        
        // Fallback to first type if something goes wrong
        return typeWeights.Keys.First();
    }

    public void GenerateInitialCustomers(int count = 5)
    {
        Debug.Log($"Generating {count} initial customers for new game");
        
        // Clear existing customers
        allCustomers.Clear();
        
        // Get reference to PurchaseManager
        var purchaseManager = FindObjectOfType<CustomerPurchaseManager>();
        if (purchaseManager != null)
        {
            purchaseManager.ClearCustomers();
        }
        
        // Calculate exact numbers for each customer type
        int budgetCount = Mathf.RoundToInt(count * customerDistribution[0]);
        int casualCount = Mathf.RoundToInt(count * customerDistribution[1]);
        int collectorCount = Mathf.RoundToInt(count * customerDistribution[2]);
        int wealthyCount = count - (budgetCount + casualCount + collectorCount); // Remainder goes to wealthy
        
        Debug.Log($"Generating exact distribution - Budget: {budgetCount}, Casual: {casualCount}, " +
                 $"Collector: {collectorCount}, Wealthy: {wealthyCount}");
        
        // Create list of customer types to generate
        List<Customer.CUSTOMERTYPE> typesToGenerate = new List<Customer.CUSTOMERTYPE>();
        for (int i = 0; i < budgetCount; i++) typesToGenerate.Add(Customer.CUSTOMERTYPE.BUDGET);
        for (int i = 0; i < casualCount; i++) typesToGenerate.Add(Customer.CUSTOMERTYPE.CASUAL);
        for (int i = 0; i < collectorCount; i++) typesToGenerate.Add(Customer.CUSTOMERTYPE.COLLECTOR);
        for (int i = 0; i < wealthyCount; i++) typesToGenerate.Add(Customer.CUSTOMERTYPE.WEALTHY);
        
        // Shuffle the list to randomize order
        for (int i = typesToGenerate.Count - 1; i > 0; i--)
        {
            int j = UnityEngine.Random.Range(0, i + 1);
            var temp = typesToGenerate[i];
            typesToGenerate[i] = typesToGenerate[j];
            typesToGenerate[j] = temp;
        }
        
        // Generate customers
        DatabaseManager.Instance.ExecuteInTransaction((connection, transaction) => {
            // First, deactivate all existing customers
            using (var command = connection.CreateCommand())
            {
                command.Transaction = transaction;
                command.CommandText = "UPDATE Customers SET IsActive = 0";
                command.ExecuteNonQuery();
                
                // Create new CustomerPreferences table if needed
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
                
                // Generate new customers using our exact distribution
                foreach (var customerType in typesToGenerate)
                {
                    // Create new customer
                    Customer customer = new Customer(customerType);
                    
                    // Insert customer into database
                    command.CommandText = @"
                        INSERT INTO Customers (CustomerType, Budget, IsActive)
                        VALUES (@type, @budget, 1);
                        SELECT last_insert_rowid();";
                    
                    command.Parameters.Clear();
                    command.Parameters.AddWithValue("@type", (int)customerType);
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
                    
                    // Initialize biases for this customer
                    InitializeCustomerBiases(connection, transaction, customer);
                    
                    // Add to purchase manager
                    if (purchaseManager != null)
                    {
                        purchaseManager.AddCustomer(customer);
                    }
                    
                    // Add to our local list
                    allCustomers.Add(customer);
                }
            }
        });
        
        Debug.Log($"Successfully generated {count} initial customers with exact distribution");
    }

    private void InitializeCustomerBiases(SqliteConnection connection, SqliteTransaction transaction, Customer customer)
    {
        using (var command = connection.CreateCommand())
        {
            command.Transaction = transaction;
            
            // First, delete any existing biases
            command.CommandText = "DELETE FROM CustomerBiases WHERE CustomerID = @customerId";
            command.Parameters.Clear();
            command.Parameters.AddWithValue("@customerId", customer.CustomerID);
            command.ExecuteNonQuery();
            
            // Initialize biases for each seller and rarity
            foreach (Customer.SellerType seller in Enum.GetValues(typeof(Customer.SellerType)))
            {
                foreach (Customer.FISHRARITY rarity in Enum.GetValues(typeof(Customer.FISHRARITY)))
                {
                    // Start with equal bias (0.2) for all sellers
                    float initialBias = 0.2f;
                    
                        command.CommandText = @"
                            INSERT INTO CustomerBiases 
                            (CustomerID, SellerID, Rarity, BiasValue)
                            VALUES 
                            (@customerId, @sellerId, @rarity, @biasValue)";
                        
                        command.Parameters.Clear();
                        command.Parameters.AddWithValue("@customerId", customer.CustomerID);
                        command.Parameters.AddWithValue("@sellerId", (int)seller);
                    command.Parameters.AddWithValue("@rarity", rarity.ToString());
                    command.Parameters.AddWithValue("@biasValue", initialBias);
                        
                        command.ExecuteNonQuery();
                    
                    // Also set in the customer object
                    customer.SetBias((int)seller, rarity, initialBias);
                }
            }
        }
    }

    private void CleanupAndInitializeBiases()
    {
        // This method ensures all customers have proper bias entries
        DatabaseManager.Instance.ExecuteInTransaction((connection, transaction) => {
            using (var command = connection.CreateCommand())
            {
                command.Transaction = transaction;
                
                // Get all active customers
                command.CommandText = "SELECT CustomerID FROM Customers WHERE IsActive = 1";
                
                List<int> customerIds = new List<int>();
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        customerIds.Add(reader.GetInt32(0));
                    }
                }
                
                // For each customer, ensure they have bias entries for all sellers and rarities
                foreach (int customerId in customerIds)
                {
                    foreach (Customer.SellerType seller in Enum.GetValues(typeof(Customer.SellerType)))
                    {
                        foreach (Customer.FISHRARITY rarity in Enum.GetValues(typeof(Customer.FISHRARITY)))
                        {
                            // Check if bias entry exists
                            command.CommandText = @"
                                SELECT COUNT(*) FROM CustomerBiases 
                                WHERE CustomerID = @customerId 
                                AND SellerID = @sellerId 
                                AND Rarity = @rarity";
                            
                            command.Parameters.Clear();
                            command.Parameters.AddWithValue("@customerId", customerId);
                            command.Parameters.AddWithValue("@sellerId", (int)seller);
                            command.Parameters.AddWithValue("@rarity", rarity.ToString());
                            
                            int count = Convert.ToInt32(command.ExecuteScalar());
                            
                            // If no entry exists, create one with default bias
                            if (count == 0)
                            {
                                command.CommandText = @"
                                    INSERT INTO CustomerBiases 
                                    (CustomerID, SellerID, Rarity, BiasValue)
                                    VALUES 
                                    (@customerId, @sellerId, @rarity, @biasValue)";
                                
                                command.Parameters.Clear();
                                command.Parameters.AddWithValue("@customerId", customerId);
                                command.Parameters.AddWithValue("@sellerId", (int)seller);
                                command.Parameters.AddWithValue("@rarity", rarity.ToString());
                                command.Parameters.AddWithValue("@biasValue", 0.2f); // Default bias
                
                command.ExecuteNonQuery();
            }
        }
                    }
                }
            }
        });
    }
}