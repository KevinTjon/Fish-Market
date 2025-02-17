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

    // Add new fields for tracking market balance
    [SerializeField] private float listingToCustomerRatio = 3f; // Threshold for generating new customers
    [SerializeField] private int maxNewCustomersPerBatch = 5;

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
            CheckAndGenerateMoreCustomers();
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

            // Add a maximum number of seller visits to prevent infinite loops
            int maxSellerVisits = System.Enum.GetValues(typeof(Customer.SellerType)).Length;
            int visitCount = 0;

            while (!customer.HasVisitedAllSellers() && !customer.HasReachedMaxPurchases() && visitCount < maxSellerVisits)
            {
                visitCount++;
                var preferences = customer.GetUnpurchasedPreferences();
                if (!preferences.Any()) break;

                int selectedSellerId = SelectSeller(customer, preferences[0].Rarity);
                if (selectedSellerId == -1) break;

                bool boughtAnything = false;
                bool foundDesiredFish = false;
                
                // Add a maximum number of purchase attempts per seller
                int maxPurchaseAttempts = 3;
                int purchaseAttempts = 0;
                
                do
                {
                    purchaseAttempts++;
                    bool shouldContinueWithSeller = true;
                    
                    while (shouldContinueWithSeller && !customer.HasReachedMaxPurchases() && purchaseAttempts <= maxPurchaseAttempts)
                    {
                        shouldContinueWithSeller = false;
                        preferences = customer.GetUnpurchasedPreferences();
                        
                        foreach (var preference in preferences)
                        {
                            var listings = GetListings(preference.Rarity)
                                .Where(l => !l.IsSold && 
                                       l.SellerID == selectedSellerId && 
                                       l.FishName == preference.FishName)
                                .ToList();

                            if (listings.Any())
                            {
                                foundDesiredFish = true;
                                
                                var decision = purchaseEvaluator.EvaluatePurchase(
                                    customer,
                                    listings
                                );

                                if (decision.WillPurchase)
                                {
                                    customer.Budget -= (int)decision.SelectedListing.ListedPrice;
                                    if (MarkListingAsSold(decision.SelectedListing.ListingID, customer.CustomerID))
                                    {
                                        madeAnyPurchase = true;
                                        boughtAnything = true;
                                        shouldContinueWithSeller = true;

                                        AdjustSellerBias(customer, selectedSellerId, preference);
                                        
                                        customerHistory.AppendLine($"Customer {customer.CustomerID} ({customer.Type}): " +
                                            $"Bought {decision.SelectedListing.FishName} (Preference: {preference.PreferenceScore:F2}) " +
                                            $"for {decision.SelectedListing.ListedPrice} gold from Seller {selectedSellerId} " +
                                            $"(New Bias: {customer.GetBias(selectedSellerId, preference.Rarity):F2})");
                                        
                                        customer.RecordPurchase(
                                            decision.SelectedListing.FishName,
                                            decision.SelectedListing.ListedPrice,
                                            decision.SelectedListing.SellerID
                                        );
                                    }
                                }
                            }
                        }
                    }
                } while (boughtAnything && !customer.HasReachedMaxPurchases() && purchaseAttempts < maxPurchaseAttempts);

                if (!foundDesiredFish)
                {
                    AdjustSellerBiasForNoDesiredFish(customer, selectedSellerId, preferences[0].Rarity);
                    customerHistory.AppendLine($"Customer {customer.CustomerID}: Decreased bias for Seller {selectedSellerId} " +
                        $"(New Bias: {customer.GetBias(selectedSellerId, preferences[0].Rarity):F2}) - No desired fish available");
                }

                customer.AddVisitedSeller(selectedSellerId);
            }

            // Always remove customer after processing
            waitingCustomers.RemoveAt(i);
            
            if (madeAnyPurchase)
            {
                var remainingPreferences = customer.GetUnpurchasedPreferences();
                customerHistory.AppendLine($"Customer {customer.CustomerID} ({customer.Type}): " +
                    $"Finished shopping with {customer.Budget:F0} gold remaining. Preferences remaining: {remainingPreferences.Count}");
            }
            else
            {
                var remainingPreferences = customer.GetUnpurchasedPreferences()
                    .Select(p => $"{p.FishName} (Score: {p.PreferenceScore:F2})");
                customerHistory.AppendLine($"Customer {customer.CustomerID} ({customer.Type}): " +
                    $"Left without buying - Budget was {customer.Budget:F0} gold. Wanted to buy: {string.Join(", ", remainingPreferences)}");
            }

            purchaseHistory.Add(customerHistory.ToString().TrimEnd());
        }

        Debug.Log($"After processing: {waitingCustomers.Count} customers still shopping");
    }

    private void CheckAndGenerateMoreCustomers()
    {
        // Count total unsold listings across all rarities
        int totalUnsoldListings = 0;
        foreach (Customer.FISHRARITY rarity in Enum.GetValues(typeof(Customer.FISHRARITY)))
        {
            var listings = GetListings(rarity);
            totalUnsoldListings += listings.Count(l => !l.IsSold);
        }

        // Count total unpurchased preferences
        int totalUnpurchasedPreferences = waitingCustomers.Sum(c => c.GetUnpurchasedPreferences().Count);

        // Calculate ratio (avoid division by zero)
        float ratio = totalUnpurchasedPreferences > 0 ? 
            (float)totalUnsoldListings / totalUnpurchasedPreferences : 
            float.MaxValue;

        // If we have many unsold listings compared to unfulfilled preferences
        if (ratio > listingToCustomerRatio && waitingCustomers.Count < maxWaitingCustomers)
        {
            int customersToAdd = Mathf.Min(maxNewCustomersPerBatch, (int)(totalUnsoldListings / listingToCustomerRatio));
            
            // Tell CustomerManager to generate more customers
            customerManager.TestGenerateInitialCustomers();
            
            Debug.Log($"Requested generation of new customers. Unsold listings: {totalUnsoldListings}, Unfulfilled preferences: {totalUnpurchasedPreferences}");
        }
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
        string output = "Current Customer Status:\n";
        foreach (var customer in activeCustomers)
        {
            var preferences = customer.GetUnpurchasedPreferences();
            output += $"\nCustomer {customer.CustomerID} ({customer.Type}):\n";
            output += $"Budget: {customer.Budget}\n";
            output += $"Preferences remaining: {preferences.Count}\n";
            foreach (var pref in preferences)
            {
                output += $"- {pref.FishName} (Score: {pref.PreferenceScore:F2})\n";
            }
            output += $"Purchases made: {customer.PurchaseHistory.Count}/{customer.MaxPurchases}\n";
        }
        return output;
    }

    private void NormalizeBiases(Customer customer, Customer.FISHRARITY rarity)
    {
        float totalBias = 0f;
        Dictionary<int, float> currentBiases = new Dictionary<int, float>();

        // Get all current biases and their sum
        for (int sellerId = 0; sellerId <= 4; sellerId++)
        {
            float bias = customer.GetBias(sellerId, rarity);
            currentBiases[sellerId] = bias;
            totalBias += bias;
        }

        // Normalize each bias so they sum to 1.0
        foreach (var sellerId in currentBiases.Keys)
        {
            float normalizedBias = currentBiases[sellerId] / totalBias;
            customer.SetBias(sellerId, rarity, normalizedBias);
        }
    }

    private void AdjustBiasesForSuccess(Customer customer, int successfulSellerId, Customer.FISHRARITY rarity)
    {
        // Increase successful seller's bias by 8%
        float currentBias = customer.GetBias(successfulSellerId, rarity);
        float newBias = currentBias + 0.08f;
        customer.SetBias(successfulSellerId, rarity, newBias);

        // Decrease other sellers' biases by 2% each
        for (int sellerId = 0; sellerId <= 4; sellerId++)
        {
            if (sellerId != successfulSellerId)
            {
                float otherBias = customer.GetBias(sellerId, rarity);
                float reducedBias = Mathf.Max(otherBias - 0.02f, 0.1f); // Don't go below 0.1
                customer.SetBias(sellerId, rarity, reducedBias);
            }
        }

        // Normalize after adjustments
        NormalizeBiases(customer, rarity);
    }

    private bool TryPurchase(Customer customer, MarketListing listing)
    {
        if (listing.IsSold || listing.ListedPrice > customer.Budget)
            return false;

        // ... existing purchase logic ...

        // After successful purchase, record it and adjust biases
        customer.RecordPurchase(listing.FishName, listing.ListedPrice, listing.SellerID);
        AdjustBiasesForSuccess(customer, listing.SellerID, listing.Rarity);
        
        return true;
    }

    private void HandleFailedPurchase(Customer customer, MarketListing listing, string reason)
    {
        // Record failed attempt and adjust bias negatively
        float currentBias = customer.GetBias(listing.SellerID, listing.Rarity);
        float newBias = Mathf.Max(currentBias - 0.05f, 0.1f);
        customer.SetBias(listing.SellerID, listing.Rarity, newBias);

        // Normalize biases after adjustment
        NormalizeBiases(customer, listing.Rarity);

        purchaseHistory.Add($"Customer {customer.CustomerID} ({customer.Type}): Rolled Seller {listing.SellerID} (Bias: {currentBias:F2}) but {listing.FishName} at {listing.ListedPrice} gold was {reason}");
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

    private void AdjustSellerBias(Customer customer, int sellerId, Customer.FishPreference preference)
    {
        float currentBias = customer.GetBias(sellerId, preference.Rarity);
        float biasChange;

        if (preference.PreferenceScore < 0.5f)
        {
            // Negative experience - decrease bias
            biasChange = -0.1f;
        }
        else
        {
            // Positive experience - increase bias
            biasChange = 0.1f;
        }

        // Scale bias change based on how far from neutral (0.5) the preference was
        biasChange *= Mathf.Abs(preference.PreferenceScore - 0.5f) * 2f;

        float newBias = Mathf.Clamp(currentBias + biasChange, 0.1f, 1.0f);
        customer.SetBias(sellerId, preference.Rarity, newBias);

        // Normalize all biases after adjustment
        NormalizeBiases(customer, preference.Rarity);

        // Save the updated bias to the database
        customerManager.SaveCustomerBias(customer, sellerId, preference.Rarity);
    }

    private void AdjustSellerBiasForNoDesiredFish(Customer customer, int sellerId, Customer.FISHRARITY rarity)
    {
        float currentBias = customer.GetBias(sellerId, rarity);
        float newBias = Mathf.Clamp(currentBias - 0.15f, 0.1f, 1.0f);
        customer.SetBias(sellerId, rarity, newBias);

        // Normalize all biases after adjustment
        NormalizeBiases(customer, rarity);

        // Save the updated bias to the database
        customerManager.SaveCustomerBias(customer, sellerId, rarity);
    }
}
