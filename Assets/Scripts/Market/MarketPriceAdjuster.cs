using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;

public class MarketPriceAdjuster : MonoBehaviour
{
    [SerializeField] private CustomerPurchaseManager purchaseManager;
    [SerializeField] private CustomerManager customerManager;
    [SerializeField] private DatabaseManager databaseManager;

    // Weights for different customer types
    private readonly Dictionary<Customer.CUSTOMERTYPE, float> CustomerTypeWeights = new()
    {
        { Customer.CUSTOMERTYPE.WEALTHY, 2.0f },
        { Customer.CUSTOMERTYPE.COLLECTOR, 1.8f },
        { Customer.CUSTOMERTYPE.CASUAL, 1.2f },
        { Customer.CUSTOMERTYPE.BUDGET, 1.0f }
    };

    // Price adjustment limits per rarity
    private readonly Dictionary<Customer.FISHRARITY, (float min, float max)> RarityPriceRanges = new()
    {
        { Customer.FISHRARITY.LEGENDARY, (800f, 1200f) },
        { Customer.FISHRARITY.EPIC, (400f, 600f) },
        { Customer.FISHRARITY.RARE, (200f, 300f) },
        { Customer.FISHRARITY.UNCOMMON, (80f, 120f) },
        { Customer.FISHRARITY.COMMON, (40f, 60f) }
    };

    private void Awake()
    {
        if (purchaseManager == null)
            purchaseManager = FindObjectOfType<CustomerPurchaseManager>();
        if (customerManager == null)
            customerManager = FindObjectOfType<CustomerManager>();
        if (databaseManager == null)
            databaseManager = DatabaseManager.Instance;

        if (purchaseManager == null || customerManager == null || databaseManager == null)
            Debug.LogError("Required components not found in MarketPriceAdjuster!");
    }

    public class FishDemandMetrics
    {
        public string FishName { get; set; }
        public Customer.FISHRARITY Rarity { get; set; }
        public float PreferenceScore { get; set; }
        public float SalesScore { get; set; }
        public int TotalListings { get; set; }
        public int SuccessfulSales { get; set; }
        public float AverageSellingPrice { get; set; }
        public float CurrentBasePrice { get; set; }
    }

    public void UpdateAllPrices()
    {
        Debug.Log("Starting daily price updates...");

        // Get all fish types from database
        var fishTypes = GetAllFishTypes();
        int nextDay = GetNextMarketDay(); // Get next day once for all fish
        Debug.Log($"Updating prices for day {nextDay}");
        
        foreach (var fish in fishTypes)
        {
            try
            {
                // Calculate metrics for this fish
                var metrics = CalculateDemandMetrics(fish.name, fish.rarity);
                
                // Calculate and set new price
                float newPrice = CalculateNextDayPrice(metrics);
                
                // Store new price in database with the same next day value
                StoreFishPrice(fish.name, newPrice, nextDay);
                
                Debug.Log($"Updated price for {fish.name}: {metrics.CurrentBasePrice:F2} -> {newPrice:F2} " +
                         $"(Preference Score: {metrics.PreferenceScore:F2}, Sales Score: {metrics.SalesScore:F2})");
            }
            catch (Exception e)
            {
                Debug.LogError($"Error updating price for {fish.name}: {e.Message}");
            }
        }

        Debug.Log("Daily price updates completed.");
    }

    private List<(string name, Customer.FISHRARITY rarity)> GetAllFishTypes()
    {
        var fishTypes = new List<(string name, Customer.FISHRARITY rarity)>();
        
        databaseManager.ExecuteReader(
            "SELECT Name, Rarity FROM Fish",
            reader => {
                while (reader.Read())
                {
                    string name = reader.GetString(0);
                    Customer.FISHRARITY rarity = (Customer.FISHRARITY)Enum.Parse(
                        typeof(Customer.FISHRARITY), 
                        reader.GetString(1)
                    );
                    fishTypes.Add((name, rarity));
                }
            }
        );
        
        return fishTypes;
    }

    private FishDemandMetrics CalculateDemandMetrics(string fishName, Customer.FISHRARITY rarity)
    {
        try
        {
            var metrics = new FishDemandMetrics
            {
                FishName = fishName,
                Rarity = rarity
            };

            // Get current customers
            var customers = customerManager.GetAllCustomers();
            
            // Calculate weighted preference score
            float totalPreferenceScore = 0f;
            float totalWeight = 0f;

            foreach (var customer in customers)
            {
                var preference = customer.FishPreferences
                    .FirstOrDefault(p => p.FishName == fishName);
                
                if (preference != null)
                {
                    float weight = CustomerTypeWeights[customer.Type];
                    totalPreferenceScore += preference.PreferenceScore * weight;
                    totalWeight += weight;
                }
            }

            metrics.PreferenceScore = totalWeight > 0 ? 
                totalPreferenceScore / totalWeight : 0.5f; // Default to neutral if no preferences

            // Get sales data
            var listings = GetTodaysListings(fishName);
            metrics.TotalListings = listings.Count;
            metrics.SuccessfulSales = listings.Count(l => l.IsSold);
            
            if (listings.Any())
            {
                var soldListings = listings.Where(l => l.IsSold);
                if (soldListings.Any())
                {
                    metrics.AverageSellingPrice = soldListings.Average(l => l.ListedPrice);
                }
                else
                {
                    metrics.AverageSellingPrice = RarityPriceRanges[rarity].min;
                }
            }
            else
            {
                metrics.AverageSellingPrice = RarityPriceRanges[rarity].min;
            }

            // Calculate sales score
            if (metrics.TotalListings > 0)
            {
                float salesRatio = metrics.SuccessfulSales / (float)metrics.TotalListings;
                float priceRatio = metrics.AverageSellingPrice / GetCurrentBasePrice(fishName);
                metrics.SalesScore = salesRatio * priceRatio;
            }
            else
            {
                metrics.SalesScore = 0.5f; // Neutral score if no listings
            }

            metrics.CurrentBasePrice = GetCurrentBasePrice(fishName);

            return metrics;
        }
        catch (Exception e)
        {
            Debug.LogError($"Error calculating metrics for {fishName}: {e.Message}");
            return new FishDemandMetrics
            {
                FishName = fishName,
                Rarity = rarity,
                PreferenceScore = 0.5f,
                SalesScore = 0.5f,
                CurrentBasePrice = RarityPriceRanges[rarity].min
            };
        }
    }

