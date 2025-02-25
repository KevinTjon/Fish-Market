using UnityEngine;
using System;  // Add this for Convert
using System.Collections.Generic;
using TMPro;  // Add this for TextMeshProUGUI
using System.Linq;  // Add this for Sum and other LINQ methods

public class CustomerPurchaseEvaluator : MonoBehaviour
{
    // Add reference to purchase manager
    [SerializeField] private CustomerPurchaseManager purchaseManager;

    // Result class to hold purchase decision details
    public class PurchaseDecision
    {
        public bool WillPurchase { get; set; }
        public int ListingID { get; set; }
        public int OfferedPrice { get; set; }
        public string Reason { get; set; }
        public CustomerPurchaseManager.MarketListing SelectedListing { get; set; }
    }

    // Updated price threshold multipliers based on your specifications
    private readonly Dictionary<Customer.CUSTOMERTYPE, (float min, float max)> PriceThresholds = 
        new Dictionary<Customer.CUSTOMERTYPE, (float min, float max)>
    {
        { Customer.CUSTOMERTYPE.BUDGET, (0.6f, 1.0f) },      // Unchanged
        { Customer.CUSTOMERTYPE.CASUAL, (0.8f, 1.2f) },      // Widened range
        { Customer.CUSTOMERTYPE.COLLECTOR, (0.8f, 2.0f) },   // Increased max for rare fish
        { Customer.CUSTOMERTYPE.WEALTHY, (0.9f, 2.5f) }      // Increased max for luxury purchases
    };

    // Rarity preferences for each customer type
    private readonly Dictionary<Customer.CUSTOMERTYPE, Dictionary<Customer.FISHRARITY, float>> RarityPreferences =
        new Dictionary<Customer.CUSTOMERTYPE, Dictionary<Customer.FISHRARITY, float>>
    {
        {
            Customer.CUSTOMERTYPE.BUDGET, new Dictionary<Customer.FISHRARITY, float> {
                { Customer.FISHRARITY.COMMON, 0.5f },
                { Customer.FISHRARITY.UNCOMMON, 0.3f },
                { Customer.FISHRARITY.RARE, 0.15f },
                { Customer.FISHRARITY.EPIC, 0.04f },
                { Customer.FISHRARITY.LEGENDARY, 0.01f }
            }
        },
        {
            Customer.CUSTOMERTYPE.CASUAL, new Dictionary<Customer.FISHRARITY, float> {
                { Customer.FISHRARITY.COMMON, 0.2f },
                { Customer.FISHRARITY.UNCOMMON, 0.4f },
                { Customer.FISHRARITY.RARE, 0.3f },
                { Customer.FISHRARITY.EPIC, 0.08f },
                { Customer.FISHRARITY.LEGENDARY, 0.02f }
            }
        },
        {
            Customer.CUSTOMERTYPE.COLLECTOR, new Dictionary<Customer.FISHRARITY, float> {
                { Customer.FISHRARITY.COMMON, 0.1f },
                { Customer.FISHRARITY.UNCOMMON, 0.2f },
                { Customer.FISHRARITY.RARE, 0.4f },
                { Customer.FISHRARITY.EPIC, 0.2f },
                { Customer.FISHRARITY.LEGENDARY, 0.1f }
            }
        },
        {
            Customer.CUSTOMERTYPE.WEALTHY, new Dictionary<Customer.FISHRARITY, float> {
                { Customer.FISHRARITY.COMMON, 0.2f },
                { Customer.FISHRARITY.UNCOMMON, 0.2f },
                { Customer.FISHRARITY.RARE, 0.3f },
                { Customer.FISHRARITY.EPIC, 0.15f },
                { Customer.FISHRARITY.LEGENDARY, 0.15f }
            }
        }
    };

    private void Awake()
    {
        if (purchaseManager == null)
        {
            purchaseManager = FindObjectOfType<CustomerPurchaseManager>();
            if (purchaseManager == null)
            {
                Debug.LogError("CustomerPurchaseManager not found!");
            }
        }
    }

