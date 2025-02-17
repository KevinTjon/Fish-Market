using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;
using Mono.Data.Sqlite;
using TMPro;
using System.Collections;
using System.Text;

public class CustomerPurchaseManager : MonoBehaviour
{
    [SerializeField] private CustomerPurchaseEvaluator purchaseEvaluator;
    private CustomerManager customerManager;
    private string dbPath;
    private List<Customer> waitingCustomers = new List<Customer>();
    private List<Customer> activeCustomers = new List<Customer>();
    private Dictionary<int, List<Customer.SellerType>> customerTriedSellers = new Dictionary<int, List<Customer.SellerType>>();
    [SerializeField] private TextMeshProUGUI debugText;
    [SerializeField] private int maxWaitingCustomers = 20; // Maximum number of waiting customers

    // Add at class level
    private static List<string> purchaseHistory = new List<string>();

    private void Awake()
    {
        customerManager = FindObjectOfType<CustomerManager>();
        if (customerManager == null)
        {
            Debug.LogError("CustomerManager not found!");
        }
        if (purchaseEvaluator == null)
        {
            Debug.LogError("CustomerPurchaseEvaluator not found!");
        }

        // Use the same path format as CustomerManager
        string dbName = "FishDB.db";
        string dbPath = System.IO.Path.Combine(Application.streamingAssetsPath, dbName);
        this.dbPath = $"URI=file:{dbPath}";
        
        Debug.Log($"Purchase Manager Database path: {this.dbPath}");
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
        if (waitingCustomers.Count == 0)
        {
            Debug.Log("No customers waiting to make purchases.");
            return;
        }

        Debug.Log($"Processing purchases for {waitingCustomers.Count} customers...");
        purchaseHistory.Clear();

        for (int i = waitingCustomers.Count - 1; i >= 0; i--)
        {
            var customer = waitingCustomers[i];
            bool madeAnyPurchase = false;
            StringBuilder customerHistory = new StringBuilder();

            // Keep trying until we've visited all sellers or bought everything
            while (!customer.HasVisitedAllSellers() && customer.ShoppingList.Any())
            {
                int selectedSellerId = SelectSeller(customer, customer.ShoppingList[0].Rarity);
                if (selectedSellerId == -1) break; // No more unvisited sellers

                bool boughtAnything;
                do
                {
                    boughtAnything = false;
                    bool shouldContinueWithSeller = true;
                    
                    while (shouldContinueWithSeller && customer.ShoppingList.Any())
                    {
                        shouldContinueWithSeller = false;
                        // Look at each item in shopping list
                        for (int itemIndex = 0; itemIndex < customer.ShoppingList.Count; itemIndex++)
                        {
                            var shoppingItem = customer.ShoppingList[itemIndex];
                            var listings = GetListings(shoppingItem.Rarity)
                                .Where(l => !l.IsSold && l.SellerID == selectedSellerId)
                                .ToList();

                            if (listings.Any())
                            {
                                var decision = purchaseEvaluator.EvaluatePurchase(
                                    customer,
                                    listings,
                                    shoppingItem.Rarity
                                );

                                if (decision.WillPurchase)
                                {
                                    customer.Budget -= (int)decision.SelectedListing.ListedPrice;
                                    if (MarkListingAsSold(decision.SelectedListing.ListingID, customer.CustomerID))
                                    {
                                        madeAnyPurchase = true;
                                        boughtAnything = true;
                                        shouldContinueWithSeller = true;
                                        
                                        customerHistory.AppendLine($"Customer {customer.CustomerID} ({customer.Type}): " +
                                            $"Bought {decision.SelectedListing.FishName} for {decision.SelectedListing.ListedPrice} gold from Seller {selectedSellerId} (Bias: {customer.GetBias(selectedSellerId, shoppingItem.Rarity):F2})");
                                        
                                        customer.RecordPurchase(
                                            decision.SelectedListing.FishName,
                                            decision.SelectedListing.ListedPrice,
                                            decision.SelectedListing.SellerID
                                        );
                                        
                                        shoppingItem.Amount--;
                                        if (shoppingItem.Amount <= 0)
                                        {
                                            customer.ShoppingList.RemoveAt(itemIndex);
                                            itemIndex--; // Adjust index since we removed an item
                                        }
                                    }
                                }
                                else
                                {
                                    customerHistory.AppendLine($"Customer {customer.CustomerID} ({customer.Type}): " +
                                        $"Rolled Seller {selectedSellerId} (Bias: {customer.GetBias(selectedSellerId, shoppingItem.Rarity):F2}) but {listings[0].FishName} at {listings[0].ListedPrice} gold was too expensive");
                                }
                            }
                            else
                            {
                                customerHistory.AppendLine($"Customer {customer.CustomerID} ({customer.Type}): " +
                                    $"Rolled Seller {selectedSellerId} (Bias: {customer.GetBias(selectedSellerId, shoppingItem.Rarity):F2}) but no {shoppingItem.Rarity} fish available");
                            }
                        }
                    }
                } while (boughtAnything && customer.ShoppingList.Any());

                customer.AddVisitedSeller(selectedSellerId);
            }

            // Always remove customer after processing
            if (madeAnyPurchase)
            {
                customerHistory.AppendLine($"Customer {customer.CustomerID} ({customer.Type}): " +
                    $"Finished shopping with {customer.Budget:F0} gold remaining. Items remaining on list: {customer.ShoppingList.Count}");
            }
            else
            {
                var remainingItems = string.Join(", ", customer.ShoppingList.Select(item => 
                    $"{item.Amount}x {item.Rarity}"));
                customerHistory.AppendLine($"Customer {customer.CustomerID} ({customer.Type}): " +
                    $"Left without buying - Budget was {customer.Budget:F0} gold. Wanted to buy: {remainingItems}");
            }
            waitingCustomers.RemoveAt(i);

            purchaseHistory.Add(customerHistory.ToString().TrimEnd());
        }

        Debug.Log($"After processing: {waitingCustomers.Count} customers still shopping");
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
        Debug.Log($"Adding customer {customer.CustomerID} to active and waiting customers");
        activeCustomers.Add(customer);
        waitingCustomers.Add(customer);  // Add to waiting customers as well
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

    public string DebugRemainingShoppingLists()
    {
        StringBuilder sb = new StringBuilder();
        
        // Purchase History
        sb.AppendLine("=== PURCHASE HISTORY ===");
        foreach (string purchase in purchaseHistory)
        {
            sb.AppendLine(purchase);
        }
        sb.AppendLine();

        // Waiting Customers
        sb.AppendLine($"\n=== WAITING CUSTOMERS ({waitingCustomers.Count}) ===");
        foreach (var customer in waitingCustomers)
        {
            sb.AppendLine($"\nCustomer {customer.CustomerID} ({customer.Type})");
            sb.AppendLine($"Budget: {customer.Budget:F2} gold");
            sb.AppendLine("Shopping List:");
            foreach (var need in customer.ShoppingList)
            {
                sb.AppendLine($"- Needs {need.Amount}x {need.Rarity}");
            }
            
            // Add seller biases
            sb.AppendLine("Seller Biases:");
            foreach (var (sellerId, rarity, value) in customer.GetBiases())
            {
                sb.AppendLine($"- Seller {sellerId} for {rarity}: {value:F2}");
            }
            
            // Add visited sellers
            sb.AppendLine("Visited Sellers:");
            foreach (Customer.SellerType seller in Enum.GetValues(typeof(Customer.SellerType)))
            {
                bool visited = customer.HasVisitedSeller((int)seller);
                sb.AppendLine($"- Seller {(int)seller}: {(visited ? "Visited" : "Not visited")}");
            }
        }

        return sb.ToString();
    }

    private bool TryPurchase(Customer customer, MarketListing listing)
    {
        if (listing.IsSold || listing.ListedPrice > customer.Budget)
            return false;

        // ... existing purchase logic ...

        // After successful purchase, record it
        customer.RecordPurchase(listing.FishName, listing.ListedPrice, listing.SellerID);
        
        return true;
    }

    // Remove or comment out the Update method since we don't need continuous processing
    /*
    void Update()
    {
        if (waitingCustomers.Count > 0)
        {
            ProcessCustomerPurchases();
        }
        DebugRemainingShoppingLists();
    }
    */

    public bool HasAnyListings()
    {
        foreach (Customer.FISHRARITY rarity in System.Enum.GetValues(typeof(Customer.FISHRARITY)))
        {
            var listings = GetListings(rarity);
            if (listings != null && listings.Count > 0)
                return true;
        }
        return false;
    }

    private int SelectSeller(Customer customer, Customer.FISHRARITY rarity)
    {
        // Get all biases for this rarity
        var biases = new List<(int sellerId, float bias)>();
        
        // Consider all sellers (0 for player, 1-4 for bots)
        for (int sellerId = 0; sellerId <= 4; sellerId++)
        {
            if (!customer.HasVisitedSeller(sellerId))
            {
                float bias = customer.GetBias(sellerId, rarity);
                biases.Add((sellerId, bias));
            }
        }

        if (biases.Count == 0)
            return -1;  // No unvisited sellers left

        // Calculate total bias
        float totalBias = biases.Sum(b => b.bias);
        
        // Roll a random number between 0 and total bias
        float roll = UnityEngine.Random.Range(0f, totalBias);
        
        // Select seller based on roll
        float currentSum = 0f;
        foreach (var (sellerId, bias) in biases)
        {
            currentSum += bias;
            if (roll <= currentSum)
                return sellerId;
        }

        // Fallback to first available seller if something goes wrong
        return biases[0].sellerId;
    }

    public void ClearCustomers()
    {
        waitingCustomers.Clear();
        activeCustomers.Clear();
    }
}
