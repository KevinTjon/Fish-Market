using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;
using Mono.Data.Sqlite;

public class CustomerPurchaseManager : MonoBehaviour
{
    [SerializeField] private CustomerPurchaseEvaluator purchaseEvaluator;
    private CustomerManager customerManager;
    private string dbPath;
    private List<Customer> waitingCustomers = new List<Customer>();
    private List<Customer> activeCustomers = new List<Customer>();

    private void Awake()
    {
        customerManager = GetComponent<CustomerManager>();
        if (customerManager == null)
        {
            Debug.LogError("CustomerManager not found on the same GameObject!");
        }
        if (purchaseEvaluator == null)
        {
            Debug.LogError("CustomerPurchaseEvaluator not found on the same GameObject!");
        }
        dbPath = "URI=file:" + Application.dataPath + "/StreamingAssets/FishDB.db";
    }

    public float GetSellerBias(int customerId, Customer.SellerType seller, Customer.FISHRARITY rarity)
    {
        using (var connection = new SqliteConnection(dbPath))
        {
            connection.Open();
            using (var command = connection.CreateCommand())
            {
                command.CommandText = @"
                    SELECT BiasValue 
                    FROM CustomerBiases 
                    WHERE CustomerID = @customerId 
                    AND SellerID = @sellerId 
                    AND Rarity = @rarity";
                
                command.Parameters.AddWithValue("@customerId", customerId);
                command.Parameters.AddWithValue("@sellerId", (int)seller);
                command.Parameters.AddWithValue("@rarity", (int)rarity);

                var result = command.ExecuteScalar();
                return result != null ? Convert.ToSingle(result) : 0.2f;
            }
        }
    }

    public Customer.SellerType SelectSellerForRarity(Customer customer, Customer.FISHRARITY rarity)
    {
        Dictionary<Customer.SellerType, float> sellerBiases = new Dictionary<Customer.SellerType, float>();
        float totalBias = 0f;

        foreach (Customer.SellerType seller in System.Enum.GetValues(typeof(Customer.SellerType)))
        {
            float bias = GetSellerBias(customer.CustomerID, seller, rarity);
            sellerBiases.Add(seller, bias);
            totalBias += bias;
        }

        float randomRoll = UnityEngine.Random.Range(0f, totalBias);
        float currentSum = 0f;

        foreach (var sellerBias in sellerBiases)
        {
            currentSum += sellerBias.Value;
            if (randomRoll <= currentSum)
            {
                return sellerBias.Key;
            }
        }

        return Customer.SellerType.Player;
    }

    public bool CheckSellerListings(Customer.SellerType seller, Customer.FISHRARITY rarity)
    {
        using (var connection = new SqliteConnection(dbPath))
        {
            connection.Open();
            using (var command = connection.CreateCommand())
            {
                command.CommandText = @"
                    SELECT COUNT(*) 
                    FROM MarketListings 
                    WHERE SellerID = @sellerId 
                    AND Rarity = @rarity 
                    AND IsSold = 0";
                
                command.Parameters.AddWithValue("@sellerId", (int)seller);
                command.Parameters.AddWithValue("@rarity", rarity.ToString());

                int count = Convert.ToInt32(command.ExecuteScalar());
                return count > 0;
            }
        }
    }

    public void ProcessCustomerPurchases()
    {
        Debug.Log("Starting ProcessCustomerPurchases...");
        
        // Get fresh list of customers each time
        activeCustomers = new List<Customer>(customerManager.GetAllCustomers());
        waitingCustomers.Clear();

        // Keep track of which sellers each customer has tried
        Dictionary<int, List<Customer.SellerType>> customerTriedSellers = new Dictionary<int, List<Customer.SellerType>>();

        bool customersStillShopping = true;
        while (customersStillShopping)
        {
            customersStillShopping = false;

            foreach (var customer in activeCustomers.ToList())
            {
                if (!customerTriedSellers.ContainsKey(customer.CustomerID))
                {
                    customerTriedSellers[customer.CustomerID] = new List<Customer.SellerType>();
                }

                foreach (var shoppingItem in customer.ShoppingList)
                {
                    var availableSellers = System.Enum.GetValues(typeof(Customer.SellerType))
                        .Cast<Customer.SellerType>()
                        .Except(customerTriedSellers[customer.CustomerID])
                        .ToList();

                    if (availableSellers.Count > 0)
                    {
                        customersStillShopping = true;
                        Customer.SellerType selectedSeller = SelectSellerForRarity(customer, shoppingItem.Rarity);
                        
                        Debug.Log($"Customer {customer.CustomerID} trying seller {selectedSeller} for {shoppingItem.Rarity}");
                        
                        if (CheckSellerListings(selectedSeller, shoppingItem.Rarity))
                        {
                            var listings = GetListings(shoppingItem.Rarity)
                                .Where(l => l.SellerID == (int)selectedSeller)
                                .ToList();

                            var decision = purchaseEvaluator.EvaluatePurchase(customer, listings, shoppingItem.Rarity);

                            if (decision.WillPurchase)
                            {
                                Debug.Log($"Customer {customer.CustomerID} decided to purchase ListingID {decision.ListingID}");
                                
                                // Execute the purchase
                                if (purchaseEvaluator.ExecutePurchase(decision, customer))
                                {
                                    Debug.Log($"Purchase successful! Listing {decision.ListingID} marked as sold to customer {customer.CustomerID}");
                                    customerTriedSellers[customer.CustomerID].Add(selectedSeller);
                                    break;
                                }
                                else
                                {
                                    Debug.LogWarning($"Purchase failed for listing {decision.ListingID}");
                                    customerTriedSellers[customer.CustomerID].Add(selectedSeller);
                                }
                            }
                            else
                            {
                                Debug.Log($"Customer {customer.CustomerID} rejected purchase: {decision.Reason}");
                                customerTriedSellers[customer.CustomerID].Add(selectedSeller);
                            }
                        }
                        else
                        {
                            Debug.Log($"Customer {customer.CustomerID} found no matching fish at {selectedSeller}");
                            customerTriedSellers[customer.CustomerID].Add(selectedSeller);
                        }
                    }
                    else
                    {
                        Debug.Log($"Customer {customer.CustomerID} has tried all sellers, moving to waiting list");
                        waitingCustomers.Add(customer);
                        activeCustomers.Remove(customer);
                        break;
                    }
                }
            }
        }

        Debug.Log($"Shopping round complete. Active customers: {activeCustomers.Count}, Waiting customers: {waitingCustomers.Count}");
    }