    // Main evaluation method - updated to use fish preferences
    public PurchaseDecision EvaluatePurchase(Customer customer, List<CustomerPurchaseManager.MarketListing> listings)
    {
        if (listings == null || !listings.Any())
        {
            return new PurchaseDecision
            {
                WillPurchase = false,
                Reason = "No listings available"
            };
        }

        // Get customer's unpurchased preferences in order of preference
        var preferences = customer.GetUnpurchasedPreferences();
        if (!preferences.Any())
        {
            foreach (var listing in listings)
            {
                purchaseManager.RecordRejectionReason(listing.ListingID, customer.CustomerID, CustomerPurchaseManager.RejectionReason.ReachedPurchaseLimit);
            }
            return new PurchaseDecision
            {
                WillPurchase = false,
                Reason = "Customer has no remaining unpurchased preferences"
            };
        }

        // For each preference, try to find a suitable listing
        foreach (var preference in preferences)
        {
            var matchingListings = listings
                .Where(l => l.FishName == preference.FishName && !l.IsSold)
                .OrderBy(l => l.ListedPrice)
                .ThenByDescending(l => customer.GetBias(l.SellerID, preference.Rarity))
                .ToList();

            if (!matchingListings.Any())
                continue;

            foreach (var listing in matchingListings)
            {
                // For wealthy customers, prioritize preferences over price
                if (customer.Type == Customer.CUSTOMERTYPE.WEALTHY)
                {
                    // Changed condition to be more lenient for wealthy customers
                    if (listing.ListedPrice <= customer.Budget && 
                        (preference.PreferenceScore >= 0.4f || // Lowered threshold
                         preference.Rarity >= Customer.FISHRARITY.EPIC)) // Always consider EPIC and LEGENDARY
                    {
                        return new PurchaseDecision
                        {
                            WillPurchase = true,
                            SelectedListing = listing,
                            Reason = $"Wealthy customer accepting {preference.Rarity} fish ({preference.FishName}) within budget"
                        };
                    }
                    purchaseManager.RecordRejectionReason(listing.ListingID, customer.CustomerID, CustomerPurchaseManager.RejectionReason.OutOfBudget);
                    continue;
                }

                var (minWTP, maxWTP) = PriceThresholds[customer.Type];
                float sellerBias = customer.GetBias(listing.SellerID, preference.Rarity);

                // Adjust WTP based on preference score, seller bias, and rarity
                float rarityMultiplier = preference.Rarity switch
                {
                    Customer.FISHRARITY.LEGENDARY => 1.5f,
                    Customer.FISHRARITY.EPIC => 1.3f,
                    Customer.FISHRARITY.RARE => 1.2f,
                    Customer.FISHRARITY.UNCOMMON => 1.1f,
                    _ => 1.0f
                };

                float adjustedMaxWTP = maxWTP * (1 + preference.PreferenceScore) * (1 + sellerBias * 0.2f) * rarityMultiplier;
                float adjustedMinWTP = minWTP * (1 - (1 - preference.PreferenceScore) * 0.2f);

                // Get market average price for this rarity
                var marketAverage = purchaseManager.GetHistoricalAveragePrices(preference.Rarity)
                    .GetValueOrDefault(listing.FishName, listing.ListedPrice);

                float priceRatio = listing.ListedPrice / marketAverage;

                // Check if price is acceptable and within budget
                if (priceRatio >= adjustedMinWTP && priceRatio <= adjustedMaxWTP && listing.ListedPrice <= customer.Budget)
                {
                    return new PurchaseDecision
                    {
                        WillPurchase = true,
                        SelectedListing = listing,
                        Reason = $"Accepting purchase of {preference.FishName} (preference score: {preference.PreferenceScore:F2})" +
                                $" Price ratio {priceRatio:F2} within range [{adjustedMinWTP:F2}-{adjustedMaxWTP:F2}]"
                    };
                }
                
                // Record rejection reason
                if (listing.ListedPrice > customer.Budget)
                {
                    purchaseManager.RecordRejectionReason(listing.ListingID, customer.CustomerID, CustomerPurchaseManager.RejectionReason.OutOfBudget);
                }
                else if (priceRatio > adjustedMaxWTP)
                {
                    purchaseManager.RecordRejectionReason(listing.ListingID, customer.CustomerID, CustomerPurchaseManager.RejectionReason.TooExpensive);
                }
                else if (preference.PreferenceScore < 0.3f)
                {
                    purchaseManager.RecordRejectionReason(listing.ListingID, customer.CustomerID, CustomerPurchaseManager.RejectionReason.LowPreference);
                }
            }
        }

        return new PurchaseDecision
        {
            WillPurchase = false,
            Reason = "No acceptable listings found matching preferences and price criteria"
        };
    }

    // Update TestAveragePrices to use CustomerPurchaseManager
    public void TestAveragePrices(TextMeshProUGUI outputText)
    {
        string output = "Market Average Prices:\n";
        
        foreach (Customer.FISHRARITY rarity in System.Enum.GetValues(typeof(Customer.FISHRARITY)))
        {
            output += $"\n=== {rarity} FISH ===\n";
            var fishPrices = purchaseManager.GetHistoricalAveragePrices(rarity);
            foreach (var fish in fishPrices)
            {
                output += $"{fish.Key}: {fish.Value:F2} gold\n";
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

    // Method to check if price is within customer's threshold
    private bool IsWithinPriceThreshold(Customer.CUSTOMERTYPE customerType, float price, float marketValue)
    {
        var (minWTP, maxWTP) = PriceThresholds[customerType];
        float minAcceptablePrice = marketValue * minWTP;
        float maxAcceptablePrice = marketValue * maxWTP;

        // Add budget consideration
        if (customerType == Customer.CUSTOMERTYPE.WEALTHY)
        {
            // Wealthy customers are less price sensitive
            maxAcceptablePrice *= 2f;
        }
        else if (customerType == Customer.CUSTOMERTYPE.COLLECTOR)
        {
            // Collectors are willing to pay more for rare/legendary
            maxAcceptablePrice *= 1.5f;
        }

        return price >= minAcceptablePrice && price <= maxAcceptablePrice;
    }

    public int RollForSeller(Customer customer, Customer.FISHRARITY rarity)
    {
        // Get biases for each seller type for this rarity
        var sellerBiases = new List<(int sellerId, float bias)>();
        foreach (Customer.SellerType seller in System.Enum.GetValues(typeof(Customer.SellerType)))
        {
            float bias = customer.GetBias((int)seller, rarity);
            sellerBiases.Add(((int)seller, bias));
        }
        
        // Calculate total bias value
        float totalBias = sellerBiases.Sum(b => b.bias);
        
        // Roll a random number between 0 and total bias
        float roll = UnityEngine.Random.Range(0f, totalBias);
        
        // Find which seller was selected
        float currentSum = 0f;
        foreach (var (sellerId, bias) in sellerBiases)
        {
            currentSum += bias;
            if (roll <= currentSum)
            {
                return sellerId;
            }
        }
        
        // Fallback to first seller if something goes wrong
        return 0;
    }
}
