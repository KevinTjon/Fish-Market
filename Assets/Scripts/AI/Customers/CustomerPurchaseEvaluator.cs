using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;
using Mono.Data.Sqlite;
using TMPro;

public class CustomerPurchaseEvaluator : MonoBehaviour
{
    private string dbPath;

    // Result class to hold purchase decision details
    public class PurchaseDecision
    {
        public bool WillPurchase { get; set; }
        public int ListingID { get; set; }
        public float OfferedPrice { get; set; }
        public string Reason { get; set; }
    }

    // Price threshold multipliers for each customer type
    private readonly Dictionary<Customer.CUSTOMERTYPE, float> MaxPriceMultipliers = new Dictionary<Customer.CUSTOMERTYPE, float>
    {
        { Customer.CUSTOMERTYPE.BUDGET, 0.9f },     // Will pay up to 90% of average
        { Customer.CUSTOMERTYPE.CASUAL, 1.1f },     // Will pay up to 110% of average
        { Customer.CUSTOMERTYPE.COLLECTOR, 1.5f },   // Will pay up to 150% of average
        { Customer.CUSTOMERTYPE.WEALTHY, 2.0f }      // Will pay up to 200% of average
    };

    private void Awake()
    {
        dbPath = "URI=file:" + Application.dataPath + "/StreamingAssets/FishDB.db";
    }

    // Main evaluation method (to be implemented)
    public PurchaseDecision EvaluatePurchase(
        Customer customer,
        List<CustomerPurchaseManager.MarketListing> listings,
        Customer.FISHRARITY targetRarity)
    {
        var marketAverages = GetMarketAveragePrices(targetRarity);
        
        if (listings == null || listings.Count == 0)
        {
            return new PurchaseDecision 
            { 
                WillPurchase = false, 
                Reason = "No listings available" 
            };
        }

        switch (customer.Type)
        {
            case Customer.CUSTOMERTYPE.BUDGET:
                return EvaluateAsBudgetCustomer(customer, listings, marketAverages, targetRarity);
            
            case Customer.CUSTOMERTYPE.CASUAL:
                return EvaluateAsCasualCustomer(customer, listings, marketAverages, targetRarity);
            
            case Customer.CUSTOMERTYPE.COLLECTOR:
                return EvaluateAsCollectorCustomer(customer, listings, marketAverages, targetRarity);
            
            case Customer.CUSTOMERTYPE.WEALTHY:
                return EvaluateAsWealthyCustomer(customer, listings, marketAverages, targetRarity);
            
            default:
                return new PurchaseDecision 
                { 
                    WillPurchase = false, 
                    Reason = "Unknown customer type" 
                };
        }
    }

