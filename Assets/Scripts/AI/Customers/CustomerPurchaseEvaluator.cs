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
        { Customer.CUSTOMERTYPE.BUDGET, (0.6f, 1.0f) },      // Lowered min from 0.8f to 0.6f
        { Customer.CUSTOMERTYPE.CASUAL, (0.9f, 1.1f) },      // Unchanged
        { Customer.CUSTOMERTYPE.COLLECTOR, (1.0f, 1.5f) },   // Unchanged
        { Customer.CUSTOMERTYPE.WEALTHY, (1.2f, 2.0f) }      // Unchanged
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

    // Main evaluation method
    public PurchaseDecision EvaluatePurchase(Customer customer, List<CustomerPurchaseManager.MarketListing> listings, Customer.FISHRARITY rarity)
    {
        if (listings == null || !listings.Any())
        {
            return new PurchaseDecision
            {
                WillPurchase = false,
                Reason = "No listings available"
            };
        }

        var marketAverages = purchaseManager.GetHistoricalAveragePrices(rarity);
        
        // Sort listings by price and seller bias
        var sortedListings = listings
            .OrderBy(l => l.ListedPrice)
            .ThenByDescending(l => customer.GetBias(l.SellerID, rarity))
            .ToList();

        foreach (var listing in sortedListings)
        {
            // Skip if already sold
            if (listing.IsSold)
                continue;

            // For wealthy customers, only check against budget
            if (customer.Type == Customer.CUSTOMERTYPE.WEALTHY)
            {
                if (listing.ListedPrice <= customer.Budget)
                {
                    return new PurchaseDecision
                    {
                        WillPurchase = true,
                        SelectedListing = listing,
                        Reason = $"Wealthy customer accepting purchase within budget: {listing.ListedPrice} <= {customer.Budget}"
                    };
                }
                continue;
            }

            float marketAverage = marketAverages.GetValueOrDefault(listing.FishName, listing.ListedPrice);
            var (minWTP, maxWTP) = PriceThresholds[customer.Type];
            float rarityPreference = RarityPreferences[customer.Type][listing.Rarity];
            float sellerBias = customer.GetBias(listing.SellerID, listing.Rarity);

            // Adjust WTP based on rarity preference and seller bias
            float adjustedMaxWTP = maxWTP * (1 + rarityPreference) * (1 + sellerBias * 0.2f);
            float adjustedMinWTP = minWTP * (1 - (1 - rarityPreference) * 0.2f);

            float priceRatio = listing.ListedPrice / marketAverage;

            if (priceRatio >= adjustedMinWTP && priceRatio <= adjustedMaxWTP)
            {
                return new PurchaseDecision
                {
                    WillPurchase = true,
                    SelectedListing = listing,
                    Reason = $"Accepting purchase: Price ratio {priceRatio:F2} within range [{adjustedMinWTP:F2}-{adjustedMaxWTP:F2}]. " +
                            $"Rarity preference: {rarityPreference:F2}, Seller bias: {sellerBias:F2}"
                };
            }
        }

        return new PurchaseDecision
        {
            WillPurchase = false,
            Reason = "No acceptable listings found within price range and preferences"
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
