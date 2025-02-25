using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using Mono.Data.Sqlite;

public class Customer
{
    public enum CUSTOMERTYPE
    {
        BUDGET,
        CASUAL,
        COLLECTOR,
        WEALTHY
    }

    public enum FISHRARITY
    {
        COMMON,
        UNCOMMON,
        RARE,
        EPIC,
        LEGENDARY
    }

    public enum SellerType
    {
        Player = 0,
        NPC1 = 1,
        NPC2 = 2,
        NPC3 = 3,
        NPC4 = 4
    }

    [System.Serializable]
    public class ShoppingListItem
    {
        public FISHRARITY Rarity { get; set; }
        public int Amount { get; set; }
    }

    [System.Serializable]
    public class SellerBias
    {
        public FISHRARITY Rarity { get; set; }
        public float BiasValue { get; set; }
    }

    public class Purchase
    {
        public string FishName;
        public float Price;
        public int SellerID;
    }

    [System.Serializable]
    public class FishPreference
    {
        public string FishName { get; set; }
        public float PreferenceScore { get; set; }  // 0.0 to 1.0
        public FISHRARITY Rarity { get; set; }
        public bool HasPurchased { get; set; }
    }

    // Basic properties
    public int CustomerID { get; set; }
    public CUSTOMERTYPE Type { get; set; }
    public int Budget { get; set; }
    public List<FishPreference> FishPreferences { get; private set; } = new List<FishPreference>();
    public List<SellerBias> SellerPreferences { get; private set; }
    public List<Purchase> PurchaseHistory { get; private set; } = new List<Purchase>();

    private Dictionary<(int sellerId, FISHRARITY rarity), float> biases = 
        new Dictionary<(int sellerId, FISHRARITY rarity), float>();

    // Add to existing properties
    private HashSet<int> visitedSellers = new HashSet<int>();

    // Add a maximum purchases property
    public int MaxPurchases { get; private set; }

    // Constructor
    public Customer(CUSTOMERTYPE type, int id = 0, int? predefinedBudget = null)
    {
        CustomerID = id;
        Type = type;
        FishPreferences = new List<FishPreference>();
        SellerPreferences = new List<SellerBias>();
        
        if (predefinedBudget.HasValue)
            Budget = predefinedBudget.Value;
        else
            SetCustomerProperties(type);

        InitializeSellerBiases();
    }

    private void InitializeSellerBiases()
    {
        // For each fish rarity
        foreach (FISHRARITY rarity in System.Enum.GetValues(typeof(FISHRARITY)))
        {
            // Start with equal bias (0.5) for all sellers
            SetBias(0, rarity, 0.5f);
        }
    }

    private void SetCustomerProperties(CUSTOMERTYPE type)
    {
        switch (type)
        {
            case CUSTOMERTYPE.BUDGET:
                Budget = Random.Range(200, 251);
                MaxPurchases = 2;
                // Increase preference ranges for common fish
                AddFishPreferencesForRarity(FISHRARITY.COMMON, 0.5f, 1.0f);      // Was (0.7f, 1.0f)
                AddFishPreferencesForRarity(FISHRARITY.UNCOMMON, 0.3f, 0.6f);    // Was (0.1f, 0.3f)
                break;

            case CUSTOMERTYPE.CASUAL:
                Budget = Random.Range(300, 501);
                MaxPurchases = 3;
                // Increase preference ranges for common/uncommon
                AddFishPreferencesForRarity(FISHRARITY.COMMON, 0.4f, 0.9f);      // Was (0.4f, 0.8f)
                AddFishPreferencesForRarity(FISHRARITY.UNCOMMON, 0.4f, 0.9f);    // Was (0.5f, 0.9f)
                AddFishPreferencesForRarity(FISHRARITY.RARE, 0.2f, 0.4f);
                break;

            case CUSTOMERTYPE.COLLECTOR:
                Budget = Random.Range(800, 1501);
                MaxPurchases = 2;
                AddFishPreferencesForRarity(FISHRARITY.RARE, 0.6f, 1.0f);
                AddFishPreferencesForRarity(FISHRARITY.EPIC, 0.7f, 1.0f);
                AddFishPreferencesForRarity(FISHRARITY.LEGENDARY, 0.4f, 0.8f);
                break;

            case CUSTOMERTYPE.WEALTHY:
                Budget = Random.Range(1500, 3001);
                MaxPurchases = 3;
                AddFishPreferencesForRarity(FISHRARITY.LEGENDARY, 0.8f, 1.0f);
                AddFishPreferencesForRarity(FISHRARITY.EPIC, 0.6f, 0.9f);
                AddFishPreferencesForRarity(FISHRARITY.RARE, 0.3f, 0.7f);
                break;
        }
    }

    private void AddFishPreferencesForRarity(FISHRARITY rarity, float minPreference, float maxPreference)
    {
        // Get all fish names of this rarity from the database
        List<string> fishNames = DatabaseManager.Instance.GetFishNamesByRarity(rarity.ToString());

        if (fishNames.Count == 0) return;

        // Calculate how many fish should be high preference (above 0.5)
        int highPrefCount = Mathf.CeilToInt(fishNames.Count * GetHighPreferenceRatio(rarity));
        int lowPrefCount = fishNames.Count - highPrefCount;

        // Shuffle the fish names
        for (int i = fishNames.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            var temp = fishNames[i];
            fishNames[i] = fishNames[j];
            fishNames[j] = temp;
        }

        // Assign high preferences to the first portion
        for (int i = 0; i < highPrefCount; i++)
        {
            float prefScore = Random.Range(0.5f, maxPreference);
            FishPreferences.Add(new FishPreference
            {
                FishName = fishNames[i],
                PreferenceScore = prefScore,
                Rarity = rarity,
                HasPurchased = false
            });
        }

        // Assign low preferences to the remainder
        for (int i = highPrefCount; i < fishNames.Count; i++)
        {
            float prefScore = Random.Range(minPreference, 0.5f);
            FishPreferences.Add(new FishPreference
            {
                FishName = fishNames[i],
                PreferenceScore = prefScore,
                Rarity = rarity,
                HasPurchased = false
            });
        }
    }

