using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;
using Mono.Data.Sqlite;
using TMPro;
using System.Collections;
using System.Text;
using Market;

namespace Market
{
    public class CustomerPurchaseManager : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private CustomerPurchaseEvaluator purchaseEvaluator;
        [SerializeField] private GameObject customerPrefab;
        [SerializeField] private Transform customerSpawnPoint;
        [SerializeField] private StallManager stallManager;

        private CustomerManager customerManager;
        private string dbPath;
        private List<Customer> waitingCustomers = new List<Customer>();
        private List<Customer> activeCustomers = new List<Customer>();
        private Dictionary<int, List<Customer.SellerType>> customerTriedSellers = new Dictionary<int, List<Customer.SellerType>>();
        [SerializeField] private TextMeshProUGUI debugText;
        [SerializeField] private int maxWaitingCustomers = 20; // Maximum number of waiting customers
        [SerializeField] private float customerProcessingDelay = 2f; // Delay between processing each customer

        // Add at class level
        private static List<string> purchaseHistory = new List<string>();

        // Modify these fields for new customer generation logic
        [SerializeField] private int unsoldListingsPerCustomer = 5; // Generate 1 customer per 5 unsold listings
        [SerializeField] private int maxNewCustomersPerBatch = 2; // Maximum customers to generate at once
        [SerializeField] private int maxTotalCustomers = 30; // Maximum total customers allowed in the system
        
        // Add cache for listings
        private Dictionary<Customer.FISHRARITY, List<MarketListing>> listingsCache = new Dictionary<Customer.FISHRARITY, List<MarketListing>>();
        private bool listingsCacheNeedsRefresh = true;

        // Track physical customers
        private Dictionary<int, PhysicalCustomer> physicalCustomers = new Dictionary<int, PhysicalCustomer>();

        private void Awake()
        {
            //Debug.Log("CustomerPurchaseManager Awake - Initializing dependencies...");

            // Initialize references
            if (customerManager == null)
            {
                customerManager = FindObjectOfType<CustomerManager>();
                if (customerManager == null)
                {
                    Debug.LogError("CustomerManager not found!");
                }
            }

            if (stallManager == null)
            {
                stallManager = FindObjectOfType<StallManager>();
                if (stallManager == null)
                {
                    Debug.LogError("StallManager not found!");
                }
            }

            if (purchaseEvaluator == null)
            {
                purchaseEvaluator = FindObjectOfType<CustomerPurchaseEvaluator>();
                if (purchaseEvaluator == null)
                {
                    Debug.LogError("CustomerPurchaseEvaluator not found!");
                }
            }

            // Initialize DatabaseManager reference
            if (DatabaseManager.Instance == null)
            {
                Debug.LogError("DatabaseManager not found!");
            }
            else
            {
                //Debug.Log("DatabaseManager found and initialized");
            }

            // Validate required references
            if (customerPrefab == null)
            {
                Debug.LogError("Customer prefab is not set in CustomerPurchaseManager!");
            }

            if (customerSpawnPoint == null)
            {
                Debug.LogError("Spawn point is not set in CustomerPurchaseManager!");
            }

            //Debug.Log("CustomerPurchaseManager initialized");
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
            StartCoroutine(ProcessCustomerPurchasesCoroutine());
        }

