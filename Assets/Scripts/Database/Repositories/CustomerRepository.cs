using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;
using Mono.Data.Sqlite;

public class CustomerRepository
{
    private static CustomerRepository _instance;
    public static CustomerRepository Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = new CustomerRepository();
            }
            return _instance;
        }
    }

    private DatabaseManager DbManager => DatabaseManager.Instance;

    // Cache for customer data
    private Dictionary<int, Customer> customerCache = new Dictionary<int, Customer>();
    private Dictionary<int, Dictionary<string, Customer.FishPreference>> preferencesCache = new Dictionary<int, Dictionary<string, Customer.FishPreference>>();
    private Dictionary<int, Dictionary<(int sellerId, Customer.FISHRARITY rarity), float>> biasCache = new Dictionary<int, Dictionary<(int sellerId, Customer.FISHRARITY rarity), float>>();

    public List<Customer> GetActiveCustomers()
    {
        List<Customer> customers = new List<Customer>();
        string sql = @"
            SELECT CustomerID, CustomerType, Budget 
            FROM Customers 
            WHERE IsActive = 1";

        DbManager.ExecuteReader(sql, reader =>
        {
            while (reader.Read())
            {
                int customerId = reader.GetInt32(0);
                
                // Check cache first
                if (customerCache.TryGetValue(customerId, out Customer cachedCustomer))
                {
                    customers.Add(cachedCustomer);
                    continue;
                }

                // Create new customer if not in cache
                Customer.CUSTOMERTYPE type = (Customer.CUSTOMERTYPE)reader.GetInt32(1);
                int budget = reader.GetInt32(2);
                
                Customer customer = new Customer(type, customerId, budget);
                LoadCustomerPreferences(customer);
                LoadCustomerBiases(customer);
                
                // Add to cache
                customerCache[customerId] = customer;
                customers.Add(customer);
            }
        });

        return customers;
    }

    public void LoadCustomerPreferences(Customer customer)
    {
        // Check cache first
        if (preferencesCache.TryGetValue(customer.CustomerID, out var cachedPreferences))
        {
            customer.FishPreferences.Clear();
            customer.FishPreferences.AddRange(cachedPreferences.Values);
            return;
        }

        string sql = @"
            SELECT FishName, PreferenceScore, Rarity, HasPurchased 
            FROM CustomerPreferences 
            WHERE CustomerID = @customerId";

        var parameters = new Dictionary<string, object> { { "@customerId", customer.CustomerID } };
        var preferences = new Dictionary<string, Customer.FishPreference>();

        DbManager.ExecuteReader(sql, reader =>
        {
            while (reader.Read())
            {
                var preference = new Customer.FishPreference
                {
                    FishName = reader.GetString(0),
                    PreferenceScore = reader.GetFloat(1),
                    Rarity = (Customer.FISHRARITY)reader.GetInt32(2),
                    HasPurchased = reader.GetBoolean(3)
                };
                preferences[preference.FishName] = preference;
                customer.FishPreferences.Add(preference);
            }
        }, parameters);

        // Add to cache
        preferencesCache[customer.CustomerID] = preferences;
    }

    public void LoadCustomerBiases(Customer customer)
    {
        // Check cache first
        if (biasCache.TryGetValue(customer.CustomerID, out var cachedBiases))
        {
            foreach (var kvp in cachedBiases)
            {
                customer.SetBias(kvp.Key.sellerId, kvp.Key.rarity, kvp.Value);
            }
            return;
        }

        string sql = @"
            SELECT SellerID, Rarity, BiasValue 
            FROM CustomerBiases 
            WHERE CustomerID = @customerId";

        var parameters = new Dictionary<string, object> { { "@customerId", customer.CustomerID } };
        var biases = new Dictionary<(int sellerId, Customer.FISHRARITY rarity), float>();

        DbManager.ExecuteReader(sql, reader =>
        {
            while (reader.Read())
            {
                int sellerId = reader.GetInt32(0);
                Customer.FISHRARITY rarity = (Customer.FISHRARITY)Enum.Parse(typeof(Customer.FISHRARITY), reader.GetString(1));
                float biasValue = reader.GetFloat(2);
                
                biases[(sellerId, rarity)] = biasValue;
                customer.SetBias(sellerId, rarity, biasValue);
            }
        }, parameters);

        // Add to cache
        biasCache[customer.CustomerID] = biases;
    }

    public void UpdateCustomerBias(int customerId, int sellerId, string rarity, float biasValue)
    {
        string sql = @"
            INSERT OR REPLACE INTO CustomerBiases 
            (CustomerID, SellerID, Rarity, BiasValue) 
            VALUES 
            (@customerId, @sellerId, @rarity, @biasValue)";

        var parameters = new Dictionary<string, object>
        {
            { "@customerId", customerId },
            { "@sellerId", sellerId },
            { "@rarity", rarity },
            { "@biasValue", biasValue }
        };

        DbManager.ExecuteNonQuery(sql, parameters);

        // Update cache
        if (biasCache.TryGetValue(customerId, out var customerBiases))
        {
            var rarityEnum = (Customer.FISHRARITY)Enum.Parse(typeof(Customer.FISHRARITY), rarity);
            customerBiases[(sellerId, rarityEnum)] = biasValue;
        }
    }

    public void UpdateCustomerPreference(int customerId, string fishName, bool hasPurchased)
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

        DbManager.ExecuteNonQuery(sql, parameters);

        // Update cache
        if (preferencesCache.TryGetValue(customerId, out var preferences) && 
            preferences.TryGetValue(fishName, out var preference))
        {
            preference.HasPurchased = hasPurchased;
        }
    }

    public void ClearCache()
    {
        customerCache.Clear();
        preferencesCache.Clear();
        biasCache.Clear();
    }

    public void AddCustomer(Customer customer)
    {
        // Add to cache
        customerCache[customer.CustomerID] = customer;
        
        // Add preferences to cache
        var preferences = new Dictionary<string, Customer.FishPreference>();
        foreach (var pref in customer.FishPreferences)
        {
            preferences[pref.FishName] = pref;
        }
        preferencesCache[customer.CustomerID] = preferences;
        
        // Add biases to cache
        var biases = new Dictionary<(int sellerId, Customer.FISHRARITY rarity), float>();
        foreach (Customer.FISHRARITY rarity in Enum.GetValues(typeof(Customer.FISHRARITY)))
        {
            foreach (Customer.SellerType seller in Enum.GetValues(typeof(Customer.SellerType)))
            {
                int sellerId = (int)seller;
                biases[(sellerId, rarity)] = customer.GetBias(sellerId, rarity);
            }
        }
        biasCache[customer.CustomerID] = biases;
    }
} 