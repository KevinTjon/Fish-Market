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

    // Maximum daily price volatility percentages per rarity
    private readonly Dictionary<Customer.FISHRARITY, float> MaxDailyVolatility = new()
    {
        { Customer.FISHRARITY.LEGENDARY, 0.75f }, // 75% max daily change
        { Customer.FISHRARITY.EPIC, 0.60f },      // 60% max daily change
        { Customer.FISHRARITY.RARE, 0.50f },      // 50% max daily change
        { Customer.FISHRARITY.UNCOMMON, 0.40f },  // 40% max daily change
        { Customer.FISHRARITY.COMMON, 0.35f }     // 35% max daily change
    };

    // Volatility multiplier based on market conditions (can be adjusted based on events)
    [SerializeField] private float globalVolatilityMultiplier = 1.0f;

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
        try
        {
            // Calculate combined demand score with more variance
            float demandScore = (metrics.PreferenceScore * 0.6f) + (metrics.SalesScore * 0.4f);
            
            // Add market sentiment based on recent trend with momentum
            float trendFactor = CalculatePriceTrend(metrics.FishName);
            float momentum = Mathf.Sign(trendFactor) * Mathf.Min(Mathf.Abs(trendFactor) * 1.5f, 0.3f);
            
            // Add random market noise with rarity-based scaling
            float noiseScale = metrics.Rarity switch
            {
                Customer.FISHRARITY.LEGENDARY => 0.75f,
                Customer.FISHRARITY.EPIC => 0.60f,
                Customer.FISHRARITY.RARE => 0.50f,
                Customer.FISHRARITY.UNCOMMON => 0.40f,
                _ => 0.35f
            };
            float randomFactor = UnityEngine.Random.Range(-noiseScale, noiseScale);
            
            // Add extra randomness to prevent stagnation
            float priceStability = CalculatePriceStability(metrics.FishName);
            if (priceStability > 0.5f) // Even lower threshold for randomness
            {
                randomFactor *= 3.0f; // More aggressive random factor
            }
            
            // Add varied random noise to prevent clustering
            float variationScale = metrics.CurrentBasePrice * 0.05f; // 5% of current price
            randomFactor += UnityEngine.Random.Range(-variationScale, variationScale);
            
            // Combine factors with momentum
            demandScore = demandScore + (momentum * 2.0f) + randomFactor;

            // Get rarity-based adjustment range with increased base volatility
            float maxAdjustment = metrics.Rarity switch
            {
                Customer.FISHRARITY.LEGENDARY => 0.45f,
                Customer.FISHRARITY.EPIC => 0.40f,
                Customer.FISHRARITY.RARE => 0.35f,
                Customer.FISHRARITY.UNCOMMON => 0.30f,
                _ => 0.25f
            };

            // Calculate base price adjustment with more sensitivity
            float adjustment = (demandScore - 1.0f) * maxAdjustment;
            
            // Add supply-based adjustment with increased impact
            if (metrics.TotalListings > 0)
            {
                float supplyFactor = metrics.SuccessfulSales / (float)metrics.TotalListings;
                float supplyImpact = (supplyFactor - 0.5f) * 0.25f; // Increased impact
                
                // Add extra impact for extreme supply conditions
                if (supplyFactor > 0.8f || supplyFactor < 0.2f)
                {
                    supplyImpact *= 1.5f;
                }
                
                adjustment += supplyImpact;
            }
            
            // Dynamic minimum movement based on price stability and base price
            float baseMinMovement = metrics.Rarity switch
            {
                Customer.FISHRARITY.LEGENDARY => 0.05f,
                Customer.FISHRARITY.EPIC => 0.04f,
                Customer.FISHRARITY.RARE => 0.035f,
                Customer.FISHRARITY.UNCOMMON => 0.03f,
                _ => 0.025f
            };

            // Ensure minimum price change in whole numbers
            float minPriceChange = Mathf.Max(
                metrics.CurrentBasePrice * baseMinMovement,
                metrics.Rarity switch
                {
                    Customer.FISHRARITY.LEGENDARY => 100f,
                    Customer.FISHRARITY.EPIC => 75f,
                    Customer.FISHRARITY.RARE => 50f,
                    Customer.FISHRARITY.UNCOMMON => 25f,
                    _ => 15f
                }
            );

            // Force minimum movement with trend consideration
            if (Mathf.Abs(adjustment * metrics.CurrentBasePrice) < minPriceChange)
            {
                if (Mathf.Abs(momentum) > 0.01f)
                {
                    adjustment = (minPriceChange / metrics.CurrentBasePrice) * Mathf.Sign(momentum);
                }
                else
                {
                    float upwardBias = metrics.TotalListings > 0 ? 
                        metrics.SuccessfulSales / (float)metrics.TotalListings : 0.5f;
                    bool moveUp = UnityEngine.Random.value < upwardBias;
                    adjustment = (minPriceChange / metrics.CurrentBasePrice) * (moveUp ? 1f : -1f);
                }
            }
            
            // Calculate initial new price
            float newPrice = metrics.CurrentBasePrice * (1 + adjustment);
            
            // Apply volatility controls with dynamic scaling
            float maxDailyVolatility = MaxDailyVolatility[metrics.Rarity] * globalVolatilityMultiplier;
            if (priceStability > 0.5f) // Lower threshold for increased volatility
            {
                maxDailyVolatility *= 3.0f; // More aggressive multiplier
            }
            float maxChange = Mathf.Max(
                metrics.CurrentBasePrice * maxDailyVolatility,
                metrics.Rarity switch
                {
                    Customer.FISHRARITY.LEGENDARY => 200f,
                    Customer.FISHRARITY.EPIC => 150f,
                    Customer.FISHRARITY.RARE => 100f,
                    Customer.FISHRARITY.UNCOMMON => 50f,
                    _ => 25f
                }
            );
            
            // Clamp the price change within volatility limits
            float minAllowedPrice = metrics.CurrentBasePrice - maxChange;
            float maxAllowedPrice = metrics.CurrentBasePrice + maxChange;
            newPrice = Mathf.Clamp(newPrice, minAllowedPrice, maxAllowedPrice);

            // Final clamp to rarity price range with minimal padding
            var (minPrice, maxPrice) = RarityPriceRanges[metrics.Rarity];
            float padding = (maxPrice - minPrice) * 0.01f; // Minimal padding
            newPrice = Mathf.Clamp(newPrice, minPrice + padding, maxPrice - padding);

            // Force price movement if too stable
            if (priceStability > 0.6f && Mathf.Abs(newPrice - metrics.CurrentBasePrice) < minPriceChange * 2f)
            {
                float direction = UnityEngine.Random.value > 0.5f ? 1f : -1f;
                newPrice = metrics.CurrentBasePrice + (minPriceChange * 4f * direction);
                
                // Add larger random variation to prevent clustering
                float variation = metrics.CurrentBasePrice * UnityEngine.Random.Range(0.05f, 0.15f);
                newPrice += variation * direction;
            }

            // Round to whole numbers with added variation to prevent clustering
            newPrice = Mathf.Round(newPrice + UnityEngine.Random.Range(-7f, 7f));
            
            // If the price hasn't changed enough, force a minimum change with variation
            if (Mathf.Abs(newPrice - metrics.CurrentBasePrice) < minPriceChange)
            {
                float direction = (newPrice >= metrics.CurrentBasePrice) ? 1f : -1f;
                float extraChange = UnityEngine.Random.Range(0f, minPriceChange * 0.5f);
                newPrice = metrics.CurrentBasePrice + ((minPriceChange + extraChange) * direction);
                newPrice = Mathf.Round(newPrice);
            }

            // Final randomization to prevent price clustering
            float finalVariation = Mathf.Max(5f, metrics.CurrentBasePrice * 0.02f);
            newPrice = Mathf.Clamp(
                Mathf.Round(newPrice + UnityEngine.Random.Range(-finalVariation, finalVariation)),
                minPrice,
                maxPrice
            );

            Debug.Log($"Price adjustment for {metrics.FishName}: " +
                     $"Current: {metrics.CurrentBasePrice:F0} -> New: {newPrice:F0} " +
                     $"(Change: {(newPrice - metrics.CurrentBasePrice):F0}, " +
                     $"Min Change: {minPriceChange:F0}, Max Change: {maxChange:F0})");

            return newPrice;
        }
        catch (Exception e)
        {
            Debug.LogError($"Error calculating next day price for {metrics.FishName}: {e.Message}");
            return metrics.CurrentBasePrice;
        }
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

    // Method to adjust global volatility (can be called by events or market conditions)
    public void SetGlobalVolatilityMultiplier(float multiplier)
    {
        globalVolatilityMultiplier = Mathf.Clamp(multiplier, 0.5f, 2.0f);
        Debug.Log($"Global volatility multiplier set to: {globalVolatilityMultiplier:F2}");
    }

    // Method to get current volatility for a fish
    public float GetCurrentVolatility(Customer.FISHRARITY rarity)
    {
        return MaxDailyVolatility[rarity] * globalVolatilityMultiplier;
    }

    // For testing in Unity Editor
    [ContextMenu("Test High Volatility")]
    public void TestHighVolatility()
    {
        SetGlobalVolatilityMultiplier(2.0f);
        UpdateAllPrices();
    }

    [ContextMenu("Test Low Volatility")]
    public void TestLowVolatility()
    {
        SetGlobalVolatilityMultiplier(0.5f);
        UpdateAllPrices();
    }

    [ContextMenu("Reset Volatility")]
    public void ResetVolatility()
    {
        SetGlobalVolatilityMultiplier(1.0f);
    }

    // Helper method to calculate price trend
    private float CalculatePriceTrend(string fishName, int lookbackDays = 3)
    {
        var prices = new List<float>();
        
        databaseManager.ExecuteReader(
            @"SELECT Price 
            FROM MarketPrices 
            WHERE FishName = @fishName 
            ORDER BY Day DESC 
            LIMIT @lookback",
            reader => {
                while (reader.Read())
                {
                    prices.Add(reader.GetFloat(0));
                }
            },
            new Dictionary<string, object> 
            { 
                { "@fishName", fishName },
                { "@lookback", lookbackDays }
            }
        );
        
        if (prices.Count >= 2)
        {
            float trend = 0f;
            for (int i = 0; i < prices.Count - 1; i++)
            {
                trend += (prices[i] - prices[i + 1]) / prices[i + 1];
            }
            return trend / (prices.Count - 1);
        }
        
        return 0f;
    }

    // Helper method to calculate price stability (0 = volatile, 1 = stable)
    private float CalculatePriceStability(string fishName, int lookbackDays = 5)
    {
        var prices = new List<float>();
        
        databaseManager.ExecuteReader(
            @"SELECT Price 
            FROM MarketPrices 
            WHERE FishName = @fishName 
            ORDER BY Day DESC 
            LIMIT @lookback",
            reader => {
                while (reader.Read())
                {
                    prices.Add(reader.GetFloat(0));
                }
            },
            new Dictionary<string, object> 
            { 
                { "@fishName", fishName },
                { "@lookback", lookbackDays }
            }
        );
        
        if (prices.Count >= 2)
        {
            float avgPrice = prices.Average();
            float variance = prices.Select(p => (p - avgPrice) * (p - avgPrice)).Average();
            float coefficient = Mathf.Sqrt(variance) / avgPrice;
            return Mathf.Clamp01(1f - coefficient * 5f); // Convert to stability score
        }
        
        return 0.5f; // Default stability for new prices
    }
} 