    public List<Customer> GetWaitingCustomers()
    {
        return waitingCustomers;
    }

    public List<Customer> GetActiveCustomers()
    {
        return activeCustomers;
    }

    public class MarketListing
    {
        public int ListingID { get; set; }
        public string FishName { get; set; }
        public float ListedPrice { get; set; }
        public Customer.FISHRARITY Rarity { get; set; }
        public bool IsSold { get; set; }
        public int? BuyerID { get; set; }
        public int SellerID { get; set; }
    }

    public bool MarkListingAsSold(int listingID, int buyerID)
    {
        Debug.Log($"Starting MarkListingAsSold for ListingID {listingID}");
        
        using (var connection = new SqliteConnection(dbPath))
        {
            try
            {
                connection.Open();
                Debug.Log("Database connection opened");
                
                using (var command = connection.CreateCommand())
                {
                    command.CommandText = @"
                        UPDATE MarketListings 
                        SET IsSold = 1 
                        WHERE ListingID = @listingID AND IsSold = 0";
                    
                    command.Parameters.AddWithValue("@listingID", listingID);
                    
                    Debug.Log($"Executing UPDATE query for ListingID {listingID}");
                    Debug.Log($"Query: {command.CommandText}");
                    
                    int rowsAffected = command.ExecuteNonQuery();
                    Debug.Log($"Query executed. Rows affected: {rowsAffected}");
                    
                    if (rowsAffected > 0)
                    {
                        Debug.Log($"Successfully marked listing {listingID} as sold");
                        return true;
                    }
                    else
                    {
                        Debug.LogWarning($"Failed to mark listing {listingID} as sold - listing might not exist or already be sold");
                        
                        // Let's check why it failed
                        using (var checkCommand = connection.CreateCommand())
                        {
                            checkCommand.CommandText = "SELECT ListingID, IsSold FROM MarketListings WHERE ListingID = @listingID";
                            checkCommand.Parameters.AddWithValue("@listingID", listingID);
                            
                            using (var reader = checkCommand.ExecuteReader())
                            {
                                if (reader.Read())
                                {
                                    Debug.LogWarning($"Listing {listingID} exists with IsSold = {reader.GetInt32(1)}");
                                }
                                else
                                {
                                    Debug.LogWarning($"Listing {listingID} does not exist in database");
                                }
                            }
                        }
                        return false;
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"Database error in MarkListingAsSold: {e.Message}");
                return false;
            }
        }
    }

    public List<MarketListing> GetListings(Customer.FISHRARITY rarity)
    {
        List<MarketListing> listings = new List<MarketListing>();
        
        using (var connection = new SqliteConnection(dbPath))
        {
            connection.Open();
            using (var command = connection.CreateCommand())
            {
                command.CommandText = @"
                    SELECT ListingID, FishName, ListedPrice, Rarity, SellerID 
                    FROM MarketListings
                    WHERE Rarity = @rarity 
                    AND IsSold = 0";  // Simplified query with correct column names
                
                command.Parameters.AddWithValue("@rarity", rarity.ToString());
                
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        listings.Add(new MarketListing
                        {
                            ListingID = reader.GetInt32(0),
                            FishName = reader.GetString(1),
                            ListedPrice = reader.GetFloat(2),
                            Rarity = (Customer.FISHRARITY)Enum.Parse(typeof(Customer.FISHRARITY), reader.GetString(3)),
                            SellerID = reader.GetInt32(4),
                            IsSold = false
                        });
                    }
                }
            }
        }
        return listings;
    }

    public void AddCustomer(Customer customer)
    {
        Debug.Log($"Adding customer {customer.CustomerID} to active customers");
        activeCustomers.Add(customer);
    }

    public Dictionary<string, float> GetHistoricalAveragePrices(Customer.FISHRARITY rarity)
    {
        Dictionary<string, float> averages = new Dictionary<string, float>();
        
        using (var connection = new SqliteConnection(dbPath))
        {
            connection.Open();
            using (var command = connection.CreateCommand())
            {
                command.CommandText = @"
                    SELECT mp.FishName, AVG(mp.Price) as AvgPrice
                    FROM MarketPrices mp
                    JOIN Fish f ON mp.FishName = f.Name
                    WHERE f.Rarity = @rarity
                    AND mp.Day >= (SELECT MAX(Day) - 4 FROM MarketPrices)
                    GROUP BY mp.FishName";
                
                command.Parameters.AddWithValue("@rarity", rarity.ToString());

                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        string fishName = reader.GetString(0);
                        float avgPrice = Convert.ToSingle(reader.GetDouble(1));
                        averages[fishName] = avgPrice;
                    }
                }
            }
        }
        return averages;
    }
}
