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

    // Modify these fields for new customer generation logic
    [SerializeField] private int unsoldListingsPerCustomer = 5; // Generate 1 customer per 5 unsold listings
    [SerializeField] private int maxNewCustomersPerBatch = 2; // Maximum customers to generate at once
    [SerializeField] private int maxTotalCustomers = 30; // Maximum total customers allowed in the system
    
    // Add cache for listings
    private Dictionary<Customer.FISHRARITY, List<MarketListing>> listingsCache = new Dictionary<Customer.FISHRARITY, List<MarketListing>>();
    private bool listingsCacheNeedsRefresh = true;

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
    }

    public float GetSellerBias(int customerId, Customer.SellerType seller, Customer.FISHRARITY rarity)
    {
        var result = DatabaseManager.Instance.ExecuteScalar(
            @"SELECT BiasValue 
              FROM CustomerBiases 
              WHERE CustomerID = @customerId 
              AND SellerID = @sellerId 
              AND Rarity = @rarity",
            new Dictionary<string, object>
            {
                { "@customerId", customerId },
                { "@sellerId", (int)seller },
                { "@rarity", rarity.ToString() }
            });
            
        return result != null ? Convert.ToSingle(result) : 0.2f;
    }

    public bool CheckSellerListings(Customer.SellerType seller, Customer.FISHRARITY rarity)
    {
        var result = DatabaseManager.Instance.ExecuteScalar(
            @"SELECT COUNT(*) 
              FROM MarketListings 
              WHERE SellerID = @sellerId 
              AND Rarity = @rarity 
              AND IsSold = 0",
            new Dictionary<string, object>
            {
                { "@sellerId", (int)seller },
                { "@rarity", rarity.ToString() }
            });
            
        return Convert.ToInt32(result) > 0;
    }

    public void ProcessCustomerPurchases()
    {
        bool shouldGenerateMore;
        int maxGenerationCycles = 2; // Reduced from 3 to 2 cycles
        int generationCycle = 0;

        do {
            shouldGenerateMore = false;
            
            // First check if we need to generate more customers
            if (waitingCustomers.Count == 0)
            {
                CheckAndGenerateMoreCustomers();
                // If we still have no customers after generation, then we can return
                if (waitingCustomers.Count == 0)
                {
                    Debug.Log("No customers waiting to make purchases.");
                    return;
                }
            }

            Debug.Log($"Processing purchases for {waitingCustomers.Count} customers...");
            purchaseHistory.Clear();
            
            // Refresh the listings cache once before processing all customers
            listingsCacheNeedsRefresh = true;
            
            // Pre-load all rarities into cache
            foreach (Customer.FISHRARITY rarity in Enum.GetValues(typeof(Customer.FISHRARITY)))
            {
                GetListings(rarity);
            }

            // Process all current waiting customers
            ProcessCurrentWaitingCustomers();

            Debug.Log($"After processing: {waitingCustomers.Count} customers still shopping");
            
            // Check if we should generate more customers
            if (generationCycle < maxGenerationCycles)
            {
                int unsoldListings = GetTotalUnsoldListings();
                if (unsoldListings >= unsoldListingsPerCustomer) // Only generate if we have enough unsold listings
                {
                    generationCycle++;
                    CheckAndGenerateMoreCustomers();
                    if (waitingCustomers.Count > 0)
                    {
                        shouldGenerateMore = true;
                    }
                }
            }
        } while (shouldGenerateMore);
    }

    private void ProcessCurrentWaitingCustomers()
    {
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

                                if (decision.WillPurchase && !customer.HasReachedMaxPurchases())
                                {
                                    customer.Budget -= (int)decision.SelectedListing.ListedPrice;
                                    if (MarkListingAsSold(decision.SelectedListing.ListingID, customer.CustomerID))
                                    {
                                        madeAnyPurchase = true;
                                        boughtAnything = true;
                                        shouldContinueWithSeller = true;

                                        // Calculate bias change based on preference score
                                        float biasChange = preference.PreferenceScore < 0.5f ? -0.1f : 0.1f;
                                        // Scale bias change based on how far from neutral (0.5) the preference was
                                        biasChange *= Mathf.Abs(preference.PreferenceScore - 0.5f) * 2f;
                                        
                                        AdjustSellerBias(customer, selectedSellerId, preference.Rarity, biasChange, 
                                            $"Bought {decision.SelectedListing.FishName} (Preference: {preference.PreferenceScore:F2}) for {decision.SelectedListing.ListedPrice} gold from Seller {selectedSellerId}");
                                        
                                        // Update the HasPurchased status in the database
                                        Debug.Log($"Attempting to update HasPurchased for Customer {customer.CustomerID}, Fish {decision.SelectedListing.FishName}");
                                        bool updateSuccess = DatabaseManager.Instance.UpdateCustomerPreference(
                                            customer.CustomerID, 
                                            decision.SelectedListing.FishName, 
                                            true
                                        );
                                        Debug.Log($"HasPurchased update {(updateSuccess ? "succeeded" : "failed")}");
                                        
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
                    AdjustSellerBias(customer, selectedSellerId, preferences[0].Rarity, -0.15f, $"No desired fish available");
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
    }

    private int GetTotalUnsoldListings()
    {
        int total = 0;
        foreach (Customer.FISHRARITY rarity in Enum.GetValues(typeof(Customer.FISHRARITY)))
        {
            total += GetListings(rarity).Count(l => !l.IsSold);
        }
        return total;
    }

    private void CheckAndGenerateMoreCustomers()
    {
        // Check total customer count first
        if (activeCustomers.Count >= maxTotalCustomers)
        {
            Debug.Log($"Not generating new customers - At maximum customer capacity ({activeCustomers.Count}/{maxTotalCustomers})");
            return;
        }

        // Calculate total remaining potential purchases across all active customers
        int totalRemainingPurchases = activeCustomers.Sum(c => c.MaxPurchases - c.PurchaseHistory.Count);
        
        if (totalRemainingPurchases >= 6)
        {
            Debug.Log($"Not generating new customers - {totalRemainingPurchases} total purchases still remaining across all customers");
            return;
        }

        int unsoldListings = GetTotalUnsoldListings();
        if (unsoldListings >= unsoldListingsPerCustomer) // Only generate if we have enough unsold listings
        {
            // Calculate new customers needed (1 per 5 unsold listings)
            int maxPossibleNewCustomers = Mathf.Min(
                maxTotalCustomers - activeCustomers.Count,
                maxNewCustomersPerBatch
            );

            int customersToAdd = Mathf.Min(
                unsoldListings / unsoldListingsPerCustomer,
                maxPossibleNewCustomers
            );

            if (customersToAdd > 0)
            {
                Debug.Log($"Generating {customersToAdd} new customers " +
                    $"(Current: {activeCustomers.Count}, Max: {maxTotalCustomers}, " +
                    $"Unsold: {unsoldListings}, Remaining purchases: {totalRemainingPurchases})");
                
                // Calculate rarity weights based on unsold listings
                Dictionary<Customer.FISHRARITY, float> rarityWeights = CalculateRarityWeights();
                
                // Generate the new customers
                customerManager.GenerateCustomersForCurrentDay(customersToAdd, rarityWeights);
            }
        }
        else
        {
            Debug.Log($"Not enough unsold listings to generate new customers " +
                $"(Need {unsoldListingsPerCustomer}, Have {unsoldListings})");
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
        
        bool success = DatabaseManager.Instance.MarkListingAsSold(listingID);
        
        if (success)
        {
            Debug.Log($"Successfully marked listing {listingID} as sold");
            // Invalidate the cache since a listing was sold
            listingsCacheNeedsRefresh = true;
        }
        else
        {
            Debug.LogWarning($"Failed to mark listing {listingID} as sold");
        }
        
        return success;
    }

    public List<MarketListing> GetListings(Customer.FISHRARITY rarity)
    {
        // If we have a cached result and it doesn't need refreshing, return it
        if (!listingsCacheNeedsRefresh && listingsCache.ContainsKey(rarity))
        {
            return listingsCache[rarity];
        }
        
        // If we need to refresh the entire cache
        if (listingsCacheNeedsRefresh)
        {
            listingsCache.Clear();
        }
        
        List<MarketListing> listings = new List<MarketListing>();
        
        var results = DatabaseManager.Instance.GetUnsoldListings(rarity.ToString());
        
        foreach (var row in results)
        {
            listings.Add(new MarketListing
            {
                ListingID = Convert.ToInt32(row["ListingID"]),
                FishName = row["FishName"].ToString(),
                ListedPrice = Convert.ToSingle(row["ListedPrice"]),
                Rarity = (Customer.FISHRARITY)Enum.Parse(typeof(Customer.FISHRARITY), row["Rarity"].ToString()),
                SellerID = Convert.ToInt32(row["SellerID"]),
                IsSold = false
            });
        }
        
        // Cache the result
        listingsCache[rarity] = listings;
        
        // If we just refreshed one rarity, we might still need to refresh others
        // But if we're refreshing all rarities at once, we can mark the cache as valid
        if (listingsCacheNeedsRefresh && listingsCache.Count == Enum.GetValues(typeof(Customer.FISHRARITY)).Length)
        {
            listingsCacheNeedsRefresh = false;
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
        return DatabaseManager.Instance.GetHistoricalAveragePrices(rarity.ToString());
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

    // New unified bias adjustment method
    private void AdjustSellerBias(Customer customer, int sellerId, Customer.FISHRARITY rarity, float adjustmentAmount, string reason = "")
    {
        // Make adjustments more significant
        adjustmentAmount *= 2.0f; // Double the adjustment impact
        
        float currentBias = customer.GetBias(sellerId, rarity);
        float newBias = Mathf.Clamp(currentBias + adjustmentAmount, 0.1f, 0.9f); // Allow wider range
        customer.SetBias(sellerId, rarity, newBias);

        // If this is a positive adjustment, decrease other sellers' biases more aggressively
        if (adjustmentAmount > 0)
        {
            float decreaseAmount = adjustmentAmount / 3.0f; // Distribute one-third of the increase as decrease
            for (int otherSellerId = 0; otherSellerId <= 4; otherSellerId++)
            {
                if (otherSellerId != sellerId)
                {
                    float otherBias = customer.GetBias(otherSellerId, rarity);
                    float reducedBias = Mathf.Max(otherBias - decreaseAmount, 0.1f);
                    customer.SetBias(otherSellerId, rarity, reducedBias);
                }
            }
        }

        // Normalize biases to ensure they sum to 1.0
        float totalBias = 0f;
        Dictionary<int, float> biases = new Dictionary<int, float>();
        
        // First pass: collect all biases
        for (int i = 0; i <= 4; i++)
        {
            float bias = customer.GetBias(i, rarity);
            biases[i] = bias;
            totalBias += bias;
        }
        
        // Second pass: normalize and update
        if (totalBias > 0)
        {
            foreach (var kvp in biases)
            {
                float normalizedBias = kvp.Value / totalBias;
                customer.SetBias(kvp.Key, rarity, normalizedBias);
                
                // Save to database immediately
                DatabaseManager.Instance.UpdateCustomerBias(
                    customer.CustomerID,
                    kvp.Key,
                    rarity.ToString(),
                    normalizedBias
                );
            }
        }

        if (!string.IsNullOrEmpty(reason))
        {
            Debug.Log($"Customer {customer.CustomerID}: Adjusted bias for Seller {sellerId} by {adjustmentAmount:F2} " +
                $"(New Bias: {customer.GetBias(sellerId, rarity):F2}) - {reason}");
        }
    }

    private void TryPurchase(Customer customer, MarketListing listing)
    {
        if (listing.IsSold || listing.ListedPrice > customer.Budget)
            return;

        // ... existing purchase logic ...

        // After successful purchase, record it and adjust biases
        customer.RecordPurchase(listing.FishName, listing.ListedPrice, listing.SellerID);
        AdjustSellerBias(customer, listing.SellerID, listing.Rarity, 0.1f, $"Bought {listing.FishName} at {listing.ListedPrice} gold");
        
        return;
    }

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

    public enum RejectionReason
    {
        TooExpensive,
        LowPreference,
        OutOfBudget,
        ReachedPurchaseLimit,
        BetterOptionAvailable,
        None
    }

    public void RecordRejectionReason(int listingId, int customerId, RejectionReason reason)
    {
        DatabaseManager.Instance.RecordRejectionReason(
            listingId,
            customerId,
            reason.ToString()
        );
    }

    public bool ProcessCustomerPurchases(Customer customer, List<MarketListing> availableListings)
    {
        bool madeAnyPurchase = false;
        
        if (customer.HasReachedMaxPurchases())
        {
            foreach (var listing in availableListings.Where(l => !l.IsSold))
            {
                RecordRejectionReason(listing.ListingID, customer.CustomerID, RejectionReason.ReachedPurchaseLimit);
            }
            return false;
        }

        // ... rest of existing code ...
        return madeAnyPurchase;
    }

    // Add method to clear the cache
    public void ClearListingsCache()
    {
        listingsCache.Clear();
        Debug.Log("Cleared listings cache in CustomerPurchaseManager");
    }

    private Dictionary<Customer.FISHRARITY, float> CalculateRarityWeights()
    {
        int unsoldListings = GetTotalUnsoldListings();
        Dictionary<Customer.FISHRARITY, float> rarityWeights = new Dictionary<Customer.FISHRARITY, float>();

        if (unsoldListings > 0)
        {
            foreach (Customer.FISHRARITY rarity in Enum.GetValues(typeof(Customer.FISHRARITY)))
            {
                var listings = GetListings(rarity);
                int unsoldCount = listings.Count(l => !l.IsSold);
                rarityWeights[rarity] = (float)unsoldCount / unsoldListings;
            }
        }
        else
        {
            // Default even distribution if no unsold listings
            foreach (Customer.FISHRARITY rarity in Enum.GetValues(typeof(Customer.FISHRARITY)))
            {
                rarityWeights[rarity] = 1.0f / Enum.GetValues(typeof(Customer.FISHRARITY)).Length;
            }
        }

        return rarityWeights;
    }
}