        private IEnumerator ProcessCustomerPurchasesCoroutine()
        {
            //Debug.Log("Starting ProcessCustomerPurchases...");
            //Debug.Log($"Initial state - Waiting customers: {waitingCustomers.Count}, Active customers: {activeCustomers.Count}");

            bool shouldGenerateMore;
            int generationCycle = 0;
            const int maxGenerationCycles = 3;

            do
            {
                shouldGenerateMore = false;
                if (waitingCustomers.Count == 0)
                {
                    //Debug.Log("No waiting customers to process");
                    yield break;
                }

                //Debug.Log($"Processing purchases for {waitingCustomers.Count} customers...");
                purchaseHistory.Clear();
                
                // Refresh the listings cache once before processing all customers
                listingsCacheNeedsRefresh = true;
                
                // Pre-load all rarities into cache
                //Debug.Log("Pre-loading listings cache for all rarities...");
                foreach (Customer.FISHRARITY rarity in Enum.GetValues(typeof(Customer.FISHRARITY)))
                {
                    var listings = GetListings(rarity);
                    //Debug.Log($"Found {listings.Count} listings for rarity {rarity}");
                }

                // Process customers in rounds
                bool allCustomersFinished = false;
                while (!allCustomersFinished)
                {
                    allCustomersFinished = true; // Will be set to false if any customer still needs processing
                    List<(PhysicalCustomer physicalCustomer, Customer customer, Transform sellerPosition, int sellerId)> movementData = new List<(PhysicalCustomer, Customer, Transform, int)>();

                    // First, determine next seller for all customers and start their movements
                    foreach (var customer in waitingCustomers.ToList())
                    {
                        if (!customer.HasVisitedAllSellers() && !customer.HasReachedMaxPurchases())
                        {
                            allCustomersFinished = false;

                            // Get or spawn physical customer
                            PhysicalCustomer physicalCustomer;
                            if (!physicalCustomers.TryGetValue(customer.CustomerID, out physicalCustomer))
                            {
                                physicalCustomer = SpawnPhysicalCustomer(customer);
                                if (physicalCustomer == null)
                                {
                                    Debug.LogError($"Failed to spawn physical customer {customer.CustomerID}");
                                    continue;
                                }
                            }

                            // Select next seller
                            int sellerId = SelectSeller(customer, customer.GetUnpurchasedPreferences().First().Rarity);
                            if (sellerId == -1)
                            {
                                //Debug.Log($"No more unvisited sellers for customer {customer.CustomerID}");
                                continue;
                            }

                            // Mark seller as visited
                            customer.AddVisitedSeller(sellerId);

                            // Get seller position
                            Transform sellerPosition = stallManager.GetSellerPosition(sellerId);
                            if (sellerPosition == null)
                            {
                                Debug.LogError($"Could not find position for seller {sellerId}");
                                continue;
                            }

                            // Collect movement data
                            movementData.Add((physicalCustomer, customer, sellerPosition, sellerId));
                        }
                    }

                    // Start all movements simultaneously
                    List<Coroutine> movementCoroutines = new List<Coroutine>();
                    foreach (var (physicalCustomer, customer, sellerPosition, sellerId) in movementData)
                    {
                        //Debug.Log($"Starting movement for customer {customer.CustomerID} to seller {sellerId}");
                        var coroutine = StartCoroutine(StartCustomerMovement(physicalCustomer, customer, sellerPosition, sellerId));
                        movementCoroutines.Add(coroutine);
                    }

                    // Wait for all movements to complete
                    foreach (var coroutine in movementCoroutines)
                    {
                        yield return coroutine;
                    }

                    // Process purchases for all customers who reached their destinations
                    List<Coroutine> returnCoroutines = new List<Coroutine>();
                    foreach (var customer in waitingCustomers.ToList())
                    {
                        bool isDoneForDay = customer.HasVisitedAllSellers() || customer.HasReachedMaxPurchases();

                        // Return customer to spawn if they're done for the day
                        if (isDoneForDay)
                        {
                            if (physicalCustomers.TryGetValue(customer.CustomerID, out PhysicalCustomer physicalCustomer))
                            {
                                //Debug.Log($"Customer {customer.CustomerID} is done shopping, returning to spawn");
                                var returnCoroutine = StartCoroutine(physicalCustomer.ReturnToSpawn());
                                returnCoroutines.Add(returnCoroutine);
                            }

                            waitingCustomers.Remove(customer);
                            activeCustomers.Remove(customer);
                        }
                        else
                        {
                            // Reset visited sellers for next round
                            customer.ClearVisitedSellers();
                        }
                    }

                    // Wait for all return movements to complete
                    foreach (var returnCoroutine in returnCoroutines)
                    {
                        yield return returnCoroutine;
                    }

                    // Clean up physical customers after they've returned
                    foreach (var customer in physicalCustomers.Keys.ToList())
                    {
                        if (!waitingCustomers.Any(c => c.CustomerID == customer))
                        {
                            physicalCustomers.Remove(customer);
                        }
                    }

                    yield return new WaitForSeconds(customerProcessingDelay);
                }

                // Check if we should generate more customers
                if (generationCycle < maxGenerationCycles)
                {
                    int unsoldListings = GetTotalUnsoldListings();
                    if (unsoldListings >= unsoldListingsPerCustomer)
                    {
                        generationCycle++;
                        CheckAndGenerateMoreCustomers();
                        yield return new WaitForSeconds(customerProcessingDelay);
                        if (waitingCustomers.Count > 0)
                        {
                            shouldGenerateMore = true;
                        }
                    }
                }
            } while (shouldGenerateMore);

            //Debug.Log($"ProcessCustomerPurchases complete. Final state - Waiting: {waitingCustomers.Count}, Active: {activeCustomers.Count}");
        }

