using UnityEngine;
using System.Collections.Generic;
using System.Linq;

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

    // Basic properties
    public int CustomerID { get; set; }
    public CUSTOMERTYPE Type { get; set; }
    public int Budget { get; set; }
    public List<ShoppingListItem> ShoppingList { get; set; }
    public List<SellerBias> SellerPreferences { get; private set; }
    public List<Purchase> PurchaseHistory { get; private set; } = new List<Purchase>();

    private Dictionary<(int sellerId, FISHRARITY rarity), float> biases = 
        new Dictionary<(int sellerId, FISHRARITY rarity), float>();

    // Add to existing properties
    private HashSet<int> visitedSellers = new HashSet<int>();

    // Constructor
    public Customer(CUSTOMERTYPE type, int id = 0, int? predefinedBudget = null)
    {
        CustomerID = id;
        Type = type;
        ShoppingList = new List<ShoppingListItem>();
        SellerPreferences = new List<SellerBias>();
        
        if (predefinedBudget.HasValue)
            Budget = predefinedBudget.Value;
        else
            SetCustomerProperties(type);

        // Initialize equal biases for all sellers and rarities
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
                Budget = Random.Range(200, 251);    // Was 150-200, now 200-250 to ensure they can buy multiple common fish
                ShoppingList.Add(new ShoppingListItem { Rarity = FISHRARITY.COMMON, Amount = Random.Range(1, 3) });
                break;

            case CUSTOMERTYPE.CASUAL:
                Budget = Random.Range(300, 501);    // Was 200-400, now 300-500 to ensure they can afford both fish types
                ShoppingList.Add(new ShoppingListItem { Rarity = FISHRARITY.COMMON, Amount = Random.Range(1, 3) });
                ShoppingList.Add(new ShoppingListItem { Rarity = FISHRARITY.UNCOMMON, Amount = 1 });
                break;

            case CUSTOMERTYPE.COLLECTOR:
                Budget = Random.Range(800, 1501);   
                ShoppingList.Add(new ShoppingListItem { Rarity = FISHRARITY.UNCOMMON, Amount = 1 });
                ShoppingList.Add(new ShoppingListItem { Rarity = FISHRARITY.RARE, Amount = Random.Range(1, 3) });
                break;

            case CUSTOMERTYPE.WEALTHY:
                Budget = Random.Range(1500, 3001);  
                ShoppingList.Add(new ShoppingListItem { Rarity = FISHRARITY.RARE, Amount = Random.Range(1, 3) });
                ShoppingList.Add(new ShoppingListItem { Rarity = FISHRARITY.LEGENDARY, Amount = 1 });
                break;
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

    public void AddToShoppingList(FISHRARITY rarity, int amount)
    {
        ShoppingList.Add(new ShoppingListItem { Rarity = rarity, Amount = amount });
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
        string shoppingListStr = string.Join(", ", ShoppingList.Select(item => 
            $"{item.Amount} {item.Rarity}"));
        
        string biasStr = string.Join(", ", SellerPreferences.Select(bias =>
            $"{bias.Rarity}: {bias.BiasValue:F2}"));
        
        return $"ID={CustomerID}, Type={Type}, Budget={Budget}, " +
               $"Shopping List=[{shoppingListStr}], " +
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
}