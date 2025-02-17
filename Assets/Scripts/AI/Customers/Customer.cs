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

    private string dbPath;  // Add this field

    // Constructor
    public Customer(CUSTOMERTYPE type, string dbPath, int id = 0, int? predefinedBudget = null)
    {
        this.dbPath = dbPath;  // Store the dbPath
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
        // This method would need to get fish names from the database
        // For now, we'll assume it's passed in or handled elsewhere
        using (var connection = new Mono.Data.Sqlite.SqliteConnection(dbPath))
        {
            connection.Open();
            using (var command = connection.CreateCommand())
            {
                command.CommandText = "SELECT Name FROM Fish WHERE Rarity = @rarity";
                command.Parameters.AddWithValue("@rarity", rarity.ToString());

                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        string fishName = reader.GetString(0);
                        FishPreferences.Add(new FishPreference
                        {
                            FishName = fishName,
                            PreferenceScore = Random.Range(minPreference, maxPreference),
                            Rarity = rarity,
                            HasPurchased = false
                        });
                    }
                }
            }
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

    public void RecordPurchase(string fishName, float price, int sellerID)
    {
        PurchaseHistory.Add(new Purchase 
        { 
            FishName = fishName,
            Price = price,
            SellerID = sellerID
        });

        // Mark the fish as purchased in preferences
        var preference = FishPreferences.FirstOrDefault(fp => fp.FishName == fishName);
        if (preference != null)
        {
            preference.HasPurchased = true;
        }
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
}