    private List<CustomerPurchaseManager.MarketListing> GetTodaysListings(string fishName)
    {
        var listings = new List<CustomerPurchaseManager.MarketListing>();
        
        databaseManager.ExecuteReader(
            @"SELECT ListingID, ListedPrice, IsSold, SellerID 
            FROM MarketListings 
            WHERE FishName = @fishName",
            reader => {
                while (reader.Read())
                {
                    var listing = new CustomerPurchaseManager.MarketListing
                    {
                        ListingID = reader.GetInt32(0),
                        FishName = fishName,
                        ListedPrice = reader.GetFloat(1),
                        IsSold = reader.GetBoolean(2),
                        SellerID = reader.GetInt32(3)
                    };
                    listings.Add(listing);
                }
            },
            new Dictionary<string, object> { { "@fishName", fishName } }
        );
        
        return listings;
    }

    private float GetCurrentBasePrice(string fishName)
    {
        var result = databaseManager.ExecuteScalar(
            @"SELECT Price 
            FROM MarketPrices 
            WHERE FishName = @fishName 
            AND Day = (SELECT MAX(Day) FROM MarketPrices)",
            new Dictionary<string, object> { { "@fishName", fishName } }
        );
        
        if (result == null || result == DBNull.Value)
        {
            Debug.LogWarning($"No current price found for {fishName}, using fallback price");
            var fishRarity = GetFishRarity(fishName);
            return RarityPriceRanges[fishRarity].min;
        }
        
        return Convert.ToSingle(result);
    }

    private float CalculateNextDayPrice(FishDemandMetrics metrics)
    {
        // Calculate combined demand score
        float demandScore = (metrics.PreferenceScore * 0.6f) + (metrics.SalesScore * 0.4f);

        // Get rarity-based adjustment range
        float maxAdjustment = metrics.Rarity switch
        {
            Customer.FISHRARITY.LEGENDARY => 0.30f,
            Customer.FISHRARITY.EPIC => 0.25f,
            Customer.FISHRARITY.RARE => 0.20f,
            Customer.FISHRARITY.UNCOMMON => 0.15f,
            _ => 0.10f
        };

        // Calculate price adjustment
        float adjustment = (demandScore - 1.0f) * maxAdjustment;
        
        // Apply adjustment to current price
        float newPrice = metrics.CurrentBasePrice * (1 + adjustment);
        
        // Clamp to rarity price range
        var (minPrice, maxPrice) = RarityPriceRanges[metrics.Rarity];
        return Mathf.Clamp(newPrice, minPrice, maxPrice);
    }

    private void StoreFishPrice(string fishName, float price, int day)
    {
        // Validate price
        if (float.IsNaN(price) || price <= 0)
        {
            Debug.LogError($"Invalid price {price} calculated for {fishName}. Using fallback price.");
            // Get rarity and use fallback price
            var fishRarity = GetFishRarity(fishName);
            price = RarityPriceRanges[fishRarity].min; // Use minimum price as fallback
        }
        
        Debug.Log($"Storing price for {fishName}: {price} gold (Day {day})");
        
        databaseManager.ExecuteNonQuery(
            @"INSERT INTO MarketPrices (FishName, Day, Price)
            VALUES (@fishName, @day, @price)",
            new Dictionary<string, object>
            {
                { "@fishName", fishName },
                { "@day", day },
                { "@price", price }
            }
        );
    }

    private Customer.FISHRARITY GetFishRarity(string fishName)
    {
        var result = databaseManager.ExecuteScalar(
            "SELECT Rarity FROM Fish WHERE Name = @fishName",
            new Dictionary<string, object> { { "@fishName", fishName } }
        );
        
        return (Customer.FISHRARITY)System.Enum.Parse(
            typeof(Customer.FISHRARITY), 
            result.ToString()
        );
    }

    private int GetNextMarketDay()
    {
        var result = databaseManager.ExecuteScalar(
            "SELECT MAX(Day) FROM MarketPrices"
        );
        
        return Convert.ToInt32(result) + 1;
    }

    // For testing in Unity Editor
    [ContextMenu("Test Price Updates")]
    public void TestPriceUpdates()
    {
        UpdateAllPrices();
    }
} 