    private float GetHighPreferenceRatio(FISHRARITY rarity)
    {
        // Higher rarities get more high preferences
        switch (rarity)
        {
            case FISHRARITY.COMMON:
                return 0.5f;     // 50% high preference
            case FISHRARITY.UNCOMMON:
                return 0.55f;    // 55% high preference
            case FISHRARITY.RARE:
                return 0.6f;     // 60% high preference
            case FISHRARITY.EPIC:
                return 0.65f;    // 65% high preference
            case FISHRARITY.LEGENDARY:
                return 0.7f;     // 70% high preference
            default:
                return 0.5f;
        }
    }

    public float GetBias(int sellerId, FISHRARITY rarity)
    {
        if (biases.TryGetValue((sellerId, rarity), out float value))
        {
            return value;
        }
        return 0f;  // Default bias if none is set
    }

    public void IncreaseBias(FISHRARITY rarity, float amount = 0.1f)
    {
        var bias = SellerPreferences.FirstOrDefault(b => b.Rarity == rarity);
        
        if (bias != null)
        {
            bias.BiasValue = Mathf.Min(bias.BiasValue + amount, 1.0f);
        }
        else
        {
            SellerPreferences.Add(new SellerBias 
            { 
                Rarity = rarity, 
                BiasValue = amount 
            });
        }
    }

    public void SetBias(int sellerId, FISHRARITY rarity, float value)
    {
        biases[(sellerId, rarity)] = value;
    }

    public IEnumerable<(int sellerId, FISHRARITY rarity, float value)> GetBiases()
    {
        foreach (var kvp in biases)
        {
            yield return (kvp.Key.sellerId, kvp.Key.rarity, kvp.Value);
        }
    }

    public override string ToString()
    {
        string preferencesStr = string.Join(", ", FishPreferences.Select(pref => 
            $"{pref.FishName} (Score: {pref.PreferenceScore:F2})"));
        
        string biasStr = string.Join(", ", SellerPreferences.Select(bias =>
            $"{bias.Rarity}: {bias.BiasValue:F2}"));
        
        return $"ID={CustomerID}, Type={Type}, Budget={Budget}, " +
               $"Preferences=[{preferencesStr}], " +
               $"Biases=[{biasStr}]";
    }

    public void RecordPurchase(string fishName, float price, int sellerId)
    {
        PurchaseHistory.Add(new Purchase
        {
            FishName = fishName,
            Price = price,
            SellerID = sellerId
        });

        // Update preferences after purchase
        UpdatePreferences(fishName);
    }

    // Add these methods
    public void AddVisitedSeller(int sellerId)
    {
        visitedSellers.Add(sellerId);
    }

    public bool HasVisitedSeller(int sellerId)
    {
        return visitedSellers.Contains(sellerId);
    }

    public void ClearVisitedSellers()
    {
        visitedSellers.Clear();
    }

    public bool HasVisitedAllSellers()
    {
        return visitedSellers.Count >= System.Enum.GetValues(typeof(SellerType)).Length;
    }

    public bool HasReachedMaxPurchases()
    {
        return PurchaseHistory.Count >= MaxPurchases;
    }

    public List<FishPreference> GetUnpurchasedPreferences()
    {
        return FishPreferences
            .Where(fp => !fp.HasPurchased)
            .OrderByDescending(fp => fp.PreferenceScore)
            .ToList();
    }

    public void UpdatePreferences(string purchasedFishName)
    {
        // Find the purchased fish preference
        var purchasedPref = FishPreferences.Find(p => p.FishName == purchasedFishName);
        if (purchasedPref == null) return;

        // Get all fish of the same rarity
        var sameFishRarity = FishPreferences.Where(p => p.Rarity == purchasedPref.Rarity).ToList();
        
        // Calculate target number of high preferences based on rarity ratio
        float targetRatio = GetHighPreferenceRatio(purchasedPref.Rarity);
        int targetHighCount = Mathf.RoundToInt(sameFishRarity.Count * targetRatio);
        
        // Decrease preference for purchased fish
        float oldScore = purchasedPref.PreferenceScore;
        purchasedPref.PreferenceScore = Mathf.Max(0.1f, oldScore - 0.15f);

        // Count current high preferences after decrease
        int currentHighCount = sameFishRarity.Count(p => p.PreferenceScore >= 0.5f);

        // Get all low-preference fish of the same rarity
        var lowPreferences = sameFishRarity
            .Where(p => p.PreferenceScore < 0.5f && p.FishName != purchasedFishName)
            .ToList();

        if (lowPreferences.Any() && currentHighCount < targetHighCount)
        {
            // Randomly select one low-preference fish to increase
            var randomLowPref = lowPreferences[Random.Range(0, lowPreferences.Count)];
            float increase = 0.15f;
            
            randomLowPref.PreferenceScore = Mathf.Min(1.0f, randomLowPref.PreferenceScore + increase);
            
            // Save both preference changes to the database using DatabaseManager
            // Update decreased preference
            DatabaseManager.Instance.UpdateCustomerPreferenceScore(
                CustomerID,
                purchasedFishName,
                purchasedPref.PreferenceScore
            );

            // Update increased preference
            DatabaseManager.Instance.UpdateCustomerPreferenceScore(
                CustomerID,
                randomLowPref.FishName,
                randomLowPref.PreferenceScore
            );
        }
    }
}