        private IEnumerator StartCustomerMovement(PhysicalCustomer physicalCustomer, Customer customer, Transform sellerPosition, int sellerId)
        {
            // Move to seller
            yield return StartCoroutine(physicalCustomer.MoveToSeller(sellerPosition));
            
            // Process purchase once arrived
            var availableListings = GetListings(customer.GetUnpurchasedPreferences().First().Rarity)
                .Where(l => l.SellerID == sellerId && !l.IsSold)
                .ToList();

            var decision = purchaseEvaluator.EvaluatePurchase(customer, availableListings);
            if (decision.WillPurchase && decision.SelectedListing != null)
            {
                HandlePurchaseSuccess(customer, decision.SelectedListing);
                //Debug.Log($"Customer {customer.CustomerID} purchased {decision.SelectedListing.FishName} from seller {sellerId}");
            }
            else
            {
                //Debug.Log($"Customer {customer.CustomerID} did not make a purchase from seller {sellerId}");
            }

            yield return new WaitForSeconds(customerProcessingDelay);
        }

        private PhysicalCustomer SpawnPhysicalCustomer(Customer customer)
        {
            if (customerPrefab == null || customerSpawnPoint == null)
            {
                Debug.LogError("Customer prefab or spawn point not set!");
                return null;
            }

            // Instantiate the physical customer at spawn point
            GameObject customerObj = Instantiate(customerPrefab, customerSpawnPoint.position, Quaternion.identity);
            PhysicalCustomer physicalCustomer = customerObj.GetComponent<PhysicalCustomer>();
            
            if (physicalCustomer == null)
            {
                Debug.LogError("PhysicalCustomer component not found on prefab!");
                Destroy(customerObj);
                return null;
            }

            // Initialize the physical customer
            physicalCustomer.Initialize(customer, customerSpawnPoint);
            physicalCustomers[customer.CustomerID] = physicalCustomer;
            
            return physicalCustomer;
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
            //Debug.Log($"Starting CheckAndGenerateMoreCustomers - Current active customers: {activeCustomers.Count}/{maxTotalCustomers}");
            
            // Check total customer count first
            if (activeCustomers.Count >= maxTotalCustomers)
            {
                //Debug.Log($"Not generating new customers - At maximum customer capacity ({activeCustomers.Count}/{maxTotalCustomers})");
                return;
            }

            // Calculate total remaining potential purchases across all active customers
            int totalRemainingPurchases = activeCustomers.Sum(c => c.MaxPurchases - c.PurchaseHistory.Count);
            //Debug.Log($"Total remaining potential purchases: {totalRemainingPurchases}");
            
            if (totalRemainingPurchases >= 6)
            {
                //Debug.Log($"Not generating new customers - {totalRemainingPurchases} total purchases still remaining across all customers");
                return;
            }

            int unsoldListings = GetTotalUnsoldListings();
            //Debug.Log($"Found {unsoldListings} unsold listings (need {unsoldListingsPerCustomer} per customer)");
            
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

                //Debug.Log($"Calculated customers to add: {customersToAdd} (Max possible: {maxPossibleNewCustomers}, Based on listings: {unsoldListings / unsoldListingsPerCustomer})");

                if (customersToAdd > 0)
                {
                    //Debug.Log($"Generating {customersToAdd} new customers " +
                        // $"(Current: {activeCustomers.Count}, Max: {maxTotalCustomers}, " +
                        // $"Unsold: {unsoldListings}, Remaining purchases: {totalRemainingPurchases})");
                    
                    // Calculate rarity weights based on unsold listings
                    Dictionary<Customer.FISHRARITY, float> rarityWeights = CalculateRarityWeights();
                    
                    // Generate the new customers
                    customerManager.GenerateCustomersForCurrentDay(customersToAdd, rarityWeights);
                }
                else
                {
                    //Debug.Log("No new customers needed based on calculations");
                }
            }
            else
            {
                //Debug.Log($"Not enough unsold listings to generate new customers " +
                    // $"(Need {unsoldListingsPerCustomer}, Have {unsoldListings})");
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

        public bool MarkListingAsSold(int listingID, int buyerID)
        {
            //Debug.Log($"Starting MarkListingAsSold for ListingID {listingID}");
            
            bool success = DatabaseManager.Instance.MarkListingAsSold(listingID);
            
            if (success)
            {
                //Debug.Log($"Successfully marked listing {listingID} as sold");
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
                    Rarity = (Customer.FISHRARITY)Enum.Parse(typeof(Customer.FISHRARITY), row["Rarity"].ToString().ToUpper()),
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
            //Debug.Log($"Adding customer {customer.CustomerID} to active and waiting customers");
            activeCustomers.Add(customer);
            waitingCustomers.Add(customer);

            // Spawn the physical representation of the customer
            SpawnPhysicalCustomer(customer);
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
                // Debug.Log($"Customer {customer.CustomerID}: Adjusted bias for Seller {sellerId} by {adjustmentAmount:F2} " +
                //     $"(New Bias: {customer.GetBias(sellerId, rarity):F2}) - {reason}");
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
            //Debug.Log($"ClearCustomers called - Stack trace:\n{Environment.StackTrace}");
            
            // Destroy all physical customers
            foreach (var physicalCustomer in physicalCustomers.Values)
            {
                if (physicalCustomer != null && physicalCustomer.gameObject != null)
                {
                    //Debug.Log($"Destroying physical customer {physicalCustomer.gameObject.name}");
                    Destroy(physicalCustomer.gameObject);
                }
            }
            
            // Clear all customer lists and dictionaries
            int physicalCount = physicalCustomers.Count;
            int waitingCount = waitingCustomers.Count;
            int activeCount = activeCustomers.Count;
            
            physicalCustomers.Clear();
            waitingCustomers.Clear();
            activeCustomers.Clear();
            
            //Debug.Log($"Cleared customers - Physical: {physicalCount}, Waiting: {waitingCount}, Active: {activeCount}");
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
            //Debug.Log("Cleared listings cache in CustomerPurchaseManager");
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

        private void HandlePurchaseSuccess(Customer customer, MarketListing selectedListing)
        {
            customer.Budget -= (int)selectedListing.ListedPrice;
            if (MarkListingAsSold(selectedListing.ListingID, customer.CustomerID))
            {
                // Get the preference that matched this purchase
                var preference = customer.GetUnpurchasedPreferences()
                    .FirstOrDefault(p => p.FishName == selectedListing.FishName);

                if (preference != null)
                {
                    // Calculate bias change based on preference score
                    float biasChange = preference.PreferenceScore < 0.5f ? -0.1f : 0.1f;
                    // Scale bias change based on how far from neutral (0.5) the preference was
                    biasChange *= Mathf.Abs(preference.PreferenceScore - 0.5f) * 2f;
                    
                    AdjustSellerBias(customer, selectedListing.SellerID, preference.Rarity, biasChange, 
                        $"Bought {selectedListing.FishName} (Preference: {preference.PreferenceScore:F2}) for {selectedListing.ListedPrice} gold from Seller {selectedListing.SellerID}");
                    
                    // Update the HasPurchased status in the database
                    //Debug.Log($"Attempting to update HasPurchased for Customer {customer.CustomerID}, Fish {selectedListing.FishName}");
                    bool updateSuccess = DatabaseManager.Instance.UpdateCustomerPreference(
                        customer.CustomerID, 
                        selectedListing.FishName, 
                        true
                    );
                    //Debug.Log($"HasPurchased update {(updateSuccess ? "succeeded" : "failed")}");
                }

                customer.RecordPurchase(
                    selectedListing.FishName,
                    selectedListing.ListedPrice,
                    selectedListing.SellerID
                );
            }
        }

        public void TestCustomerMovement(Transform targetTransform)
        {
            if (waitingCustomers.Count == 0)
            {
                Debug.LogWarning("No customers available to test movement");
                return;
            }

            var customer = waitingCustomers[0];
            if (physicalCustomers.TryGetValue(customer.CustomerID, out PhysicalCustomer physicalCustomer))
            {
                StartCoroutine(physicalCustomer.MoveToSeller(targetTransform));
            }
            else
            {
                Debug.LogWarning($"No physical customer found for customer ID {customer.CustomerID}");
            }
        }
    }
}