    // Method to get market average price for a specific rarity
    private Dictionary<string, float> GetMarketAveragePrices(Customer.FISHRARITY rarity)
    {
        Dictionary<string, float> fishPrices = new Dictionary<string, float>();
        
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
                        fishPrices[fishName] = avgPrice;
                    }
                }
            }
        }
        return fishPrices;
    }

    // Test method to show average prices
    public void TestAveragePrices(TextMeshProUGUI outputText)
    {
        string output = "Market Average Prices:\n";
        
        foreach (Customer.FISHRARITY rarity in System.Enum.GetValues(typeof(Customer.FISHRARITY)))
        {
            output += $"\n=== {rarity} FISH ===\n";
            var fishPrices = GetMarketAveragePrices(rarity);
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
    private bool IsPriceAcceptable(float price, float averagePrice, Customer.CUSTOMERTYPE customerType)
    {
        if (!MaxPriceMultipliers.ContainsKey(customerType))
            return false;

        float maxAcceptablePrice = averagePrice * MaxPriceMultipliers[customerType];
        return price <= maxAcceptablePrice;
    }

    private PurchaseDecision EvaluateAsBudgetCustomer(
        Customer customer,
        List<CustomerPurchaseManager.MarketListing> listings,
        Dictionary<string, float> marketAverages,
        Customer.FISHRARITY targetRarity)
    {
        var decision = new PurchaseDecision();
        
        // Get first listing since we evaluate one at a time
        var listing = listings[0];
        float marketAverage = marketAverages[listing.FishName];

        // First check if this is the rarity we're looking for
        if (listing.Rarity != targetRarity)
        {
            decision.WillPurchase = false;
            decision.Reason = $"Not the target rarity. Looking for {targetRarity}, found {listing.Rarity}";
            return decision;
        }

        // Check budget first
        if (listing.ListedPrice > customer.Budget)
        {
            decision.WillPurchase = false;
            decision.Reason = $"Cannot afford. Price: {listing.ListedPrice:F2} gold, Budget: {customer.Budget:F2} gold";
            return decision;
        }

        // Set price threshold based on rarity
        float priceThreshold;
        string reason;
        
        switch (targetRarity)
        {
            case Customer.FISHRARITY.COMMON:
                priceThreshold = marketAverage;
                reason = "Common fish at market price";
                break;
                
            case Customer.FISHRARITY.UNCOMMON:
                priceThreshold = marketAverage * 0.8f;
                reason = "Uncommon fish at 80% market price";
                break;
                
            case Customer.FISHRARITY.RARE:
            case Customer.FISHRARITY.LEGENDARY:
                priceThreshold = marketAverage * 0.6f;
                reason = $"{listing.Rarity} fish at 60% market price";
                break;
                
            default:
                decision.WillPurchase = false;
                decision.Reason = "Unknown rarity";
                return decision;
        }

        // Check if price is acceptable (budget already checked)
        if (listing.ListedPrice <= priceThreshold)
        {
            decision.WillPurchase = true;
            decision.ListingID = listing.ListingID;
            decision.OfferedPrice = listing.ListedPrice;
            decision.Reason = $"Accepting {reason}. Price: {listing.ListedPrice:F2} gold (Market: {marketAverage:F2})";
        }
        else
        {
            decision.WillPurchase = false;
            decision.Reason = $"Price too high for {listing.Rarity}. Listed: {listing.ListedPrice:F2} gold, Maximum: {priceThreshold:F2}";
        }

        return decision;
    }

    private PurchaseDecision EvaluateAsCasualCustomer(
        Customer customer,
        List<CustomerPurchaseManager.MarketListing> listings,
        Dictionary<string, float> marketAverages,
        Customer.FISHRARITY targetRarity)
    {
        var decision = new PurchaseDecision();
        
        // Get first listing since we evaluate one at a time
        var listing = listings[0];
        float marketAverage = marketAverages[listing.FishName];

        // First check if this is the rarity we're looking for
        if (listing.Rarity != targetRarity)
        {
            decision.WillPurchase = false;
            decision.Reason = $"Not the target rarity. Looking for {targetRarity}, found {listing.Rarity}";
            return decision;
        }

        // Casual customers are more flexible with prices than budget customers
        // They care more about getting the fish they want at a reasonable price
        float priceThreshold;
        string reason;
        
        switch (targetRarity)
        {
            case Customer.FISHRARITY.COMMON:
                priceThreshold = marketAverage * 1.1f;  // Will pay up to 110% of market price
                reason = "Common fish at market price +10%";
                break;
                
            case Customer.FISHRARITY.UNCOMMON:
                priceThreshold = marketAverage * 1.0f;  // Will pay market price
                reason = "Uncommon fish at market price";
                break;
                
            case Customer.FISHRARITY.RARE:
                priceThreshold = marketAverage * 0.9f;  // Want a 10% discount
                reason = "Rare fish at 90% market price";
                break;
                
            case Customer.FISHRARITY.EPIC:
            case Customer.FISHRARITY.LEGENDARY:
                priceThreshold = marketAverage * 0.8f;  // Want a 20% discount on expensive fish
                reason = $"{listing.Rarity} fish at 80% market price";
                break;
                
            default:
                decision.WillPurchase = false;
                decision.Reason = "Unknown rarity";
                return decision;
        }

        // Check if price is acceptable and within budget
        if (listing.ListedPrice <= priceThreshold && listing.ListedPrice <= customer.Budget)
        {
            decision.WillPurchase = true;
            decision.ListingID = listing.ListingID;
            decision.OfferedPrice = listing.ListedPrice;
            decision.Reason = $"Accepting {reason}. Price: {listing.ListedPrice:F2} gold (Market: {marketAverage:F2})";
        }
        else
        {
            decision.WillPurchase = false;
            decision.Reason = $"Price too high for {listing.Rarity}. Listed: {listing.ListedPrice:F2} gold, Maximum: {priceThreshold:F2}";
        }

        return decision;
    }

    private PurchaseDecision EvaluateAsCollectorCustomer(
        Customer customer,
        List<CustomerPurchaseManager.MarketListing> listings,
        Dictionary<string, float> marketAverages,
        Customer.FISHRARITY targetRarity)
    {
        var decision = new PurchaseDecision();
        
        // Get first listing since we evaluate one at a time
        var listing = listings[0];
        float marketAverage = marketAverages[listing.FishName];

        // First check if this is the rarity we're looking for
        if (listing.Rarity != targetRarity)
        {
            decision.WillPurchase = false;
            decision.Reason = $"Not the target rarity. Looking for {targetRarity}, found {listing.Rarity}";
            return decision;
        }

        // Collectors are most interested in rare and above fish
        // They're willing to pay more for higher rarity fish
        float priceThreshold;
        string reason;
        
        switch (targetRarity)
        {
            case Customer.FISHRARITY.LEGENDARY:
                priceThreshold = marketAverage * 1.5f;  // Will pay up to 150% for legendary
                reason = "Legendary fish at 150% market price";
                break;
                
            case Customer.FISHRARITY.EPIC:
                priceThreshold = marketAverage * 1.3f;  // Will pay up to 130% for epic
                reason = "Epic fish at 130% market price";
                break;
                
            case Customer.FISHRARITY.RARE:
                priceThreshold = marketAverage * 1.2f;  // Will pay up to 120% for rare
                reason = "Rare fish at 120% market price";
                break;
                
            case Customer.FISHRARITY.UNCOMMON:
                priceThreshold = marketAverage * 1.0f;  // Market price for uncommon
                reason = "Uncommon fish at market price";
                break;
                
            case Customer.FISHRARITY.COMMON:
                priceThreshold = marketAverage * 0.8f;  // Want discount on common
                reason = "Common fish at 80% market price";
                break;
                
            default:
                decision.WillPurchase = false;
                decision.Reason = "Unknown rarity";
                return decision;
        }

        // Check if price is acceptable and within budget
        if (listing.ListedPrice <= priceThreshold && listing.ListedPrice <= customer.Budget)
        {
            decision.WillPurchase = true;
            decision.ListingID = listing.ListingID;
            decision.OfferedPrice = listing.ListedPrice;
            decision.Reason = $"Accepting {reason}. Price: {listing.ListedPrice:F2} gold (Market: {marketAverage:F2})";
        }
        else
        {
            decision.WillPurchase = false;
            string budgetReason = listing.ListedPrice > customer.Budget ? " (Exceeds budget)" : "";
            decision.Reason = $"Price too high for {listing.Rarity}. Listed: {listing.ListedPrice:F2} gold, Maximum: {priceThreshold:F2}{budgetReason}";
        }

        return decision;
    }

    private PurchaseDecision EvaluateAsWealthyCustomer(
        Customer customer,
        List<CustomerPurchaseManager.MarketListing> listings,
        Dictionary<string, float> marketAverages,
        Customer.FISHRARITY targetRarity)
    {
        var decision = new PurchaseDecision();
        
        // Get first listing since we evaluate one at a time
        var listing = listings[0];
        float marketAverage = marketAverages[listing.FishName];

        // First check if this is the rarity we're looking for
        if (listing.Rarity != targetRarity)
        {
            decision.WillPurchase = false;
            decision.Reason = $"Not the target rarity. Looking for {targetRarity}, found {listing.Rarity}";
            return decision;
        }

        // Wealthy customers are willing to pay premium prices, especially for rare fish
        // They care more about quality and rarity than price
        float priceThreshold;
        string reason;
        
        switch (targetRarity)
        {
            case Customer.FISHRARITY.LEGENDARY:
                priceThreshold = marketAverage * 2.0f;  // Will pay up to 200% for legendary
                reason = "Legendary fish at 200% market price";
                break;
                
            case Customer.FISHRARITY.EPIC:
                priceThreshold = marketAverage * 1.8f;  // Will pay up to 180% for epic
                reason = "Epic fish at 180% market price";
                break;
                
            case Customer.FISHRARITY.RARE:
                priceThreshold = marketAverage * 1.5f;  // Will pay up to 150% for rare
                reason = "Rare fish at 150% market price";
                break;
                
            case Customer.FISHRARITY.UNCOMMON:
                priceThreshold = marketAverage * 1.3f;  // Will pay 130% for uncommon
                reason = "Uncommon fish at 130% market price";
                break;
                
            case Customer.FISHRARITY.COMMON:
                priceThreshold = marketAverage * 1.2f;  // Even willing to pay above market for common
                reason = "Common fish at 120% market price";
                break;
                
            default:
                decision.WillPurchase = false;
                decision.Reason = "Unknown rarity";
                return decision;
        }

        // Wealthy customers still have a budget, but they're more likely to spend it
        if (listing.ListedPrice <= priceThreshold && listing.ListedPrice <= customer.Budget)
        {
            decision.WillPurchase = true;
            decision.ListingID = listing.ListingID;
            decision.OfferedPrice = listing.ListedPrice;
            decision.Reason = $"Accepting {reason}. Price: {listing.ListedPrice:F2} gold (Market: {marketAverage:F2})";
        }
        else
        {
            decision.WillPurchase = false;
            string budgetReason = listing.ListedPrice > customer.Budget ? " (Exceeds budget)" : " (Above acceptable markup)";
            decision.Reason = $"Price too high for {listing.Rarity}. Listed: {listing.ListedPrice:F2} gold, Maximum: {priceThreshold:F2}{budgetReason}";
        }

        return decision;
    }
}
